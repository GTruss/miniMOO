using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddStarterWorld(InMemoryObjectRepository repo) {
        AddFileObjects(repo, "world");
    }
}
