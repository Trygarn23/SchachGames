namespace Schach.Logic;

public sealed record MoveRecord(
    int MoveNumber,
    PieceColor Color,
    BoardPosition From,
    BoardPosition To,
    string Notation,
    MoveKind Kind,
    ChessPiece MovedPiece,
    ChessPiece? CapturedPiece);
