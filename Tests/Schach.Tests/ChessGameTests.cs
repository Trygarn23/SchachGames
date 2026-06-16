using Schach.Logic;
using Schach.Persistence;
using Schach.AI;

namespace Schach.Tests;

public sealed class ChessGameTests
{
    [Fact]
    public void Reset_CreatesStartPositionAndWhiteToMove()
    {
        ChessGame game = new();

        Assert.Equal(PieceColor.White, game.CurrentTurn);
        Assert.Equal(PieceType.King, game.GetPiece(Pos("e1"))?.Type);
        Assert.Equal(PieceColor.White, game.GetPiece(Pos("e1"))?.Color);
        Assert.Equal(PieceType.Queen, game.GetPiece(Pos("d8"))?.Type);
        Assert.Equal(PieceColor.Black, game.GetPiece(Pos("d8"))?.Color);
        Assert.Equal(PieceType.Pawn, game.GetPiece(Pos("a2"))?.Type);
        Assert.Equal(PieceType.Pawn, game.GetPiece(Pos("h7"))?.Type);
    }

    [Fact]
    public void WhitePawn_CanMoveOneOrTwoSquaresFromStart()
    {
        ChessGame game = new();

        IReadOnlyList<BoardPosition> moves = game.GetLegalMoves(Pos("e2"));

        Assert.Contains(Pos("e3"), moves);
        Assert.Contains(Pos("e4"), moves);
    }

    [Fact]
    public void Knight_CanJumpButCannotCaptureOwnPiece()
    {
        ChessGame game = new();

        IReadOnlyList<BoardPosition> moves = game.GetLegalMoves(Pos("g1"));

        Assert.Contains(Pos("f3"), moves);
        Assert.Contains(Pos("h3"), moves);
        Assert.DoesNotContain(Pos("e2"), moves);
    }

    [Fact]
    public void SlidingPieces_AreBlockedInStartPosition()
    {
        ChessGame game = new();

        Assert.Empty(game.GetLegalMoves(Pos("a1")));
        Assert.Empty(game.GetLegalMoves(Pos("c1")));
        Assert.Empty(game.GetLegalMoves(Pos("d1")));
    }

    [Fact]
    public void Castling_IsAvailableAfterClearingKingSide()
    {
        ChessGame game = new();

        Move(game, "e2", "e4");
        Move(game, "e7", "e5");
        Move(game, "g1", "f3");
        Move(game, "b8", "c6");
        Move(game, "f1", "c4");
        Move(game, "g8", "f6");

        Assert.Contains(Pos("g1"), game.GetLegalMoves(Pos("e1")));
    }

    [Fact]
    public void EnPassant_IsAvailableImmediatelyAfterDoublePawnMove()
    {
        ChessGame game = new();

        Move(game, "e2", "e4");
        Move(game, "a7", "a6");
        Move(game, "e4", "e5");
        Move(game, "d7", "d5");

        Assert.Contains(Pos("d6"), game.GetLegalMoves(Pos("e5")));
    }

    [Fact]
    public void Castling_IsNotAvailableThroughAttackedSquare()
    {
        ChessGame game = new();
        game.LoadFen("4k3/8/8/8/8/5r2/8/R3K2R w KQ - 0 1");

        Assert.DoesNotContain(Pos("g1"), game.GetLegalMoves(Pos("e1")));
    }

    [Fact]
    public void Promotion_CanChooseKnight()
    {
        ChessGame game = new();
        game.LoadFen("7k/P7/8/8/8/8/8/7K w - - 0 1");

        Assert.True(game.TryMove(Pos("a7"), Pos("a8"), PieceType.Knight));

        Assert.Equal(PieceType.Knight, game.GetPiece(Pos("a8"))?.Type);
    }

    [Fact]
    public void FoolMate_EndsWithCheckmate()
    {
        ChessGame game = new();

        Move(game, "f2", "f3");
        Move(game, "e7", "e5");
        Move(game, "g2", "g4");
        Move(game, "d8", "h4");

        Assert.True(game.IsGameOver);
        Assert.Equal(GameEndReason.Checkmate, game.EndReason);
        Assert.Equal(PieceColor.Black, game.Winner);
    }

