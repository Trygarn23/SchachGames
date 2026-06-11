using Schach.Logic;

namespace Schach.AI;

public sealed class ChessAi
{
    private readonly OpeningBook openingBook = new();
    private readonly Random random = new();

    public LegalMove? SelectMove(ChessGame game, AiDifficulty difficulty, AiPersonality personality = AiPersonality.Balanced)
    {
        IReadOnlyList<LegalMove> moves = game.GetLegalMovesForCurrentTurn();
        if (moves.Count == 0)
        {
            return null;
        }

        if (game.Variant == GameVariant.Classic && difficulty != AiDifficulty.Easy)
        {
            LegalMove? bookMove = openingBook.SelectMove(game, random);
            if (bookMove is not null)
            {
                return bookMove;
            }
        }

        return difficulty switch
        {
            AiDifficulty.Easy => SelectRandomMove(moves),
            AiDifficulty.Medium => SelectBestMove(moves, move => ScoreMediumMove(move) + ScorePersonalityMove(game, move, personality)),
            AiDifficulty.Hard => SelectBestMove(moves, move => ScoreHardMove(game, move, depth: 3) + ScorePersonalityMove(game, move, personality)),
            _ => SelectRandomMove(moves)
        };
    }

    public IReadOnlyList<string> GetCandidateSummaries(
        ChessGame game,
        AiDifficulty difficulty,
        AiPersonality personality = AiPersonality.Balanced,
        int maxCount = 3)
    {
        return game.GetLegalMovesForCurrentTurn()
            .Select(move => new
            {
                Move = move,
                Score = (difficulty == AiDifficulty.Hard ? ScoreHardMove(game, move, 2) : ScoreMediumMove(move)) +
                    ScorePersonalityMove(game, move, personality)
            })
            .OrderByDescending(candidate => candidate.Score)
            .Take(maxCount)
            .Select(candidate => $"{candidate.Move.Notation}: {candidate.Score}")
            .ToList();
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

    private int ScorePersonalityMove(ChessGame game, LegalMove move, AiPersonality personality)
    {
        int score = 0;
        ChessGame simulation = game.Clone();
        bool canSimulate = simulation.TryMove(move, out _);
        PieceColor mover = move.MovedPiece.Color;
        PieceColor opponent = mover == PieceColor.White ? PieceColor.Black : PieceColor.White;

        switch (personality)
        {
            case AiPersonality.Aggressive:
                score += move.CapturedPiece is null ? 0 : GetPieceValue(move.CapturedPiece.Type) / 2;
                score += canSimulate && simulation.IsInCheck(opponent) ? 180 : 0;
                score += GetPawnProgressBonus(move) * 5;
                break;
            case AiPersonality.Defensive:
                score += move.Kind is MoveKind.CastlingKingSide or MoveKind.CastlingQueenSide ? 220 : 0;
                score += canSimulate && !simulation.IsInCheck(mover) ? 50 : 0;
                score -= GetBestOpponentCaptureValue(simulation);
                break;
            case AiPersonality.Tactical:
                score += canSimulate && simulation.IsInCheck(opponent) ? 240 : 0;
                score += move.Kind == MoveKind.Promotion ? 300 : 0;
                score += GetCenterBonus(move.To) * 12;
                break;
            case AiPersonality.Chaotic:
                score += random.Next(-260, 261);
                score += move.CapturedPiece is null ? 70 : -40;
                break;
        }

        return score;
    }

    private int ScoreHardMove(ChessGame game, LegalMove move, int depth)
    {
        ChessGame simulation = game.Clone();
        if (!simulation.TryMove(move, out _))
        {
            return int.MinValue;
        }

        return Minimax(
            simulation,
            depth - 1,
            move.MovedPiece.Color,
            int.MinValue + 1,
            int.MaxValue - 1,
            maximizing: false);
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

    private int Minimax(
        ChessGame game,
        int depth,
        PieceColor aiSide,
        int alpha,
        int beta,
        bool maximizing)
    {
        if (depth == 0 || game.IsGameOver)
        {
            return EvaluatePosition(game, aiSide);
        }

        IReadOnlyList<LegalMove> moves = game.GetLegalMovesForCurrentTurn();
        if (moves.Count == 0)
        {
            return EvaluatePosition(game, aiSide);
        }

        if (maximizing)
        {
            int best = int.MinValue + 1;
            foreach (LegalMove move in moves)
            {
                ChessGame simulation = game.Clone();
                simulation.TryMove(move, out _);
                best = Math.Max(best, Minimax(simulation, depth - 1, aiSide, alpha, beta, maximizing: false));
                alpha = Math.Max(alpha, best);
                if (beta <= alpha)
                {
                    break;
                }
            }

            return best;
        }

        int worst = int.MaxValue - 1;
        foreach (LegalMove move in moves)
        {
            ChessGame simulation = game.Clone();
            simulation.TryMove(move, out _);
            worst = Math.Min(worst, Minimax(simulation, depth - 1, aiSide, alpha, beta, maximizing: true));
            beta = Math.Min(beta, worst);
            if (beta <= alpha)
            {
                break;
            }
        }

        return worst;
    }

    private int EvaluatePosition(ChessGame game, PieceColor aiSide)
    {
        int score = GetMaterialBalance(game, aiSide);
        score += game.GetLegalMovesForCurrentTurn().Count * (game.CurrentTurn == aiSide ? 4 : -4);
        if (game.IsInCheck(aiSide))
        {
            score -= 120;
        }

        PieceColor opponent = aiSide == PieceColor.White ? PieceColor.Black : PieceColor.White;
        if (game.IsInCheck(opponent))
        {
            score += 120;
        }

        if (game.IsGameOver)
        {
            if (game.Winner == aiSide)
            {
                score += 100_000;
            }
            else if (game.Winner == opponent)
            {
                score -= 100_000;
            }
        }

        return score;
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
