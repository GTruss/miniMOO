using miniMOO.Engine.Repositories;
using miniMOO.Engine.Things;

namespace miniMOO.Engine.Parser;

public sealed class ObjectMatcher {
    private readonly IObjectRepository _objects;

    public ObjectMatcher(IObjectRepository objects) {
        _objects = objects;
    }

    public MatchResult Match(ObjectId playerId, string text) {
        text = text.Trim();

        if (text.Length == 0)
            return MatchResult.None();

        if (TryParseObjectId(text, out var explicitId))
            return _objects.Exists(explicitId)
                ? MatchResult.Found(explicitId)
                : MatchResult.NotFound();

        var player = _objects.Get(playerId);

        if (player is null)
            return MatchResult.NotFound();

        if (text.Equals("me", StringComparison.OrdinalIgnoreCase))
            return MatchResult.Found(playerId);

        if (text.Equals("here", StringComparison.OrdinalIgnoreCase))
            return player.LocationId is { } locationId
                ? MatchResult.Found(locationId)
                : MatchResult.NotFound();

        return MatchVisibleObjects(player, text);
    }

    private MatchResult MatchVisibleObjects(MooObject player, string text) {
        var candidates = new List<MooObject>();

        candidates.AddRange(GetContents(player.Id));

        if (player.LocationId is { } locationId)
            candidates.AddRange(GetContents(locationId));

        return MatchByNameOrAlias(candidates, text);
    }

    private IEnumerable<MooObject> GetContents(ObjectId containerId) {
        return _objects
            .All()
            .Where(obj => obj.LocationId == containerId);
    }

    private static MatchResult MatchByNameOrAlias(
        IEnumerable<MooObject> candidates,
        string text) {
        var exactMatches = candidates
            .Where(obj => MatchesExact(obj, text))
            .Select(obj => obj.Id)
            .Distinct()
            .ToList();

        if (exactMatches.Count == 1)
            return MatchResult.Found(exactMatches[0]);

        if (exactMatches.Count > 1)
            return MatchResult.Ambiguous();

        var partialMatches = candidates
            .Where(obj => MatchesPartial(obj, text))
            .Select(obj => obj.Id)
            .Distinct()
            .ToList();

        if (partialMatches.Count == 1)
            return MatchResult.Found(partialMatches[0]);

        if (partialMatches.Count > 1)
            return MatchResult.Ambiguous();

        return MatchResult.NotFound();
    }

    private static bool MatchesExact(MooObject obj, string text) {
        return obj.MatchNames().Any(name =>
            name.Equals(text, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesPartial(MooObject obj, string text) {
        return obj.MatchNames().Any(name =>
            name.StartsWith(text, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryParseObjectId(string text, out ObjectId objectId) {
        objectId = default;

        if (!text.StartsWith('#'))
            return false;

        if (!int.TryParse(text[1..], out var value))
            return false;

        objectId = new ObjectId(value);
        return true;
    }
}
