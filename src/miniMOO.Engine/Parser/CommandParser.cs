using miniMOO.Core.Things;

namespace miniMOO.Engine.Parser;

public sealed class CommandParser {
    private static readonly string[] Prepositions =
    [
        "in",
        "on",
        "at",
        "to",
        "from",
        "with",
        "using",
        "into",
        "through",
        "named",
        "called"
    ];

    private readonly ObjectMatcher _matcher;

    public CommandParser(ObjectMatcher matcher) {
        _matcher = matcher;
    }

    public ParsedCommand Parse(ObjectId playerId, string input) {
        var raw = input;
        input = input.Trim();

        input = ExpandShortcut(input);

        var words = SplitWords(input);

        if (words.Count == 0) {
            return new ParsedCommand {
                Raw = raw,
                Verb = "",
                ArgumentText = "",
                Arguments = new List<string>(),
                DirectObjectText = "",
                DirectObject = null,
                Preposition = "",
                IndirectObjectText = "",
                IndirectObject = null
            };
        }

        var verb = words[0];
        var args = words.Skip(1).ToList();
        var argumentText = string.Join(" ", args);

        var prepIndex = FindPreposition(args);

        string directText;
        string prep;
        string indirectText;

        if (prepIndex >= 0) {
            directText = string.Join(" ", args.Take(prepIndex));
            prep = args[prepIndex];
            indirectText = string.Join(" ", args.Skip(prepIndex + 1));
        }
        else {
            directText = argumentText;
            prep = "";
            indirectText = "";
        }

        return new ParsedCommand {
            Raw = raw,
            Verb = verb,
            ArgumentText = argumentText,
            Arguments = args,
            DirectObjectText = directText,
            DirectObject = _matcher.Match(playerId, directText),
            Preposition = prep,
            IndirectObjectText = indirectText,
            IndirectObject = _matcher.Match(playerId, indirectText)
        };
    }

    private static string ExpandShortcut(string input) {
        if (input.StartsWith("\""))
            return "say " + input[1..];

        if (input.StartsWith("::"))
            return "emote_nospace " + input[2..];

        if (input.StartsWith(":"))
            return "emote " + input[1..];

        if (input.StartsWith(";"))
            return "eval ;" + input[1..];

        return input;
    }

    private static int FindPreposition(IReadOnlyList<string> words) {
        for (var i = 0; i < words.Count; i++) {
            if (Prepositions.Contains(words[i], StringComparer.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static List<string> SplitWords(string input) {
        return input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
