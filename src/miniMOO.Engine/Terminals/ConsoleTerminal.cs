using System;
using System.Collections.Generic;
using System.Text;

namespace miniMOO.Engine.Terminals;

internal class ConsoleTerminal : ITerminal {
    public void Write(string text) => Console.Write(text);

    public void WriteLine(string text) => Console.WriteLine(text);
}
