using miniMOO.Core.Things;

namespace miniMOO.Data.FileSystem;

public sealed class FileWorldLoader {
    public FileWorldDefinition LoadDirectory(string rootPath) {
        if (!Directory.Exists(rootPath))
            throw new FileWorldLoadException($"World data directory does not exist: {rootPath}");

        var world = new FileWorldDefinition();

        foreach (var file in Directory.EnumerateFiles(rootPath, "*.moo.md", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)) {
            world.Objects.Add(LoadObject(file));
        }

        ValidateWorld(world);
        return world;
    }

    public FileObjectDefinition LoadObject(string path) {
        try {
            var text = File.ReadAllText(path).Replace("\r\n", "\n").Replace('\r', '\n');
            return ParseObject(text, path);
        }
        catch (FileWorldLoadException) {
            throw;
        }
        catch (FormatException ex) {
            throw new FileWorldLoadException($"Could not parse object definition: {path}: {ex.Message}", ex);
        }
        catch (OverflowException ex) {
            throw new FileWorldLoadException($"Could not parse object definition: {path}: {ex.Message}", ex);
        }
        catch (IOException ex) {
            throw new FileWorldLoadException($"Could not read object definition: {path}", ex);
        }
    }

    private static FileObjectDefinition ParseObject(string text, string path) {
        var lines = text.Split('\n');
        var frontmatter = ReadFrontmatter(lines, path, out var contentStartLine);

        var properties = new List<FilePropertyDefinition>();
        var verbs = new List<FileVerbDefinition>();

        for (var i = contentStartLine; i < lines.Length; i++) {
            if (!TryReadFence(lines, i, out var fence))
                continue;

            if (fence.Language == "yaml") {
                var metadata = YamlLite.Parse(fence.Content);
                if (metadata.Has("name") && metadata.Has("names"))
                    throw new FileWorldLoadException($"{path}: YAML block cannot be both a property and a verb.");

                if (!metadata.Has("names")) {
                    properties.Add(ParseProperty(metadata, path));
                    i = fence.EndLine;
                    continue;
                }

                var nextLine = fence.EndLine + 1;

                while (nextLine < lines.Length && !TryReadFence(lines, nextLine, out _))
                    nextLine++;

                if (nextLine >= lines.Length || !TryReadFence(lines, nextLine, out var codeFence) || codeFence.Language != "csharp")
                    throw new FileWorldLoadException($"{path}: moo-verb block must be followed by a ```csharp code block.");

                verbs.Add(ParseVerb(metadata, codeFence.Content, path));
                i = codeFence.EndLine;
            }
            else if (fence.Language == "csharp") {
                throw new FileWorldLoadException($"{path}: orphan ```csharp code block. Verb code must follow a ```yaml metadata block with 'names'.");
            }
            else if (fence.Language.Length > 0) {
                throw new FileWorldLoadException($"{path}: unsupported markdown fence language '{fence.Language}'. Use ```yaml for metadata and ```csharp for verb code.");
            }
        }

        var definition = new FileObjectDefinition {
            Id = ParseObjectId(frontmatter.RequiredString("id", path), path, "id"),
            ParentId = ParseOptionalObjectId(frontmatter.OptionalString("parent"), path, "parent"),
            LocationId = ParseOptionalObjectId(frontmatter.OptionalString("location"), path, "location"),
            OwnerId = ParseObjectId(frontmatter.OptionalString("owner") ?? "#0", path, "owner"),
            Name = frontmatter.RequiredString("name", path),
            Aliases = frontmatter.OptionalArray("aliases") ?? [],
            Flags = ParseFlags<ObjectFlags>(frontmatter.OptionalArray("flags")),
            Properties = properties,
            Verbs = verbs
        };

        ValidateObject(definition, path);
        return definition;
    }

