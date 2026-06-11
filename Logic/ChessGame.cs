namespace Schach.Logic;

public sealed class ChessGame
{
    private static readonly Random VariantRandom = new();

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
    private string startingFen = string.Empty;
    private int blackKingSideRookStartColumn = 7;
    private int blackKingStartColumn = 4;
    private int blackQueenSideRookStartColumn = 0;
    private int blackThreeCheckCount;
    private int whiteKingSideRookStartColumn = 7;
    private int whiteKingStartColumn = 4;
    private int whiteQueenSideRookStartColumn = 0;
    private int whiteThreeCheckCount;

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

    public GameVariant Variant { get; private set; } = GameVariant.Classic;

    public string StartingFen => startingFen;

    public int WhiteChecksDelivered => whiteThreeCheckCount;

    public int BlackChecksDelivered => blackThreeCheckCount;

    public void Reset(GameVariant variant = GameVariant.Classic)
    {
        Array.Clear(board);
        moveHistory.Clear();
        positionOccurrences.Clear();
        Variant = variant;
        CurrentTurn = PieceColor.White;
        IsGameOver = false;
        EndReason = GameEndReason.None;
        Winner = null;
        enPassantTarget = null;
        fullMoveNumber = 1;
        halfMoveClock = 0;
        whiteThreeCheckCount = 0;
        blackThreeCheckCount = 0;
        whiteKingMoved = false;
        blackKingMoved = false;
        whiteKingSideRookMoved = false;
        whiteQueenSideRookMoved = false;
        blackKingSideRookMoved = false;
        blackQueenSideRookMoved = false;
        whiteKingStartColumn = 4;
        blackKingStartColumn = 4;
        whiteKingSideRookStartColumn = 7;
        whiteQueenSideRookStartColumn = 0;
        blackKingSideRookStartColumn = 7;
        blackQueenSideRookStartColumn = 0;

        SetupStartPosition(variant);
        startingFen = ToFen();
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

    public void LoadFen(string fen, GameVariant variant = GameVariant.Classic)
    {
        string[] parts = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            throw new InvalidOperationException("FEN benoetigt mindestens Brett, Zugfarbe, Rochaderechte und En-passant-Feld.");
        }

        Array.Clear(board);
        moveHistory.Clear();
        positionOccurrences.Clear();
        Variant = variant;
        whiteThreeCheckCount = 0;
        blackThreeCheckCount = 0;
        whiteKingStartColumn = 4;
        blackKingStartColumn = 4;
        whiteKingSideRookStartColumn = 7;
        whiteQueenSideRookStartColumn = 0;
        blackKingSideRookStartColumn = 7;
        blackQueenSideRookStartColumn = 0;

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

        InferCastlingStartColumnsFromBoard();
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
        startingFen = ToFen();
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
                GameEndReason.ThreeCheck => $"{prefix}\n{GetColorName(Winner!.Value)} gewinnt im Drei-Schach-Modus.",
                GameEndReason.KingOfTheHill => $"{prefix}\n{GetColorName(Winner!.Value)} gewinnt durch King of the Hill.",
                _ => prefix
            };
        }

        string turn = GetColorName(CurrentTurn);
        string status = IsInCheck(CurrentTurn)
            ? $"{prefix}\nSchach gegen {turn}. {turn} muss den Koenig schuetzen."
            : $"{prefix}\n{turn} am Zug";
        return Variant == GameVariant.ThreeCheck
            ? $"{status}\nDrei-Schach: Weiss {whiteThreeCheckCount}/3, Schwarz {blackThreeCheckCount}/3"
            : status;
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
        clone.whiteKingStartColumn = whiteKingStartColumn;
        clone.blackKingStartColumn = blackKingStartColumn;
        clone.whiteKingSideRookStartColumn = whiteKingSideRookStartColumn;
        clone.whiteQueenSideRookStartColumn = whiteQueenSideRookStartColumn;
        clone.blackKingSideRookStartColumn = blackKingSideRookStartColumn;
        clone.blackQueenSideRookStartColumn = blackQueenSideRookStartColumn;
        clone.whiteThreeCheckCount = whiteThreeCheckCount;
        clone.blackThreeCheckCount = blackThreeCheckCount;
        clone.Variant = Variant;
        clone.startingFen = startingFen;
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

        if (move.Kind is MoveKind.CastlingKingSide or MoveKind.CastlingQueenSide)
        {
            ApplyCastlingToBoard(move, movedPiece);
        }
        else
        {
            board[move.From.Row, move.From.Column] = null;
            if (move.Kind == MoveKind.EnPassant)
            {
                board[capturedPosition.Row, capturedPosition.Column] = null;
            }

            ChessPiece pieceToPlace = move.Kind == MoveKind.Promotion
                ? new ChessPiece(move.PromotionType ?? PieceType.Queen, movedPiece.Color)
                : movedPiece;

            board[move.To.Row, move.To.Column] = pieceToPlace;
        }

        ChessPiece pieceToPlaceForRecord = move.Kind == MoveKind.Promotion
            ? new ChessPiece(move.PromotionType ?? PieceType.Queen, movedPiece.Color)
            : movedPiece;

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
            pieceToPlaceForRecord,
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
            pieceToPlaceForRecord,
            capturedPiece));

        if (movedPiece.Color == PieceColor.Black)
        {
            fullMoveNumber++;
        }

        CurrentTurn = Opposite(CurrentTurn);
        if (IsInCheck(CurrentTurn))
        {
            RegisterDeliveredCheck(movedPiece.Color);
        }

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
        if (TryApplyVariantEndState())
        {
            return;
        }

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

    private bool TryApplyVariantEndState()
    {
        if (Variant == GameVariant.ThreeCheck)
        {
            if (whiteThreeCheckCount >= 3)
            {
                IsGameOver = true;
                EndReason = GameEndReason.ThreeCheck;
                Winner = PieceColor.White;
                return true;
            }

            if (blackThreeCheckCount >= 3)
            {
                IsGameOver = true;
                EndReason = GameEndReason.ThreeCheck;
                Winner = PieceColor.Black;
                return true;
            }
        }

        if (Variant == GameVariant.KingOfTheHill)
        {
            foreach (PieceColor color in new[] { PieceColor.White, PieceColor.Black })
            {
                BoardPosition? kingPosition = FindKing(color);
                if (kingPosition is { Row: >= 3 and <= 4, Column: >= 3 and <= 4 })
                {
                    IsGameOver = true;
                    EndReason = GameEndReason.KingOfTheHill;
                    Winner = color;
                    return true;
                }
            }
        }

        return false;
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

    private void SetupStartPosition(GameVariant variant)
    {
        switch (variant)
        {
            case GameVariant.Chess960:
                PieceType[] chess960BackRank = GenerateChess960BackRank();
                PlaceChess960BackRank(0, PieceColor.Black, chess960BackRank);
                PlacePawns(1, PieceColor.Black);
                PlacePawns(6, PieceColor.White);
                PlaceChess960BackRank(7, PieceColor.White, chess960BackRank);
                break;
            case GameVariant.PawnsWar:
                PlacePawns(1, PieceColor.Black);
                PlacePawns(6, PieceColor.White);
                board[0, 4] = new ChessPiece(PieceType.King, PieceColor.Black);
                board[7, 4] = new ChessPiece(PieceType.King, PieceColor.White);
                blackKingSideRookMoved = true;
                blackQueenSideRookMoved = true;
                whiteKingSideRookMoved = true;
                whiteQueenSideRookMoved = true;
                break;
            case GameVariant.KnightsDuel:
                board[0, 4] = new ChessPiece(PieceType.King, PieceColor.Black);
                board[0, 1] = new ChessPiece(PieceType.Knight, PieceColor.Black);
                board[0, 6] = new ChessPiece(PieceType.Knight, PieceColor.Black);
                board[7, 4] = new ChessPiece(PieceType.King, PieceColor.White);
                board[7, 1] = new ChessPiece(PieceType.Knight, PieceColor.White);
                board[7, 6] = new ChessPiece(PieceType.Knight, PieceColor.White);
                blackKingSideRookMoved = true;
                blackQueenSideRookMoved = true;
                whiteKingSideRookMoved = true;
                whiteQueenSideRookMoved = true;
                break;
            case GameVariant.NoQueens:
                PlaceBackRank(0, PieceColor.Black);
                PlacePawns(1, PieceColor.Black);
                PlacePawns(6, PieceColor.White);
                PlaceBackRank(7, PieceColor.White);
                board[0, 3] = null;
                board[7, 3] = null;
                break;
            default:
                PlaceBackRank(0, PieceColor.Black);
                PlacePawns(1, PieceColor.Black);
                PlacePawns(6, PieceColor.White);
                PlaceBackRank(7, PieceColor.White);
                break;
        }
    }

    private void PlaceChess960BackRank(int row, PieceColor color, PieceType[] pieces)
    {
        for (int column = 0; column < 8; column++)
        {
            board[row, column] = new ChessPiece(pieces[column], color);
        }

        int kingColumn = Array.IndexOf(pieces, PieceType.King);
        int[] rookColumns = pieces
            .Select((piece, column) => (piece, column))
            .Where(item => item.piece == PieceType.Rook)
            .Select(item => item.column)
            .Order()
            .ToArray();

        if (color == PieceColor.White)
        {
            whiteKingStartColumn = kingColumn;
            whiteQueenSideRookStartColumn = rookColumns[0];
            whiteKingSideRookStartColumn = rookColumns[1];
        }
        else
        {
            blackKingStartColumn = kingColumn;
            blackQueenSideRookStartColumn = rookColumns[0];
            blackKingSideRookStartColumn = rookColumns[1];
        }
    }

    private static PieceType[] GenerateChess960BackRank()
    {
        PieceType?[] pieces = new PieceType?[8];
        int firstBishop = VariantRandom.Next(0, 4) * 2;
        int secondBishop = VariantRandom.Next(0, 4) * 2 + 1;
        pieces[firstBishop] = PieceType.Bishop;
        pieces[secondBishop] = PieceType.Bishop;

        int queenColumn = PickEmptyColumn(pieces);
        pieces[queenColumn] = PieceType.Queen;

        int firstKnight = PickEmptyColumn(pieces);
        pieces[firstKnight] = PieceType.Knight;
        int secondKnight = PickEmptyColumn(pieces);
        pieces[secondKnight] = PieceType.Knight;

        int[] remaining = pieces
            .Select((piece, column) => (piece, column))
            .Where(item => item.piece is null)
            .Select(item => item.column)
            .Order()
            .ToArray();

        pieces[remaining[0]] = PieceType.Rook;
        pieces[remaining[1]] = PieceType.King;
        pieces[remaining[2]] = PieceType.Rook;
        return pieces.Select(piece => piece!.Value).ToArray();
    }

    private static int PickEmptyColumn(PieceType?[] pieces)
    {
        int[] emptyColumns = pieces
            .Select((piece, column) => (piece, column))
            .Where(item => item.piece is null)
            .Select(item => item.column)
            .ToArray();

        return emptyColumns[VariantRandom.Next(emptyColumns.Length)];
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
        if (from != new BoardPosition(row, GetKingStartColumn(color)))
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

        int kingStartColumn = GetKingStartColumn(color);
        int kingTargetColumn = kingside ? 6 : 2;
        int rookColumn = GetRookStartColumn(color, kingside);
        int rookTargetColumn = kingside ? 5 : 3;
        ChessPiece? rook = GetPiece(new BoardPosition(row, rookColumn));
        if (rook is not { Type: PieceType.Rook } || rook.Color != color)
        {
            return false;
        }

        int firstBetween = Math.Min(kingStartColumn, rookColumn) + 1;
        int lastBetween = Math.Max(kingStartColumn, rookColumn) - 1;
        for (int column = firstBetween; column <= lastBetween; column++)
        {
            if (GetPiece(new BoardPosition(row, column)) is not null)
            {
                return false;
            }
        }

        foreach (int column in new[] { kingTargetColumn, rookTargetColumn })
        {
            if (column != kingStartColumn &&
                column != rookColumn &&
                GetPiece(new BoardPosition(row, column)) is not null)
            {
                return false;
            }
        }

        int firstKingColumn = Math.Min(kingStartColumn, kingTargetColumn);
        int lastKingColumn = Math.Max(kingStartColumn, kingTargetColumn);
        for (int column = firstKingColumn; column <= lastKingColumn; column++)
        {
            if (!IsSquareAttacked(new BoardPosition(row, column), Opposite(color)))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void ApplyCastlingToBoard(LegalMove move, ChessPiece kingPiece)
    {
        int row = move.MovedPiece.Color == PieceColor.White ? 7 : 0;
        bool kingside = move.Kind == MoveKind.CastlingKingSide;
        int rookSourceColumn = GetRookStartColumn(move.MovedPiece.Color, kingside);
        int rookTargetColumn = kingside ? 5 : 3;
        ChessPiece? rookPiece = board[row, rookSourceColumn];

        board[row, move.From.Column] = null;
        board[row, rookSourceColumn] = null;
        board[row, move.To.Column] = kingPiece;
        board[row, rookTargetColumn] = rookPiece;
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

        if (position == new BoardPosition(7, whiteQueenSideRookStartColumn))
        {
            whiteQueenSideRookMoved = true;
        }
        else if (position == new BoardPosition(7, whiteKingSideRookStartColumn))
        {
            whiteKingSideRookMoved = true;
        }
        else if (position == new BoardPosition(0, blackQueenSideRookStartColumn))
        {
            blackQueenSideRookMoved = true;
        }
        else if (position == new BoardPosition(0, blackKingSideRookStartColumn))
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
        if (move.Kind is MoveKind.CastlingKingSide or MoveKind.CastlingQueenSide)
        {
            int row = move.MovedPiece.Color == PieceColor.White ? 7 : 0;
            bool kingside = move.Kind == MoveKind.CastlingKingSide;
            BoardPosition rookSource = new(row, GetRookStartColumn(move.MovedPiece.Color, kingside));
            BoardPosition rookTarget = new(row, kingside ? 5 : 3);
            BoardPosition[] snapshotPositions = [move.From, move.To, rookSource, rookTarget];
            Dictionary<BoardPosition, ChessPiece?> snapshot = snapshotPositions
                .Distinct()
                .ToDictionary(position => position, position => board[position.Row, position.Column]);

            ApplyCastlingToBoard(move, move.MovedPiece);
            bool isCastlingCheck = IsInCheck(move.MovedPiece.Color);
            foreach ((BoardPosition position, ChessPiece? piece) in snapshot)
            {
                board[position.Row, position.Column] = piece;
            }

            return isCastlingCheck;
        }

        ChessPiece? sourcePiece = board[move.From.Row, move.From.Column];
        ChessPiece? targetPiece = board[move.To.Row, move.To.Column];
        BoardPosition capturedPosition = move.CapturedPosition ?? move.To;
        ChessPiece? enPassantCapturedPiece = null;

        board[move.From.Row, move.From.Column] = null;
        if (move.Kind == MoveKind.EnPassant)
        {
            enPassantCapturedPiece = board[capturedPosition.Row, capturedPosition.Column];
            board[capturedPosition.Row, capturedPosition.Column] = null;
        }

        board[move.To.Row, move.To.Column] = sourcePiece;

        bool isInCheck = IsInCheck(move.MovedPiece.Color);

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
        if (!whiteKingMoved && !whiteKingSideRookMoved && GetPiece(new BoardPosition(7, whiteKingSideRookStartColumn)) is { Type: PieceType.Rook, Color: PieceColor.White })
        {
            rights += "K";
        }

        if (!whiteKingMoved && !whiteQueenSideRookMoved && GetPiece(new BoardPosition(7, whiteQueenSideRookStartColumn)) is { Type: PieceType.Rook, Color: PieceColor.White })
        {
            rights += "Q";
        }

        if (!blackKingMoved && !blackKingSideRookMoved && GetPiece(new BoardPosition(0, blackKingSideRookStartColumn)) is { Type: PieceType.Rook, Color: PieceColor.Black })
        {
            rights += "k";
        }

        if (!blackKingMoved && !blackQueenSideRookMoved && GetPiece(new BoardPosition(0, blackQueenSideRookStartColumn)) is { Type: PieceType.Rook, Color: PieceColor.Black })
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

    private void InferCastlingStartColumnsFromBoard()
    {
        InferCastlingStartColumnsFromBackRank(PieceColor.White, 7);
        InferCastlingStartColumnsFromBackRank(PieceColor.Black, 0);
    }

    private void InferCastlingStartColumnsFromBackRank(PieceColor color, int row)
    {
        int kingColumn = Enumerable.Range(0, 8)
            .FirstOrDefault(column => board[row, column] is { Type: PieceType.King } piece && piece.Color == color);
        int[] rookColumns = Enumerable.Range(0, 8)
            .Where(column => board[row, column] is { Type: PieceType.Rook } piece && piece.Color == color)
            .ToArray();

        int queenSideRookColumn = rookColumns.Where(column => column < kingColumn).DefaultIfEmpty(0).Max();
        int kingSideRookColumn = rookColumns.Where(column => column > kingColumn).DefaultIfEmpty(7).Min();

        if (color == PieceColor.White)
        {
            whiteKingStartColumn = kingColumn;
            whiteQueenSideRookStartColumn = queenSideRookColumn;
            whiteKingSideRookStartColumn = kingSideRookColumn;
        }
        else
        {
            blackKingStartColumn = kingColumn;
            blackQueenSideRookStartColumn = queenSideRookColumn;
            blackKingSideRookStartColumn = kingSideRookColumn;
        }
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

    private int GetKingStartColumn(PieceColor color)
    {
        return color == PieceColor.White ? whiteKingStartColumn : blackKingStartColumn;
    }

    private int GetRookStartColumn(PieceColor color, bool kingside)
    {
        return (color, kingside) switch
        {
            (PieceColor.White, true) => whiteKingSideRookStartColumn,
            (PieceColor.White, false) => whiteQueenSideRookStartColumn,
            (PieceColor.Black, true) => blackKingSideRookStartColumn,
            (PieceColor.Black, false) => blackQueenSideRookStartColumn,
            _ => kingside ? 7 : 0
        };
    }

    private void RegisterDeliveredCheck(PieceColor color)
    {
        if (Variant != GameVariant.ThreeCheck)
        {
            return;
        }

        if (color == PieceColor.White)
        {
            whiteThreeCheckCount++;
        }
        else
        {
            blackThreeCheckCount++;
        }
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
