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
    Comma,
    Semicolon,
    Colon,
    Dot,

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

    At,

    If, 
    Else,
    ElseIf,
    EndIf,
    Return,
    For,
    In,
    EndFor
}