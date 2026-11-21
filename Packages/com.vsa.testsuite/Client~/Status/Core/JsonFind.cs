using System.Text.Json;

namespace VSA.TestSuite.Client.Status
{
    /// <summary>Finds a property by name anywhere in a JSON document. Pipeline answers nest values in different places.</summary>
    public static class JsonFind
    {
        public static JsonElement? Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            try { return JsonDocument.Parse(text).RootElement.Clone(); }
            catch (JsonException) { return null; }
        }

        public static JsonElement? Find(JsonElement? root, string name)
        {
            if (root is not JsonElement e) return null;
            if (e.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in e.EnumerateObject())
                    if (p.NameEquals(name)) return p.Value;
                foreach (var p in e.EnumerateObject())
                    if (Find(p.Value, name) is JsonElement found) return found;
            }
            else if (e.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in e.EnumerateArray())
                    if (Find(item, name) is JsonElement found) return found;
            }
            return null;
        }

        public static string Str(JsonElement? root, string name)
            => Find(root, name) is JsonElement v ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString()) : null;

        public static bool? Bool(JsonElement? root, string name)
            => Find(root, name) is JsonElement v && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : null;

        public static int? Int(JsonElement? root, string name)
            => Find(root, name) is JsonElement v && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : null;
    }
}
