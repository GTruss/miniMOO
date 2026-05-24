using miniMOO.Core.Things;

using miniMOO.Core.Language;

namespace miniMOO.Engine.Parser;

public sealed class CommandParser {
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

        var prepMatch = FindPreposition(args);

        string directText;
        string prep;
        string indirectText;

        if (prepMatch.Index >= 0) {
            directText = string.Join(" ", args.Take(prepMatch.Index));
            prep = prepMatch.Text;
            indirectText = string.Join(" ", args.Skip(prepMatch.Index + prepMatch.Length));
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

    private static (int Index, int Length, string Text) FindPreposition(IReadOnlyList<string> words) {
        for (var i = 0; i < words.Count; i++) {
            foreach (var preposition in MooPrepositions.ParserPrepositions) {
                var prepWords = preposition.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (i + prepWords.Length > words.Count)
                    continue;

                var matches = true;

                for (var j = 0; j < prepWords.Length; j++) {
                    if (string.Equals(words[i + j], prepWords[j], StringComparison.OrdinalIgnoreCase))
                        continue;

                    matches = false;
                    break;
                }

                if (matches)
                    return (i, prepWords.Length, preposition.Canonical);
            }
        }

        return (-1, 0, "");
    }

    private static List<string> SplitWords(string input) {
        return input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
