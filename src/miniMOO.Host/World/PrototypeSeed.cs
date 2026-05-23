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
        AddGenericOptionsPrototype(repo);
        AddBuildOptionsPrototype(repo);
        AddMailPlayer(repo);
        AddFrandsPlayerClass(repo);
    }

    private static void AddRoot(InMemoryObjectRepository repo) {
        var root = Obj(WorldIds.Root, ObjectId.System, null, null, "$root");
        root.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(root, "description", "You see nothing special.");

        root.Verbs.Add(ScriptVerb(["moveto"], """
            return `move(this, args[1]) ! ANY => E_NONE';
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        root.Verbs.Add(ScriptVerb(["title"], """
            return this.name;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));
         
        root.Verbs.Add(ScriptVerb(["titlec"], """
            return `this.namec ! E_PROPNF => this:title()';
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

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
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        root.Verbs.Add(ScriptVerb(["match"], """
            c = this:contents();
            return $string_utils:match(args[1], c, "name", c, "aliases");
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        root.Verbs.Add(ScriptVerb(["contents"], """
            return this.contents;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        root.Verbs.Add(ScriptVerb(["set_description"], """
             if (typeof(desc = args[1]) in {LIST, STR})
              this.description = desc;
              return 1;
            else
              return E_TYPE;
            endif            
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        repo.Add(root);
    }

    private static void AddRoom(InMemoryObjectRepository repo) {
        var genRoom = Obj(WorldIds.Room, ObjectId.System, WorldIds.Root, null, "$room");
        genRoom.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genRoom, "name", "generic room");
        Prop(genRoom, "namec", "generic room");
        Prop(genRoom, "description", "An empty room.");
        Prop(genRoom, "exits", new MooValue.List([]));
        Prop(genRoom, "entrances", new MooValue.List([]));
        Prop(genRoom, "ctype", 1);

        genRoom.Verbs.Add(ScriptVerb(["add_exit"], """
            return `this.exits = setadd(this.exits, args[1]) ! E_PERM => E_NONE' != E_PERM;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genRoom.Verbs.Add(ScriptVerb(["add_entrance"], """
            return `this.entrances = setadd(this.entrances, args[1]) ! E_PERM => E_NONE' != E_PERM;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genRoom.Verbs.Add(ScriptVerb(["contents"], """
            return this.contents;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genRoom.Verbs.Add(ScriptVerb(["acceptable"], """
            what = args[1];
            
            return 1;
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genRoom.Verbs.Add(ScriptVerb(["announce"], """
            notify(player, tostr(@args));
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genRoom.Verbs.Add(ScriptVerb(["announce_all_but"], """
            {ignore, @text} = args;
                
            contents = this:contents();
            for l in (ignore)
              contents = setremove(contents, l);
            endfor

            for listener in (contents)
              if (is_player(listener))
                try
                    listener:tell(@text);
                except (ANY)
                    "continue listener;";
                endtry
              endif
            endfor            
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

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

        genRoom.Verbs.Add(ScriptVerb(["look_brief"], """
            player:tell(this:title());

            if (this.description)
              player:tell(this.description);
            endif
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genRoom.Verbs.Add(ScriptVerb(["look_self"], """
            {?brief = 0} = args;
            player:tell(this:title());

            if (!brief)
              pass();
            endif

            this:tell_contents(setremove(this:contents(), player), this.ctype);
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genRoom.Verbs.Add(ScriptVerb(["tell_contents"], """
            {contents, ctype} = args;

            things = {};
            players = {};

            for obj in (contents)
              if (is_player(obj))
                players = setadd(players, obj);
              else
                things = setadd(things, obj);
              endif
            endfor

            if (ctype == 0)
              if (things || players)
                player:tell("Contents:");
                for thing in (things)
                  player:tell("  ", thing.name);
                endfor
                for dude in (players)
                  player:tell("  ", dude.name);
                endfor
              endif
            elseif (ctype == 1)
              for dude in (players)
                player:tell(dude.name, " is here.");
              endfor
              for thing in (things)
                player:tell("You see ", thing.name, " here.");
              endfor
            else

              if (things)
                thing_names = {};
                for thing in (things)
                  thing_names = {@thing_names, thing.name};
                endfor

                player:tell("You see ", $string_utils:english_list(thing_names), " here.");
              endif

              if (players)
                player_names = {};
                for dude in (players)
                  player_names = {@player_names, dude.name};
                endfor
  
                player:tell($string_utils:english_list(player_names), " here.");
              endif
            endif
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));
        /*
            if (exits)
                names = {};
                for exit in (exits)
                names = {@names, exit.name};
                endfor
                player:tell("Obvious exits: ", $string_utils:english_list(names), ".");
            endif
        */

        genRoom.Verbs.Add(ScriptVerb(["go"], """
            if (!args || !(dir = args[1]))
              player:tell("You need to specify a direction.");
              return E_INVARG;
            elseif (valid(exit = player.location:match_exit(dir)))

              exit:invoke();

              if (length(args) > 1)
                old_room = player.location;
                "Now give objects in the room we just entered a chance to act.";
                "not used: suspend(0)";
                if (player.location == old_room)
                  "player didn't move or get moved while we were suspended";
                  player.location:go(@listdelete(args, 1));
                endif
              endif
            elseif (exit == $failed_match)
              player:tell("You can't go that way (", dir, ").");
            else
              player:tell("I don't know which direction `", dir, "' you mean.");
            endif
        """, VerbObjectSpec.Any, "any", VerbObjectSpec.Any));

        const string lookScript = """
            if (dobjstr == "" && iobjstr == "")
              this:look_self(0);
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

        //"player:tell("Matching ", args[1], " against exits: ", $string_utils:english_list(this.exits));";

        genRoom.Verbs.Add(ScriptVerb(["match_exit"], """
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

        genRoom.Verbs.Add(ScriptVerb(["@exits"], """
            if (this.exits == {})
              player:tell("This room has no conventional exits.");
            else
              try
                for exit in (this.exits)
                  try
                    player:tell(exit.name, " (", exit, ") leads to ", valid(exit.dest) ? exit.dest.name | "???", " (", exit.dest, ") via {", $string_utils:from_list(exit.aliases, ", "), "}.");
                  except (ANY)
                    player:tell("Bad exit or missing .dest property:  ", $string_utils:nn(exit));
                    "continue exit;";
                  endtry
                endfor
              except (E_TYPE)
                player:tell("Bad .exits property. This should be a list of exit objects. Please fix this.");
              endtry
            endif
        """, VerbObjectSpec.None, "none", VerbObjectSpec.None));

        repo.Add(genRoom);
    }

    private static void AddExitPrototype(InMemoryObjectRepository repo) {
        var genExit = Obj(WorldIds.Exit, ObjectId.System, WorldIds.Root, null, "$exit");
        genExit.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genExit, "description", "You see an exit.");
        Prop(genExit, "key", new MooValue.Integer(0));
        Prop(genExit, "obvious", new MooValue.Integer(1));
        Prop(genExit, "blessed_object", MooValue.NothingValue);
        Prop(genExit, "leave_msg", "");
        Prop(genExit, "oleave_msg", "has left.");
        Prop(genExit, "arrive_msg", "");
        Prop(genExit, "oarrive_msg", "has arrived.");
        Prop(genExit, "nogo_msg", "You can't go that way.");
        Prop(genExit, "onogo_msg", "");

        genExit.Verbs.Add(ScriptVerb(["announce_msg"], """
            msg = args[3];
            what = args[2];
            title = what:titlec();

            if (!$string_utils:index_delimited(msg, title))
              msg = tostr(title, " ", msg);
            endif
            args[1]:announce_all_but({what}, msg);            
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genExit.Verbs.Add(ScriptVerb(["is_unlocked_for"], """
            return this.key == 0 || $lock_utils:eval_key(this.key, args[1]);
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genExit.Verbs.Add(ScriptVerb(["leave_msg", "oleave_msg", "arrive_msg", "oarrive_msg", "nogo_msg", "onogo_msg"], """
            if (verb == "leave_msg")
              msg = this.leave_msg;
            elseif (verb == "oleave_msg")
              msg = this.oleave_msg;
            elseif (verb == "arrive_msg")
              msg = this.arrive_msg;
            elseif (verb == "oarrive_msg")
              msg = this.oarrive_msg;
            elseif (verb == "nogo_msg")
              msg = this.nogo_msg;
            elseif (verb == "onogo_msg")
              msg = this.onogo_msg;
            else
              msg = "";
            endif

            return msg ? msg | "";
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

       genExit.Verbs.Add(ScriptVerb(["invoke"], """
            this:move(player);
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

       genExit.Verbs.Add(ScriptVerb(["move"], """
            what = args[1];
            unlocked = this:is_unlocked_for(what);
            if (unlocked)
              this.dest.blessed_object = what;
            endif
            if (unlocked)
              start = what.location;
              if (msg = this:leave_msg(what))
                what:tell_lines(msg);
              endif
              what:moveto(this.dest);
              if (what.location != start)
                this:announce_msg(start, what, this:oleave_msg(what) || this:defaulting_oleave_msg(what) || "has left.");
              endif
              if (what.location == this.dest)

                if (what == player)
                  what.location:look_brief();
                endif

                if (msg = this:arrive_msg(what))
                  what:tell_lines(msg);
                endif
                this:announce_msg(what.location, what, this:oarrive_msg(what) || "has arrived.");
              endif
            else
              if (msg = this:nogo_msg(what))
                what:tell_lines(msg);
              else
                what:tell("You can't go that way.");
              endif
              if (msg = this:onogo_msg(what))
                this:announce_msg(what.location, what, msg);
              endif
            endif        
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

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
        Prop(genPlayer, "namec", "generic player");
            
        genPlayer.Verbs.Add(ScriptVerb(["titlec"], """
            return `this.namec ! E_PROPNF => this:title()';
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genPlayer.Verbs.Add(ScriptVerb(["moveto"], """
            if (args[1] == #-1)
              return E_INVARG;
              this:notify("You are now in #-1, The Void.  Type `home' to get back.");
            endif
            pass(@args);        
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genPlayer.Verbs.Add(ScriptVerb(["tell_lines"], """
            lines = args[1];
            if (typeof(lines) != LIST)
              lines = {lines};
            endif
            this:notify_lines(lines);       
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genPlayer.Verbs.Add(ScriptVerb(["notify_lines"], """
            for line in (typeof(lines = args[1]) != LIST ? {lines} | lines)
              this:notify(tostr(line));
            endfor       
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genPlayer.Verbs.Add(ScriptVerb(["notify"], """
            player:tell(tostr(@args));
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genPlayer.Verbs.Add(ScriptVerb(["@describe", "@desc"], """
            dobj = player:my_match_object(dobjstr);
            if ($command_utils:object_match_failed(dobj, dobjstr))
              "...lose...";
            elseif (e = dobj:set_description(iobjstr))
              player:notify("Description set.");
            else
              player:notify(tostr(e));
            endif
        """, VerbObjectSpec.Any, "as", VerbObjectSpec.Any));



        genPlayer.Verbs.Add(ScriptVerb(["wave"], """
            player:tell("You wave.");
            player.location:announce(player.name, " waves.");
        """));

        genPlayer.Verbs.Add(ScriptVerb(["list inventory", "i"], """
            for obj in (player.contents)
              player:tell(obj.name);
            endfor
        """));



        repo.Add(genPlayer);
    }

    private static void AddBuilder(InMemoryObjectRepository repo) {
        var genBuilder = Obj(WorldIds.Builder, ObjectId.System, WorldIds.FrandsPlayerClass, null, "$builder");
        genBuilder.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genBuilder, "build_options", new MooValue.List([]));

        genBuilder.Verbs.Add(ScriptVerb(["my_match_object"], """
            return $string_utils:match_object(@{@args, this.location}[1..2], this);
        """, VerbObjectSpec.Any));

        genBuilder.Verbs.Add(ScriptVerb(["build_option"], """
            return $build_options:get(this.build_options, args[1]);
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genBuilder.Verbs.Add(ScriptVerb(["_create"], """
            return `create(@args) ! ANY => E_NONE';
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

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
            nargs = length(args);
            if (nargs == 1)
              room = args[1];
              exit_spec = "";
            elseif (nargs >= 3 && args[2] == "to")
              exit_spec = args[1];
              room = $string_utils:from_list(args[3..$], " ");
            elseif (argstr && !prepstr)
              room = argstr;
              exit_spec = "";
            else
              player:notify(tostr("Usage:  ", verb, " <new-room-name>"));
              player:notify(tostr("    or  ", verb, " <exit-description> to <new-room-name-or-old-room-object-number>"));
              return;
            endif
            if (room != tostr(other_room = toobj(room)))
              room_kind = player:build_option("dig_room");
              if (room_kind == 0)
                room_kind = $room;
              endif
              other_room = player:_create(room_kind);
              if (typeof(other_room) == ERR)
                player:notify(tostr("Cannot create new room as a child of ", $string_utils:nn(room_kind), ": ", other_room, ".  See `help @build-options' for information on how to specify the kind of room this command tries to create."));
                return;
              endif
              for f in ($string_utils:char_list(player:build_option("create_flags") || ""))
                other_room.(f) = 1;
              endfor
              other_room.name = room;
              other_room.aliases = {room};
              move(other_room, $nothing);
              player:notify(tostr(other_room.name, " (", other_room, ") created."));
            elseif (nargs == 1)
              player:notify("You can't dig a room that already exists!");
              return;
            elseif (!valid(player.location) || !($room in $object_utils:ancestors(player.location)))
              player:notify(tostr("You may only use the ", verb, " command from inside a room."));
              return;
            elseif (!valid(other_room) || !($room in $object_utils:ancestors(other_room)))
              player:notify(tostr(other_room, " doesn't look like a room to me..."));
              return;
            endif
            if (exit_spec)
              exit_kind = player:build_option("dig_exit");
              if (exit_kind == 0)
                exit_kind = $exit;
              endif
              exits = $string_utils:explode(exit_spec, "|");
              if (length(exits) < 1 || length(exits) > 2)
                player:notify("The exit-description must have the form");
                player:notify("     [name:]alias,...,alias");
                player:notify("or   [name:]alias,...,alias|[name:]alias,...,alias");
                return;
              endif
              do_recreate = !player:build_option("bi_create");
              to_ok = $building_utils:make_exit(exits[1], player.location, other_room, do_recreate, exit_kind);
              if (to_ok && length(exits) == 2)
                $building_utils:make_exit(exits[2], other_room, player.location, do_recreate, exit_kind);
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
            if (program[1] == ";")
              program = program[2..length(program)];
            endif
            
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

    private static void AddGenericOptionsPrototype(InMemoryObjectRepository repo) {
        var genOptions = Obj(WorldIds.GenericOptionPackage, ObjectId.System, WorldIds.Root, null, "Generic Option Package");
        genOptions.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;


        genOptions.Verbs.Add(ScriptVerb(["get"], """
            {options, name} = args;
            if (name in options)
              return 1;
            elseif (a = $list_utils:assoc(name, options))
              return a[2];
            else
              return 0;
            endif
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        repo.Add(genOptions);
    }

    private static void AddBuildOptionsPrototype(InMemoryObjectRepository repo) {
        var genBuildOptions = Obj(WorldIds.BuilderOptions, ObjectId.System, WorldIds.GenericOptionPackage, null, "$build_options");
        genBuildOptions.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;

        repo.Add(genBuildOptions);
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
            exits = args[1];
            answer = {};
            for e in (exits)
              answer = {@answer, e.name + " (" + $string_utils:english_list(e.aliases) + ")"};
            endfor
            player:tell("Obvious exits: ", $string_utils:english_list(answer), ".");
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genFrandsPlayerClass.Verbs.Add(ScriptVerb(["obvious_exits"], """
            return {"n", "ne", "e", "se", "s", "sw", "w", "nw", "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest", "u", "d", "up", "down", "out", "exit", "leave", "enter"};       
        """, VerbObjectSpec.This, "none", VerbObjectSpec.This));

        genFrandsPlayerClass.Verbs.Add(ScriptVerb(["findexits"], """
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

        repo.Add(genFrandsPlayerClass);
    }
}
