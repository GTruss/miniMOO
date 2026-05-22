using miniMOO.Core.Things;
using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddPrototypes(InMemoryObjectRepository repo) {
        AddRoot(repo);
        AddRoom(repo);
        AddExitPrototype(repo);
        AddThing(repo);
        AddPlayerPrototype(repo);
        AddBuilder(repo);
        AddContainer(repo);
        AddNote(repo);
        AddProgrammer(repo);
        AddWizardPrototype(repo);
        AddMailPlayer(repo);
        AddFrandsPlayerClass(repo);
    }

    private static void AddRoot(InMemoryObjectRepository repo) {
        var root = Obj(WorldIds.Root, ObjectId.System, null, null, "$root");
        root.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(root, "description", "You see nothing special.");

        root.Verbs.Add(ScriptVerb(["tell"], """
            notify(this, tostr(@args));
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        root.Verbs.Add(ScriptVerb(["look_self"], """
            desc = this.description;
            if (desc)
              player:tell(desc);
            else
              player:tell("You see nothing special.");
            endif
        """));

        repo.Add(root);
    }

    private static void AddRoom(InMemoryObjectRepository repo) {
        var genRoom = Obj(WorldIds.Room, ObjectId.System, WorldIds.Root, null, "$room");
        genRoom.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genRoom, "description", "An empty room.");

        genRoom.Verbs.Add(ScriptVerb(["announce"], """
            notify(player, tostr(@args));
        """));

        genRoom.Verbs.Add(ScriptVerb(["say", "\""], """
            player:tell("You say, \"", argstr, "\"");
            this:announce(player.name, " says, \"", argstr, "\"");
        """, VerbObjectSpec.Any, "any", VerbObjectSpec.Any));

        genRoom.Verbs.Add(ScriptVerb(["emote", ":"], """
            this:announce(player.name, " ", argstr);
        """, VerbObjectSpec.Any, "any", VerbObjectSpec.Any));

        genRoom.Verbs.Add(ScriptVerb(["emote_nospace", "::"], """
            this:announce(player.name, argstr);
        """, VerbObjectSpec.Any, "any", VerbObjectSpec.Any));

        genRoom.Verbs.Add(ScriptVerb(["look_self"], """
            player:tell(this.name);
            pass();
            this:tell_contents();
        """));

        genRoom.Verbs.Add(ScriptVerb(["tell_contents"], """
            shown = 0;
            for obj in (this.contents)
              if (obj != player && !obj.obvious)
                if (!shown)
                  player:tell("You see");
                  shown = 1;
                endif
                player:tell("  ", obj.name);
              endif
            endfor
        """));

        const string lookScript = """
            if (dobjstr == "" && iobjstr == "")
              this:look_self();
            elseif (valid(dobj))
              dobj:look_self();
            elseif (valid(iobj))
              iobj:look_self();
            else
              player:tell("You don't see that here.");
            endif
        """;

        genRoom.Verbs.Add(ScriptVerb(["look", "l"], lookScript,
            VerbObjectSpec.Any, "none", VerbObjectSpec.None));

        genRoom.Verbs.Add(ScriptVerb(["look", "l"], lookScript,
            VerbObjectSpec.None, "at", VerbObjectSpec.Any));

        repo.Add(genRoom);
    }

    private static void AddExitPrototype(InMemoryObjectRepository repo) {
        var genExit = Obj(WorldIds.Exit, ObjectId.System, WorldIds.Root, null, "$exit");
        genExit.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genExit, "description", "You see an exit.");
        Prop(genExit, "obvious", new MooValue.Integer(1));
        repo.Add(genExit);
    }

    private static void AddThing(InMemoryObjectRepository repo) {
        var genThing = Obj(WorldIds.Thing, ObjectId.System, WorldIds.Root, null, "$thing");
        genThing.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genThing, "description", "You see nothing special about it.");
        repo.Add(genThing);
    }

    private static void AddPlayerPrototype(InMemoryObjectRepository repo) {
        var genPlayer = Obj(WorldIds.Player, ObjectId.System, WorldIds.Root, null, "$player");
        genPlayer.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genPlayer, "description", "A nondescript person.");
        AddPlayerVerbs(genPlayer);
        repo.Add(genPlayer);
    }

    private static void AddBuilder(InMemoryObjectRepository repo) {
        var genBuilder = Obj(WorldIds.Builder, ObjectId.System, WorldIds.FrandsPlayerClass, null, "$builder");
        genBuilder.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;

        genBuilder.Verbs.Add(ScriptVerb(["@create"], """
            parent = dobj;
            if (!valid(parent))
              player:tell("Usage: @create <parent> [named [name:]alias,alias,...]");
              return;
            endif
            newobj = create(parent);
            move(newobj, player);
            if (iobjstr != "")
              $building_utils:set_names(newobj, iobjstr);
            endif
            player:tell("You now have ", newobj.name, " with object number ", newobj, " and parent ", parent.name, " (", parent, ").");
        """, VerbObjectSpec.Any, "any", VerbObjectSpec.Any));

        genBuilder.Verbs.Add(ScriptVerb(["@dig"], """
            if (dobjstr == "")
              player:tell("Usage: @dig <room-name>");
              player:tell("       @dig <exit-spec>[|<return-spec>] to <room-name-or-#id>");
              return;
            endif
            if (iobjstr == "")
              newroom = create($room);
              $building_utils:set_names(newroom, dobjstr);
              player:tell("Room ", newroom.name, " (", newroom, ") created.");
            else
              if (valid(iobj))
                destroom = iobj;
                newroom = 0;
              else
                newroom = create($room);
                $building_utils:set_names(newroom, iobjstr);
                destroom = newroom;
              endif
              if (valid(newroom))
                player:tell("Room ", newroom.name, " (", newroom, ") created.");
              endif
              exits = $string_utils:explode(dobjstr, "|");
              $building_utils:make_exit(exits[1], here, destroom);
              if (length(exits) == 2)
                $building_utils:make_exit(exits[2], destroom, here);
              endif
            endif
        """, VerbObjectSpec.Any, "any", VerbObjectSpec.Any));

        repo.Add(genBuilder);
    }

    private static void AddContainer(InMemoryObjectRepository repo) {
        var genContainer = Obj(WorldIds.Container, ObjectId.System, WorldIds.Thing, null, "$container");
        genContainer.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genContainer, "description", "A container.");
        repo.Add(genContainer);
    }

    private static void AddNote(InMemoryObjectRepository repo) {
        var genNote = Obj(WorldIds.Note, ObjectId.System, WorldIds.Thing, null, "$note");
        genNote.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genNote, "description", "A note.");
        repo.Add(genNote);
    }

    private static void AddProgrammer(InMemoryObjectRepository repo) {
        var genProg = Obj(WorldIds.Prog, ObjectId.System, WorldIds.Builder, null, "$prog");
        genProg.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;

        genProg.Verbs.Add(ScriptVerb(["@parents"], """
            obj = dobj;
            if (!valid(obj))
                player:tell("Usage: @parents <object>");
                return;
            endif
            player:tell(tostr(obj.name, " (", obj, ")"));
            while (valid(obj = parent(obj)))
                player:tell("  ", obj.name, " (", obj, ")");
            endwhile
        """, VerbObjectSpec.Any));

        repo.Add(genProg);
    }

    private static void AddWizardPrototype(InMemoryObjectRepository repo) {
        var genWiz = Obj(WorldIds.Wiz, ObjectId.System, WorldIds.Prog, null, "$wiz");
        genWiz.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        repo.Add(genWiz);
    }

    private static void AddMailPlayer(InMemoryObjectRepository repo) {
        var genMailPlayer = Obj(WorldIds.MailPlayer, ObjectId.System, WorldIds.Player, null, "generic mail player");
        genMailPlayer.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        repo.Add(genMailPlayer);
    }

    private static void AddFrandsPlayerClass(InMemoryObjectRepository repo) {
        var genFrandsPlayerClass = Obj(WorldIds.FrandsPlayerClass, ObjectId.System, WorldIds.MailPlayer, null, "Frand's player class");
        genFrandsPlayerClass.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        repo.Add(genFrandsPlayerClass);
    }
}
