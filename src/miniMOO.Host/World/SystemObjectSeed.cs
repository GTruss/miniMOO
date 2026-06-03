using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddSystemObject(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0000-system-object.moo.md");
    }
}
