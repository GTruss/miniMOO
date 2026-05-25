using miniMOO.Core.Things;

namespace miniMOO.Data.FileSystem;

public sealed class FileObjectMaterializer {
    public MooObject ToMooObject(FileObjectDefinition definition) {
        var obj = new MooObject {
            Id = definition.Id,
            ParentId = definition.ParentId,
            LocationId = definition.LocationId,
            OwnerId = definition.OwnerId,
            Name = definition.Name,
            Flags = definition.Flags
        };

        obj.Aliases.AddRange(definition.Aliases);

        foreach (var property in definition.Properties)
            obj.Properties[property.Name] = new MooProperty {
                Name = property.Name,
                OwnerId = property.OwnerId ?? definition.OwnerId,
                Flags = property.Flags,
                Value = ToMooValue(property.Value)
            };

        foreach (var verb in definition.Verbs) {
            var mooVerb = new MooVerb {
                OwnerId = verb.OwnerId ?? definition.OwnerId,
                Flags = verb.Flags,
                DirectObject = verb.DirectObject,
                Preposition = verb.Preposition,
                IndirectObject = verb.IndirectObject,
                ImplementationKind = verb.ImplementationKind,
                Code = verb.Code
            };

            mooVerb.Names.AddRange(verb.Names);
            obj.Verbs.Add(mooVerb);
        }

        return obj;
    }

    private static MooValue ToMooValue(FileMooValueDefinition definition)
        => definition.Type.ToLowerInvariant() switch {
            "nothing" => MooValue.NothingValue,
            "integer" => new MooValue.Integer(Convert.ToInt64(definition.Value)),
            "float" => new MooValue.Float(Convert.ToDouble(definition.Value)),
            "string" => new MooValue.String(Convert.ToString(definition.Value) ?? ""),
            "object" => new MooValue.Object(ParseObjectId(Convert.ToString(definition.Value) ?? "")),
            "list" => new MooValue.List(((IEnumerable<FileMooValueDefinition>)(definition.Value
                    ?? Array.Empty<FileMooValueDefinition>()))
                .Select(ToMooValue)
                .ToList()),
            _ => throw new FileWorldLoadException($"Unsupported MOO value type: {definition.Type}")
        };

    private static ObjectId ParseObjectId(string value) {
        if (value.StartsWith('#') && int.TryParse(value[1..], out var id))
            return new ObjectId(id);

        throw new FileWorldLoadException($"Expected object id like #1, got: {value}");
    }
}
