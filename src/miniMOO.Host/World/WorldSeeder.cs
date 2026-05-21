using miniMOO.Core.Things;
using miniMOO.Engine.Repositories;

using System.Numerics;

namespace miniMOO.Host.World;

/// <summary>
/// Builds the initial miniMOO world: prototype classes + a small navigable area.
///
/// Object ID allocation:
///   #0  - System      — reserved
///   #1  - $root       — base ancestor of all objects
///   #2  - Wizard      — the default player (parent: $wiz)
///   #3  - $room       — generic room prototype (parent: $root)
///   #4  - $builder    — generic builder prototype (parent: $player)
///   #5  - $thing      — generic pick-up-able object prototype (parent: $root)
///   #6  - $player     — generic player prototype (parent: $root)
///   #7  - $exit       — generic exit prototype (parent: $root)
///   #8  - $container  — generic container prototype (parent: $thing)
///   #9  - $note       — generic note prototype (parent: $thing)
///   #57 - $wiz        — generic wizard prototype (parent: $prog)
///   #58 - $prog       — generic programmer prototype (parent: $builder)
///   #10 - The Void    — $player_start, where new players appear
///   #11 - The Foyer   — first real room
///   #12 - The Library — second room
///   #13 - exit: Foyer -> Library (east)
///   #14 - exit: Library -> Foyer (west)
///   #20 - a worn book (thing in the Library)
///   #101- a gnarled staff (in Wizard's inventory)
/// </summary>
public static class WorldSeeder {
    public static readonly ObjectId RootId = new(1);
    public static readonly ObjectId WizardId = new(2);   
    public static readonly ObjectId GenRoomId = new(3);   
    public static readonly ObjectId GenBuilderId = new(4);   
    public static readonly ObjectId GenThingId = new(5);   
    public static readonly ObjectId GenPlayerId = new(6);   
    public static readonly ObjectId GenExitId = new(7);   
    public static readonly ObjectId GenContainerId = new(8);
    public static readonly ObjectId GenNoteId      = new(9);
    public static readonly ObjectId PlayerStartId  = new(10);
    public static readonly ObjectId GenObjectUtilsId = new(52);
    public static readonly ObjectId GenWizId       = new(57);
    public static readonly ObjectId GenProgId      = new(58);

    // Starter Rooms
    public static readonly ObjectId FoyerId = new(101);  
    public static readonly ObjectId LibraryId = new(102);

    public static IObjectRepository Seed() {
        var repo = new InMemoryObjectRepository();

        AddPrototypes(repo);
        AddWorld(repo);
        AddPlayer(repo);

        return repo;
    }

    // -------------------------------------------------------------------------
    // Prototype classes
    // -------------------------------------------------------------------------

