namespace miniMOO.Data.FileSystem;

internal sealed class YamlLite {
    private readonly Dictionary<string, string> _values;

    private YamlLite(Dictionary<string, string> values) {
        _values = values;
    }

    public static YamlLite Parse(string text) {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? pendingListKey = null;
        List<string>? pendingList = null;

        foreach (var rawLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
            var line = StripComment(rawLine).Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith("- ", StringComparison.Ordinal)) {
                if (pendingListKey is null || pendingList is null)
                    throw new FileWorldLoadException($"List item without a key: {rawLine}");

                pendingList.Add(Unquote(line[2..].Trim()));
                values[pendingListKey] = "[" + string.Join(", ", pendingList) + "]";
                continue;
            }

            var colon = line.IndexOf(':');
            if (colon <= 0)
                throw new FileWorldLoadException($"Invalid YAML-lite line: {rawLine}");

            var key = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            values[key] = value;

            if (value.Length == 0) {
                pendingListKey = key;
                pendingList = [];
            }
            else {
                pendingListKey = null;
                pendingList = null;
            }
        }

        return new YamlLite(values);
    }

    public string RequiredString(string key, string path)
        => OptionalString(key)
            ?? throw new FileWorldLoadException($"{path}: missing required field '{key}'.");

    public bool Has(string key)
        => _values.ContainsKey(key);

    public string? OptionalString(string key)
        => _values.TryGetValue(key, out var value)
            ? NullIfNeeded(Unquote(value))
            : null;

    public string? OptionalRaw(string key)
        => _values.TryGetValue(key, out var value)
            ? NullLiteralIfNeeded(value)
            : null;

    public IReadOnlyList<string> RequiredArray(string key, string path) {
        var values = OptionalArray(key);
        if (values is null || values.Count == 0)
            throw new FileWorldLoadException($"{path}: missing required array field '{key}'.");

        return values;
    }

    public IReadOnlyList<string>? OptionalArray(string key)
        => _values.TryGetValue(key, out var value)
            ? ParseArray(value)
            : null;

    public static IReadOnlyList<string> ParseArray(string value) {
        value = value.Trim();
        if (value.Length == 0 || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            return [];

        if (!(value.StartsWith('[') && value.EndsWith(']')))
            return [Unquote(value)];

        var inner = value[1..^1].Trim();
        if (inner.Length == 0)
            return [];

        var items = new List<string>();
        var current = "";
        var quote = '\0';

        foreach (var ch in inner) {
            if (quote != '\0') {
                if (ch == quote)
                    quote = '\0';
                else
                    current += ch;

                continue;
            }

            if (ch is '"' or '\'') {
                quote = ch;
                continue;
            }

            if (ch == ',') {
                items.Add(Unquote(current.Trim()));
                current = "";
                continue;
            }

            current += ch;
        }

        if (quote != '\0')
            throw new FileWorldLoadException("Unterminated quoted array item.");

        items.Add(Unquote(current.Trim()));
        return items;
    }

    public static string Unquote(string value) {
        value = value.Trim();
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\''))) {
            return value[1..^1]
                .Replace("\\n", "\n")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }

        return value;
    }

    private static string? NullIfNeeded(string value)
        => value.Length == 0 || value.Equals("null", StringComparison.OrdinalIgnoreCase)
            ? null
            : value;

    private static string? NullLiteralIfNeeded(string value)
        => value.Equals("null", StringComparison.OrdinalIgnoreCase)
            ? null
            : value;

    private static string StripComment(string line) {
        var quote = '\0';

        for (var i = 0; i < line.Length; i++) {
            var ch = line[i];

            if (quote != '\0') {
                if (ch == quote)
                    quote = '\0';

                continue;
            }

            if (ch is '"' or '\'') {
                quote = ch;
                continue;
            }

            if (ch == '#')
                return line[..i];
        }

        return line;
    }
}
