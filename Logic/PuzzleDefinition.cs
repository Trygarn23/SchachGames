namespace Schach.Logic;

public sealed record PuzzleDefinition(
    string Title,
    string StartingFen,
    IReadOnlyList<string> SolutionMoves,
    PieceColor SideToMove,
    string? Theme = null,
    int Rating = 800);
