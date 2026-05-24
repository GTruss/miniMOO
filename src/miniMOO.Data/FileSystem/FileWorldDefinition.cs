using miniMOO.Core.Things;

namespace miniMOO.Data.FileSystem;

public sealed class FileWorldDefinition {
    public List<FileObjectDefinition> Objects { get; } = [];
}

public sealed class FileObjectDefinition {
    public required ObjectId Id { get; init; }
    public ObjectId? ParentId { get; init; }
    public ObjectId? LocationId { get; init; }
    public required ObjectId OwnerId { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<string> Aliases { get; init; } = [];
    public ObjectFlags Flags { get; init; } = ObjectFlags.None;
    public IReadOnlyList<FilePropertyDefinition> Properties { get; init; } = [];
    public IReadOnlyList<FileVerbDefinition> Verbs { get; init; } = [];
}

public sealed class FilePropertyDefinition {
    public required string Name { get; init; }
    public required FileMooValueDefinition Value { get; init; }
    public ObjectId? OwnerId { get; init; }
    public PropertyFlags Flags { get; init; } = PropertyFlags.Readable;
}

public sealed class FileVerbDefinition {
    public required IReadOnlyList<string> Names { get; init; }
    public ObjectId? OwnerId { get; init; }
    public VerbFlags Flags { get; init; } =
        VerbFlags.Readable | VerbFlags.Executable;

    public VerbObjectSpec DirectObject { get; init; } = VerbObjectSpec.None;
    public string Preposition { get; init; } = "none";
    public VerbObjectSpec IndirectObject { get; init; } = VerbObjectSpec.None;
    public VerbImplementationKind ImplementationKind { get; init; } =
        VerbImplementationKind.Script;

    public required string Code { get; init; }
}

public sealed class FileMooValueDefinition {
    public required string Type { get; init; }
    public object? Value { get; init; }
}
