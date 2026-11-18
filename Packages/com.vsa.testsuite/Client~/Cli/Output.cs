using System.Text.Json;
using System.Text.Json.Serialization;

namespace VSA.TestSuite.Cli
{
    /// <summary>Writes the envelope of the unity CLI to stdout: success, command, data, errors, warnings.
    /// Stream lines and the envelope are JSON. Progress text goes to stderr.</summary>
    static class Output
    {
        static readonly JsonSerializerOptions Json = new()
        {
            IncludeFields = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        public static bool Human { get; set; }

        /// <summary>Writes the envelope and returns the exit code of the error code. A null code is a success.</summary>
        public static int Envelope(string command, object data, string code = null, string message = null)
        {
            if (Human)
            {
                Console.WriteLine(code == null ? command + ": ok" : command + ": " + code + " - " + message);
                if (ExitCodes.Hint(code) is string h) Console.WriteLine("hint: " + h);
                if (data != null) Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions(Json) { WriteIndented = true }));
                return ExitCodes.For(code);
            }
            var errors = code == null
                ? Array.Empty<object>()
                : new object[] { new { code, message, hint = ExitCodes.Hint(code), retryable = ExitCodes.Retryable(code) } };
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                success = code == null,
                command,
                data,
                errors,
                warnings = Array.Empty<object>(),
            }, Json));
            return ExitCodes.For(code);
        }

        /// <summary>Writes one stream line (NDJSON).</summary>
        public static void Line(object line)
        {
            lock (Json) Console.WriteLine(JsonSerializer.Serialize(line, Json));
        }

        public static void Progress(string text) => Console.Error.WriteLine(text);
    }
}
