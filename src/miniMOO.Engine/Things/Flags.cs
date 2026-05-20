namespace miniMOO.Engine.Things;

[Flags]
public enum ObjectFlags {
    None = 0,
    User = 1 << 0,
    Programmer = 1 << 1,
    Wizard = 1 << 2,

    Readable = 1 << 4,
    Writable = 1 << 5,
    Fertile = 1 << 7
}

[Flags]
public enum PropertyFlags {
    None = 0,
    Readable = 1 << 0,
    Writable = 1 << 1,
    Chown = 1 << 2
}

[Flags]
public enum VerbFlags {
    None = 0,
    Readable = 1 << 0,
    Writable = 1 << 1,
    Executable = 1 << 2,
    Debug = 1 << 3
}

public enum VerbObjectSpec {
    None,
    Any,
    This
}

public enum VerbImplementationKind {
    Builtin,
    Script
}

public enum VerbResultKind {
    Success,
    Failure
}

public enum MatchResultKind {
    None,
    Found,
    NotFound,
    Ambiguous
}