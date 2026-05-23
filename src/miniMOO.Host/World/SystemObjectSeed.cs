using miniMOO.Core.Things;
using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddSystemObject(InMemoryObjectRepository repo) {
        var sysObj = Obj(ObjectId.System, ObjectId.System, null, null, "The System Object");

        Prop(sysObj, "root", new MooValue.Object(WorldIds.Root));
        Prop(sysObj, "room", new MooValue.Object(WorldIds.Room));
        Prop(sysObj, "builder", new MooValue.Object(WorldIds.Builder));
        Prop(sysObj, "thing", new MooValue.Object(WorldIds.Thing));
        Prop(sysObj, "player", new MooValue.Object(WorldIds.Player));
        Prop(sysObj, "exit", new MooValue.Object(WorldIds.Exit));
        Prop(sysObj, "container", new MooValue.Object(WorldIds.Container));
        Prop(sysObj, "note", new MooValue.Object(WorldIds.Note));
        Prop(sysObj, "player_start", new MooValue.Object(WorldIds.PlayerStart));
        Prop(sysObj, "prog", new MooValue.Object(WorldIds.Prog));
        Prop(sysObj, "wiz", new MooValue.Object(WorldIds.Wiz));
        Prop(sysObj, "string_utils",   new MooValue.Object(WorldIds.StringUtils));
        Prop(sysObj, "building_utils", new MooValue.Object(WorldIds.BuildingUtils));
        Prop(sysObj, "object_utils",   new MooValue.Object(WorldIds.ObjectUtils));
        Prop(sysObj, "command_utils",   new MooValue.Object(WorldIds.CommandUtils));
        Prop(sysObj, "list_utils",   new MooValue.Object(WorldIds.ListUtils));
        Prop(sysObj, "code_utils",   new MooValue.Object(WorldIds.CodeUtils));
        Prop(sysObj, "build_options",   new MooValue.Object(WorldIds.BuilderOptions));
        Prop(sysObj, "nothing", new MooValue.Object(new ObjectId(-1)));
        Prop(sysObj, "failed_match", new MooValue.Object(new ObjectId(-2)));
        Prop(sysObj, "ambiguous_match", new MooValue.Object(new ObjectId(-3)));

        repo.Add(sysObj);
    }
}
