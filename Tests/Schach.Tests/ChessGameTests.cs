using Schach.Logic;

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
