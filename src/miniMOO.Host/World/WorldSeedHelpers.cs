using miniMOO.Core.Things;
using miniMOO.Data.FileSystem;
using miniMOO.Engine.Repositories;

namespace miniMOO.Host.World;

public static partial class WorldSeeder {
    private static string _worldDataRoot = Path.Combine(AppContext.BaseDirectory, "data");

    private static void SetWorldDataRoot(string? dataRootPath) {
        _worldDataRoot = string.IsNullOrWhiteSpace(dataRootPath)
            ? Path.Combine(AppContext.BaseDirectory, "data")
            : Path.GetFullPath(dataRootPath);
    }

    private static string WorldDataPath(params string[] parts)
        => Path.Combine([_worldDataRoot, .. parts]);

    private static void AddFileObjects(InMemoryObjectRepository repo, params string[] path) {
        var loader = new FileWorldLoader();
        var materializer = new FileObjectMaterializer();

        foreach (var definition in loader.LoadDirectory(WorldDataPath(path)).Objects)
            repo.Add(materializer.ToMooObject(definition));
    }

    private static MooVerb ScriptVerb(string[] names, string code, VerbObjectSpec dobj = VerbObjectSpec.None,
            string prep = "none", VerbObjectSpec iobj = VerbObjectSpec.None) {

        var verb = new MooVerb {
            OwnerId = ObjectId.System,
            DirectObject = dobj,
            Preposition = prep,
            IndirectObject = iobj,
            ImplementationKind = VerbImplementationKind.Script,
            Code = code
        };

        foreach (var name in names)
            verb.Names.Add(name);

        return verb;
    }
}
