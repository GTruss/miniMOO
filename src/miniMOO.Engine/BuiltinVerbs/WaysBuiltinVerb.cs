using miniMOO.Core.Things;
using miniMOO.Engine.Verbs;

namespace miniMOO.Engine.BuiltinVerbs;

public sealed class WaysBuiltinVerb : IBuiltinVerb {
    public string Name => "ways";

    public Task<VerbResult> ExecuteAsync(VerbContext context) {
        var player = context.Objects.Get(context.PlayerId);

        if (player is null)
            return Fail(context, "You don't exist.");

        if (player.LocationId is not { } roomId)
            return Fail(context, "You are nowhere.");

        var room = context.Objects.Get(roomId);

        if (room is null)
            return Fail(context, "You are nowhere.");

        var exits = context.Objects.ContentsOf(room.Id)
            .Where(obj => IsObviousExit(context, obj))
            .Select(FormatExit)
            .ToList();

        if (exits.Count == 0) {
            context.Output.Notify(context.PlayerId, "There are no obvious exits.");
            return Task.FromResult(VerbResult.Success());
        }

        context.Output.Notify(
            context.PlayerId,
            "Obvious exits: " + ToEnglishList(exits) + ".");

        return Task.FromResult(VerbResult.Success());
    }

    private static bool IsObviousExit(VerbContext context, MooObject obj)
        => context.Resolver.FindPropertyValue(obj.Id, "destination") is MooValue.Object
        && context.Resolver.FindPropertyValue(obj.Id, "obvious") is MooValue.Integer i
        && i.Value != 0;

    private static string FormatExit(MooObject exit) {
        var names = new List<string> { exit.Name };
        names.AddRange(exit.Aliases);

        var distinctNames = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctNames.Count <= 1)
            return exit.Name;

        return $"{exit.Name} ({ToEnglishList(distinctNames)})";
    }

    private static string ToEnglishList(IReadOnlyList<string> items) {
        return items.Count switch {
            0 => "",
            1 => items[0],
            2 => $"{items[0]} and {items[1]}",
            _ => string.Join(", ", items.Take(items.Count - 1)) + ", and " + items[^1]
        };
    }

    private static Task<VerbResult> Fail(VerbContext context, string message) {
        context.Output.Notify(context.PlayerId, message);
        return Task.FromResult(VerbResult.Failure(message));
    }
}