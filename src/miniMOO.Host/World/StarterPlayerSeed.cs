using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddStarterPlayer(InMemoryObjectRepository repo) {
        AddFileObject(repo, "world", "0002-wizard.moo.md");
        AddFileObject(repo, "world", "0103-gnarled-staff.moo.md");
    }
}
