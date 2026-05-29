using System.IO;
using System.Text.Json;
using Schach.Logic;

namespace Schach.Persistence;

public sealed class GameFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public void Save(ChessGame game, string path)
    {
        SavedGame savedGame = new(
            game.MoveHistory.Select(move => new SavedMove(move.From.ToAlgebraic(), move.To.ToAlgebraic())).ToList());

        File.WriteAllText(path, JsonSerializer.Serialize(savedGame, JsonOptions));
    }

    public void Load(ChessGame game, string path)
    {
        string json = File.ReadAllText(path);
        SavedGame? savedGame = JsonSerializer.Deserialize<SavedGame>(json, JsonOptions);
        if (savedGame is null)
        {
            throw new InvalidOperationException("Die Spielstand-Datei konnte nicht gelesen werden.");
        }

        ReplayMoves(game, savedGame.Moves);
    }

    public void ReplayMoves(ChessGame game, IReadOnlyList<SavedMove> moves)
    {
        game.Reset();
        foreach (SavedMove move in moves)
        {
            if (!BoardPosition.TryParse(move.From, out BoardPosition from) ||
                !BoardPosition.TryParse(move.To, out BoardPosition to) ||
                !game.TryMove(from, to))
            {
                throw new InvalidOperationException($"Der Zug {move.From}-{move.To} konnte nicht geladen werden.");
            }
        }
    }

    public sealed record SavedGame(IReadOnlyList<SavedMove> Moves);

    public sealed record SavedMove(string From, string To);
}
