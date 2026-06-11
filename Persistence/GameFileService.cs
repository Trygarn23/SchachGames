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
        SavedGame savedGame = new()
        {
            Variant = game.Variant.ToString(),
            StartingFen = game.StartingFen,
            Moves = game.MoveHistory
                .Select(move => new SavedMove(move.From.ToAlgebraic(), move.To.ToAlgebraic()))
                .ToList()
        };

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

        GameVariant variant = Enum.TryParse(savedGame.Variant, out GameVariant parsedVariant)
            ? parsedVariant
            : GameVariant.Classic;
        if (!string.IsNullOrWhiteSpace(savedGame.StartingFen))
        {
            game.LoadFen(savedGame.StartingFen, variant);
            ApplyMoves(game, savedGame.Moves);
            return;
        }

        ReplayMoves(game, savedGame.Moves, variant);
    }

    public void ReplayMoves(ChessGame game, IReadOnlyList<SavedMove> moves)
    {
        ReplayMoves(game, moves, GameVariant.Classic);
    }

    public void ReplayMoves(ChessGame game, IReadOnlyList<SavedMove> moves, GameVariant variant)
    {
        game.Reset(variant);
        ApplyMoves(game, moves);
    }

    private static void ApplyMoves(ChessGame game, IReadOnlyList<SavedMove> moves)
    {
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

    public sealed record SavedGame
    {
        public string Variant { get; init; } = GameVariant.Classic.ToString();

        public string StartingFen { get; init; } = string.Empty;

        public IReadOnlyList<SavedMove> Moves { get; init; } = [];
    }

    public sealed record SavedMove(string From, string To);
}
