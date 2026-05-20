using miniMOO.Engine.Terminals;
using miniMOO.Core.Things;

namespace miniMOO.Engine.Services;

public sealed class OutputService {
    private readonly ITerminal _terminal;

    public OutputService(ITerminal terminal) {
        _terminal = terminal;
    }

    public void Notify(ObjectId playerId, string text) {
        _terminal.WriteLine(text);
    }
}
