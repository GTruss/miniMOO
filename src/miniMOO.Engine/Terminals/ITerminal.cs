using System;
using System.Collections.Generic;
using System.Text;

namespace miniMOO.Engine.Terminals;

public interface ITerminal {
    void Write(string text);
    void WriteLine(string text);
}
