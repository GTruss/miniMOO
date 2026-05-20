using miniMOO.Core.ScriptRuntime;
using miniMOO.Core.Things;

namespace miniMOO.Script.Runtimes;

public sealed class TinyScriptRuntime : IScriptRuntime {
    public async Task<ScriptResult> ExecuteAsync(ScriptContext context, string script) {
        foreach (var rawLine in script.Split('\n')) {
            var line = rawLine.Trim();

            if (line.Length == 0)
                continue;

            if (line.StartsWith("tell player ", StringComparison.OrdinalIgnoreCase)) {
                var text = line["tell player ".Length..];
                await NotifyAsync(context, context.PlayerId, Expand(context, text));
                continue;
            }

            if (line.StartsWith("announce room ", StringComparison.OrdinalIgnoreCase)) {
                var text = line["announce room ".Length..];
                await AnnounceRoomAsync(context, Expand(context, text));
                continue;
            }

            if (line.Equals("list inventory", StringComparison.OrdinalIgnoreCase)) {
                await ListInventoryAsync(context);
                continue;
            }

            return ScriptResult.Failure($"Unknown script command: {line}");
        }

        return ScriptResult.Success();
    }

    private static async Task AnnounceRoomAsync(ScriptContext context, string text) {
        var player = context.World.Get(context.PlayerId);

        if (player?.LocationId is null)
            return;

        // Single-user CLI for now.
        await NotifyAsync(context, context.PlayerId, text);
    }

    private static string Expand(ScriptContext context, string text) {
        var player = context.World.Get(context.PlayerId);

        return text
            .Replace("{player}", player?.Name ?? "Someone", StringComparison.OrdinalIgnoreCase)
            .Replace("{argstr}", context.ArgStr, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ListInventoryAsync(ScriptContext context) {
        var contents = context.World
            .ContentsOf(context.PlayerId)
            .ToList();

        if (contents.Count == 0) {
            await NotifyAsync(context, context.PlayerId, "You are empty-handed.");
            return;
        }

        await NotifyAsync(context, context.PlayerId, "Carrying:");

        foreach (var thing in contents)
            await NotifyAsync(context, context.PlayerId, $"  {thing.Name}");
    }

    private static Task NotifyAsync(ScriptContext context, ObjectId playerId, string text)
        => context.World.NotifyAsync(playerId, [new MooValue.String(text)]);
}
