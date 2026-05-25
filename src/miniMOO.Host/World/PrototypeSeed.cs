using miniMOO.Core.Things;
using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static void AddPrototypes(InMemoryObjectRepository repo) {
        AddRoot(repo);
        AddLastHuhVerbs(repo);
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
        AddDisplayOptionsPrototype(repo);
        AddBuildOptionsPrototype(repo);
        AddProgrammerOptionsPrototype(repo);
        AddMailPlayer(repo);
        AddFrandsPlayerClass(repo);
        AddGenericEditor(repo);
        AddVerbEditor(repo);
        AddQuotaUtils(repo);
    }

    private static void AddRoot(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0001-root.moo.md");
    }

    private static void AddLastHuhVerbs(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0011-last-huh-verbs.moo.md");
    }

    private static void AddRoom(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0003-room.moo.md");
    }

    private static void AddExitPrototype(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0007-exit.moo.md");
    }

    private static void AddThing(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0005-thing.moo.md");
    }

    private static void AddPlayerPrototype(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0006-player.moo.md");
    }

    private static void AddBuilder(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0004-builder.moo.md");
    }

    private static void AddContainer(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0008-container.moo.md");
    }

    private static void AddNote(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0009-note.moo.md");
    }

    private static void AddProgrammer(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0058-prog.moo.md");
    }

    private static void AddWizardPrototype(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0057-wiz.moo.md");
    }

    private static void AddGenericOptionsPrototype(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0068-generic-options.moo.md");
    }

    private static void AddBuildOptionsPrototype(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0077-build-options.moo.md");
    }

    private static void AddProgrammerOptionsPrototype(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0076-prog-options.moo.md");
    }

    private static void AddDisplayOptionsPrototype(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0067-display-options.moo.md");
    }

    private static void AddMailPlayer(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0040-mail-player.moo.md");
    }

    private static void AddFrandsPlayerClass(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0088-frands-player-class.moo.md");
    }

    private static void AddGenericEditor(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0050-generic-editor.moo.md");
    }

    private static void AddVerbEditor(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0049-verb-editor.moo.md");
    }

    private static void AddQuotaUtils(InMemoryObjectRepository repo) {
        AddFileObject(repo, "core", "0079-quota-utils.moo.md");
    }
}
