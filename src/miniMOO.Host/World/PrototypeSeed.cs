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
        Prop(genRoom, "exits", new MooValue.List([]));
        Prop(genRoom, "entrances", new MooValue.List([]));

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

        genRoom.Verbs.Add(ScriptVerb(["obvious_exits", "obvious_entrances"], """
            exits = {};
            for exit in (this.exits)
                if (exit.obvious)
                    exits = setadd(exits, exit);
                endif
            endfor
            return exits;        
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genRoom.Verbs.Add(ScriptVerb(["match_exit"], """
            ":match_exit(name) => exit | $failed_match | $ambiguous_match";
            "Matches NAME against this.exits by exit.name and exit.aliases.";
            player:tell("Matching ", args[1], " against exits: ", $string_utils:english_list(this.exits));
            what = args[1];
            if (what)
              yes = $failed_match;
              for e in (this.exits)
                if (valid(e) && what in {e.name, @e.aliases})
                  if (yes == $failed_match)
                    yes = e;
                  elseif (yes != e)
                    return $ambiguous_match;
                  endif
                endif
              endfor
              return yes;
            else
              return $nothing;
            endif
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

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
        Prop(genProg, "programmer", 1);

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

        genProg.Verbs.Add(ScriptVerb(["eval_cmd_string"], """
            program = args[1];
            program = program + ";";


            if (!match(program, "^ *%(;%|%(if%|fork?%|return%|while%|try%)[^a-z0-9A-Z_]%)"))
              program = "return " + program;
            endif

            start_ticks = ticks_left();
            start_seconds = seconds_left();

            value = eval(program);

            ticks = start_ticks - ticks_left();
            seconds = start_seconds - seconds_left();

            return {1, value, ticks, seconds};
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genProg.Verbs.Add(ScriptVerb(["eval", "eval-d", ";"], """
            if (player != this)
              player:tell("I don't understand that.");
              return;
            elseif (!player.programmer)
              player:tell("You need to be a programmer to eval code.");
              return;
            endif

            result = player:eval_cmd_string(argstr, verb != "eval-d");

            if (result[1])
              player:tell(tostr(result[2]));
            else
              player:tell(result[2]);
            endif

            player:tell("[used ", result[3], " ticks, ", result[4], " seconds.]");

        """, VerbObjectSpec.Any, "any", VerbObjectSpec.Any));

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

        genFrandsPlayerClass.Verbs.Add(ScriptVerb(["tell_ways"], """
            ":tell_ways (<list of exits>)' - Tell yourself a list of exits, for @ways. You can override it to print the exits in any format.";
            exits = args[1];
            answer = {};
            for e in (exits)
              answer = {@answer, e.name + " (" + $string_utils:english_list(e.aliases) + ")"};
            endfor
            player:tell("Obvious exits: ", $string_utils:english_list(answer), ".");
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genFrandsPlayerClass.Verbs.Add(ScriptVerb(["obvious_exits"], """
            "'obvious_exits()' - Return a list of common exit names which are obviously worth looking for in a room.";
            return {"n", "ne", "e", "se", "s", "sw", "w", "nw", "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest", "u", "d", "up", "down", "out", "exit", "leave", "enter"};       
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genFrandsPlayerClass.Verbs.Add(ScriptVerb(["findexits"], """
            "Add to the 'exits' list any exits in the room which have a single-letter alias.";
            {room, exits} = args;
            alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
            for i in [1..length(alphabet)]
              found = room:match_exit(alphabet[i]);
              if (valid(found) && !(found in exits))
                exits = {@exits, found};
              endif
            endfor

            return exits;        
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genFrandsPlayerClass.Verbs.Add(ScriptVerb(["checkexits"], """
            "Check a list of exits to see if any of them are in the given room.";
            {to_check, room, exits} = args;
            for word in (to_check)
              found = room:match_exit(word);
              if (valid(found) && !(found in exits))
                exits = {@exits, found};
              endif
            endfor
            return exits;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genFrandsPlayerClass.Verbs.Add(ScriptVerb(["@ways"], """
            "'@ways', '@ways <room>' - List any obvious exits from the given room (or this room, if none is given).";
            if (dobjstr)
              room = dobj;
            else
              room = this.location;
            endif

            if (!valid(room) || !($room in $object_utils:ancestors(room)))
              player:tell("You can only pry into the exits of a room.");
              return;
            endif

            exits = {};

            if ($object_utils:has_verb(room, "obvious_exits"))
              exits = room:obvious_exits();
            endif

            exits = this:checkexits(this:obvious_exits(), room, exits);
            exits = this:findexits(room, exits);
            this:tell_ways(exits);
        
        """, VerbObjectSpec.Any, "none", VerbObjectSpec.None));

        genFrandsPlayerClass.Verbs.Add(ScriptVerb(["@ways_old"], """
            
            for obj in (here.contents)
              if (valid(obj.destination))
                player:tell("Obvious exits: ", obj.name, " leads to ", obj.destination.name, " (", obj.destination, ").");
              endif
            endfor
            this:tell_ways(here.contents, here);
        """, VerbObjectSpec.Any, "none", VerbObjectSpec.None));


        repo.Add(genFrandsPlayerClass);
    }
}
