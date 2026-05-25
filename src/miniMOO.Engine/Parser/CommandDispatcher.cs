using miniMOO.Core.ScriptRuntime;
using miniMOO.Core.Things;
using miniMOO.Engine.Repositories;
using miniMOO.Engine.Services;
using miniMOO.Engine.Verbs;
using miniMOO.Script.Evaluation;

namespace miniMOO.Engine.Parser;

public sealed class CommandDispatcher {
    private readonly record struct ResolvedVerb(ObjectId ThisId, MooVerb Verb);

    private readonly IObjectRepository _objects;
    private readonly OutputService _output;
    private readonly PermissionService _permission;
    private readonly IScriptRuntime _scripts;
    private readonly IScriptWorld _scriptWorld;
    private readonly IObjectResolver _resolver;

    public CommandDispatcher(
        IObjectRepository objects,
        OutputService output,
        PermissionService permission,
        IScriptRuntime scripts,
        IScriptWorld scriptWorld,
        IObjectResolver resolver) {

        _objects = objects;
        _output = output;
        _permission = permission;
        _scripts = scripts;
        _scriptWorld = scriptWorld;
        _resolver = resolver;
    }

    public void Dispatch(ObjectId playerId, ParsedCommand command) {
        if (string.IsNullOrWhiteSpace(command.Verb))
            return;

        var player = _objects.Get(playerId);

        if (player is null) {
            _output.Notify(playerId, "You don't exist.");
            return;
        }

        var resolved = FindCommandVerb(player, command);

        if (resolved is null) {
            _output.Notify(playerId, "I couldn't understand that.");
            return;
        }

        var (thisId, verb) = resolved.Value;

        var context = new VerbContext {
            PlayerId = playerId,
            ThisId = thisId,
            Command = command,
            Objects = _objects,
            Output = _output,
            Permissions = _permission,
            Resolver = _resolver
        };

        var result = ExecuteScript(playerId, thisId, command, verb);

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Message))
            _output.Notify(playerId, result.Message);
    }

    private ResolvedVerb? FindCommandVerb(MooObject player, ParsedCommand command) {
        if (TryFindVerbOn(player.Id, command.Verb, command, out var playerVerb))
            return new ResolvedVerb(player.Id, playerVerb);

        if (player.LocationId is { } locationId) {
            if (TryFindVerbOn(locationId, command.Verb, command, out var locationVerb))
                return new ResolvedVerb(locationId, locationVerb);

            foreach (var exitId in ExitIdsOf(locationId)) {
                if (TryFindVerbOn(exitId, command.Verb, command, out var exitVerb))
                    return new ResolvedVerb(exitId, exitVerb);

                if (ExitMatchesCommand(exitId, command.Verb) &&
                    TryFindVerbByNameIgnoringSpec(exitId, "invoke", out var invokeVerb))
                    return new ResolvedVerb(exitId, invokeVerb);
            }

            foreach (var obj in _objects.ContentsOf(locationId)) {
                if (TryFindVerbOn(obj.Id, command.Verb, command, out var contentsVerb))
                    return new ResolvedVerb(obj.Id, contentsVerb);
            }
        }

        if (command.DirectObject?.Kind == MatchResultKind.Found &&
            command.DirectObject.ObjectId is { } directId &&
            TryFindVerbOn(directId, command.Verb, command, out var directVerb))
            return new ResolvedVerb(directId, directVerb);

        if (command.IndirectObject?.Kind == MatchResultKind.Found &&
            command.IndirectObject.ObjectId is { } indirectId &&
            TryFindVerbOn(indirectId, command.Verb, command, out var indirectVerb))
            return new ResolvedVerb(indirectId, indirectVerb);

        return null;
    }

    private bool TryFindVerbOn(ObjectId startId, string name, ParsedCommand command, out MooVerb verb) {
        foreach (var obj in _resolver.SelfAndAncestors(startId)) {
            var found = obj.Verbs.FirstOrDefault(v =>
                v.MatchesName(name) && SpecMatches(v, command, startId));

            if (found is not null) {
                verb = found;
                return true;
            }
        }

        verb = null!;
        return false;
    }

    private VerbResult ExecuteScript(ObjectId playerId, ObjectId thisId, ParsedCommand command, MooVerb verb) {

        var scriptContext = new ScriptContext {
            PlayerId = playerId,
            ThisId = thisId,
            Verb = command.Verb,
            ArgStr = command.ArgumentText,
            Args = command.Arguments
                .Select(arg => (MooValue)new MooValue.String(arg))
                .ToList(),
            DirectObjectId = command.DirectObject?.ObjectId,
            IndirectObjectId = command.IndirectObject?.ObjectId,
            DobjStr = command.DirectObjectText ?? "",
            IobjStr = command.IndirectObjectText ?? "",
            World = _scriptWorld
        };

        ScriptResult result;

        try {
            result = _scripts.ExecuteAsync(scriptContext, verb.Code)
                .GetAwaiter()
                .GetResult();
        }
        catch (MooTaskAbortException) {
            return VerbResult.Success();
        }

        if (result.IsSuccess)
            return VerbResult.Success(result.Value);

        var error = result.ErrorDetail;

        if (error is not null && error.SourceLabel != CurrentCommandSourceLabel(thisId, command.Verb)) {
            var framed = result.WithFrame(new ScriptTraceFrame(
                thisId,
                thisId,
                command.Verb,
                null,
                $"... (cmd) {thisId}:{string.Join("/", verb.Names)} (this == {thisId})"));

            return VerbResult.Failure(framed.Error ?? "Script failed.");
        }

        return VerbResult.Failure(result.Error ?? "Script failed.");

    }

    private static bool SpecMatches(MooVerb verb, ParsedCommand command, ObjectId thisId) {
        if (verb.DirectObject == VerbObjectSpec.None && command.HasDirectObject)
            return false;

        if (verb.DirectObject == VerbObjectSpec.This &&
            command.DirectObject?.ObjectId != thisId)
            return false;

        if (verb.IndirectObject == VerbObjectSpec.None && command.HasIndirectObject)
            return false;

        if (verb.IndirectObject == VerbObjectSpec.This &&
            command.IndirectObject?.ObjectId != thisId)
            return false;

        if (verb.Preposition != "any") {
            if (verb.Preposition == "none" && command.HasPreposition)
                return false;

            if (verb.Preposition != "none" &&
                !string.Equals(verb.Preposition, command.Preposition, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private IEnumerable<ObjectId> ExitIdsOf(ObjectId roomId) {
        if (_resolver.FindPropertyValue(roomId, "exits") is not MooValue.List exits)
            yield break;

        foreach (var item in exits.Items) {
            if (item is MooValue.Object obj && _objects.Exists(obj.Value))
                yield return obj.Value;
        }
    }

    private bool ExitMatchesCommand(ObjectId exitId, string commandName) {
        var exit = _objects.Get(exitId);
        if (exit is null)
            return false;

        if (string.Equals(exit.Name, commandName, StringComparison.OrdinalIgnoreCase))
            return true;

        return exit.Aliases.Any(alias =>
            string.Equals(alias, commandName, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryFindVerbByNameIgnoringSpec(ObjectId startId, string name, out MooVerb verb) {
        foreach (var obj in _resolver.SelfAndAncestors(startId)) {
            var found = obj.Verbs.FirstOrDefault(v => v.MatchesName(name));

            if (found is not null) {
                verb = found;
                return true;
            }
        }

        verb = null!;
        return false;
    }

    private static string CurrentCommandSourceLabel(ObjectId thisId, string verb)
        => $"{thisId}:{verb}";
}
