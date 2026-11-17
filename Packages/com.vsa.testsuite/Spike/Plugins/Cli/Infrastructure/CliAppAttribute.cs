using System;

namespace VSA.TestSuite.Spike
{
    /// <summary>
    /// Marks the VSA app that the CLI commands use. AppBus finds the one class with this attribute
    /// and starts it on the first send, so the plugin needs no reference to the app.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CliAppAttribute : Attribute
    {
    }
}
