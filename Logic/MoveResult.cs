namespace Schach.Logic;

public sealed record MoveResult(
    BoardPosition From,
    BoardPosition To,
    ChessPiece MovedPiece,
    ChessPiece? CapturedPiece)
{
    public string Notation => $"{From.ToAlgebraic()} - {To.ToAlgebraic()}";
}
