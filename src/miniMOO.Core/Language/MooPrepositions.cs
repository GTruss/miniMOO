namespace miniMOO.Core.Language;

public static class MooPrepositions {
    public static readonly string[] Display =
    [
        "with/using",
        "at/to",
        "in front of",
        "in/inside/into",
        "on top of/on/onto/upon",
        "out of/from inside/from",
        "over",
        "through",
        "under/underneath/beneath",
        "behind",
        "beside",
        "for/about",
        "is",
        "as",
        "off/off of"
    ];

    public static readonly string[] Short =
    [
        "with",
        "to",
        "in front of",
        "in",
        "on",
        "from",
        "over",
        "through",
        "under",
        "behind",
        "beside",
        "for",
        "is",
        "as",
        "off"
    ];

    public static readonly string[] Other =
    [
        "using",
        "at",
        "inside",
        "into",
        "on top of",
        "onto",
        "upon",
        "out of",
        "from inside",
        "underneath",
        "beneath",
        "about",
        "off of"
    ];
    public static readonly string[] Multi =
    [
        "off", 
        "from", 
        "out", 
        "on", 
        "on top", 
        "in", 
        "in front"
    ];

    public static readonly int[] OtherIndexes =
    [
        1,
        2,
        4,
        4,
        5,
        5,
        5,
        6,
        6,
        9,
        9,
        12,
        15
    ];

    public static IReadOnlyList<MooPreposition> ParserPrepositions { get; } =
        BuildParserPrepositions();

    private static IReadOnlyList<MooPreposition> BuildParserPrepositions() {
        var prepositions = Short
            .Select(preposition => new MooPreposition(preposition, preposition))
            .ToList();

        for (var i = 0; i < Other.Length; i++)
            prepositions.Add(new MooPreposition(Other[i], Short[OtherIndexes[i] - 1]));

        return prepositions
            .OrderByDescending(preposition => preposition.Text.Split(' ').Length)
            .ThenByDescending(preposition => preposition.Text.Length)
            .ToArray();
    }
}

public sealed record MooPreposition(string Text, string Canonical);
