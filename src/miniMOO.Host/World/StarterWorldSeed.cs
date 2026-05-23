using miniMOO.Core.Things;
using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddStarterWorld(InMemoryObjectRepository repo) {
        var theVoid = Obj(WorldIds.PlayerStart, ObjectId.System, WorldIds.Room, null, "The Void");
        Prop(theVoid, "description",
            "A featureless expanse. You feel like you haven't quite arrived yet.");
        repo.Add(theVoid);

        var foyer = Obj(WorldIds.Foyer, ObjectId.System, WorldIds.Room, null, "The Foyer");
        Prop(foyer, "description",
            "A modest entry hall. Pale light filters through frosted glass. " +
            "A corridor leads east toward the library,\nand ornate double doors on the north side of the room " +
            "open to the grand ballroom");

        var library = Obj(WorldIds.Library, ObjectId.System, WorldIds.Room, null, "The Library");
        Prop(library, "description",
            "Tall shelves line the walls, filled with dusty volumes. " +
            "The foyer lies to the west.");

        var exitEast = Exit(WorldIds.ExitEast, WorldIds.Foyer, WorldIds.Library, "east", "e");
        Prop(exitEast, "description", "The world shimmers around you.");
        //Prop(exitEast, "obvious", new MooValue.Integer(0));
        repo.Add(exitEast);

        var exitWest = Exit(WorldIds.ExitWest, WorldIds.Library, WorldIds.Foyer, "west", "w");
        Prop(exitWest, "description", "The world shimmers around you.");
        repo.Add(exitWest);

        Prop(foyer, "exits", ObjList(WorldIds.ExitEast));
        Prop(foyer, "entrances", ObjList(WorldIds.ExitWest));

        Prop(library, "exits", ObjList(WorldIds.ExitWest));
        Prop(library, "entrances", ObjList(WorldIds.ExitEast)); 
        repo.Add(foyer);
        repo.Add(library);

        var book = Obj(WorldIds.WornBook, ObjectId.System, WorldIds.Thing, WorldIds.Library, "a worn book");
        book.Aliases.Add("book");
        book.Aliases.Add("worn book");
        Prop(book, "description",
            "The cover is cracked and the pages yellowed, but the text inside " +
            "is still legible. It appears to be a manual of some kind.");
        repo.Add(book);
    }

    private static MooObject Exit(ObjectId id, ObjectId sourceId, ObjectId destId, string primaryName, string alias) {
        var exit = Obj(id, ObjectId.System, WorldIds.Exit, sourceId, primaryName);
        exit.Aliases.Add(primaryName);
        exit.Aliases.Add(alias);

        Prop(exit, "source", new MooValue.Object(sourceId));
        Prop(exit, "destination", new MooValue.Object(destId));

        exit.Verbs.Add(ScriptVerb([primaryName, alias], """
            this:invoke();
        """));

        return exit;
    }
}
