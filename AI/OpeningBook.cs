using System.IO;
using System.Text.Json;
using Schach.Logic;

namespace Schach.AI;

public sealed class OpeningBook
{
    private readonly Dictionary<string, string[]> lines;

    public OpeningBook()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "AI", "opening-book.json");
        lines = LoadLines(path);
    }

    public LegalMove? SelectMove(ChessGame game, Random random)
    {
        string key = string.Join(' ', game.MoveHistory.Select(move => $"{move.From.ToAlgebraic()}{move.To.ToAlgebraic()}"));
        if (!lines.TryGetValue(key, out string[]? candidates) || candidates.Length == 0)
        {
            return null;
        }

        IReadOnlyList<LegalMove> legalMoves = game.GetLegalMovesForCurrentTurn();
        List<LegalMove> bookMoves = [];
        foreach (string candidate in candidates.OrderBy(_ => random.Next()))
        {
            LegalMove? move = legalMoves.FirstOrDefault(move =>
                $"{move.From.ToAlgebraic()}{move.To.ToAlgebraic()}".Equals(candidate, StringComparison.OrdinalIgnoreCase));
            if (move is not null)
            {
                bookMoves.Add(move);
            }
        }

        return bookMoves.Count == 0 ? null : bookMoves[random.Next(bookMoves.Count)];
    }

    private static Dictionary<string, string[]> LoadLines(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(path)) ?? CreateFallbackLines();
            }
        }
        catch (JsonException)
        {
            return CreateFallbackLines();
        }

        return CreateFallbackLines();
    }

    private static Dictionary<string, string[]> CreateFallbackLines()
    {
        return new Dictionary<string, string[]>
        {
            [""] = ["e2e4", "d2d4", "g1f3", "c2c4"],
            ["e2e4"] = ["e7e5", "c7c5", "e7e6"],
            ["d2d4"] = ["d7d5", "g8f6"],
            ["g1f3"] = ["d7d5", "g8f6"],
            ["c2c4"] = ["e7e5", "g8f6"],
            ["e2e4 e7e5"] = ["g1f3", "f1c4"],
            ["e2e4 c7c5"] = ["g1f3", "d2d4"],
            ["d2d4 d7d5"] = ["c2c4", "g1f3"]
        };
    }
}