    private static FilePropertyDefinition ParseProperty(YamlLite block, string path) {
        var name = block.RequiredString("name", path);
        if (string.IsNullOrWhiteSpace(name))
            throw new FileWorldLoadException($"{path}: property name cannot be empty.");

        var type = block.OptionalString("type") ?? "string";
        var rawValue = block.OptionalRaw("value");

        if (rawValue is null && type != "nothing")
            throw new FileWorldLoadException($"{path}: property '{name}' is missing required field 'value'.");

        return new FilePropertyDefinition {
            Name = name,
            OwnerId = ParseOptionalObjectId(block.OptionalString("owner"), path, $"property {name} owner"),
            Flags = ParseFlags(block.OptionalArray("flags"), PropertyFlags.Readable),
            Value = ParseMooValue(type, rawValue)
        };
    }

    private static FileVerbDefinition ParseVerb(YamlLite block, string code, string path) {
        var names = block.RequiredArray("names", path);
        if (names.Any(string.IsNullOrWhiteSpace))
            throw new FileWorldLoadException($"{path}: verb names cannot be empty.");

        return new FileVerbDefinition {
            Names = names,
            OwnerId = ParseOptionalObjectId(block.OptionalString("owner"), path, $"verb {string.Join(' ', names)} owner"),
            Flags = ParseFlags(block.OptionalArray("flags"), VerbFlags.Readable | VerbFlags.Executable),
            DirectObject = ParseVerbObjectSpec(block.OptionalString("dobj") ?? "none", path, "dobj"),
            Preposition = block.OptionalString("prep") ?? "none",
            IndirectObject = ParseVerbObjectSpec(block.OptionalString("iobj") ?? "none", path, "iobj"),
            ImplementationKind = ParseImplementationKind(block.OptionalString("kind") ?? "script", path),
            Code = TrimOneTrailingNewline(code)
        };
    }

    private static YamlLite ReadFrontmatter(string[] lines, string path, out int contentStartLine) {
        contentStartLine = 0;

        if (lines.Length == 0 || lines[0].Trim() != "---")
            throw new FileWorldLoadException($"{path}: object file must begin with YAML-lite frontmatter.");

        for (var i = 1; i < lines.Length; i++) {
            if (lines[i].Trim() != "---")
                continue;

            contentStartLine = i + 1;
            return YamlLite.Parse(string.Join('\n', lines[1..i]));
        }

        throw new FileWorldLoadException($"{path}: frontmatter is missing closing '---'.");
    }

    private static bool TryReadFence(string[] lines, int startLine, out MarkdownFence fence) {
        fence = default;

        var line = lines[startLine].Trim();
        if (!line.StartsWith("```", StringComparison.Ordinal))
            return false;

        var language = line[3..].Trim();
        var content = new List<string>();

        for (var i = startLine + 1; i < lines.Length; i++) {
            if (lines[i].Trim() == "```") {
                fence = new MarkdownFence(language, string.Join('\n', content), i);
                return true;
            }

            content.Add(lines[i]);
        }

        throw new FileWorldLoadException($"Unclosed markdown fence starting at line {startLine + 1}.");
    }

    private static FileMooValueDefinition ParseMooValue(string type, string? rawValue) {
        try {
            return type.ToLowerInvariant() switch {
                "nothing" => new FileMooValueDefinition { Type = "nothing" },
                "integer" or "int" => new FileMooValueDefinition {
                    Type = "integer",
                    Value = long.Parse(rawValue ?? "0")
                },
                "float" => new FileMooValueDefinition {
                    Type = "float",
                    Value = double.Parse(rawValue ?? "0")
                },
                "string" or "str" => new FileMooValueDefinition {
                    Type = "string",
                    Value = YamlLite.Unquote(rawValue ?? "")
                },
                "object" or "obj" => new FileMooValueDefinition {
                    Type = "object",
                    Value = YamlLite.Unquote(rawValue ?? "#-1")
                },
                "list" => new FileMooValueDefinition {
                    Type = "list",
                    Value = YamlLite.ParseArray(rawValue ?? "[]")
                        .Select(InferScalarValue)
                        .ToList()
                },
                _ => throw new FileWorldLoadException($"Unsupported MOO value type: {type}")
            };
        }
        catch (FormatException ex) {
            throw new FileWorldLoadException($"Invalid {type} value: {rawValue}", ex);
        }
        catch (OverflowException ex) {
            throw new FileWorldLoadException($"Invalid {type} value: {rawValue}", ex);
        }
    }

