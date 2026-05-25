
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

    public string Code { get; set; } = "";

    public bool HasFlag(VerbFlags flag)
        => (Flags & flag) != 0;

    public bool MatchesName(string verb)
        => Names.Any(name => VerbNameMatches(name, verb));

    private static bool VerbNameMatches(string pattern, string verb) {
        if (string.Equals(pattern, verb, StringComparison.OrdinalIgnoreCase))
            return true;

        var star = pattern.IndexOf('*');

        if (star < 0)
            return false;

        var minimumPrefix = pattern[..star];

        if (verb.Length < minimumPrefix.Length)
            return false;

        if (!verb.StartsWith(minimumPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        if (star == pattern.Length - 1)
            return true;

        var fullName = pattern.Remove(star, 1);

        return fullName.StartsWith(verb, StringComparison.OrdinalIgnoreCase);
    }
}