    [Fact]
    public void StalemateFen_EndsWithStalemate()
    {
        ChessGame game = new();

        game.LoadFen("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1");

        Assert.True(game.IsGameOver);
        Assert.Equal(GameEndReason.Stalemate, game.EndReason);
    }

    [Fact]
    public void FiftyMoveRuleFen_EndsWithDraw()
    {
        ChessGame game = new();

        game.LoadFen("r6k/8/8/8/8/8/8/R6K w - - 100 1");

        Assert.True(game.IsGameOver);
        Assert.Equal(GameEndReason.FiftyMoveRule, game.EndReason);
    }

    [Fact]
    public void InsufficientMaterialFen_EndsWithDraw()
    {
        ChessGame game = new();

        game.LoadFen("8/8/8/8/8/8/8/K6k w - - 0 1");

        Assert.True(game.IsGameOver);
        Assert.Equal(GameEndReason.InsufficientMaterial, game.EndReason);
    }

    [Fact]
    public void RepeatedPosition_EndsWithDraw()
    {
        ChessGame game = new();

        Move(game, "g1", "f3");
        Move(game, "g8", "f6");
        Move(game, "f3", "g1");
        Move(game, "f6", "g8");
        Move(game, "g1", "f3");
        Move(game, "g8", "f6");
        Move(game, "f3", "g1");
        Move(game, "f6", "g8");

        Assert.True(game.IsGameOver);
        Assert.Equal(GameEndReason.ThreefoldRepetition, game.EndReason);
    }

    [Fact]
    public void Fen_RoundTripsStartPosition()
    {
        ChessGame game = new();

        Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", game.ToFen());
    }

