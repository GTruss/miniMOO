using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddStarterWorld(InMemoryObjectRepository repo) {
        AddFileObject(repo, "world", "0010-player-start.moo.md");
        AddFileObject(repo, "world", "0101-foyer.moo.md");
        AddFileObject(repo, "world", "0102-library.moo.md");
        AddFileObject(repo, "world", "0104-exit-east.moo.md");
        AddFileObject(repo, "world", "0105-exit-west.moo.md");
        AddFileObject(repo, "world", "0106-worn-book.moo.md");
    }
}
