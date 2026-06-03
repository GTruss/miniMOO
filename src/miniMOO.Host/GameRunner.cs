using System.Text.Json;
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
    private string _databaseRootPath = "";

    private readonly ObjectId _playerId = WorldSeeder.WizardId;

    public GameRunner() {
        
    }

    public void RunCLI(string[] args) {
        _terminal = new ConsoleTerminal();
        _output = new OutputService(_terminal);

        if (IsTestRun(args))
            RefreshTestDatabase();

        _databaseRootPath = LoadDatabaseRootPath(args);
        _objects = WorldSeeder.Seed(_databaseRootPath);

        var matcher = new ObjectMatcher(_objects);
        var scriptRuntime = new TinyScriptRuntime();

        _parser = new CommandParser(matcher);
        _permissionService = new PermissionService(_objects);
        _resolver = new ObjectResolver(_objects);

        var scriptWorld = new EngineScriptWorld(_objects, _resolver, _output, scriptRuntime);
        scriptWorld.SetCheckpoint(() => Task.FromResult(CheckpointDatabase()));
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

    private MooValue CheckpointDatabase() {
        var writer = new FileWorldWriter();
        var corePath = Path.Combine(_databaseRootPath, "core");
        var worldPath = Path.Combine(_databaseRootPath, "world");
        var coreCount = writer.WriteDirectory(corePath, CoreObjects());
        var worldCount = writer.WriteDirectory(worldPath, WorldObjects());

        return new MooValue.String($"Checkpoint complete: {coreCount} core objects and {worldCount} world objects written.");
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

    private IEnumerable<MooObject> CoreObjects()
        => _objects.All().Where(IsCoreObject);

    private IEnumerable<MooObject> WorldObjects()
        => _objects.All()
            .Where(obj => !IsCoreObject(obj));

    private bool IsCoreObject(MooObject obj) {
        if (obj.Id == ObjectId.System)
            return true;

        if (obj.Id == _playerId)
            return false;

        if (TryGetSystemObjectReference("player_start") is { } playerStartId && obj.Id == playerStartId)
            return false;

        return obj.Id.Value > 0 && obj.Id.Value < 100;
    }

    private ObjectId? TryGetSystemObjectReference(string propertyName) {
        var system = _objects.Get(ObjectId.System);
        if (system is null)
            return null;

        return _resolver.FindPropertyValue(system.Id, propertyName) is MooValue.Object obj
            ? obj.Value
            : null;
    }

    private static string LoadDatabaseRootPath(IReadOnlyList<string> args) {
        var fallback = Path.Combine(AppContext.BaseDirectory, "data");
        var configName = SelectConfigName(args);
        var configPath = FindAppSettingsPath(configName);

        if (configPath is null)
            return fallback;

        try {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            var root = document.RootElement;

            var configuredPath = TryGetString(root, "MiniMoo", "DatabasePath")
                                 ?? TryGetString(root, "DatabasePath");

            if (string.IsNullOrWhiteSpace(configuredPath))
                return fallback;

            if (Path.IsPathRooted(configuredPath))
                return Path.GetFullPath(configuredPath);

            var relativeBase = FindSolutionRoot(configPath)
                               ?? FindSolutionRoot(Directory.GetCurrentDirectory())
                               ?? Path.GetDirectoryName(configPath)
                               ?? Directory.GetCurrentDirectory();

            return Path.GetFullPath(Path.Combine(relativeBase, configuredPath));
        }
        catch (JsonException ex) {
            throw new InvalidOperationException($"Invalid {configName}: {configPath}", ex);
        }
    }

    private static bool IsTestRun(IReadOnlyList<string> args)
        => args.Any(arg =>
            arg.Equals("--test", StringComparison.OrdinalIgnoreCase)
            || arg.Equals("--tests", StringComparison.OrdinalIgnoreCase));

    private static void RefreshTestDatabase() {
        var liveRoot = LoadDatabaseRootPath(["--live"]);
        var testRoot = LoadDatabaseRootPath(["--test"]);

        if (!Directory.Exists(liveRoot))
            throw new DirectoryNotFoundException($"Live database directory not found: {liveRoot}");

        if (Path.GetFullPath(liveRoot).Equals(Path.GetFullPath(testRoot), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Test database path must be different from live database path.");

        RefreshDatabaseFolder(liveRoot, testRoot, "core");
        RefreshDatabaseFolder(liveRoot, testRoot, "world");
    }

    private static void RefreshDatabaseFolder(string liveRoot, string testRoot, string folderName) {
        var source = Path.Combine(liveRoot, folderName);
        var target = Path.Combine(testRoot, folderName);

        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException($"Live database folder not found: {source}");

        Directory.CreateDirectory(target);

        foreach (var file in Directory.EnumerateFiles(target, "*", SearchOption.TopDirectoryOnly))
            File.Delete(file);

        foreach (var directory in Directory.EnumerateDirectories(target, "*", SearchOption.TopDirectoryOnly))
            Directory.Delete(directory, recursive: true);

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.TopDirectoryOnly))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
    }

    private static string SelectConfigName(IReadOnlyList<string> args) {
        for (var i = 0; i < args.Count; i++) {
            if (args[i].Equals("--test", StringComparison.OrdinalIgnoreCase)
                || args[i].Equals("--tests", StringComparison.OrdinalIgnoreCase))
                return "appsettings.tests.json";

            if (args[i].Equals("--live", StringComparison.OrdinalIgnoreCase))
                return "appsettings.live.json";

            if (args[i].Equals("--settings", StringComparison.OrdinalIgnoreCase)
                && i + 1 < args.Count)
                return args[i + 1];
        }

        return "appsettings.live.json";
    }

    private static string? FindAppSettingsPath(string configName) {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory }) {
            var dir = Path.GetFullPath(start);

            while (!string.IsNullOrWhiteSpace(dir)) {
                var candidate = Path.Combine(dir, configName);
                if (File.Exists(candidate))
                    return candidate;

                var cliProjectCandidate = Path.Combine(dir, "src", "miniMOO.Cli", configName);
                if (File.Exists(cliProjectCandidate))
                    return cliProjectCandidate;

                var parent = Directory.GetParent(dir)?.FullName;
                if (parent is null || parent == dir)
                    break;

                dir = parent;
            }
        }

        return null;
    }

    private static string? FindSolutionRoot(string start) {
        var dir = File.Exists(start)
            ? Path.GetDirectoryName(start)
            : start;

        if (string.IsNullOrWhiteSpace(dir))
            return null;

        dir = Path.GetFullPath(dir);

        while (!string.IsNullOrWhiteSpace(dir)) {
            if (Directory.EnumerateFiles(dir, "*.slnx").Any()
                || Directory.EnumerateFiles(dir, "*.sln").Any())
                return dir;

            var parent = Directory.GetParent(dir)?.FullName;
            if (parent is null || parent == dir)
                break;

            dir = parent;
        }

        return null;
    }

    private static string? TryGetString(JsonElement root, params string[] path) {
        var current = root;

        foreach (var segment in path) {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current))
                return null;
        }

        return current.ValueKind == JsonValueKind.String
            ? current.GetString()
            : null;
    }
}
