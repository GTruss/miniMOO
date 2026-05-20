using miniMOO.Engine.Things;
using miniMOO.Engine.Verbs;

namespace miniMOO.Engine.BuiltinVerbs;

public sealed class GoBuiltinVerb : IBuiltinVerb {
    public string Name => "go";

    public Task<VerbResult> ExecuteAsync(VerbContext context) {
        var exit = context.Objects.Get(context.ThisId);
        if (exit is null)
            return Fail(context, "That exit doesn't exist.");

        if (!exit.Properties.TryGetValue("destination", out var destProp) ||
            destProp.Value is not MooValue.Object destVal)
            return Fail(context, "That exit leads nowhere.");

        var player = context.Objects.Get(context.PlayerId);
        if (player is null)
            return Fail(context, "You don't exist.");

        player.LocationId = destVal.Value;

        var dest = context.Objects.Get(destVal.Value);
        if (dest is null)
            return Fail(context, "You move, but arrive nowhere.");

        context.Output.Notify(context.PlayerId, dest.Name);

        var desc = dest.Properties.TryGetValue("description", out var dp)
            ? dp.Value.ToString()
            : "You see nothing special.";
        context.Output.Notify(context.PlayerId, desc);

        var contents = context.Objects.ContentsOf(dest.Id)
            .Where(o => o.Id != context.PlayerId && !IsExit(o))
            .ToList();

        if (contents.Count > 0) {
            context.Output.Notify(context.PlayerId, "");
            context.Output.Notify(context.PlayerId, "You see:");
            foreach (var obj in contents)
                context.Output.Notify(context.PlayerId, $"  {obj.Name}");
        }

        return Task.FromResult(VerbResult.Success());
    }

    private static bool IsExit(MooObject obj)
        => obj.Properties.ContainsKey("destination");

    private static Task<VerbResult> Fail(VerbContext context, string msg) {
        context.Output.Notify(context.PlayerId, msg);
        return Task.FromResult(VerbResult.Failure(msg));
    }
}
