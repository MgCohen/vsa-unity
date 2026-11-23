using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Spike {
    // A prototype of the closing step of the plan. After compose, it replaces each open descriptor
    // with closed descriptors, so the container never closes a type itself.
    // Type arguments come from [AutoClose] types and from a walk of the constructor parameters.
    public static class OpenClosing {
        public static void Close(IServiceCollection services, IEnumerable<Type> typeArguments) {
            var open = services.Where(d => d.ServiceType.IsGenericTypeDefinition && d.ImplementationType != null).ToList();
            if (open.Count == 0) {
                return;
            }
            var closedBy = new Dictionary<ServiceDescriptor, int>();
            var known = new HashSet<(Type, Type)>(services
                .Where(d => !d.ServiceType.ContainsGenericParameters && d.ImplementationType != null)
                .Select(d => (d.ServiceType, d.ImplementationType)));
            var walked = new HashSet<Type>();
            var queue = new Queue<Type>(services
                .Select(d => d.ImplementationType)
                .Where(t => t != null && !t.ContainsGenericParameters));

            // A closed form that a module registered already also counts, so that the open descriptor
            // does not add a second copy to the list (case Edge).
            void Add(ServiceDescriptor source, Type service, Type implementation) {
                closedBy[source] = closedBy.TryGetValue(source, out var n) ? n + 1 : 1;
                if (!known.Add((service, implementation))) {
                    return;
                }
                services.Add(new ServiceDescriptor(service, implementation, source.Lifetime));
                queue.Enqueue(implementation);
            }

            void Request(Type service) {
                if (!service.IsGenericType || service.ContainsGenericParameters) {
                    return;
                }
                var definition = service.GetGenericTypeDefinition();
                foreach (var source in open.Where(d => d.ServiceType == definition)) {
                    if (Unify(source.ImplementationType, service) is Type implementation) {
                        Add(source, service, implementation);
                    }
                }
            }

            // Source 1: [AutoClose] types, for each open implementation with one type parameter.
            foreach (var source in open) {
                var implementation = source.ImplementationType;
                if (implementation.GetGenericArguments().Length != 1) {
                    continue;
                }
                foreach (var argument in typeArguments) {
                    if (!Satisfies(implementation.GetGenericArguments()[0], argument)) {
                        continue;
                    }
                    var closed = implementation.MakeGenericType(argument);
                    foreach (var service in Implemented(closed).Where(s => s.IsGenericType && s.GetGenericTypeDefinition() == source.ServiceType)) {
                        Add(source, service, closed);
                    }
                }
            }

            // Source 2: the constructor walk, until no new closed type is found.
            while (queue.Count > 0) {
                var type = queue.Dequeue();
                if (!walked.Add(type)) {
                    continue;
                }
                foreach (var parameter in Parameters(type)) {
                    var service = parameter.ParameterType;
                    if (service.IsGenericType && service.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                        service = service.GetGenericArguments()[0];
                    }
                    Request(service);
                }
            }

            // Replace an open descriptor only when at least one closed form was added.
            foreach (var source in open.Where(closedBy.ContainsKey)) {
                services.Remove(source);
            }
        }

        // Finds the closed implementation for a closed service: it matches the service against each type
        // that the open implementation implements, and solves each type parameter.
        public static Type Unify(Type openImplementation, Type service) {
            var parameters = openImplementation.GetGenericArguments();
            foreach (var candidate in Implemented(openImplementation)) {
                var map = new Dictionary<Type, Type>();
                if (!Match(candidate, service, map) || parameters.Any(p => !map.ContainsKey(p))) {
                    continue;
                }
                var arguments = parameters.Select(p => map[p]).ToArray();
                if (parameters.Where((p, i) => !Satisfies(p, arguments[i])).Any()) {
                    continue;
                }
                return openImplementation.MakeGenericType(arguments);
            }
            return null;
        }

        static IEnumerable<Type> Implemented(Type type) {
            for (var t = type; t != null && t != typeof(object); t = t.BaseType) {
                yield return t;
            }
            foreach (var i in type.GetInterfaces()) {
                yield return i;
            }
        }

        static bool Match(Type pattern, Type actual, Dictionary<Type, Type> map) {
            if (pattern.IsGenericParameter) {
                if (map.TryGetValue(pattern, out var bound)) {
                    return bound == actual;
                }
                map[pattern] = actual;
                return true;
            }
            if (!pattern.ContainsGenericParameters) {
                return pattern == actual;
            }
            if (!pattern.IsGenericType || !actual.IsGenericType || pattern.GetGenericTypeDefinition() != actual.GetGenericTypeDefinition()) {
                return false;
            }
            var p = pattern.GetGenericArguments();
            var a = actual.GetGenericArguments();
            for (var i = 0; i < p.Length; i++) {
                if (!Match(p[i], a[i], map)) {
                    return false;
                }
            }
            return true;
        }

        // IL2CPP does not check constraints in MakeGenericType, so the prototype checks them.
        static bool Satisfies(Type parameter, Type argument) {
            var flags = parameter.GenericParameterAttributes;
            if ((flags & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0
                && (!argument.IsValueType || Nullable.GetUnderlyingType(argument) != null)) {
                return false;
            }
            if ((flags & GenericParameterAttributes.ReferenceTypeConstraint) != 0 && argument.IsValueType) {
                return false;
            }
            if ((flags & GenericParameterAttributes.DefaultConstructorConstraint) != 0
                && !argument.IsValueType && argument.GetConstructor(Type.EmptyTypes) == null) {
                return false;
            }
            return parameter.GetGenericParameterConstraints()
                .Where(c => !c.ContainsGenericParameters)
                .All(c => c.IsAssignableFrom(argument));
        }

        static IEnumerable<ParameterInfo> Parameters(Type type) {
            try {
                return type.GetConstructors().SelectMany(c => c.GetParameters()).ToList();
            } catch (Exception) {
                return Enumerable.Empty<ParameterInfo>();
            }
        }
    }
}
