using System.Text;
using System.Text.RegularExpressions;
using Schach.Logic;

namespace Schach.Persistence;

public sealed partial class PgnService
{
    public string Export(ChessGame game)
    {
        StringBuilder builder = new();
        builder.AppendLine("[Event \"Casual Game\"]");
        builder.AppendLine("[Site \"Schach\"]");
        builder.AppendLine("[Result \"*\"]");
        builder.AppendLine();

        foreach (IGrouping<int, MoveRecord> group in game.MoveHistory.GroupBy(move => move.MoveNumber))
        {
            builder.Append($"{group.Key}. ");
            MoveRecord? white = group.FirstOrDefault(move => move.Color == PieceColor.White);
            MoveRecord? black = group.FirstOrDefault(move => move.Color == PieceColor.Black);
            if (white is not null)
            {
                builder.Append($"{ToSan(white)} ");
            }

            if (black is not null)
            {
                builder.Append($"{ToSan(black)} ");
            }
        }

        builder.Append('*');
        return builder.ToString();
    }

    public IReadOnlyList<GameFileService.SavedMove> ImportMoves(string pgn)
    {
        string withoutHeaders = HeaderRegex().Replace(pgn, " ");
        string withoutComments = CommentRegex().Replace(withoutHeaders, " ");
        string[] tokens = withoutComments
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !token.EndsWith('.') && token is not "*" and not "1-0" and not "0-1" and not "1/2-1/2")
            .ToArray();

        ChessGame game = new();
        List<GameFileService.SavedMove> moves = [];
        foreach (string token in tokens)
        {
            string cleaned = token.Trim().TrimEnd('+', '#', '=');
            LegalMove move = ResolveMove(game, cleaned);
            moves.Add(new GameFileService.SavedMove(move.From.ToAlgebraic(), move.To.ToAlgebraic()));
            if (!game.TryMove(move, out _))
            {
                throw new InvalidOperationException($"PGN-Zug konnte nicht gespielt werden: {token}");
            }
        }

        return moves;
    }

    private static LegalMove ResolveMove(ChessGame game, string token)
    {
        IReadOnlyList<LegalMove> legalMoves = game.GetLegalMovesForCurrentTurn();
        MatchCollection coordinateMatches = SquareRegex().Matches(token);
        if (coordinateMatches.Count >= 2)
        {
            BoardPosition.TryParse(coordinateMatches[0].Value, out BoardPosition from);
            BoardPosition.TryParse(coordinateMatches[1].Value, out BoardPosition to);
            LegalMove? coordinateMove = legalMoves.FirstOrDefault(move => move.From == from && move.To == to);
            if (coordinateMove is not null)
            {
                return coordinateMove;
            }
        }

        string normalized = token
            .Replace("e.p.", string.Empty, StringComparison.Ordinal)
            .Trim();
        return legalMoves.FirstOrDefault(move => ToSan(move).TrimEnd('+', '#', '=') == normalized) ??
            throw new InvalidOperationException($"PGN-Zug ist nicht eindeutig oder ungueltig: {token}");
    }

    private static string ToSan(MoveRecord move)
    {
        string suffix = move.Notation.EndsWith('+') || move.Notation.EndsWith('#') || move.Notation.EndsWith('=')
            ? move.Notation[^1].ToString()
            : string.Empty;

        return move.Kind switch
        {
            MoveKind.CastlingKingSide => $"O-O{suffix}",
            MoveKind.CastlingQueenSide => $"O-O-O{suffix}",
            MoveKind.Promotion => $"{move.To.ToAlgebraic()}={GetPieceLetter(move.MovedPiece.Type)}{suffix}",
            _ when move.MovedPiece.Type == PieceType.Pawn && move.CapturedPiece is not null =>
                $"{move.From.ToAlgebraic()[0]}x{move.To.ToAlgebraic()}{suffix}",
            _ when move.MovedPiece.Type == PieceType.Pawn => $"{move.To.ToAlgebraic()}{suffix}",
            _ => $"{GetPieceLetter(move.MovedPiece.Type)}{(move.CapturedPiece is null ? string.Empty : "x")}{move.To.ToAlgebraic()}{suffix}"
        };
    }

    private static string ToSan(LegalMove move)
    {
        return move.Kind switch
        {
            MoveKind.CastlingKingSide => "O-O",
            MoveKind.CastlingQueenSide => "O-O-O",
            MoveKind.Promotion => $"{move.To.ToAlgebraic()}={GetPieceLetter(move.PromotionType ?? PieceType.Queen)}",
            _ when move.MovedPiece.Type == PieceType.Pawn && move.CapturedPiece is not null =>
                $"{move.From.ToAlgebraic()[0]}x{move.To.ToAlgebraic()}",
            _ when move.MovedPiece.Type == PieceType.Pawn => move.To.ToAlgebraic(),
            _ => $"{GetPieceLetter(move.MovedPiece.Type)}{(move.CapturedPiece is null ? string.Empty : "x")}{move.To.ToAlgebraic()}"
        };
    }

    private static string GetPieceLetter(PieceType type)
    {
        return type switch
        {
            PieceType.King => "K",
            PieceType.Queen => "Q",
            PieceType.Rook => "R",
            PieceType.Bishop => "B",
            PieceType.Knight => "N",
            _ => string.Empty
        };
    }

    [GeneratedRegex(@"\[[^\]]+\]")]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"\{[^}]*\}")]
    private static partial Regex CommentRegex();

    [GeneratedRegex("[a-h][1-8]", RegexOptions.IgnoreCase)]
    private static partial Regex SquareRegex();
}
