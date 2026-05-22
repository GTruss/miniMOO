using miniMOO.Core.Things;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static MooObject Obj(
        ObjectId id,
        ObjectId ownerId,
        ObjectId? parentId,
        ObjectId? locationId,
        string name)
        => new() {
            Id = id,
            OwnerId = ownerId,
            ParentId = parentId,
            LocationId = locationId,
            Name = name,
            Flags = ObjectFlags.Readable
        };

    private static MooVerb ScriptVerb(
        string[] names,
        string code,
        VerbObjectSpec dobj = VerbObjectSpec.None,
        string prep = "none",
        VerbObjectSpec iobj = VerbObjectSpec.None) {

        var verb = new MooVerb {
            OwnerId = ObjectId.System,
            DirectObject = dobj,
            Preposition = prep,
            IndirectObject = iobj,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = code
        };

        foreach (var name in names)
            verb.Names.Add(name);

        return verb;
    }

    private static MooVerb BuiltinVerb(
        string[] names,
        string implementation,
        VerbObjectSpec dobj = VerbObjectSpec.None,
        string prep = "none",
        VerbObjectSpec iobj = VerbObjectSpec.None) {

        var verb = new MooVerb {
            OwnerId = ObjectId.System,
            DirectObject = dobj,
            Preposition = prep,
            IndirectObject = iobj,
            ImplementationKind = VerbImplementationKind.Builtin,
            Implementation = implementation
        };

        foreach (var name in names)
            verb.Names.Add(name);

        return verb;
    }

    private static MooValue.List ObjList(params ObjectId[] ids)
        => new(ids.Select(id => (MooValue)new MooValue.Object(id)).ToList());

    private static void Prop(MooObject obj, string name, string value)
        => Prop(obj, name, new MooValue.String(value));

    private static void Prop(MooObject obj, string name, int value)
        => Prop(obj, name, new MooValue.Integer(value));

    private static void Prop(MooObject obj, string name, MooValue value)
        => obj.Properties[name] = new MooProperty {
            Name = name,
            OwnerId = obj.OwnerId,
            Value = value
        };
}
