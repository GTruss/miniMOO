namespace miniMOO.Core.Things;

public sealed class MooObject {
    public ObjectId Id { get; init; }
    public ObjectId? ParentId { get; set; }
    public ObjectId? LocationId { get; set; }
    public ObjectId OwnerId { get; set; }

    public string Name { get; set; } = "";
    public List<string> Aliases { get; } = [];

    public ObjectFlags Flags { get; set; } = ObjectFlags.None;

    public Dictionary<string, MooProperty> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<MooVerb> Verbs { get; } = new();

    public bool HasFlag(ObjectFlags flag) => (Flags & flag) != 0;

    public IEnumerable<string> MatchNames() {
        yield return Name;

        foreach (var alias in Aliases)
            yield return alias;
    }

    public override string ToString()
        => $"{Id} ({Name})";
}
