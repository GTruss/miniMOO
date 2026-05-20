using miniMOO.Core.Things;
using miniMOO.Engine.Verbs;

namespace miniMOO.Engine.BuiltinVerbs;

public sealed class LookBuiltinVerb : IBuiltinVerb {
    public string Name => "look";

    public Task<VerbResult> ExecuteAsync(VerbContext context) {
        // Prefer dobj, then iobj ("look at X"), then fall back to the room
        var objectMatch = context.Command.HasDirectObject ? context.Command.DirectObject
                        : context.Command.HasIndirectObject ? context.Command.IndirectObject
                        : null;

        ObjectId? targetId;

        if (objectMatch is not null) {
            if (objectMatch.Kind == MatchResultKind.NotFound) {
                context.Output.Notify(context.PlayerId, "You don't see that here.");
                return Task.FromResult(VerbResult.Failure("Object not found."));
            }
            if (objectMatch.Kind == MatchResultKind.Ambiguous) {
                context.Output.Notify(context.PlayerId, "I don't know which one you mean.");
                return Task.FromResult(VerbResult.Failure("Ambiguous object."));
            }
            targetId = objectMatch.ObjectId;
        }
        else {
            var player = context.Objects.Get(context.PlayerId);
            targetId = player?.LocationId;
        } 

        if (targetId is null) {
            context.Output.Notify(context.PlayerId, "You are nowhere.");
            return Task.FromResult(VerbResult.Failure("No look target."));
        }

        var target = context.Objects.Get(targetId.Value);

        if (target is null) {
            context.Output.Notify(context.PlayerId, "You don't see that here.");
            return Task.FromResult(VerbResult.Failure("Invalid target."));
        }

        context.Output.Notify(context.PlayerId, target.Name);

        var description = context.Resolver.FindPropertyValue(target.Id, "description")?.ToString()
            ?? "";

        context.Output.Notify(context.PlayerId, description);

        var allContents = context.Objects.ContentsOf(target.Id).ToList();

        var contents = allContents
            .Where(obj => obj.Id != context.PlayerId && !IsExit(context, obj))
            .ToList();

        if (contents.Count > 0) {
            context.Output.Notify(context.PlayerId, "");
            context.Output.Notify(context.PlayerId, "You see:");
            foreach (var obj in contents)
                context.Output.Notify(context.PlayerId, $"  {obj.Name}");
        }

        var obviousExits = allContents
            .Where(obj => IsObviousExit(context, obj))
            .Select(e => e.Name)
            .ToList();

        if (obviousExits.Count > 0) {
            context.Output.Notify(context.PlayerId, "");
            context.Output.Notify(context.PlayerId,
                "Obvious exits: " + string.Join(", ", obviousExits));
        }

        return Task.FromResult(VerbResult.Success());
    }

    private static bool IsExit(VerbContext context, MooObject obj)
        => context.Resolver.FindPropertyValue(obj.Id, "destination") is MooValue.Object;

    private static bool IsObviousExit(VerbContext context, MooObject obj)
        => IsExit(context, obj)
        && context.Resolver.FindPropertyValue(obj.Id, "obvious") is MooValue.Integer i
        && i.Value != 0;
}
