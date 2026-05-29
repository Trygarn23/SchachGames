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
                builder.Append($"{white.Notation} ");
            }

            if (black is not null)
            {
                builder.Append($"{black.Notation} ");
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

        List<GameFileService.SavedMove> moves = [];
        PieceColor side = PieceColor.White;
        foreach (string token in tokens)
        {
            string cleaned = token.Trim().TrimEnd('+', '#');
            if (cleaned is "O-O" or "0-0")
            {
                moves.Add(CreateCastlingMove(side, kingside: true));
            }
            else if (cleaned is "O-O-O" or "0-0-0")
            {
                moves.Add(CreateCastlingMove(side, kingside: false));
            }
            else
            {
                MatchCollection matches = SquareRegex().Matches(cleaned);
                if (matches.Count >= 2)
                {
                    moves.Add(new GameFileService.SavedMove(matches[0].Value, matches[1].Value));
                }
            }

            side = side == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        return moves;
    }

    private static GameFileService.SavedMove CreateCastlingMove(PieceColor side, bool kingside)
    {
        string from = side == PieceColor.White ? "e1" : "e8";
        string to = (side, kingside) switch
        {
            (PieceColor.White, true) => "g1",
            (PieceColor.White, false) => "c1",
            (PieceColor.Black, true) => "g8",
            _ => "c8"
        };

        return new GameFileService.SavedMove(from, to);
    }

    [GeneratedRegex(@"\[[^\]]+\]")]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"\{[^}]*\}")]
    private static partial Regex CommentRegex();

    [GeneratedRegex("[a-h][1-8]", RegexOptions.IgnoreCase)]
    private static partial Regex SquareRegex();
}
