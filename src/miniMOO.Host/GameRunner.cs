using System.Text.Json;
using miniMOO.Core.Things;
using miniMOO.Core.ScriptRuntime;
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
    private EngineScriptWorld _scriptWorld = null!;
    private OutputService _output = null!;
    private PermissionService _permissionService = null!;
    private ITerminal _terminal = null!;
    private bool _shutdownRequested;
    private string _shutdownMessage = "";
    private bool _disconnectRequested;
    private bool _dispatchingCommand;
    private string _databaseRootPath = "";
    private bool _requireLogin;
    private bool _autoRunTests;
    private ObjectId? _currentPlayerId;
    private static readonly ObjectId ConnectionId = new(-100);

    public GameRunner() {
        
    }

    public void RunCLI(string[] args) {
        _terminal = new ConsoleTerminal();
        _output = new OutputService(_terminal);

        (_databaseRootPath, _requireLogin) = LoadConfiguration(args);
        _autoRunTests = ShouldAutoRunTests(args);

        if (TryHandleDatabaseAction(args))
            return;

        if (!EnsureDatabaseReady(args))
            return;

        _objects = WorldSeeder.Seed(_databaseRootPath);

        var matcher = new ObjectMatcher(_objects);
        var scriptRuntime = new TinyScriptRuntime();

        _parser = new CommandParser(matcher);
        _permissionService = new PermissionService(_objects);
        _resolver = new ObjectResolver(_objects);

        _scriptWorld = new EngineScriptWorld(_objects, _resolver, _output, scriptRuntime);
        _scriptWorld.SetCheckpoint(() => Task.FromResult(CheckpointDatabase()));
        _scriptWorld.SetShutdown(message => Task.FromResult(RequestShutdown(message)));
        _scriptWorld.SetConnectedPlayers(() =>
            _currentPlayerId is { } currentPlayerId ? [currentPlayerId] : []);
        _scriptWorld.SetBootPlayer(playerId => HandleBootPlayerAsync(playerId));

        _dispatcher = new CommandDispatcher(_objects, _output, 
            _permissionService, scriptRuntime, _scriptWorld, _resolver);
        _scriptWorld.SetCommandEvaluator((playerId, commandText) => {
            var command = _parser.Parse(playerId, commandText);
            _dispatcher.Dispatch(playerId, command);
            return Task.CompletedTask;
        });
        _scriptWorld.SetInputReader(_ =>
            Task.FromResult(_terminal.ReadLine()));

        //_terminal.WriteLine("Welcome to miniMOO!");

        InitializeSession();

        if (_autoRunTests) {
            RunAutomatedTests().GetAwaiter().GetResult();
            _terminal.WriteLine("Goodbye!");
            return;
        }
    
        while (true) {
            _terminal.Write(_currentPlayerId is null ? "connect> " : "> ");
            var input = _terminal.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("q", StringComparison.OrdinalIgnoreCase)) {
                _terminal.WriteLine("Goodbye!");
                break;
            }

            _dispatchingCommand = true;
            try {
                if (_currentPlayerId is null) {
                    HandleLoginInput(input).GetAwaiter().GetResult();
                }
                else {
                    var command = _parser.Parse(_currentPlayerId.Value, input);
                    _dispatcher.Dispatch(_currentPlayerId.Value, command);
                }
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

            if (_disconnectRequested) {
                _terminal.WriteLine("Goodbye!");
                break;
            }
        }
    }

    private void InitializeSession() {
        InitializeRuntimeSystemProperties();

        if (_requireLogin && HasLoginCommand()) {
            HandleLoginInput("").GetAwaiter().GetResult();
            return;
        }

        _currentPlayerId = WorldSeeder.WizardId;
        DescribeLocation();
    }

    private async Task HandleLoginInput(string input) {
        if (!HasLoginCommand()) {
            _currentPlayerId = WorldSeeder.WizardId;
            DescribeLocation();
            return;
        }

        var words = SplitLoginWords(input);
        var args = words.Select(word => (MooValue)new MooValue.String(word)).ToList();

        var context = new ScriptContext {
            PlayerId = ConnectionId,
            ThisId = ObjectId.System,
            CallerId = ObjectId.System,
            Verb = "do_login_command",
            ArgStr = input,
            Args = args,
            World = _scriptWorld,
            DefiningObjectId = ObjectId.System
        };

        var result = await _scriptWorld.InvokeVerbAsync(
            context,
            ObjectId.System,
            "do_login_command",
            args,
            ObjectId.System);

        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error ?? "Login failed.");

        if (result.Value is MooValue.Object obj
            && obj.Value.Value >= 0
            && _objects.Exists(obj.Value)) {
            _currentPlayerId = obj.Value;
            DescribeLocation();
        }
    }

    private bool HasLoginCommand()
        => _resolver.FindVerbWithOwner(ObjectId.System, "do_login_command").Verb is not null;

    private static List<string> SplitLoginWords(string input) {
        var words = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuote = false;

        foreach (var ch in input) {
            if (ch == '"') {
                inQuote = !inQuote;
                continue;
            }

            if (!inQuote && char.IsWhiteSpace(ch)) {
                if (current.Length > 0) {
                    words.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0)
            words.Add(current.ToString());

        return words;
    }

    private void DescribeLocation() {
        if (_currentPlayerId is null)
            return;

        var player = _objects.Get(_currentPlayerId.Value);
        if (player?.LocationId is not { } locId) return;

        var room = _objects.Get(locId);
        if (room is null) return;

        _output.Notify(_currentPlayerId.Value, room.Name);

        var desc = _resolver.FindPropertyValue(room.Id, "description")?.ToString()
            ?? "You see nothing special.";

        _output.Notify(_currentPlayerId.Value, desc);
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

    private Task HandleBootPlayerAsync(ObjectId playerId) {
        if (_currentPlayerId is { } currentPlayerId && playerId == currentPlayerId)
            _disconnectRequested = true;
        else if (_currentPlayerId is null && playerId == ConnectionId)
            _disconnectRequested = true;

        return Task.CompletedTask;
    }

    private IEnumerable<MooObject> CoreObjects()
        => _objects.All().Where(IsCoreObject);

    private IEnumerable<MooObject> WorldObjects()
        => _objects.All()
            .Where(obj => !IsCoreObject(obj));

    private bool IsCoreObject(MooObject obj) {
        if (IsMarkedCoreObject(obj))
            return true;

        if (obj.Id == ObjectId.System)
            return true;

        if (obj.Flags.HasFlag(ObjectFlags.User))
            return false;

        if (_currentPlayerId is { } currentPlayerId && obj.Id == currentPlayerId)
            return false;

        if (TryGetSystemObjectReference("player_start") is { } playerStartId && obj.Id == playerStartId)
            return false;

        return obj.Id.Value > 0 && obj.Id.Value < 100;
    }

    private static bool IsMarkedCoreObject(MooObject obj)
        => obj.Properties.TryGetValue("_db", out var property)
           && property.Value is MooValue.String s
           && s.Value.Equals("core", StringComparison.OrdinalIgnoreCase);

    private ObjectId? TryGetSystemObjectReference(string propertyName) {
        var system = _objects.Get(ObjectId.System);
        if (system is null)
            return null;

        return _resolver.FindPropertyValue(system.Id, propertyName) is MooValue.Object obj
            ? obj.Value
            : null;
    }

    private void InitializeRuntimeSystemProperties() {
        var system = _objects.Get(ObjectId.System);
        if (system is null)
            return;

        system.Properties["last_restart_time"] = new MooProperty {
            Name = "last_restart_time",
            OwnerId = ObjectId.System,
            Flags = PropertyFlags.Readable,
            Value = new MooValue.Integer(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };
    }

    private async Task RunAutomatedTests() {
        if (_currentPlayerId is null)
            await HandleLoginInput("connect tester tester");

        if (_currentPlayerId is null)
            throw new InvalidOperationException("Automated test run could not log in as Tester.");

        await DispatchCommandAsync(_currentPlayerId.Value, "@test-builtins");
        await DispatchCommandAsync(_currentPlayerId.Value, "@test-scripts");
    }

    private Task DispatchCommandAsync(ObjectId playerId, string input) {
        var command = _parser.Parse(playerId, input);
        _dispatcher.Dispatch(playerId, command);
        return Task.CompletedTask;
    }

    private static (string DatabaseRootPath, bool RequireLogin) LoadConfiguration(IReadOnlyList<string> args) {
        var fallback = Path.Combine(AppContext.BaseDirectory, "data");
        var configName = SelectConfigName(args);
        var configPath = FindAppSettingsPath(configName);

        if (configPath is null)
            return (fallback, false);

        try {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            var root = document.RootElement;

            var configuredPath = TryGetString(root, "MiniMoo", "DatabasePath")
                                 ?? TryGetString(root, "DatabasePath");
            var requireLogin = TryGetBool(root, "MiniMoo", "RequireLogin")
                               ?? TryGetBool(root, "RequireLogin")
                               ?? false;

            if (string.IsNullOrWhiteSpace(configuredPath))
                return (fallback, requireLogin);

            if (Path.IsPathRooted(configuredPath))
                return (Path.GetFullPath(configuredPath), requireLogin);

            var relativeBase = FindSolutionRoot(configPath)
                               ?? FindSolutionRoot(Directory.GetCurrentDirectory())
                               ?? Path.GetDirectoryName(configPath)
                               ?? Directory.GetCurrentDirectory();

            return (Path.GetFullPath(Path.Combine(relativeBase, configuredPath)), requireLogin);
        }
        catch (JsonException ex) {
            throw new InvalidOperationException($"Invalid {configName}: {configPath}", ex);
        }
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

    private static bool? TryGetBool(JsonElement root, params string[] path) {
        var current = root;

        foreach (var segment in path) {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current))
                return null;
        }

        return current.ValueKind == JsonValueKind.True || current.ValueKind == JsonValueKind.False
            ? current.GetBoolean()
            : null;
    }

    private bool TryHandleDatabaseAction(IReadOnlyList<string> args) {
        var action = ParseDatabaseAction(args);
        if (action is null)
            return false;

        switch (action.Value) {
            case DatabaseAction.Clone:
                CloneDatabase();
                _terminal.WriteLine($"Database clone written to {_databaseRootPath}");
                return true;

            default:
                return false;
        }
    }

    private void CloneDatabase() {
        var sourceRoot = GetCloneSourceRoot();
        CloneFolder("core", sourceRoot, _databaseRootPath);
        CloneFolder("world", sourceRoot, _databaseRootPath);
    }

    private string GetCloneSourceRoot() {
        var cloneRoot = new DirectoryInfo(_databaseRootPath);
        var parent = cloneRoot.Parent?.FullName
            ?? throw new DirectoryNotFoundException($"Clone source parent not found: {_databaseRootPath}");

        ValidateFolderExists(Path.Combine(parent, "core"), "Source database folder");
        ValidateFolderExists(Path.Combine(parent, "world"), "Source database folder");
        return parent;
    }

    private static void CloneFolder(string folderName, string sourceRoot, string targetRoot) {
        var source = Path.Combine(sourceRoot, folderName);
        var target = Path.Combine(targetRoot, folderName);

        ValidateFolderExists(source, "Source database folder");
        Directory.CreateDirectory(target);
        ClearDirectory(target);

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.TopDirectoryOnly)) {
            if (!File.Exists(file))
                throw new FileNotFoundException($"Source database file disappeared during clone: {file}", file);

            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }
    }

    private static void ValidateFolderExists(string path, string label) {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"{label} not found: {path}");
    }

    private static void ClearDirectory(string path) {
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly))
            File.Delete(file);

        foreach (var directory in Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly))
            Directory.Delete(directory, recursive: true);
    }

    private bool EnsureDatabaseReady(IReadOnlyList<string> args) {
        if (!IsTestMode(args))
            return true;

        var corePath = Path.Combine(_databaseRootPath, "core");
        var worldPath = Path.Combine(_databaseRootPath, "world");

        if (Directory.Exists(corePath) && Directory.Exists(worldPath))
            return true;

        _terminal.WriteLine("Test clone not found. Run --test clone first.");
        return false;
    }

    private static DatabaseAction? ParseDatabaseAction(IReadOnlyList<string> args) {
        for (var i = 0; i < args.Count; i++) {
            var arg = args[i];

            if (arg.Equals("--settings", StringComparison.OrdinalIgnoreCase)) {
                i++;
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
                continue;

            if (arg.Equals("clone", StringComparison.OrdinalIgnoreCase))
                return DatabaseAction.Clone;
        }

        return null;
    }

    private static bool ShouldAutoRunTests(IReadOnlyList<string> args)
        => IsTestMode(args)
           && args.Any(arg => arg.Equals("run", StringComparison.OrdinalIgnoreCase));

    private static bool IsTestMode(IReadOnlyList<string> args)
        => args.Any(arg => arg.Equals("--test", StringComparison.OrdinalIgnoreCase)
                           || arg.Equals("--tests", StringComparison.OrdinalIgnoreCase));

    private enum DatabaseAction {
        Clone
    }
}
