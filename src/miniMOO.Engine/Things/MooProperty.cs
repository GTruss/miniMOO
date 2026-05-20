
namespace miniMOO.Engine.Things;

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

    public static readonly MooValue NothingValue = new Nothing();
    public static readonly MooValue ClearValue = new Clear();
}