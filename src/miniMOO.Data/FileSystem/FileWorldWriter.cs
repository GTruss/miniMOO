using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using miniMOO.Core.Things;

namespace miniMOO.Data.FileSystem;

public sealed class FileWorldWriter {
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public int WriteDirectory(string rootPath, IEnumerable<MooObject> objects) {
        Directory.CreateDirectory(rootPath);

        var tempPath = Path.Combine(
            Path.GetDirectoryName(rootPath) ?? ".",
            $"{Path.GetFileName(rootPath)}.checkpoint-{Guid.NewGuid():N}");

        Directory.CreateDirectory(tempPath);

        var count = 0;
        try {
            foreach (var obj in objects.OrderBy(o => o.Id.Value)) {
                File.WriteAllText(
                    Path.Combine(tempPath, FileNameFor(obj)),
                    SerializeObject(obj),
                    Utf8NoBom);

                count++;
            }

            new FileWorldLoader().LoadDirectory(tempPath);

            foreach (var file in Directory.EnumerateFiles(rootPath, "*.moo.md", SearchOption.TopDirectoryOnly))
                File.Delete(file);

            foreach (var file in Directory.EnumerateFiles(tempPath, "*.moo.md", SearchOption.TopDirectoryOnly)) {
                File.Move(
                    file,
                    Path.Combine(rootPath, Path.GetFileName(file)));
            }
        }
        finally {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, recursive: true);
        }

