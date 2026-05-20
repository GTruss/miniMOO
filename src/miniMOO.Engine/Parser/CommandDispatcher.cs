using miniMOO.Engine.BuiltinVerbs;
using miniMOO.Engine.Repositories;
using miniMOO.Engine.ScriptRuntime;
using miniMOO.Engine.Services;
using miniMOO.Engine.Things;
using miniMOO.Engine.Verbs;

namespace miniMOO.Engine.Parser;

public sealed class CommandDispatcher {
    private readonly record struct ResolvedVerb(ObjectId ThisId, MooVerb Verb);

    private readonly IObjectRepository _objects;
    private readonly BuiltinVerbRegistry _builtins;
    private readonly OutputService _output;
    private readonly PermissionService _permission;
    private readonly IScriptRuntime _scripts;
    private readonly IObjectResolver _resolver;

    public CommandDispatcher(IObjectRepository objects, BuiltinVerbRegistry builtins, 
                             OutputService output, PermissionService permission, 
                             IScriptRuntime scripts, IObjectResolver resolver) {
        _objects = objects;
        _builtins = builtins;
        _output = output;
        _permission = permission;
        _scripts = scripts;
        _resolver = resolver;
        _permission = permission;
        _scripts = scripts;
    }

    public void Dispatch(ObjectId playerId, ParsedCommand command) {
        if (string.IsNullOrWhiteSpace(command.Verb))
            return;

        var player = _objects.Get(playerId);

        if (player is null) {
            _output.Notify(playerId, "You don't exist.");
            return;
        }

        var targetId = player.LocationId ?? playerId;
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

        var result = verb.ImplementationKind switch {
            VerbImplementationKind.Builtin =>
                ExecuteBuiltin(context, verb),

            VerbImplementationKind.Script =>
                _scripts.ExecuteAsync(context, verb.Implementation).GetAwaiter().GetResult(),

            _ =>
                VerbResult.Failure("Unknown verb implementation.")
        };

        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Message)) {
            _output.Notify(playerId, result.Message);
        }

    }

    private ResolvedVerb? FindCommandVerb(MooObject player, ParsedCommand command) {
        if (TryFindVerbOn(player.Id, command.Verb, out var playerVerb))
            return new ResolvedVerb(player.Id, playerVerb);

        if (player.LocationId is { } locationId) {
            if (TryFindVerbOn(locationId, command.Verb, out var locationVerb))
                return new ResolvedVerb(locationId, locationVerb);

            foreach (var obj in _objects.ContentsOf(locationId)) {
                if (TryFindVerbOn(obj.Id, command.Verb, out var contentsVerb))
                    return new ResolvedVerb(obj.Id, contentsVerb);
            }
        }

        if (command.DirectObject.Kind == MatchResultKind.Found &&
            command.DirectObject.ObjectId is { } directId &&
            TryFindVerbOn(directId, command.Verb, out var directVerb))
            return new ResolvedVerb(directId, directVerb);

        if (command.IndirectObject.Kind == MatchResultKind.Found &&
            command.IndirectObject.ObjectId is { } indirectId &&
            TryFindVerbOn(indirectId, command.Verb, out var indirectVerb))
            return new ResolvedVerb(indirectId, indirectVerb);

        return null;
    }

    private bool TryFindVerbOn(ObjectId startId, string name, out MooVerb verb) {
        var found = _resolver.FindVerb(startId, name);

        if (found is not null) {
            verb = found;
            return true;
        } 

        verb = null!;
        return false;
    }

    private VerbResult ExecuteBuiltin(VerbContext context, MooVerb verb) {
        var builtin = _builtins.Find(verb.Implementation);

        if (builtin is null)
            return VerbResult.Failure($"Builtin verb not found: {verb.Implementation}");

        return builtin.ExecuteAsync(context).GetAwaiter().GetResult();
    }

}
