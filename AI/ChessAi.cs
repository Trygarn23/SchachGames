using Schach.Logic;

namespace Schach.AI;

public sealed class ChessAi
{
    private readonly Random random = new();

    public LegalMove? SelectMove(ChessGame game, AiDifficulty difficulty)
    {
        IReadOnlyList<LegalMove> moves = game.GetLegalMovesForCurrentTurn();
        if (moves.Count == 0)
        {
            return null;
        }

        return difficulty switch
        {
            AiDifficulty.Easy => SelectRandomMove(moves),
            AiDifficulty.Medium => SelectBestMove(moves, ScoreMediumMove),
            AiDifficulty.Hard => SelectBestMove(moves, move => ScoreHardMove(game, move)),
            _ => SelectRandomMove(moves)
        };
    }

    private LegalMove SelectRandomMove(IReadOnlyList<LegalMove> moves)
    {
        return moves[random.Next(moves.Count)];
    }

    private LegalMove SelectBestMove(
        IReadOnlyList<LegalMove> moves,
        Func<LegalMove, int> scoreMove)
    {
        int bestScore = int.MinValue;
        List<LegalMove> bestMoves = [];

        foreach (LegalMove move in moves)
        {
            int score = scoreMove(move);
            if (score > bestScore)
            {
                bestScore = score;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (score == bestScore)
            {
                bestMoves.Add(move);
            }
        }

        return SelectRandomMove(bestMoves);
    }

    private int ScoreMediumMove(LegalMove move)
    {
        int score = 0;

        if (move.CapturedPiece is not null)
        {
            score += GetPieceValue(move.CapturedPiece.Type) * 10;
            score -= GetPieceValue(move.MovedPiece.Type);
        }

        if (move.Kind == MoveKind.Promotion)
        {
            score += GetPieceValue(move.PromotionType ?? PieceType.Queen);
        }

        if (move.Kind is MoveKind.CastlingKingSide or MoveKind.CastlingQueenSide)
        {
            score += 80;
        }

        if (move.Kind == MoveKind.EnPassant)
        {
            score += 40;
        }

        score += GetCenterBonus(move.To);
        score += GetPawnProgressBonus(move);
        return score;
    }

    private int ScoreHardMove(ChessGame game, LegalMove move)
    {
        ChessGame simulation = game.Clone();
        if (!simulation.TryMove(move.From, move.To))
        {
            return int.MinValue;
        }

        int score = ScoreMediumMove(move);
        score += GetMaterialBalance(simulation, move.MovedPiece.Color);
        score -= GetBestOpponentCaptureValue(simulation) * 8;
        return score;
    }

    private int GetBestOpponentCaptureValue(ChessGame simulation)
    {
        int bestCapture = 0;
        foreach (LegalMove opponentMove in simulation.GetLegalMovesForCurrentTurn())
        {
            if (opponentMove.CapturedPiece is not null)
            {
                bestCapture = Math.Max(bestCapture, GetPieceValue(opponentMove.CapturedPiece.Type));
            }
        }

        return bestCapture;
    }

    private int GetMaterialBalance(ChessGame game, PieceColor side)
    {
        int balance = 0;

        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                ChessPiece? piece = game.GetPiece(new BoardPosition(row, column));
                if (piece is null)
                {
                    continue;
                }

                int value = GetPieceValue(piece.Type);
                balance += piece.Color == side ? value : -value;
            }
        }

        return balance;
    }

    private static int GetCenterBonus(BoardPosition position)
    {
        int rowDistance = Math.Abs(3 - position.Row);
        int columnDistance = Math.Abs(3 - position.Column);
        return Math.Max(0, 6 - rowDistance - columnDistance);
    }

    private static int GetPawnProgressBonus(LegalMove move)
    {
        if (move.MovedPiece.Type != PieceType.Pawn)
        {
            return 0;
        }

        return move.MovedPiece.Color == PieceColor.White
            ? 6 - move.To.Row
            : move.To.Row - 1;
    }

    private static int GetPieceValue(PieceType type)
    {
        return type switch
        {
            PieceType.Pawn => 100,
            PieceType.Knight => 320,
            PieceType.Bishop => 330,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            PieceType.King => 20_000,
            _ => 0
        };
    }
}