    private static void AddPrototypes(InMemoryObjectRepository repo) {
        // #0 — The System Object — all $name properties live here
        var sysObj = Obj(ObjectId.System, ObjectId.System, null, null, "The System Object");
        Prop(sysObj, "root", new MooValue.Object(RootId));
        Prop(sysObj, "room", new MooValue.Object(GenRoomId));
        Prop(sysObj, "builder", new MooValue.Object(GenBuilderId));
        Prop(sysObj, "thing", new MooValue.Object(GenThingId));
        Prop(sysObj, "player", new MooValue.Object(GenPlayerId));
        Prop(sysObj, "exit", new MooValue.Object(GenExitId));
        Prop(sysObj, "container", new MooValue.Object(GenContainerId));
        Prop(sysObj, "note", new MooValue.Object(GenNoteId));
        Prop(sysObj, "player_start", new MooValue.Object(PlayerStartId));
        Prop(sysObj, "prog", new MooValue.Object(GenProgId));
        Prop(sysObj, "wiz", new MooValue.Object(GenWizId));
        Prop(sysObj, "object_utils", new MooValue.Object(GenObjectUtilsId));
        repo.Add(sysObj);

        // $root — every object inherits from this
        var root = Obj(RootId, ObjectId.System, null, null, "$root");
        root.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(root, "description", "You see nothing special.");
        root.Verbs.Add(new MooVerb {
            Names = { "tell" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.This,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.This,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                notify(this, tostr(@args));
            """
        });
        root.Verbs.Add(new MooVerb {
            Names = { "look_self" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                desc = this.description;
                if (desc)
                  player:tell(desc);
                else
                  player:tell("You see nothing special.");
                endif
            """
        });
        repo.Add(root);

        // $room
        var genRoom = Obj(GenRoomId, ObjectId.System, RootId, null, "$room");
        genRoom.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genRoom, "description", "An empty room.");
        genRoom.Verbs.Add(new MooVerb {
            Names = { "announce" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                notify(player, tostr(@args));
            """
        });

        genRoom.Verbs.Add(new MooVerb {
            Names = { "say", "\"" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.Any,
            Preposition = "any",
            IndirectObject = VerbObjectSpec.Any,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                player:tell("You say, \"", argstr, "\"");
                this:announce(player.name, " says, \"", argstr, "\"");
            """
        });

        genRoom.Verbs.Add(new MooVerb {
            Names = { "emote", ":" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.Any,
            Preposition = "any",
            IndirectObject = VerbObjectSpec.Any,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                this:announce(player.name, " ", argstr);
            """
        });

        genRoom.Verbs.Add(new MooVerb {
            Names = { "emote_nospace", "::" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.Any,
            Preposition = "any",
            IndirectObject = VerbObjectSpec.Any,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                this:announce(player.name, argstr);
            """
        });
        genRoom.Verbs.Add(new MooVerb {
            Names = { "look_self" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                player:tell(this.name);
                pass();
                this:tell_contents();
            """
        });

        genRoom.Verbs.Add(new MooVerb {
            Names = { "tell_contents" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
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
            """
        });

        var lookScript = """
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

        genRoom.Verbs.Add(new MooVerb {
            Names = { "look", "l" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.Any,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = lookScript
        });

        genRoom.Verbs.Add(new MooVerb {
            Names = { "look", "l" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "at",
            IndirectObject = VerbObjectSpec.Any,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = lookScript
        });
        repo.Add(genRoom);

        // $exit
        var genExit = Obj(GenExitId, ObjectId.System, RootId, null, "$exit");
        genExit.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genExit, "description", "You see an exit.");
        Prop(genExit, "obvious", new MooValue.Integer(1));
        repo.Add(genExit);

        // $thing
        var genThing = Obj(GenThingId, ObjectId.System, RootId, null, "$thing");
        genThing.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genThing, "description", "You see nothing special about it.");
        repo.Add(genThing);

        // $player
        var genPlayer = Obj(GenPlayerId, ObjectId.System, RootId, null, "$player");
        genPlayer.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genPlayer, "description", "A nondescript person.");
        AddPlayerVerbs(genPlayer);
        repo.Add(genPlayer);

        // $builder — inherits from $player; home of @create, @dig, etc.
        var genBuilder = Obj(GenBuilderId, ObjectId.System, GenPlayerId, null, "$builder");
        genBuilder.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        genBuilder.Verbs.Add(new MooVerb {
            Names = { "@create" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.Any,
            Preposition = "any",
            IndirectObject = VerbObjectSpec.Any,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                parent = dobj;
                if (!valid(parent))
                  player:tell("Usage: @create <parent> [named [name:]alias,alias,...]");
                  return;
                endif
                newobj = create(parent);
                move(newobj, player);
                if (iobjstr != "")
                  colon = index(iobjstr, ":");
                  if (colon)
                    display_name = substr(iobjstr, 1, colon - 1);
                    alias_str = substr(iobjstr, colon + 1);
                  else
                    first_comma = index(iobjstr, ",");
                    if (first_comma)
                      display_name = substr(iobjstr, 1, first_comma - 1);
                    else
                      display_name = iobjstr;
                    endif
                    alias_str = iobjstr;
                  endif 
                  set_name(newobj, display_name);
                  remainder = alias_str;
                  done = 0;
                  while (!done)
                    comma = index(remainder, ",");
                    if (comma)
                      add_alias(newobj, substr(remainder, 1, comma - 1));
                      remainder = substr(remainder, comma + 1);
                    else
                      add_alias(newobj, remainder);
                      done = 1;
                    endif
                  endwhile
                endif
                player:tell("You now have ", newobj.name, " with object number ", newobj, " and parent ", parent.name, " (", parent, ").");
            """
        });

        genBuilder.Verbs.Add(new MooVerb {
            Names = { "@dig" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                if (argstr == "")
                  player:tell("Usage: @dig <room name>");
                  return;
                endif
                newroom = create($room);
                set_name(newroom, argstr);
                player:tell("You dig ", newroom.name, " (", newroom, ").");
            """
        });
        repo.Add(genBuilder);

        // $container — inherits from $thing; can hold other objects
        var genContainer = Obj(GenContainerId, ObjectId.System, GenThingId, null, "$container");
        genContainer.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genContainer, "description", "A container.");
        repo.Add(genContainer);

        // $note — inherits from $thing; can be read
        var genNote = Obj(GenNoteId, ObjectId.System, GenThingId, null, "$note");
        genNote.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        Prop(genNote, "description", "A note.");
        repo.Add(genNote);

        // $prog — inherits from $builder; home of @parents, @examine, @list, etc.
        // $prog — inherits from $builder; home of @parents, @examine, @list, etc.
        var genProg = Obj(GenProgId, ObjectId.System, GenBuilderId, null, "$prog");
        genProg.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        genProg.Verbs.Add(new MooVerb {
            Names = { "@parents" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.Any,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                obj = dobj;
                if (!valid(obj))
                    player:tell("Usage: @parents <object>");
                    return;
                endif
                player:tell(tostr(obj.name, " (", obj, ")"));
                while (valid(obj = parent(obj)))
                    player:tell("  ", obj.name, " (", obj, ")");
                endwhile
            """
        });
        repo.Add(genProg);

        // $wiz — inherits from $prog; home of wizard-only commands
        var genWiz = Obj(GenWizId, ObjectId.System, GenProgId, null, "$wiz");
        genWiz.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        repo.Add(genWiz);

        // $object_utils (#52) — inheritance and location hierarchy utilities
        var genObjectUtils = Obj(GenObjectUtilsId, ObjectId.System, null, null, "$object_utils");
        genObjectUtils.Flags = ObjectFlags.Readable;

        genObjectUtils.Verbs.Add(new MooVerb {
            Names = { "ancestors" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                ret = {};
                for o in (args)
                  what = o;
                  while (valid(what = parent(what)))
                    ret = setadd(ret, what);
                  endwhile
                endfor
                return ret;
            """
        });

        genObjectUtils.Verbs.Add(new MooVerb {
            Names = { "isa" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                what = args[1];
                targ = args[2];
                while (valid(what))
                  if (what == targ)
                    return 1;
                  endif
                  what = parent(what);
                endwhile
                return 0;
            """
        });

        genObjectUtils.Verbs.Add(new MooVerb {
            Names = { "contains" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                loc = args[1];
                what = args[2];
                while (valid(what))
                  what = what.location;
                  if (what == loc)
                    return 1;
                  endif
                endwhile
                return 0;
            """
        });

        genObjectUtils.Verbs.Add(new MooVerb {
            Names = { "locations" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                ret = {};
                what = args[1];
                while (valid(what = what.location))
                  ret = {@ret, what};
                endwhile
                return ret;
            """
        });

        genObjectUtils.Verbs.Add(new MooVerb {
            Names = { "descendants", "descendents" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                r = children(args[1]);
                i = 1;
                while (i <= length(r))
                  kids = children(r[i]);
                  if (kids)
                    r = {@r, @kids};
                  endif
                  i = i + 1;
                endwhile
                return r;
            """
        });

        genObjectUtils.Verbs.Add(new MooVerb {
            Names = { "isoneof" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                what = args[1];
                targ = args[2];
                while (valid(what))
                  i = 1;
                  while (i <= length(targ))
                    if (what == targ[i])
                      return 1;
                    endif
                    i = i + 1;
                  endwhile
                  what = parent(what);
                endwhile
                return 0;
            """
        });

        repo.Add(genObjectUtils);
    }

    // -------------------------------------------------------------------------
    // World rooms, exits, objects
    // -------------------------------------------------------------------------

    private static void AddWorld(InMemoryObjectRepository repo) {
        // The Void — $player_start, where players land before being placed
        var theVoid = Obj(PlayerStartId, ObjectId.System, GenRoomId, null, "The Void");
        Prop(theVoid, "description",
            "A featureless expanse. You feel like you haven't quite arrived yet.");
        repo.Add(theVoid);

        // The Foyer
        var foyer = Obj(FoyerId, ObjectId.System, GenRoomId, null, "The Foyer");
        Prop(foyer, "description",
            "A modest entry hall. Pale light filters through frosted glass. " +
            "A corridor leads east toward the library.");
        repo.Add(foyer);

        // The Library
        var library = Obj(LibraryId, ObjectId.System, GenRoomId, null, "The Library");
        Prop(library, "description",
            "Tall shelves line the walls, filled with dusty volumes. " +
            "The foyer lies to the west.");
        repo.Add(library);

        // Exit: Foyer -> Library (east)
        var exitEast = Exit(new ObjectId(104), FoyerId, LibraryId, "east", "e");
        Prop(exitEast, "description",
            "The world shimmers around you.");
        repo.Add(exitEast);

        // Exit: Library -> Foyer (west)
        var exitWest = Exit(new ObjectId(105), LibraryId, FoyerId, "west", "w");
        Prop(exitWest, "description",
            "The world shimmers around you.");
        repo.Add(exitWest);

        // A worn book sitting in the library
        var book = Obj(new ObjectId(106), ObjectId.System, GenThingId, LibraryId, "a worn book");
        book.Aliases.Add("book");
        book.Aliases.Add("worn book");
        Prop(book, "description",
            "The cover is cracked and the pages yellowed, but the text inside " +
            "is still legible. It appears to be a manual of some kind.");
        repo.Add(book);
    }

    // -------------------------------------------------------------------------
    // The wizard player
    // -------------------------------------------------------------------------

    private static void AddPlayer(InMemoryObjectRepository repo) {
        var wizard = Obj(WizardId, WizardId, GenWizId, FoyerId, "Wizard");
        wizard.Flags = ObjectFlags.User | ObjectFlags.Programmer | ObjectFlags.Wizard;
        Prop(wizard, "description", "The all-powerful wizard of miniMOO.");
        Prop(wizard, "debug", new MooValue.Integer(0));
        repo.Add(wizard);

        var staff = Obj(new ObjectId(103), WizardId, GenThingId, WizardId, "a gnarled staff");
        staff.Aliases.Add("staff");
        staff.Aliases.Add("gnarled staff");
        Prop(staff, "description", "A twisted length of dark wood, warm to the touch.");
        repo.Add(staff);
    }

    // -------------------------------------------------------------------------
    // Verb definitions
    // -------------------------------------------------------------------------

    private static void AddPlayerVerbs(MooObject player) {
        player.Verbs.Add(new MooVerb {
            Names = { "@ways" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Builtin,
            Implementation = "ways"
        });
        player.Verbs.Add(new MooVerb {
            Names = { "wave" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                player:tell("You wave.");
                player.location:announce(player.name, " waves.");
            """
        });

        player.Verbs.Add(new MooVerb {
            Names = { "list inventory", "i" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                for obj in (player.contents)
                  player:tell(obj.name);
                endfor
            """
        });
    }

    // -------------------------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------------------------

    private static MooObject Obj(ObjectId id, ObjectId ownerId, ObjectId? parentId, ObjectId? locationId, string name) {
        return new MooObject {
            Id = id,
            OwnerId = ownerId,
            ParentId = parentId,
            LocationId = locationId,
            Name = name,
            Flags = ObjectFlags.Readable
        };
    }

    private static MooObject Exit(ObjectId id, ObjectId sourceId, ObjectId destId, string primaryName, string alias) {

        var exit = Obj(id, ObjectId.System, GenExitId, sourceId, primaryName);
        exit.Aliases.Add(alias);

        Prop(exit, "source", new MooValue.Object(sourceId));
        Prop(exit, "destination", new MooValue.Object(destId));

        exit.Verbs.Add(new MooVerb {
            Names = { "go", primaryName, alias },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            Preposition = "none",
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Builtin,
            Implementation = "go"
        });
        return exit;
    }

    private static void Prop(MooObject obj, string name, string value)
        => obj.Properties[name] = new MooProperty {
            Name = name,
            OwnerId = obj.OwnerId,
            Value = new MooValue.String(value)
        };

    private static void Prop(MooObject obj, string name, MooValue value)
        => obj.Properties[name] = new MooProperty {
            Name = name,
            OwnerId = obj.OwnerId,
            Value = value
        };
}
