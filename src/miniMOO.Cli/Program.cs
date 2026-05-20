using miniMOO.Host;

namespace miniMOO.Cli;

internal class Program {
    static void Main(string[] args) {
        GameRunner gr = new GameRunner();
        gr.RunCLI(args);
    }
}