        return count;
    }

    public void WriteObject(string rootPath, MooObject obj) {
        Directory.CreateDirectory(rootPath);

        var path = Path.Combine(rootPath, FileNameFor(obj));
        File.WriteAllText(
            path,
            SerializeObject(obj),
            Utf8NoBom);

        new FileWorldLoader().LoadObject(path);
    }

    private static string FileNameFor(MooObject obj)
        => $"{Math.Abs(obj.Id.Value):0000}-{Slugify(obj.Name)}.moo.md";

    private static string Slugify(string value) {
        var slug = value.Trim().ToLowerInvariant();

        if (slug.StartsWith("a ", StringComparison.Ordinal))
            slug = slug[2..];
        else if (slug.StartsWith("an ", StringComparison.Ordinal))
            slug = slug[3..];
        else if (slug.StartsWith("the ", StringComparison.Ordinal))
            slug = slug[4..];

        slug = Regex.Replace(slug, @"[^a-z0-9]+", "-").Trim('-');
        return slug.Length == 0 ? "object" : slug;
    }

    private static string SerializeObject(MooObject obj) {
        var sb = new StringBuilder();

        sb.AppendLine("---");
        sb.AppendLine($"id: {Quote(obj.Id.ToString())}");
        sb.AppendLine($"name: {QuoteIfNeeded(obj.Name)}");
        sb.AppendLine($"owner: {Quote(obj.OwnerId.ToString())}");
        sb.AppendLine(obj.ParentId is { } parent
            ? $"parent: {Quote(parent.ToString())}"
            : "parent:");
        sb.AppendLine(obj.LocationId is { } location
            ? $"location: {Quote(location.ToString())}"
            : "location:");
        AppendFlags(sb, "flags", ObjectFlagsToNames(obj.Flags));
        AppendStringList(sb, "aliases", obj.Aliases);
        sb.AppendLine($"updated: {DateTimeOffset.Now:yyyy-MM-ddTHH:mm:sszzz}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {obj.Name}");
        sb.AppendLine();

        foreach (var property in obj.Properties.Values.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)) {
            sb.AppendLine("```yaml");
            sb.AppendLine($"name: {QuoteIfNeeded(property.Name)}");
            sb.AppendLine($"type: {ValueTypeName(property.Value)}");
            AppendPropertyValue(sb, property.Value);

            if (property.OwnerId != obj.OwnerId)
                sb.AppendLine($"owner: {Quote(property.OwnerId.ToString())}");

            AppendFlags(sb, "flags", PropertyFlagsToNames(property.Flags), inlineWhenEmpty: true);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        foreach (var verb in obj.Verbs) {
            sb.AppendLine($"## Verb: {string.Join("/", verb.Names)}");
            sb.AppendLine();
            sb.AppendLine("```yaml");
            sb.AppendLine($"names: {FormatStringList(verb.Names)}");
            sb.AppendLine($"dobj: {VerbObjectSpecToString(verb.DirectObject)}");
            sb.AppendLine($"prep: {QuoteIfNeeded(verb.Preposition)}");
            sb.AppendLine($"iobj: {VerbObjectSpecToString(verb.IndirectObject)}");
            sb.AppendLine($"owner: {Quote(verb.OwnerId.ToString())}");
            AppendFlags(sb, "flags", VerbFlagsToNames(verb.Flags), inlineWhenEmpty: true);

            if (verb.ImplementationKind != VerbImplementationKind.Script)
                sb.AppendLine($"kind: {verb.ImplementationKind.ToString().ToLowerInvariant()}");

            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(verb.Code.Replace("\r\n", "\n").Replace('\r', '\n'));
            sb.AppendLine("```");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static void AppendPropertyValue(StringBuilder sb, MooValue value) {
        if (value is MooValue.Nothing or MooValue.Clear)
            return;

        sb.AppendLine($"value: {FormatValue(value)}");
    }

    private static string ValueTypeName(MooValue value)
        => value switch {
            MooValue.Nothing => "nothing",
            MooValue.Clear => "clear",
            MooValue.Integer => "integer",
            MooValue.Float => "float",
            MooValue.String => "string",
            MooValue.Object => "object",
            MooValue.List => "list",
            MooValue.Error => "error",
            _ => "string"
        };

    private static string FormatValue(MooValue value)
        => value switch {
            MooValue.Integer i => i.Value.ToString(CultureInfo.InvariantCulture),
            MooValue.Float f => f.Value.ToString(CultureInfo.InvariantCulture),
            MooValue.String s => Quote(s.Value),
            MooValue.Object o => Quote(o.Value.ToString()),
            MooValue.List l => FormatMooList(l.Items),
            MooValue.Error e => e.Code.ToString(CultureInfo.InvariantCulture),
            _ => Quote(value.ToString())
        };

    private static string FormatMooList(IReadOnlyList<MooValue> values)
        => "[" + string.Join(", ", values.Select(FormatValue)) + "]";

    private static void AppendStringList(StringBuilder sb, string name, IReadOnlyList<string> values) {
        if (values.Count == 0) {
            sb.AppendLine($"{name}: []");
            return;
        }

        sb.AppendLine($"{name}:");
        foreach (var value in values)
            sb.AppendLine($"  - {Quote(value)}");
    }

    private static string FormatStringList(IReadOnlyList<string> values)
        => "[" + string.Join(", ", values.Select(Quote)) + "]";

    private static void AppendFlags(StringBuilder sb, string name, IReadOnlyList<string> values, bool inlineWhenEmpty = false) {
        if (values.Count == 0) {
            sb.AppendLine(inlineWhenEmpty ? $"{name}: []" : $"{name}:");
            return;
        }

        sb.AppendLine($"{name}:");
        foreach (var value in values)
            sb.AppendLine($"  - {value}");
    }

    private static IReadOnlyList<string> ObjectFlagsToNames(ObjectFlags flags)
        => Enum.GetValues<ObjectFlags>()
            .Where(flag => flag != ObjectFlags.None && flags.HasFlag(flag))
            .Select(flag => flag.ToString().ToLowerInvariant())
            .ToList();

    private static IReadOnlyList<string> PropertyFlagsToNames(PropertyFlags flags)
        => Enum.GetValues<PropertyFlags>()
            .Where(flag => flag != PropertyFlags.None && flags.HasFlag(flag))
            .Select(flag => flag.ToString().ToLowerInvariant())
            .ToList();

    private static IReadOnlyList<string> VerbFlagsToNames(VerbFlags flags)
        => Enum.GetValues<VerbFlags>()
            .Where(flag => flag != VerbFlags.None && flags.HasFlag(flag))
            .Select(flag => flag.ToString().ToLowerInvariant())
            .ToList();

    private static string VerbObjectSpecToString(VerbObjectSpec spec)
        => spec.ToString().ToLowerInvariant();

    private static string QuoteIfNeeded(string value)
        => NeedsQuoting(value) ? Quote(value) : value;

    private static bool NeedsQuoting(string value)
        => value.Length == 0
           || value.StartsWith('#')
           || value.StartsWith('@')
           || value.Contains(':')
           || value.Contains('"')
           || value.Contains('\n')
           || value.Contains('[')
           || value.Contains(']')
           || value.Trim() != value;

    private static string Quote(string value)
        => "\"" + value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "")
            + "\"";
}
