namespace miniMOO.Core.ScriptRuntime;
 
public static class MooErrorCode {
    public const int E_NONE = 0;
    public const int E_TYPE = 1;
    public const int E_DIV = 2;
    public const int E_PERM = 3;
    public const int E_PROPNF = 4;
    public const int E_VERBNF = 5;
    public const int E_VARNF = 6;
    public const int E_INVIND = 7;
    public const int E_RECMOVE = 8;
    public const int E_MAXREC = 9;
    public const int E_RANGE = 10;
    public const int E_ARGS = 11;
    public const int E_NACC = 12;
    public const int E_INVARG = 13;
    public const int E_QUOTA = 14;
    public const int E_FLOAT = 15;

    public const int Any = -1;  // miniMOO catch-all sentinel for try/except
}