    private static FileMooValueDefinition InferScalarValue(string value) {
        if (value.StartsWith('#') && int.TryParse(value[1..], out _))
            return new FileMooValueDefinition { Type = "object", Value = value };

        if (long.TryParse(value, out var integer))
            return new FileMooValueDefinition { Type = "integer", Value = integer };

        if (double.TryParse(value, out var number))
            return new FileMooValueDefinition { Type = "float", Value = number };

        return new FileMooValueDefinition { Type = "string", Value = value };
    }

    private static ObjectId ParseObjectId(string value, string path, string field) {
        if (value.StartsWith('#') && int.TryParse(value[1..], out var id))
            return new ObjectId(id);

        throw new FileWorldLoadException($"{path}: expected {field} to be an object id like #1.");
    }

    private static ObjectId? ParseOptionalObjectId(string? value, string path, string field)
        => string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase)
            ? null
            : ParseObjectId(value, path, field);

    private static TEnum ParseFlags<TEnum>(
        IReadOnlyList<string>? values,
        TEnum defaultValue = default)
        where TEnum : struct, Enum {

        if (values is null || values.Count == 0)
            return defaultValue;

        var result = 0;
        foreach (var value in values) {
            if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var flag))
                throw new FileWorldLoadException($"Unknown {typeof(TEnum).Name} value: {value}");

            result |= Convert.ToInt32(flag);
        }

        return (TEnum)Enum.ToObject(typeof(TEnum), result);
    }

    private static VerbObjectSpec ParseVerbObjectSpec(string value, string path, string field)
        => value.ToLowerInvariant() switch {
            "none" => VerbObjectSpec.None,
            "any" => VerbObjectSpec.Any,
            "this" => VerbObjectSpec.This,
            _ => throw new FileWorldLoadException($"{path}: invalid {field} value: {value}")
        };

    private static VerbImplementationKind ParseImplementationKind(string value, string path)
        => value.ToLowerInvariant() switch {
            "script" => VerbImplementationKind.Script,
            "builtin" => VerbImplementationKind.Builtin,
            _ => throw new FileWorldLoadException($"{path}: invalid verb kind: {value}")
        };

    private static string TrimOneTrailingNewline(string value)
        => value.EndsWith('\n') ? value[..^1] : value;

    private static void ValidateWorld(FileWorldDefinition world) {
        var ids = new Dictionary<ObjectId, string>();
        foreach (var obj in world.Objects) {
            if (!ids.TryAdd(obj.Id, obj.Name))
                throw new FileWorldLoadException($"Duplicate object id {obj.Id} used by '{ids[obj.Id]}' and '{obj.Name}'.");
        }
    }

    private static void ValidateObject(FileObjectDefinition definition, string path) {
        if (string.IsNullOrWhiteSpace(definition.Name))
            throw new FileWorldLoadException($"{path}: object name cannot be empty.");

        var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in definition.Properties) {
            if (!propertyNames.Add(property.Name))
                throw new FileWorldLoadException($"{path}: duplicate property '{property.Name}'.");
        }

        foreach (var verb in definition.Verbs) {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in verb.Names) {
                if (!names.Add(name))
                    throw new FileWorldLoadException($"{path}: duplicate verb name '{name}' in one verb definition.");
            }
        }
    }

    private readonly record struct MarkdownFence(string Language, string Content, int EndLine);
}
