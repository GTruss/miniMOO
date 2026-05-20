using miniMOO.Core.Things;
using miniMOO.Engine.BuiltinVerbs;
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
    private BuiltinVerbRegistry _builtinRegistry = null!;
    private CommandParser _parser = null!;
    private CommandDispatcher _dispatcher = null!;
    private OutputService _output = null!;
    private PermissionService _permissionService = null!;
    private ITerminal _terminal = null!;

    private readonly ObjectId _playerId = WorldSeeder.WizardId;

    public GameRunner() {
        
    }

    public void RunCLI(string[] args) {
        _terminal = new ConsoleTerminal();
        _output = new OutputService(_terminal);


        RegisterVerbs();

        _objects = WorldSeeder.Seed();

        var matcher = new ObjectMatcher(_objects);
        _parser = new CommandParser(matcher);
        _permissionService = new PermissionService(_objects);
        _resolver = new ObjectResolver(_objects);
        var scriptWorld = new EngineScriptWorld(_objects, _resolver, _output);
        _dispatcher = new CommandDispatcher(_objects, _builtinRegistry, _output, 
            _permissionService, new TinyScriptRuntime(), scriptWorld, _resolver);

        _terminal.WriteLine("Welcome to miniMOO!");

        DescribeLocation();       
    
        while (true) {
            _terminal.Write("> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("q", StringComparison.OrdinalIgnoreCase)) {
                _terminal.WriteLine("Goodbye!");
                break;
            }

            var command = _parser.Parse(_playerId, input);
            _dispatcher.Dispatch(_playerId, command);
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

    private void RegisterVerbs() {
        _builtinRegistry = new BuiltinVerbRegistry();
        _builtinRegistry.Register(new LookBuiltinVerb());
        _builtinRegistry.Register(new GoBuiltinVerb());
        _builtinRegistry.Register(new WaysBuiltinVerb());

    }
}
