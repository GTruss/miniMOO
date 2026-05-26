using miniMOO.Core.Things;
using miniMOO.Data.FileSystem;
using miniMOO.Engine.Parser;
using miniMOO.Host.World;
using miniMOO.Engine.Repositories;
using miniMOO.Engine.ScriptRuntime;
using miniMOO.Engine.Services;
using miniMOO.Engine.Terminals;
using miniMOO.Script.Runtimes;

namespace miniMOO.Host;

public class GameRunner {
    private IObjectRepository _objects = null!;
    private IObjectResolver _resolver = null!;
    private CommandParser _parser = null!;
    private CommandDispatcher _dispatcher = null!;
    private OutputService _output = null!;
    private PermissionService _permissionService = null!;
    private ITerminal _terminal = null!;
    private bool _shutdownRequested;
    private string _shutdownMessage = "";
    private bool _dispatchingCommand;

    private readonly ObjectId _playerId = WorldSeeder.WizardId;

    public GameRunner() {
        
    }

    public void RunCLI(string[] args) {
        _terminal = new ConsoleTerminal();
        _output = new OutputService(_terminal);
        _objects = WorldSeeder.Seed();

        var matcher = new ObjectMatcher(_objects);
        var scriptRuntime = new TinyScriptRuntime();

        _parser = new CommandParser(matcher);
        _permissionService = new PermissionService(_objects);
        _resolver = new ObjectResolver(_objects);

        var scriptWorld = new EngineScriptWorld(_objects, _resolver, _output, scriptRuntime);
        scriptWorld.SetCheckpoint(() => Task.FromResult(CheckpointWorld()));
        scriptWorld.SetShutdown(message => Task.FromResult(RequestShutdown(message)));

        _dispatcher = new CommandDispatcher(_objects, _output, 
            _permissionService, scriptRuntime, scriptWorld, _resolver);
        scriptWorld.SetCommandEvaluator((playerId, commandText) => {
            var command = _parser.Parse(playerId, commandText);
            _dispatcher.Dispatch(playerId, command);
            return Task.CompletedTask;
        });
        scriptWorld.SetInputReader(_ =>
            Task.FromResult(_terminal.ReadLine()));

        _terminal.WriteLine("Welcome to miniMOO!");

        DescribeLocation();       
    
        while (true) {
            _terminal.Write("> ");
            var input = _terminal.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("q", StringComparison.OrdinalIgnoreCase)) {
                _terminal.WriteLine("Goodbye!");
                break;
            }

            var command = _parser.Parse(_playerId, input);

            _dispatchingCommand = true;
            try {
                _dispatcher.Dispatch(_playerId, command);
            }
            finally {
                _dispatchingCommand = false;
            }

            if (_shutdownRequested) {
                if (!string.IsNullOrWhiteSpace(_shutdownMessage))
                    _terminal.WriteLine(_shutdownMessage);

                _terminal.WriteLine("Goodbye!");
                break;
            }
        }
    }

    private void DescribeLocation() {
        var player = _objects.Get(_playerId);
        if (player?.LocationId is not { } locId) return;

        var room = _objects.Get(locId);
        if (room is null) return;

        _output.Notify(_playerId, room.Name);

        var desc = _resolver.FindPropertyValue(room.Id, "description")?.ToString()
            ?? "You see nothing special.";

        _output.Notify(_playerId, desc);
    }

    private MooValue CheckpointWorld() {
        var writer = new FileWorldWriter();
        var worldPath = Path.Combine(AppContext.BaseDirectory, "data", "world");
        var count = writer.WriteDirectory(worldPath, WorldObjects());

        return new MooValue.String($"Checkpoint complete: {count} world objects written.");
    }

    private MooValue RequestShutdown(string message) {
        _shutdownRequested = true;
        _shutdownMessage = message;

        if (!_dispatchingCommand) {
            if (!string.IsNullOrWhiteSpace(_shutdownMessage))
                _terminal.WriteLine(_shutdownMessage);

            _terminal.WriteLine("Goodbye!");
            Environment.Exit(0);
        }

        return MooValue.NothingValue;
    }

    private IEnumerable<MooObject> WorldObjects()
        => _objects.All()
            .Where(obj => obj.Id == WorldIds.Wizard
                          || obj.Id == WorldIds.PlayerStart
                          || obj.Id.Value >= WorldIds.Foyer.Value);
}
