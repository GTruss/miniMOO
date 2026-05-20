using miniMOO.Engine.Things;
using miniMOO.Engine.Verbs;

using System;
using System.Collections.Generic;
using System.Text;

namespace miniMOO.Engine.BuiltinVerbs;

public sealed class LookBuiltinVerb : IBuiltinVerb {
    public string Name => "look";

    public Task<VerbResult> ExecuteAsync(VerbContext context) {
        ObjectId? targetId;

        if (!context.Command.HasDirectObject) {
            var player = context.Objects.Get(context.PlayerId);
            targetId = player?.LocationId;
        }
        else {
            targetId = context.Command.DirectObject.Kind switch {
                MatchResultKind.Found => context.Command.DirectObject.ObjectId,
                MatchResultKind.NotFound => null,
                MatchResultKind.Ambiguous => null,
                _ => null
            };
        } 

        if (context.Command.HasDirectObject &&
            context.Command.DirectObject.Kind == MatchResultKind.NotFound) {
            context.Output.Notify(context.PlayerId, "You don't see that here.");
            return Task.FromResult(VerbResult.Failure("Object not found."));
        }

        if (context.Command.HasDirectObject &&
            context.Command.DirectObject.Kind == MatchResultKind.Ambiguous) {
            context.Output.Notify(context.PlayerId, "I don't know which one you mean.");
            return Task.FromResult(VerbResult.Failure("Ambiguous object."));
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

        var description = target.Properties.TryGetValue("description", out var prop)
            ? prop.Value.ToString()
            : "You see nothing special.";

        context.Output.Notify(context.PlayerId, description);

        var allContents = context.Objects.ContentsOf(target.Id).ToList();

        var contents = allContents
            .Where(obj => obj.Id != context.PlayerId && !IsExit(obj))
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

    private static bool IsExit(MooObject obj)
        => obj.Properties.ContainsKey("destination");

    private static bool IsObviousExit(VerbContext context, MooObject obj)
        => context.Resolver.FindPropertyValue(obj.Id, "destination") is MooValue.Object
        && context.Resolver.FindPropertyValue(obj.Id, "obvious") is MooValue.Integer i
        && i.Value != 0; 
}