    [Fact]
    public void SaveAndLoad_ReplaysMoves()
    {
        ChessGame game = new();
        Move(game, "e2", "e4");
        Move(game, "e7", "e5");
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.schach");

        try
        {
            GameFileService service = new();
            service.Save(game, path);

            ChessGame loaded = new();
            service.Load(loaded, path);

            Assert.Equal(PieceType.Pawn, loaded.GetPiece(Pos("e4"))?.Type);
            Assert.Equal(PieceType.Pawn, loaded.GetPiece(Pos("e5"))?.Type);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void PgnExport_ContainsPlayedMoves()
    {
        ChessGame game = new();
        Move(game, "e2", "e4");
        Move(game, "e7", "e5");

        string pgn = new PgnService().Export(game);

        Assert.Contains("e4", pgn);
        Assert.Contains("e5", pgn);
    }

    [Fact]
    public void PgnImport_ReplaysSanMoves()
    {
        IReadOnlyList<GameFileService.SavedMove> moves = new PgnService().ImportMoves("1. e4 e5 2. Nf3 Nc6 *");

        Assert.Equal(4, moves.Count);
        Assert.Equal("g1", moves[2].From);
        Assert.Equal("f3", moves[2].To);
    }

    [Fact]
    public void Ai_SelectsLegalMoveWithoutCrash()
    {
        ChessGame game = new();
        Move(game, "e2", "e4");

        LegalMove? move = new ChessAi().SelectMove(game, AiDifficulty.Hard);

        Assert.NotNull(move);
        Assert.Contains(move.To, game.GetLegalMoves(move.From));
    }

    [Fact]
    public void Ai_UsesOpeningBookInClassicStartPosition()
    {
        ChessGame game = new();

        LegalMove? move = new ChessAi().SelectMove(game, AiDifficulty.Medium);

        Assert.NotNull(move);
        Assert.Contains($"{move.From.ToAlgebraic()}{move.To.ToAlgebraic()}", new[] { "e2e4", "d2d4", "g1f3", "c2c4" });
    }

    [Fact]
    public void Ai_HighSkillStillSelectsLegalMove()
    {
        ChessGame game = new();
        Move(game, "e2", "e4");
        Move(game, "e7", "e5");
        Move(game, "g1", "f3");

        LegalMove? move = new ChessAi().SelectMove(game, AiDifficulty.Hard, AiPersonality.Tactical, skillLevel: 10);

        Assert.NotNull(move);
        Assert.Contains(move.To, game.GetLegalMoves(move.From));
    }

    [Fact]
    public void Ai_RespectsCancellationToken()
    {
        ChessGame game = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new ChessAi().SelectMove(
                game,
                AiDifficulty.Hard,
                AiPersonality.Balanced,
                skillLevel: 10,
                cancellationToken: cancellation.Token));
    }

    [Fact]
    public void Ai_TinyTimeBudgetStillReturnsLegalMove()
    {
        ChessGame game = new();
        Move(game, "e2", "e4");
        Move(game, "e7", "e5");

        LegalMove? move = new ChessAi().SelectMove(
            game,
            AiDifficulty.Hard,
            AiPersonality.Balanced,
            skillLevel: 10,
            timeBudget: TimeSpan.FromMilliseconds(1));

        Assert.NotNull(move);
        Assert.Contains(move.To, game.GetLegalMoves(move.From));
    }

    [Fact]
    public void Chess960_StartPositionKeepsBackRanksSymmetric()
    {
        ChessGame game = new();

        game.Reset(GameVariant.Chess960);

        PieceType[] whiteBackRank = Enumerable.Range(0, 8)
            .Select(column => game.GetPiece(new BoardPosition(7, column))!.Type)
            .ToArray();
        PieceType[] blackBackRank = Enumerable.Range(0, 8)
            .Select(column => game.GetPiece(new BoardPosition(0, column))!.Type)
            .ToArray();

        Assert.Equal(whiteBackRank, blackBackRank);
        int[] bishops = whiteBackRank
            .Select((piece, column) => (piece, column))
            .Where(item => item.piece == PieceType.Bishop)
            .Select(item => item.column)
            .ToArray();
        Assert.NotEqual(bishops[0] % 2, bishops[1] % 2);

        int king = Array.IndexOf(whiteBackRank, PieceType.King);
        int[] rooks = whiteBackRank
            .Select((piece, column) => (piece, column))
            .Where(item => item.piece == PieceType.Rook)
            .Select(item => item.column)
            .Order()
            .ToArray();
        Assert.True(rooks[0] < king && king < rooks[1]);
    }

    [Fact]
    public void KingOfTheHill_EndsWhenKingStartsOnCenter()
    {
        ChessGame game = new();

        game.LoadFen("8/8/8/8/4K3/8/8/7k w - - 0 1", GameVariant.KingOfTheHill);

        Assert.True(game.IsGameOver);
        Assert.Equal(GameEndReason.KingOfTheHill, game.EndReason);
        Assert.Equal(PieceColor.White, game.Winner);
    }

    [Fact]
    public void GameFileService_PreservesVariantAndChess960StartingFen()
    {
        ChessGame game = new();
        game.Reset(GameVariant.Chess960);
        string startingFen = game.StartingFen;
        LegalMove firstMove = game.GetLegalMovesForCurrentTurn().First();
        Assert.True(game.TryMove(firstMove, out _));
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.schach");

        try
        {
            GameFileService service = new();
            service.Save(game, path);

            ChessGame loaded = new();
            service.Load(loaded, path);

            Assert.Equal(GameVariant.Chess960, loaded.Variant);
            Assert.Equal(startingFen, loaded.StartingFen);
            Assert.Equal(game.ToFen(), loaded.ToFen());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void AnalysisVariation_CanContainSideLines()
    {
        ChessGame game = new();
        Move(game, "e2", "e4");
        MoveRecord move = game.MoveHistory.Single();

        AnalysisVariation variation = new("Hauptvariante", [move], [new AnalysisVariation("Nebenvariante", [])]);

        Assert.Single(variation.Moves);
        Assert.Single(variation.Children);
    }

    [Fact]
    public void PuzzleDefinition_StoresSolutionAndRating()
    {
        PuzzleDefinition puzzle = new(
            "Matt in eins",
            "7k/5Q2/6K1/8/8/8/8/8 w - - 0 1",
            ["f7f8"],
            PieceColor.White,
            "Matt",
            900);

        Assert.Equal("Matt", puzzle.Theme);
        Assert.Equal(900, puzzle.Rating);
        Assert.Single(puzzle.SolutionMoves);
    }

    private static void Move(ChessGame game, string from, string to)
    {
        Assert.True(game.TryMove(Pos(from), Pos(to)), $"{from}-{to} should be legal.");
    }

    private static BoardPosition Pos(string value)
    {
        Assert.True(BoardPosition.TryParse(value, out BoardPosition position), $"{value} should parse.");
        return position;
    }
}
