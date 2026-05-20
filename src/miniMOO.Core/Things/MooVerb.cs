
namespace miniMOO.Core.Things;

public sealed class MooVerb {
    public List<string> Names { get; } = new();

    public ObjectId OwnerId { get; set; }
     
    public VerbFlags Flags { get; set; } =
        VerbFlags.Readable | VerbFlags.Executable;

    public VerbObjectSpec DirectObject { get; set; } = VerbObjectSpec.Any;

    public string Preposition { get; set; } = "any";

    public VerbObjectSpec IndirectObject { get; set; } = VerbObjectSpec.Any;
    
    public VerbImplementationKind ImplementationKind { get; set; }

    public string Implementation { get; set; } = "";

    public string Code { get; set; } = "";

    public bool HasFlag(VerbFlags flag)
        => (Flags & flag) != 0;

    public bool MatchesName(string verb)
        => Names.Any(name => string.Equals(name, verb, StringComparison.OrdinalIgnoreCase));
}
