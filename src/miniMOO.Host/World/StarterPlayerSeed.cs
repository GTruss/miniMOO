using miniMOO.Core.Things;
using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddStarterPlayer(InMemoryObjectRepository repo) {
        var wizard = Obj(WorldIds.Wizard, WorldIds.Wizard, WorldIds.Wiz, WorldIds.Foyer, "Wizard");
        wizard.Flags = ObjectFlags.User | ObjectFlags.Programmer | ObjectFlags.Wizard;
        Prop(wizard, "description", "The all-powerful wizard of miniMOO.");
        Prop(wizard, "debug", new MooValue.Integer(0));
        repo.Add(wizard);

        var staff = Obj(WorldIds.Staff, WorldIds.Wizard, WorldIds.Thing, WorldIds.Wizard, "a gnarled staff");
        staff.Aliases.Add("staff");
        staff.Aliases.Add("gnarled staff");
        Prop(staff, "description", "A twisted length of dark wood, warm to the touch.");
        repo.Add(staff);
    }

    private static void AddPlayerVerbs(MooObject player) {
        player.Verbs.Add(BuiltinVerb(["@ways"], "ways"));

        player.Verbs.Add(ScriptVerb(["wave"], """
            player:tell("You wave.");
            player.location:announce(player.name, " waves.");
        """));

        player.Verbs.Add(ScriptVerb(["list inventory", "i"], """
            for obj in (player.contents)
              player:tell(obj.name);
            endfor
        """));
    }
}
