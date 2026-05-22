using miniMOO.Engine.Repositories;
using miniMOO.Core.Things;

namespace miniMOO.Host.World;

/// <summary>
/// Builds the initial miniMOO world: prototype classes + a small navigable area.
///
/// Object ID allocation:
///   #0  - System      - reserved
///   #1  - $root       - base ancestor of all objects
///   #2  - Wizard      - the default player (parent: $wiz)
///   #3  - $room       - generic room prototype (parent: $root)
///   #4  - $builder    - generic builder prototype (parent: $player)
///   #5  - $thing      - generic pick-up-able object prototype (parent: $root)
///   #6  - $player     - generic player prototype (parent: $root)
///   #7  - $exit       - generic exit prototype (parent: $root)
///   #8  - $container  - generic container prototype (parent: $thing)
///   #9  - $note       - generic note prototype (parent: $thing)
///   #20 - $string_utils   - string manipulation utility object
///   #21 - $building_utils - building utility object (make_exit, set_names, parse_names)
///   #52 - $object_utils   - object introspection utility object
///   #57 - $wiz        - generic wizard prototype (parent: $prog)
///   #58 - $prog       - generic programmer prototype (parent: $builder)
///   #10  - The Void    - $player_start, where new players appear
///   #101 - The Foyer   - first real room
///   #102 - The Library - second room
///   #103 - a gnarled staff (in Wizard's inventory)
///   #104 - exit: Foyer -> Library (east)
///   #105 - exit: Library -> Foyer (west)
///   #106 - a worn book (thing in the Library)
/// </summary>
public static partial class WorldSeeder {
    public static readonly ObjectId RootId = WorldIds.Root;
    public static readonly ObjectId WizardId = WorldIds.Wizard;
    public static readonly ObjectId GenRoomId = WorldIds.Room;
    public static readonly ObjectId GenBuilderId = WorldIds.Builder;
    public static readonly ObjectId GenThingId = WorldIds.Thing;
    public static readonly ObjectId GenPlayerId = WorldIds.Player;
    public static readonly ObjectId GenExitId = WorldIds.Exit;
    public static readonly ObjectId GenContainerId = WorldIds.Container;
    public static readonly ObjectId GenNoteId = WorldIds.Note;
    public static readonly ObjectId PlayerStartId = WorldIds.PlayerStart;
    public static readonly ObjectId GenStringUtilsId   = WorldIds.StringUtils;
    public static readonly ObjectId GenBuildingUtilsId = WorldIds.BuildingUtils;
    public static readonly ObjectId GenObjectUtilsId   = WorldIds.ObjectUtils;
    public static readonly ObjectId GenWizId = WorldIds.Wiz;
    public static readonly ObjectId GenProgId = WorldIds.Prog;
    public static readonly ObjectId GenMailPlayerId = WorldIds.MailPlayer;
    public static readonly ObjectId GenFrandsPlayerClassId = WorldIds.FrandsPlayerClass;

    public static readonly ObjectId FoyerId = WorldIds.Foyer;
    public static readonly ObjectId LibraryId = WorldIds.Library;

    public static IObjectRepository Seed() {
        var repo = new InMemoryObjectRepository();

        AddSystemObject(repo);
        AddPrototypes(repo);
        AddUtilityObjects(repo);
        AddStarterWorld(repo);
        AddStarterPlayer(repo);
        AddUnitTests(repo);

        return repo;
    }
}
