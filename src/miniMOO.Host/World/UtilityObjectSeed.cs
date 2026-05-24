using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddUtilityObjects(InMemoryObjectRepository repo) {
        AddGenericUtilitiesPackage(repo);
        AddStringUtils(repo);
        AddBuildingUtils(repo);
        AddGenderUtils(repo);
        AddObjectUtils(repo);
        AddListUtils(repo);
        AddSeqUtils(repo);
        AddCodeUtils(repo);
        AddCommandUtils(repo);
    }

    private static void AddGenericUtilitiesPackage(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0078-generic-utils.moo.md");
    }

    private static void AddStringUtils(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0020-string-utils.moo.md");
    }

    private static void AddBuildingUtils(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0021-building-utils.moo.md");
    }

    private static void AddGenderUtils(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0041-gender-utils.moo.md");
    }

    private static void AddObjectUtils(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0052-object-utils.moo.md");
    }

    private static void AddListUtils(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0055-list-utils.moo.md");
    }

    private static void AddSeqUtils(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0033-seq-utils.moo.md");
    }

    private static void AddCodeUtils(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0059-code-utils.moo.md");
    }

    private static void AddCommandUtils(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0056-command-utils.moo.md");
    }
}
