namespace Schach.Logic;

public sealed record LegalMove(
    BoardPosition From,
    BoardPosition To,
    ChessPiece MovedPiece,
    ChessPiece? CapturedPiece,
    MoveKind Kind = MoveKind.Normal,
    PieceType? PromotionType = null,
    BoardPosition? CapturedPosition = null)
{
    public string Notation => Kind switch
    {
        MoveKind.CastlingKingSide => "O-O",
        MoveKind.CastlingQueenSide => "O-O-O",
        MoveKind.Promotion => $"{From.ToAlgebraic()}-{To.ToAlgebraic()}={GetPieceLetter(PromotionType ?? PieceType.Queen)}",
        MoveKind.EnPassant => $"{From.ToAlgebraic()}x{To.ToAlgebraic()} e.p.",
        _ => CapturedPiece is null
            ? $"{From.ToAlgebraic()}-{To.ToAlgebraic()}"
            : $"{From.ToAlgebraic()}x{To.ToAlgebraic()}"
    };

    private static string GetPieceLetter(PieceType type)
    {
        return type switch
        {
            PieceType.Queen => "Q",
            PieceType.Rook => "R",
            PieceType.Bishop => "B",
            PieceType.Knight => "N",
            PieceType.King => "K",
            _ => string.Empty
        };
    }
}
