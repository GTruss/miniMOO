namespace miniMOO.Engine.Things;

public readonly record struct ObjectId(int Value) {
    public static readonly ObjectId System = new(0);
    public static readonly ObjectId Nothing = new(-1);

    public bool IsNothing => Value == -1;

    public override string ToString() => Value == -1 ? "#-1" : $"#{Value}";
}

