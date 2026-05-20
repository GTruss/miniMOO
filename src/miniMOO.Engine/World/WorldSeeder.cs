using miniMOO.Engine.Repositories;
using miniMOO.Engine.Things;

namespace miniMOO.Engine.World;

/// <summary>
/// Builds the initial miniMOO world: prototype classes + a small navigable area.
///
/// Object ID allocation:
///   #0  - System (reserved)
///   #1  - $root       — base ancestor of all objects
///   #2  - $room       — generic room prototype
///   #3  - $exit       — generic exit prototype
///   #4  - $thing      — generic pick-up-able object prototype
///   #5  - $player     — generic player prototype
///   #10 - The Void    — $player_start, where new players appear
///   #11 - The Foyer   — first real room
///   #12 - The Library — second room
///   #13 - exit: Foyer -> Library (east)
///   #14 - exit: Library -> Foyer (west)
///   #20 - a worn book (thing in the Library)
///   #100- Wizard      — the default player
///   #101- a gnarled staff (in Wizard's inventory)
/// </summary>
public static class WorldSeeder {
    public static readonly ObjectId RootId       = new(1);
    public static readonly ObjectId GenRoomId    = new(2);
    public static readonly ObjectId GenExitId    = new(3);
    public static readonly ObjectId GenThingId   = new(4);
    public static readonly ObjectId GenPlayerId  = new(5);
    public static readonly ObjectId PlayerStartId = new(10);
    public static readonly ObjectId FoyerId      = new(11);
    public static readonly ObjectId LibraryId    = new(12);
    public static readonly ObjectId WizardId     = new(100);

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
        // $root — every object inherits from this
        var root = Obj(RootId, "#0", null, null, "$root");
        root.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        repo.Add(root);

        // $room
        var genRoom = Obj(GenRoomId, "#0", RootId, null, "$room");
        genRoom.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        repo.Add(genRoom);

        // $exit
        var genExit = Obj(GenExitId, "#0", RootId, null, "$exit");
        genExit.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        repo.Add(genExit);

        // $thing
        var genThing = Obj(GenThingId, "#0", RootId, null, "$thing");
        genThing.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        repo.Add(genThing);

        // $player
        var genPlayer = Obj(GenPlayerId, "#0", RootId, null, "$player");
        genPlayer.Flags = ObjectFlags.Readable | ObjectFlags.Fertile;
        AddPlayerVerbs(genPlayer);
        repo.Add(genPlayer);
    }

    // -------------------------------------------------------------------------
    // World rooms, exits, objects
    // -------------------------------------------------------------------------

    private static void AddWorld(InMemoryObjectRepository repo) {
        // The Void — $player_start, where players land before being placed
        var theVoid = Obj(PlayerStartId, "#0", GenRoomId, null, "The Void");
        Prop(theVoid, "description",
            "A featureless expanse. You feel like you haven't quite arrived yet.");
        repo.Add(theVoid);

        // The Foyer
        var foyer = Obj(FoyerId, "#0", GenRoomId, null, "The Foyer");
        Prop(foyer, "description",
            "A modest entry hall. Pale light filters through frosted glass. " +
            "A corridor leads east toward the library.");
        repo.Add(foyer);

        // The Library
        var library = Obj(LibraryId, "#0", GenRoomId, null, "The Library");
        Prop(library, "description",
            "Tall shelves line the walls, filled with dusty volumes. " +
            "The foyer lies to the west.");
        repo.Add(library);

        // Exit: Foyer -> Library (east)
        var exitEast = Exit(new ObjectId(13), FoyerId, LibraryId, "east", "e");
        repo.Add(exitEast);

        // Exit: Library -> Foyer (west)
        var exitWest = Exit(new ObjectId(14), LibraryId, FoyerId, "west", "w");
        repo.Add(exitWest);

        // A worn book sitting in the library
        var book = Obj(new ObjectId(20), "#0", GenThingId, LibraryId, "a worn book");
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
        var wizard = Obj(WizardId, "#100", GenPlayerId, FoyerId, "Wizard");
        wizard.Flags = ObjectFlags.User | ObjectFlags.Programmer | ObjectFlags.Wizard;
        Prop(wizard, "description", "The all-powerful wizard of miniMOO.");
        repo.Add(wizard);

        var staff = Obj(new ObjectId(101), "#100", GenThingId, WizardId, "a gnarled staff");
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
            Names = { "look", "l" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.Any,
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Builtin,
            Implementation = "look"
        });

        player.Verbs.Add(new MooVerb {
            Names = { "say", "\"" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.Any,
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                tell player You say, "{argstr}"
                announce room {player} says, "{argstr}"
                """
        });

        player.Verbs.Add(new MooVerb {
            Names = { "emote", ":" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.Any,
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                tell player {player} {argstr}
                announce room {player} {argstr}
                """
        });

        player.Verbs.Add(new MooVerb {
            Names = { "emote_nospace", "::" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.Any,
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                tell player {player}{argstr}
                announce room {player}{argstr}
                """
        });

        player.Verbs.Add(new MooVerb {
            Names = { "wave" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                tell player You wave.
                announce room {player} waves.
                """
        });

        player.Verbs.Add(new MooVerb {
            Names = { "list inventory", "i" },
            OwnerId = ObjectId.System,
            DirectObject = VerbObjectSpec.None,
            IndirectObject = VerbObjectSpec.None,
            ImplementationKind = VerbImplementationKind.Script,
            Implementation = """
                list inventory
                """
        });
    }

    // -------------------------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------------------------

    private static MooObject Obj(
        ObjectId id, string ownerStr, ObjectId? parentId,
        ObjectId? locationId, string name) {

        var ownerId = ownerStr == "#0" ? ObjectId.System : id;

        return new MooObject {
            Id = id,
            OwnerId = ownerId,
            ParentId = parentId,
            LocationId = locationId,
            Name = name,
            Flags = ObjectFlags.Readable
        };
    }

    private static MooObject Exit(
        ObjectId id, ObjectId sourceId, ObjectId destId,
        string primaryName, string alias) {

        var exit = Obj(id, "#0", GenExitId, sourceId, primaryName);
        exit.Aliases.Add(alias);

        Prop(exit, "source", new MooValue.Object(sourceId));
        Prop(exit, "destination", new MooValue.Object(destId));
        Prop(exit, "obvious", new MooValue.Integer(1));

        exit.Verbs.Add(new MooVerb {
            Names = { "go", primaryName, alias },
            OwnerId = ObjectId.System,
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
