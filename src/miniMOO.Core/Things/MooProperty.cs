
using miniMOO.Core.ScriptRuntime;

namespace miniMOO.Core.Things;

public sealed class MooProperty {
    public required string Name { get; init; }

    public required MooValue Value { get; set; }

    public ObjectId OwnerId { get; set; }

    public PropertyFlags Flags { get; set; } =
        PropertyFlags.Readable;

    public bool HasFlag(PropertyFlags flag)
        => (Flags & flag) != 0;
}

public abstract record MooValue {
    public sealed record Nothing : MooValue {
        public override string ToString() => "";
    }
    public sealed record Clear : MooValue {
        public override string ToString() => "";
    }

    public sealed record Integer(long Value) : MooValue {
        public override string ToString() => Value.ToString();
    }

    public sealed record Float(double Value) : MooValue {
        public override string ToString() => Value.ToString();
    }

    public sealed record String(string Value) : MooValue {
        public override string ToString() => Value;
    }

    public sealed record Object(ObjectId Value) : MooValue {
        public override string ToString() => Value.ToString();
    }

    public sealed record List(IReadOnlyList<MooValue> Items) : MooValue {
        public override string ToString()
            => "{" + string.Join(", ", Items.Select(item => item.ToString())) + "}";
    }

    public sealed record Error(int Code) : MooValue {
        public override string ToString() => MooErrorName(Code);
    }

    private static string MooErrorName(int code) => code switch {
        MooErrorCode.E_NONE => "E_NONE",
        MooErrorCode.E_TYPE => "E_TYPE",
        MooErrorCode.E_DIV => "E_DIV",
        MooErrorCode.E_PERM => "E_PERM",
        MooErrorCode.E_PROPNF => "E_PROPNF",
        MooErrorCode.E_VERBNF => "E_VERBNF",
        MooErrorCode.E_VARNF => "E_VARNF",
        MooErrorCode.E_INVIND => "E_INVIND",
        MooErrorCode.E_RECMOVE => "E_RECMOVE",
        MooErrorCode.E_MAXREC => "E_MAXREC",
        MooErrorCode.E_RANGE => "E_RANGE",
        MooErrorCode.E_ARGS => "E_ARGS",
        MooErrorCode.E_NACC => "E_NACC",
        MooErrorCode.E_INVARG => "E_INVARG",
        MooErrorCode.E_QUOTA => "E_QUOTA",
        MooErrorCode.E_FLOAT => "E_FLOAT",
        _ => $"E_UNKNOWN({code})"
    };

    public static readonly MooValue NothingValue = new Nothing();
    public static readonly MooValue ClearValue = new Clear();
}