namespace miniMOO.Script.Lexing;

public enum TokenKind {
    EndOfFile,
    Identifier,
    String,
    Integer,
    ObjectId,

    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Comma,
    Semicolon,
    Colon,
    Dot,
    DotDot,
    Dollar,
    DollarIdentifier,  // $name — shorthand

    Plus,
    Minus,
    Star,
    Slash,
    Percent,

    Equal,
    EqualEqual,
    Bang,
    BangEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    AmpAmp,
    Pipe,
    PipePipe,

    At,
    Question,

    If, 
    Else,
    ElseIf,
    EndIf,
    Return,
    For,
    In,
    EndFor,
    While,
    EndWhile,

    Try,
    Except,
    EndTry,
    Backtick,
    Apostrophe,
    FatArrow,
}
