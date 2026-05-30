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
    private readonly List<MoveRecord> moveHistory = [];
    private readonly Dictionary<string, int> positionOccurrences = [];
    private BoardPosition? enPassantTarget;
    private int fullMoveNumber;
    private int halfMoveClock;
    private bool blackKingMoved;
    private bool blackKingSideRookMoved;
    private bool blackQueenSideRookMoved;
    private bool whiteKingMoved;
    private bool whiteKingSideRookMoved;
    private bool whiteQueenSideRookMoved;

    public ChessGame()
    {
        Reset();
    }

    public PieceColor CurrentTurn { get; private set; }

    public bool IsGameOver { get; private set; }

    public GameEndReason EndReason { get; private set; }

    public PieceColor? Winner { get; private set; }

    public IReadOnlyList<MoveRecord> MoveHistory => moveHistory;

    public int FullMoveNumber => fullMoveNumber;

    public int HalfMoveClock => halfMoveClock;

    public void Reset()
    {
        Array.Clear(board);
        moveHistory.Clear();
        positionOccurrences.Clear();
        CurrentTurn = PieceColor.White;
        IsGameOver = false;
        EndReason = GameEndReason.None;
        Winner = null;
        enPassantTarget = null;
        fullMoveNumber = 1;
        halfMoveClock = 0;
        whiteKingMoved = false;
        blackKingMoved = false;
        whiteKingSideRookMoved = false;
        whiteQueenSideRookMoved = false;
        blackKingSideRookMoved = false;
        blackQueenSideRookMoved = false;

        PlaceBackRank(0, PieceColor.Black);
        PlacePawns(1, PieceColor.Black);
        PlacePawns(6, PieceColor.White);
        PlaceBackRank(7, PieceColor.White);
        RecordCurrentPosition();
    }

    public ChessPiece? GetPiece(BoardPosition position)
    {
        return position.IsValid ? board[position.Row, position.Column] : null;
    }

    public IReadOnlyList<BoardPosition> GetLegalMoves(BoardPosition from)
    {
        return GetLegalMovesFrom(from).Select(move => move.To).ToList();
    }

    public IReadOnlyList<LegalMove> GetLegalMovesFrom(BoardPosition from)
    {
        ChessPiece? piece = GetPiece(from);
        if (IsGameOver || piece is null || piece.Color != CurrentTurn)
        {
            return [];
        }

        List<LegalMove> legalMoves = [];
        foreach (LegalMove move in GetPseudoMoves(from, piece))
        {
            if (move.CapturedPiece?.Type == PieceType.King)
            {
                continue;
            }

            if (!WouldLeaveKingInCheck(move))
            {
                legalMoves.Add(move);
            }
        }

        return legalMoves;
    }

    public IReadOnlyList<LegalMove> GetLegalMovesForCurrentTurn()
    {
        List<LegalMove> moves = [];

        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                BoardPosition from = new(row, column);
                ChessPiece? movedPiece = GetPiece(from);
                if (movedPiece is null || movedPiece.Color != CurrentTurn)
                {
                    continue;
                }

                moves.AddRange(GetLegalMovesFrom(from));
            }
        }

        return moves;
    }

    public bool IsInCheck(PieceColor color)
    {
        BoardPosition? kingPosition = FindKing(color);
        return kingPosition is not null && IsSquareAttacked(kingPosition.Value, Opposite(color));
    }

    public BoardPosition? GetKingPosition(PieceColor color)
    {
        return FindKing(color);
    }

    public string ToFen()
    {
        string boardPart = string.Join("/", Enumerable.Range(0, 8).Select(row =>
        {
            int empty = 0;
            List<string> parts = [];
            for (int column = 0; column < 8; column++)
            {
                ChessPiece? piece = board[row, column];
                if (piece is null)
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    parts.Add(empty.ToString());
                    empty = 0;
                }

                parts.Add(GetFenPieceChar(piece).ToString());
            }

            if (empty > 0)
            {
                parts.Add(empty.ToString());
            }

            return string.Concat(parts);
        }));

        string activeColor = CurrentTurn == PieceColor.White ? "w" : "b";
        string castling = GetFenCastlingRights();
        string enPassant = enPassantTarget?.ToAlgebraic() ?? "-";
        return $"{boardPart} {activeColor} {castling} {enPassant} {halfMoveClock} {fullMoveNumber}";
    }

    public void LoadFen(string fen)
    {
        string[] parts = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            throw new InvalidOperationException("FEN benoetigt mindestens Brett, Zugfarbe, Rochaderechte und En-passant-Feld.");
        }

        Array.Clear(board);
        moveHistory.Clear();
        positionOccurrences.Clear();

        string[] ranks = parts[0].Split('/');
        if (ranks.Length != 8)
        {
            throw new InvalidOperationException("FEN-Brett muss acht Reihen enthalten.");
        }

        for (int row = 0; row < 8; row++)
        {
            int column = 0;
            foreach (char token in ranks[row])
            {
                if (char.IsDigit(token))
                {
                    column += token - '0';
                    continue;
                }

                if (column >= 8)
                {
                    throw new InvalidOperationException("FEN-Reihe ist zu lang.");
                }

                board[row, column] = ParseFenPiece(token);
                column++;
            }

            if (column != 8)
            {
                throw new InvalidOperationException("FEN-Reihe ist unvollstaendig.");
            }
        }

        CurrentTurn = parts[1] switch
        {
            "w" => PieceColor.White,
            "b" => PieceColor.Black,
            _ => throw new InvalidOperationException("FEN-Zugfarbe muss w oder b sein.")
        };

        SetCastlingRightsFromFen(parts[2]);
        enPassantTarget = parts[3] == "-"
            ? null
            : BoardPosition.TryParse(parts[3], out BoardPosition target)
                ? target
                : throw new InvalidOperationException("FEN-En-passant-Feld ist ungueltig.");
        halfMoveClock = parts.Length > 4 && int.TryParse(parts[4], out int halfMoves) ? halfMoves : 0;
        fullMoveNumber = parts.Length > 5 && int.TryParse(parts[5], out int fullMoves) ? Math.Max(1, fullMoves) : 1;
        IsGameOver = false;
        EndReason = GameEndReason.None;
        Winner = null;
        RecordCurrentPosition();
        UpdateGameState();
    }

    public string GetStatusText(string prefix)
    {
        if (IsGameOver)
        {
            return EndReason switch
            {
                GameEndReason.Checkmate => $"{prefix}\nSchachmatt. {GetColorName(Winner!.Value)} gewinnt.",
                GameEndReason.Stalemate => $"{prefix}\nPatt. Das Spiel endet remis.",
                GameEndReason.FiftyMoveRule => $"{prefix}\nRemis nach 50-Zuege-Regel.",
                GameEndReason.ThreefoldRepetition => $"{prefix}\nRemis durch dreifache Stellungswiederholung.",
                GameEndReason.InsufficientMaterial => $"{prefix}\nRemis durch unzureichendes Material.",
                GameEndReason.Resignation => $"{prefix}\n{GetColorName(Winner!.Value)} gewinnt durch Aufgabe.",
                GameEndReason.DrawAgreement => $"{prefix}\nRemis durch Einigung.",
                GameEndReason.Timeout => $"{prefix}\n{GetColorName(Winner!.Value)} gewinnt auf Zeit.",
                _ => prefix
            };
        }

        string turn = GetColorName(CurrentTurn);
        return IsInCheck(CurrentTurn)
            ? $"{prefix}\nSchach gegen {turn}. {turn} muss den Koenig schuetzen."
            : $"{prefix}\n{turn} am Zug";
    }

    public ChessGame Clone()
    {
        ChessGame clone = new();
        Array.Clear(clone.board);
        clone.moveHistory.Clear();
        clone.positionOccurrences.Clear();

        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                ChessPiece? piece = board[row, column];
                if (piece is not null)
                {
                    clone.board[row, column] = new ChessPiece(piece.Type, piece.Color);
                }
            }
        }

        clone.CurrentTurn = CurrentTurn;
        clone.IsGameOver = IsGameOver;
        clone.EndReason = EndReason;
        clone.Winner = Winner;
        clone.enPassantTarget = enPassantTarget;
        clone.fullMoveNumber = fullMoveNumber;
        clone.halfMoveClock = halfMoveClock;
        clone.whiteKingMoved = whiteKingMoved;
        clone.blackKingMoved = blackKingMoved;
        clone.whiteKingSideRookMoved = whiteKingSideRookMoved;
        clone.whiteQueenSideRookMoved = whiteQueenSideRookMoved;
        clone.blackKingSideRookMoved = blackKingSideRookMoved;
        clone.blackQueenSideRookMoved = blackQueenSideRookMoved;
        clone.moveHistory.AddRange(moveHistory);
        foreach ((string key, int count) in positionOccurrences)
        {
            clone.positionOccurrences[key] = count;
        }

        return clone;
    }

    public bool TryMove(BoardPosition from, BoardPosition to)
    {
        return TryMove(from, to, out _);
    }

    public bool TryMove(BoardPosition from, BoardPosition to, PieceType promotionType)
    {
        return TryMove(from, to, promotionType, out _);
    }

    public bool TryMove(BoardPosition from, BoardPosition to, out MoveResult? result)
    {
        return TryMove(from, to, PieceType.Queen, out result);
    }

    public bool TryMove(BoardPosition from, BoardPosition to, PieceType promotionType, out MoveResult? result)
    {
        result = null;
        if (IsGameOver)
        {
            return false;
        }

        LegalMove? move = GetLegalMovesFrom(from)
            .FirstOrDefault(candidate => candidate.To == to &&
                (candidate.PromotionType is null || candidate.PromotionType == promotionType));
        if (move is null)
        {
            return false;
        }

        result = ApplyMove(move);
        return true;
    }

    public bool TryMove(LegalMove move, out MoveResult? result)
    {
        return TryMove(move.From, move.To, move.PromotionType ?? PieceType.Queen, out result);
    }

    public void Resign(PieceColor color)
    {
        IsGameOver = true;
        EndReason = GameEndReason.Resignation;
        Winner = Opposite(color);
    }

    public void AcceptDraw()
    {
        IsGameOver = true;
        EndReason = GameEndReason.DrawAgreement;
        Winner = null;
    }

    public void Timeout(PieceColor color)
    {
        IsGameOver = true;
        EndReason = GameEndReason.Timeout;
        Winner = Opposite(color);
    }

    private MoveResult ApplyMove(LegalMove move)
    {
        ChessPiece movedPiece = move.MovedPiece;
        ChessPiece? capturedPiece = move.CapturedPiece;
        BoardPosition capturedPosition = move.CapturedPosition ?? move.To;

        board[move.From.Row, move.From.Column] = null;
        if (move.Kind == MoveKind.EnPassant)
        {
            board[capturedPosition.Row, capturedPosition.Column] = null;
        }

        ChessPiece pieceToPlace = move.Kind == MoveKind.Promotion
            ? new ChessPiece(move.PromotionType ?? PieceType.Queen, movedPiece.Color)
            : movedPiece;

        board[move.To.Row, move.To.Column] = pieceToPlace;

        if (move.Kind is MoveKind.CastlingKingSide or MoveKind.CastlingQueenSide)
        {
            MoveCastlingRook(move);
        }

        UpdateCastlingRights(move);
        halfMoveClock = movedPiece.Type == PieceType.Pawn || capturedPiece is not null
            ? 0
            : halfMoveClock + 1;
        enPassantTarget = GetNewEnPassantTarget(move);

        int moveNumber = fullMoveNumber;
        MoveResult result = new(
            moveNumber,
            move.From,
            move.To,
            pieceToPlace,
            capturedPiece,
            move.Kind,
            move.PromotionType);

        string notation = GetMoveNotation(result);
        moveHistory.Add(new MoveRecord(
            moveNumber,
            movedPiece.Color,
            move.From,
            move.To,
            notation,
            move.Kind,
            pieceToPlace,
            capturedPiece));

        if (movedPiece.Color == PieceColor.Black)
        {
            fullMoveNumber++;
        }

        CurrentTurn = Opposite(CurrentTurn);
        RecordCurrentPosition();
        UpdateGameState();
        return result with { };
    }

    private string GetMoveNotation(MoveResult result)
    {
        string notation = result.Notation;
        PieceColor nextTurn = Opposite(result.MovedPiece.Color);
        if (WouldGameEndAfterCurrentPosition(nextTurn, out GameEndReason reason))
        {


            return reason == GameEndReason.Checkmate ? $"{notation}#" : $"{notation}=";
        }

        return IsInCheck(nextTurn) ? $"{notation}+" : notation;
    }

    private bool WouldGameEndAfterCurrentPosition(PieceColor sideToMove, out GameEndReason reason)
    {
        PieceColor originalTurn = CurrentTurn;
        CurrentTurn = sideToMove;
        if (halfMoveClock >= 100)
        {
            CurrentTurn = originalTurn;
            reason = GameEndReason.FiftyMoveRule;
            return true;
        }

        if (positionOccurrences.TryGetValue(GetRepetitionKey(), out int repetitions) && repetitions >= 3)
        {
            CurrentTurn = originalTurn;
            reason = GameEndReason.ThreefoldRepetition;
            return true;
        }

        if (HasInsufficientMaterial())
        {
            CurrentTurn = originalTurn;
            reason = GameEndReason.InsufficientMaterial;
            return true;
        }

        bool hasLegalMoves = GetLegalMovesForCurrentTurn().Count > 0;
        CurrentTurn = originalTurn;

        if (hasLegalMoves)
        {
            reason = GameEndReason.None;
            return false;
        }

        reason = IsInCheck(sideToMove) ? GameEndReason.Checkmate : GameEndReason.Stalemate;
        return true;
    }

    private void UpdateGameState()
    {
        if (halfMoveClock >= 100)
        {
            IsGameOver = true;
            EndReason = GameEndReason.FiftyMoveRule;
            Winner = null;
            return;
        }

        if (positionOccurrences.TryGetValue(GetRepetitionKey(), out int repetitions) && repetitions >= 3)
        {
            IsGameOver = true;
            EndReason = GameEndReason.ThreefoldRepetition;
            Winner = null;
            return;
        }

        if (HasInsufficientMaterial())
        {
            IsGameOver = true;
            EndReason = GameEndReason.InsufficientMaterial;
            Winner = null;
            return;
        }

        bool hasLegalMoves = GetLegalMovesForCurrentTurn().Count > 0;
        if (hasLegalMoves)
        {
            IsGameOver = false;
            EndReason = GameEndReason.None;
            Winner = null;
            return;
        }

        IsGameOver = true;
        if (IsInCheck(CurrentTurn))
        {
            EndReason = GameEndReason.Checkmate;
            Winner = Opposite(CurrentTurn);
        }
        else
        {
            EndReason = GameEndReason.Stalemate;
            Winner = null;
        }
    }

    private IEnumerable<LegalMove> GetPseudoMoves(BoardPosition from, ChessPiece piece)
    {
        return piece.Type switch
        {
            PieceType.Pawn => GetPawnMoves(from, piece.Color),
            PieceType.Knight => GetJumpMoves(from, piece.Color, KnightOffsets),
            PieceType.Bishop => GetSlidingMoves(from, piece.Color, BishopDirections),
            PieceType.Rook => GetSlidingMoves(from, piece.Color, RookDirections),
            PieceType.Queen => GetSlidingMoves(from, piece.Color, QueenDirections),
            PieceType.King => GetKingMoves(from, piece.Color),
            _ => []
        };
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

    private IReadOnlyList<LegalMove> GetPawnMoves(BoardPosition from, PieceColor color)
    {
        List<LegalMove> moves = [];
        ChessPiece piece = GetPiece(from)!;
        int direction = color == PieceColor.White ? -1 : 1;
        int startRow = color == PieceColor.White ? 6 : 1;

        BoardPosition oneStep = new(from.Row + direction, from.Column);
        if (oneStep.IsValid && GetPiece(oneStep) is null)
        {
            AddPawnMove(moves, from, oneStep, piece, null, null, MoveKind.Normal);

            BoardPosition twoSteps = new(from.Row + (direction * 2), from.Column);
            if (from.Row == startRow && twoSteps.IsValid && GetPiece(twoSteps) is null)
            {
                moves.Add(new LegalMove(from, twoSteps, piece, null));
            }
        }

        AddPawnCapture(moves, from, new BoardPosition(from.Row + direction, from.Column - 1), piece);
        AddPawnCapture(moves, from, new BoardPosition(from.Row + direction, from.Column + 1), piece);
        return moves;
    }

    private void AddPawnMove(
        List<LegalMove> moves,
        BoardPosition from,
        BoardPosition to,
        ChessPiece piece,
        ChessPiece? capturedPiece,
        BoardPosition? capturedPosition,
        MoveKind moveKind)
    {
        bool promotes = to.Row is 0 or 7;
        if (!promotes)
        {
            moves.Add(new LegalMove(from, to, piece, capturedPiece, moveKind, null, capturedPosition));
            return;
        }

        foreach (PieceType promotionType in new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight })
        {
            moves.Add(new LegalMove(from, to, piece, capturedPiece, MoveKind.Promotion, promotionType, capturedPosition));
        }
    }

    private void AddPawnCapture(List<LegalMove> moves, BoardPosition from, BoardPosition target, ChessPiece piece)
    {
        if (!target.IsValid)
        {
            return;
        }

        ChessPiece? targetPiece = GetPiece(target);
        if (targetPiece is not null && targetPiece.Color != piece.Color)
        {
            AddPawnMove(moves, from, target, piece, targetPiece, target, MoveKind.Normal);
            return;
        }

        if (enPassantTarget == target)
        {
            BoardPosition capturedPosition = new(from.Row, target.Column);
            ChessPiece? capturedPawn = GetPiece(capturedPosition);
            if (capturedPawn is { Type: PieceType.Pawn } && capturedPawn.Color != piece.Color)
            {
                moves.Add(new LegalMove(
                    from,
                    target,
                    piece,
                    capturedPawn,
                    MoveKind.EnPassant,
                    null,
                    capturedPosition));
            }
        }
    }

    private IReadOnlyList<LegalMove> GetJumpMoves(
        BoardPosition from,
        PieceColor color,
        IReadOnlyList<(int Row, int Column)> offsets)
    {
        List<LegalMove> moves = [];
        ChessPiece piece = GetPiece(from)!;
        foreach ((int rowOffset, int columnOffset) in offsets)
        {
            BoardPosition target = new(from.Row + rowOffset, from.Column + columnOffset);
            if (CanOccupy(target, color))
            {
                moves.Add(new LegalMove(from, target, piece, GetPiece(target), CapturedPosition: target));
            }
        }

        return moves;
    }

    private IReadOnlyList<LegalMove> GetKingMoves(BoardPosition from, PieceColor color)
    {
        List<LegalMove> moves = [.. GetJumpMoves(from, color, KingOffsets)];
        AddCastlingMoves(moves, from, color);
        return moves;
    }

    private IReadOnlyList<LegalMove> GetSlidingMoves(
        BoardPosition from,
        PieceColor color,
        IReadOnlyList<(int Row, int Column)> directions)
    {
        List<LegalMove> moves = [];
        ChessPiece piece = GetPiece(from)!;
        foreach ((int rowDirection, int columnDirection) in directions)
        {
            BoardPosition target = new(from.Row + rowDirection, from.Column + columnDirection);
            while (target.IsValid)
            {
                ChessPiece? targetPiece = GetPiece(target);
                if (targetPiece is null)
                {
                    moves.Add(new LegalMove(from, target, piece, null));
                }
                else
                {
                    if (targetPiece.Color != color)
                    {
                        moves.Add(new LegalMove(from, target, piece, targetPiece, CapturedPosition: target));
                    }

                    break;
                }

                target = new BoardPosition(target.Row + rowDirection, target.Column + columnDirection);
            }
        }

        return moves;
    }

    private void AddCastlingMoves(List<LegalMove> moves, BoardPosition from, PieceColor color)
    {
        ChessPiece piece = GetPiece(from)!;
        int row = color == PieceColor.White ? 7 : 0;
        if (from != new BoardPosition(row, 4))
        {
            return;
        }

        if (CanCastle(color, kingside: true))
        {
            moves.Add(new LegalMove(from, new BoardPosition(row, 6), piece, null, MoveKind.CastlingKingSide));
        }

        if (CanCastle(color, kingside: false))
        {
            moves.Add(new LegalMove(from, new BoardPosition(row, 2), piece, null, MoveKind.CastlingQueenSide));
        }
    }

    private bool CanCastle(PieceColor color, bool kingside)
    {
        int row = color == PieceColor.White ? 7 : 0;
        if (IsInCheck(color) || HasKingMoved(color) || HasRookMoved(color, kingside))
        {
            return false;
        }

        int rookColumn = kingside ? 7 : 0;
        ChessPiece? rook = GetPiece(new BoardPosition(row, rookColumn));
        if (rook is not { Type: PieceType.Rook } || rook.Color != color)
        {
            return false;
        }

        int[] emptyColumns = kingside ? [5, 6] : [1, 2, 3];
        if (emptyColumns.Any(column => GetPiece(new BoardPosition(row, column)) is not null))
        {
            return false;
        }

        int[] safeColumns = kingside ? [5, 6] : [3, 2];
        return safeColumns.All(column => !IsSquareAttacked(new BoardPosition(row, column), Opposite(color)));
    }

    private void MoveCastlingRook(LegalMove move)
    {
        int row = move.MovedPiece.Color == PieceColor.White ? 7 : 0;
        if (move.Kind == MoveKind.CastlingKingSide)
        {
            board[row, 5] = board[row, 7];
            board[row, 7] = null;
        }
        else
        {
            board[row, 3] = board[row, 0];
            board[row, 0] = null;
        }
    }

    private BoardPosition? GetNewEnPassantTarget(LegalMove move)
    {
        if (move.MovedPiece.Type != PieceType.Pawn || Math.Abs(move.To.Row - move.From.Row) != 2)
        {
            return null;
        }

        int targetRow = (move.From.Row + move.To.Row) / 2;
        return new BoardPosition(targetRow, move.From.Column);
    }

    private void UpdateCastlingRights(LegalMove move)
    {
        if (move.MovedPiece.Type == PieceType.King)
        {
            if (move.MovedPiece.Color == PieceColor.White)
            {
                whiteKingMoved = true;
            }
            else
            {
                blackKingMoved = true;
            }
        }

        MarkRookMoved(move.MovedPiece, move.From);
        if (move.CapturedPiece?.Type == PieceType.Rook && move.CapturedPosition is not null)
        {
            MarkRookMoved(move.CapturedPiece, move.CapturedPosition.Value);
        }
    }

    private void MarkRookMoved(ChessPiece piece, BoardPosition position)
    {
        if (piece.Type != PieceType.Rook)
        {
            return;
        }

        if (position == new BoardPosition(7, 0))
        {
            whiteQueenSideRookMoved = true;
        }
        else if (position == new BoardPosition(7, 7))
        {
            whiteKingSideRookMoved = true;
        }
        else if (position == new BoardPosition(0, 0))
        {
            blackQueenSideRookMoved = true;
        }
        else if (position == new BoardPosition(0, 7))
        {
            blackKingSideRookMoved = true;
        }
    }

    private bool CanOccupy(BoardPosition position, PieceColor color)
    {
        ChessPiece? piece = GetPiece(position);
        return position.IsValid && (piece is null || piece.Color != color);
    }

    private bool WouldLeaveKingInCheck(LegalMove move)
    {
        ChessPiece? sourcePiece = board[move.From.Row, move.From.Column];
        ChessPiece? targetPiece = board[move.To.Row, move.To.Column];
        BoardPosition capturedPosition = move.CapturedPosition ?? move.To;
        ChessPiece? enPassantCapturedPiece = null;
        ChessPiece? rookSourcePiece = null;
        ChessPiece? rookTargetPiece = null;
        BoardPosition? rookSource = null;
        BoardPosition? rookTarget = null;

        board[move.From.Row, move.From.Column] = null;
        if (move.Kind == MoveKind.EnPassant)
        {
            enPassantCapturedPiece = board[capturedPosition.Row, capturedPosition.Column];
            board[capturedPosition.Row, capturedPosition.Column] = null;
        }

        board[move.To.Row, move.To.Column] = sourcePiece;

        if (move.Kind is MoveKind.CastlingKingSide or MoveKind.CastlingQueenSide)
        {
            int row = move.MovedPiece.Color == PieceColor.White ? 7 : 0;
            rookSource = new BoardPosition(row, move.Kind == MoveKind.CastlingKingSide ? 7 : 0);
            rookTarget = new BoardPosition(row, move.Kind == MoveKind.CastlingKingSide ? 5 : 3);
            rookSourcePiece = board[rookSource.Value.Row, rookSource.Value.Column];
            rookTargetPiece = board[rookTarget.Value.Row, rookTarget.Value.Column];
            board[rookTarget.Value.Row, rookTarget.Value.Column] = rookSourcePiece;
            board[rookSource.Value.Row, rookSource.Value.Column] = null;
        }

        bool isInCheck = IsInCheck(move.MovedPiece.Color);

        if (rookSource is not null && rookTarget is not null)
        {
            board[rookSource.Value.Row, rookSource.Value.Column] = rookSourcePiece;
            board[rookTarget.Value.Row, rookTarget.Value.Column] = rookTargetPiece;
        }

        board[move.From.Row, move.From.Column] = sourcePiece;
        board[move.To.Row, move.To.Column] = targetPiece;
        if (move.Kind == MoveKind.EnPassant)
        {
            board[capturedPosition.Row, capturedPosition.Column] = enPassantCapturedPiece;
        }

        return isInCheck;
    }

    private bool IsSquareAttacked(BoardPosition square, PieceColor attackerColor)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                BoardPosition from = new(row, column);
                ChessPiece? piece = GetPiece(from);
                if (piece is null || piece.Color != attackerColor)
                {
                    continue;
                }

                if (GetAttackSquares(from, piece).Contains(square))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private IReadOnlyList<BoardPosition> GetAttackSquares(BoardPosition from, ChessPiece piece)
    {
        if (piece.Type == PieceType.Pawn)
        {
            int direction = piece.Color == PieceColor.White ? -1 : 1;
            return
            [
                new BoardPosition(from.Row + direction, from.Column - 1),
                new BoardPosition(from.Row + direction, from.Column + 1)
            ];
        }

        if (piece.Type == PieceType.King)
        {
            return KingOffsets
                .Select(offset => new BoardPosition(from.Row + offset.Row, from.Column + offset.Column))
                .Where(position => position.IsValid)
                .ToList();
        }

        return piece.Type switch
        {
            PieceType.Knight => KnightOffsets
                .Select(offset => new BoardPosition(from.Row + offset.Row, from.Column + offset.Column))
                .Where(position => position.IsValid)
                .ToList(),
            PieceType.Bishop => GetSlidingAttackSquares(from, piece.Color, BishopDirections),
            PieceType.Rook => GetSlidingAttackSquares(from, piece.Color, RookDirections),
            PieceType.Queen => GetSlidingAttackSquares(from, piece.Color, QueenDirections),
            _ => []
        };
    }

    private IReadOnlyList<BoardPosition> GetSlidingAttackSquares(
        BoardPosition from,
        PieceColor color,
        IReadOnlyList<(int Row, int Column)> directions)
    {
        List<BoardPosition> squares = [];
        foreach ((int rowDirection, int columnDirection) in directions)
        {
            BoardPosition target = new(from.Row + rowDirection, from.Column + columnDirection);
            while (target.IsValid)
            {
                squares.Add(target);
                ChessPiece? targetPiece = GetPiece(target);
                if (targetPiece is not null)
                {
                    break;
                }

                target = new BoardPosition(target.Row + rowDirection, target.Column + columnDirection);
            }
        }

        return squares;
    }

    private BoardPosition? FindKing(PieceColor color)
    {
        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                ChessPiece? piece = board[row, column];
                if (piece?.Color == color && piece.Type == PieceType.King)
                {
                    return new BoardPosition(row, column);
                }
            }
        }

        return null;
    }

    private void RecordCurrentPosition()
    {
        string key = GetRepetitionKey();
        positionOccurrences[key] = positionOccurrences.TryGetValue(key, out int count) ? count + 1 : 1;
    }

    private string GetRepetitionKey()
    {
        string fen = ToFen();
        string[] parts = fen.Split(' ');
        return string.Join(' ', parts.Take(4));
    }

    private bool HasInsufficientMaterial()
    {
        List<ChessPiece> pieces = [];
        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                ChessPiece? piece = board[row, column];
                if (piece is not null)
                {
                    pieces.Add(piece);
                }
            }
        }

        List<ChessPiece> nonKings = pieces.Where(piece => piece.Type != PieceType.King).ToList();
        if (nonKings.Count == 0)
        {
            return true;
        }

        if (nonKings.Count == 1)
        {
            return nonKings[0].Type is PieceType.Bishop or PieceType.Knight;
        }

        return false;
    }

    private string GetFenCastlingRights()
    {
        string rights = string.Empty;
        if (!whiteKingMoved && !whiteKingSideRookMoved && GetPiece(new BoardPosition(7, 7)) is { Type: PieceType.Rook, Color: PieceColor.White })
        {
            rights += "K";
        }

        if (!whiteKingMoved && !whiteQueenSideRookMoved && GetPiece(new BoardPosition(7, 0)) is { Type: PieceType.Rook, Color: PieceColor.White })
        {
            rights += "Q";
        }

        if (!blackKingMoved && !blackKingSideRookMoved && GetPiece(new BoardPosition(0, 7)) is { Type: PieceType.Rook, Color: PieceColor.Black })
        {
            rights += "k";
        }

        if (!blackKingMoved && !blackQueenSideRookMoved && GetPiece(new BoardPosition(0, 0)) is { Type: PieceType.Rook, Color: PieceColor.Black })
        {
            rights += "q";
        }

        return rights.Length == 0 ? "-" : rights;
    }

    private void SetCastlingRightsFromFen(string castlingRights)
    {
        whiteKingMoved = !castlingRights.Contains('K') && !castlingRights.Contains('Q');
        blackKingMoved = !castlingRights.Contains('k') && !castlingRights.Contains('q');
        whiteKingSideRookMoved = !castlingRights.Contains('K');
        whiteQueenSideRookMoved = !castlingRights.Contains('Q');
        blackKingSideRookMoved = !castlingRights.Contains('k');
        blackQueenSideRookMoved = !castlingRights.Contains('q');
    }

    private static char GetFenPieceChar(ChessPiece piece)
    {
        char token = piece.Type switch
        {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            PieceType.King => 'k',
            _ => throw new InvalidOperationException("Unbekannte Figur.")
        };

        return piece.Color == PieceColor.White ? char.ToUpperInvariant(token) : token;
    }

    private static ChessPiece ParseFenPiece(char token)
    {
        PieceColor color = char.IsUpper(token) ? PieceColor.White : PieceColor.Black;
        PieceType type = char.ToLowerInvariant(token) switch
        {
            'p' => PieceType.Pawn,
            'n' => PieceType.Knight,
            'b' => PieceType.Bishop,
            'r' => PieceType.Rook,
            'q' => PieceType.Queen,
            'k' => PieceType.King,
            _ => throw new InvalidOperationException($"Ungueltige FEN-Figur: {token}")
        };

        return new ChessPiece(type, color);
    }

    private bool HasKingMoved(PieceColor color)
    {
        return color == PieceColor.White ? whiteKingMoved : blackKingMoved;
    }

    private bool HasRookMoved(PieceColor color, bool kingside)
    {
        return (color, kingside) switch
        {
            (PieceColor.White, true) => whiteKingSideRookMoved,
            (PieceColor.White, false) => whiteQueenSideRookMoved,
            (PieceColor.Black, true) => blackKingSideRookMoved,
            (PieceColor.Black, false) => blackQueenSideRookMoved,
            _ => true
        };
    }

    private static PieceColor Opposite(PieceColor color)
    {
        return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }

    private static string GetColorName(PieceColor color)
    {
        return color == PieceColor.White ? "Weiss" : "Schwarz";
    }
}
