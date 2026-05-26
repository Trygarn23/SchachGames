namespace Schach.Logic;

public sealed class ChessPiece
{
    public ChessPiece(PieceType type, PieceColor color)
    {
        Type = type;
        Color = color;
    }

    public PieceType Type { get; }

    public PieceColor Color { get; }

    public string Symbol => (Color, Type) switch
    {
        (PieceColor.White, PieceType.King) => "\u2654",
        (PieceColor.White, PieceType.Queen) => "\u2655",
        (PieceColor.White, PieceType.Rook) => "\u2656",
        (PieceColor.White, PieceType.Bishop) => "\u2657",
        (PieceColor.White, PieceType.Knight) => "\u2658",
        (PieceColor.White, PieceType.Pawn) => "\u2659",
        (PieceColor.Black, PieceType.King) => "\u265A",
        (PieceColor.Black, PieceType.Queen) => "\u265B",
        (PieceColor.Black, PieceType.Rook) => "\u265C",
        (PieceColor.Black, PieceType.Bishop) => "\u265D",
        (PieceColor.Black, PieceType.Knight) => "\u265E",
        (PieceColor.Black, PieceType.Pawn) => "\u265F",
        _ => string.Empty
    };
}
