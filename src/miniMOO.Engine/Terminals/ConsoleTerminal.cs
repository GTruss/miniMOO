namespace miniMOO.Engine.Terminals;

public class ConsoleTerminal : ITerminal {
    public void Write(string text) => Console.Write(text);

    public void WriteLine(string text) => Console.WriteLine(text);
}
