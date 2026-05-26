namespace Schach.Logic;

public sealed class ChessGame
{
    private static readonly (int Row, int Column)[] KnightOffsets =
    [
        (-2, -1), (-2, 1), (-1, -2), (-1, 2),
        (1, -2), (1, 2), (2, -1), (2, 1)
    ];

    private static readonly (int Row, int Column)[] KingOffsets =
    [
        (-1, -1), (-1, 0), (-1, 1),
        (0, -1), (0, 1),
        (1, -1), (1, 0), (1, 1)
    ];

    private static readonly (int Row, int Column)[] BishopDirections =
    [
        (-1, -1), (-1, 1), (1, -1), (1, 1)
    ];

    private static readonly (int Row, int Column)[] RookDirections =
    [
        (-1, 0), (0, 1), (1, 0), (0, -1)
    ];

    private static readonly (int Row, int Column)[] QueenDirections =
    [
        (-1, -1), (-1, 0), (-1, 1),
        (0, -1), (0, 1),
        (1, -1), (1, 0), (1, 1)
    ];

    private readonly ChessPiece?[,] board = new ChessPiece?[8, 8];

    public ChessGame()
    {
        Reset();
    }

    public PieceColor CurrentTurn { get; private set; }

    public void Reset()
    {
        Array.Clear(board);
        CurrentTurn = PieceColor.White;

        PlaceBackRank(0, PieceColor.Black);
        PlacePawns(1, PieceColor.Black);
        PlacePawns(6, PieceColor.White);
        PlaceBackRank(7, PieceColor.White);
    }

    public ChessPiece? GetPiece(BoardPosition position)
    {
        return position.IsValid ? board[position.Row, position.Column] : null;
    }

    public IReadOnlyList<BoardPosition> GetLegalMoves(BoardPosition from)
    {
        ChessPiece? piece = GetPiece(from);
        if (piece is null || piece.Color != CurrentTurn)
        {
            return [];
        }

        return piece.Type switch
        {
            PieceType.Pawn => GetPawnMoves(from, piece.Color),
            PieceType.Knight => GetJumpMoves(from, piece.Color, KnightOffsets),
            PieceType.Bishop => GetSlidingMoves(from, piece.Color, BishopDirections),
            PieceType.Rook => GetSlidingMoves(from, piece.Color, RookDirections),
            PieceType.Queen => GetSlidingMoves(from, piece.Color, QueenDirections),
            PieceType.King => GetJumpMoves(from, piece.Color, KingOffsets),
            _ => []
        };
    }

    public bool TryMove(BoardPosition from, BoardPosition to)
    {
        return TryMove(from, to, out _);
    }

    public bool TryMove(BoardPosition from, BoardPosition to, out MoveResult? result)
    {
        result = null;
        IReadOnlyList<BoardPosition> legalMoves = GetLegalMoves(from);
        if (!legalMoves.Contains(to))
        {
            return false;
        }

        ChessPiece? movedPiece = board[from.Row, from.Column];
        if (movedPiece is null)
        {
            return false;
        }

        ChessPiece? capturedPiece = board[to.Row, to.Column];
        board[to.Row, to.Column] = movedPiece;
        board[from.Row, from.Column] = null;
        CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
        result = new MoveResult(from, to, movedPiece, capturedPiece);
        return true;
    }

    private void PlaceBackRank(int row, PieceColor color)
    {
        PieceType[] pieces =
        [
            PieceType.Rook,
            PieceType.Knight,
            PieceType.Bishop,
            PieceType.Queen,
            PieceType.King,
            PieceType.Bishop,
            PieceType.Knight,
            PieceType.Rook
        ];

        for (int column = 0; column < 8; column++)
        {
            board[row, column] = new ChessPiece(pieces[column], color);
        }
    }

    private void PlacePawns(int row, PieceColor color)
    {
        for (int column = 0; column < 8; column++)
        {
            board[row, column] = new ChessPiece(PieceType.Pawn, color);
        }
    }

    private IReadOnlyList<BoardPosition> GetPawnMoves(BoardPosition from, PieceColor color)
    {
        List<BoardPosition> moves = [];
        int direction = color == PieceColor.White ? -1 : 1;
        int startRow = color == PieceColor.White ? 6 : 1;

        BoardPosition oneStep = new(from.Row + direction, from.Column);
        if (oneStep.IsValid && GetPiece(oneStep) is null)
        {
            moves.Add(oneStep);

            BoardPosition twoSteps = new(from.Row + (direction * 2), from.Column);
            if (from.Row == startRow && twoSteps.IsValid && GetPiece(twoSteps) is null)
            {
                moves.Add(twoSteps);
            }
        }

        AddPawnCapture(moves, new BoardPosition(from.Row + direction, from.Column - 1), color);
        AddPawnCapture(moves, new BoardPosition(from.Row + direction, from.Column + 1), color);
        return moves;
    }

    private void AddPawnCapture(List<BoardPosition> moves, BoardPosition target, PieceColor color)
    {
        ChessPiece? targetPiece = GetPiece(target);
        if (target.IsValid && targetPiece is not null && targetPiece.Color != color)
        {
            moves.Add(target);
        }
    }

    private IReadOnlyList<BoardPosition> GetJumpMoves(
        BoardPosition from,
        PieceColor color,
        IReadOnlyList<(int Row, int Column)> offsets)
    {
        List<BoardPosition> moves = [];
        foreach ((int rowOffset, int columnOffset) in offsets)
        {
            BoardPosition target = new(from.Row + rowOffset, from.Column + columnOffset);
            if (CanOccupy(target, color))
            {
                moves.Add(target);
            }
        }

        return moves;
    }

    private IReadOnlyList<BoardPosition> GetSlidingMoves(
        BoardPosition from,
        PieceColor color,
        IReadOnlyList<(int Row, int Column)> directions)
    {
        List<BoardPosition> moves = [];
        foreach ((int rowDirection, int columnDirection) in directions)
        {
            BoardPosition target = new(from.Row + rowDirection, from.Column + columnDirection);
            while (target.IsValid)
            {
                ChessPiece? targetPiece = GetPiece(target);
                if (targetPiece is null)
                {
                    moves.Add(target);
                }
                else
                {
                    if (targetPiece.Color != color)
                    {
                        moves.Add(target);
                    }

                    break;
                }

                target = new BoardPosition(target.Row + rowDirection, target.Column + columnDirection);
            }
        }

        return moves;
    }

    private bool CanOccupy(BoardPosition position, PieceColor color)
    {
        ChessPiece? piece = GetPiece(position);
        return position.IsValid && (piece is null || piece.Color != color);
    }
}
