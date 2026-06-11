namespace Schach.Logic;

public sealed record AnalysisVariation(
    string Name,
    IReadOnlyList<MoveRecord> Moves,
    IReadOnlyList<AnalysisVariation>? Children = null)
{
    public IReadOnlyList<AnalysisVariation> Children { get; init; } = Children ?? [];
}
