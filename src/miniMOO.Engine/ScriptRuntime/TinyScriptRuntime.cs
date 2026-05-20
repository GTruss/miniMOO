using miniMOO.Engine.Verbs;

using System;
using System.Collections.Generic;
using System.Text;

namespace miniMOO.Engine.ScriptRuntime;

public sealed class TinyScriptRuntime : IScriptRuntime {
    public Task<VerbResult> ExecuteAsync(VerbContext context, string script) {
        foreach (var rawLine in script.Split('\n')) {
            var line = rawLine.Trim();

            if (line.Length == 0)
                continue;

            if (line.StartsWith("tell player ", StringComparison.OrdinalIgnoreCase)) {
                var text = line["tell player ".Length..];
                context.Output.Notify(context.PlayerId, Expand(context, text));
                continue;
            }

            if (line.StartsWith("announce room ", StringComparison.OrdinalIgnoreCase)) {
                var text = line["announce room ".Length..];
                AnnounceRoom(context, Expand(context, text));
                continue;
            }
            
            if (line.Equals("list inventory", StringComparison.OrdinalIgnoreCase)) {
                ListInventory(context);
                continue;
            }

            return Task.FromResult(VerbResult.Failure($"Unknown script command: {line}"));
        }

        return Task.FromResult(VerbResult.Success());
    }

    private static void AnnounceRoom(VerbContext context, string text) {
        var player = context.Objects.Get(context.PlayerId);

        if (player?.LocationId is null)
            return;

        // Single-user CLI for now.
        context.Output.Notify(context.PlayerId, text);
    }

    private static string Expand(VerbContext context, string text) {
        var player = context.Objects.Get(context.PlayerId);

        return text
            .Replace("{player}", player?.Name ?? "Someone", StringComparison.OrdinalIgnoreCase)
            .Replace("{argstr}", context.Command.ArgumentText, StringComparison.OrdinalIgnoreCase);
    }

    private static void ListInventory(VerbContext context) {
        var contents = context.Objects
            .ContentsOf(context.PlayerId)
            .ToList();

        if (contents.Count == 0) {
            context.Output.Notify(context.PlayerId, "You are empty-handed.");
            return;
        }

        context.Output.Notify(context.PlayerId, "Carrying:");

        foreach (var thing in contents)
            context.Output.Notify(context.PlayerId, $"  {thing.Name}");
    }
}
