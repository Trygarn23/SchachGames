using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Schach.Logic;
using Schach.Visuals;

namespace Schach.UI;

public sealed class ChessBoardView : Grid
{
    private const int BoardSize = 8;
    private const int CoordinateSize = 28;

    private readonly ChessGame game;
    private readonly BoardTheme theme;
    private readonly Button[,] squares = new Button[BoardSize, BoardSize];
    private readonly TextBlock[] rankLabels = new TextBlock[BoardSize];
    private readonly TextBlock[] fileLabels = new TextBlock[BoardSize];
    private readonly Dictionary<BoardPosition, BoardMarkerColor> manualMarkers = [];
    private readonly HashSet<(BoardPosition From, BoardPosition To)> manualArrows = [];
    private readonly Canvas arrowCanvas = new() { IsHitTestVisible = false };
    private BoardPosition? selectedSquare;
    private IReadOnlyList<BoardPosition> selectedMoves = [];
    private BoardPosition? highlightedFrom;
    private BoardPosition? highlightedTo;
    private BoardPosition keyboardPosition = new(7, 4);
    private Point dragStartPoint;
    private BoardPosition? rightDragStart;

    public ChessBoardView(ChessGame game, BoardTheme theme)
    {
        this.game = game;
        this.theme = theme;

        Width = 668;
        Height = 668;
        Focusable = true;
        KeyDown += ChessBoardView_KeyDown;
        SizeChanged += (_, _) => RefreshArrows();

        BuildBoard();
        Refresh();
    }

    public event Action<string>? StatusChanged;

    public event Action<MoveResult>? MoveCompleted;

    public Func<PieceColor, PieceType>? PromotionRequested { get; set; }

    public bool AnimationsEnabled { get; set; } = true;

    public bool InteractionLocked { get; set; }

    public bool IsFlipped { get; private set; }

    public void StartNewGame(GameVariant variant = GameVariant.Classic)
    {
        game.Reset(variant);
        selectedSquare = null;
        selectedMoves = [];
        highlightedFrom = null;
        highlightedTo = null;
        keyboardPosition = new BoardPosition(7, 4);
        manualMarkers.Clear();
        manualArrows.Clear();
        Refresh();
        StatusChanged?.Invoke(game.GetStatusText("Neues Spiel gestartet"));
    }

    public void SetFlipped(bool isFlipped)
    {
        IsFlipped = isFlipped;
        Refresh();
    }

    public void SetTheme(BoardThemeMode themeMode)
    {
        theme.Mode = themeMode;
        Refresh();
    }

    public void ReloadFromGame(string message)
    {
        selectedSquare = null;
        selectedMoves = [];
        manualMarkers.Clear();
        manualArrows.Clear();
        Refresh();
        StatusChanged?.Invoke(game.GetStatusText(message));
    }

    public void HighlightMove(MoveRecord move)
    {
        highlightedFrom = move.From;
        highlightedTo = move.To;
        Refresh();
        squares[move.To.Row, move.To.Column].Focus();
    }

    public bool TryPlayMove(LegalMove move)
    {
        if (!game.TryMove(move, out MoveResult? result) || result is null)
        {
            return false;
        }

        CompleteMove(result);
        return true;
    }

    public void ClearSelection()
    {
        selectedSquare = null;
        selectedMoves = [];
        Refresh();
        StatusChanged?.Invoke(game.GetStatusText("Auswahl aufgehoben"));
    }

    private void BuildBoard()
    {
        RowDefinitions.Clear();
        ColumnDefinitions.Clear();
        Children.Clear();

        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CoordinateSize) });
        for (int index = 0; index < BoardSize; index++)
        {
            ColumnDefinitions.Add(new ColumnDefinition());
            RowDefinitions.Add(new RowDefinition());
        }

        RowDefinitions.Add(new RowDefinition { Height = new GridLength(CoordinateSize) });

        for (int index = 0; index < BoardSize; index++)
        {
            rankLabels[index] = CreateCoordinateLabel();
            SetRow(rankLabels[index], index);
            SetColumn(rankLabels[index], 0);
            Children.Add(rankLabels[index]);

            fileLabels[index] = CreateCoordinateLabel();
            SetRow(fileLabels[index], BoardSize);
            SetColumn(fileLabels[index], index + 1);
            Children.Add(fileLabels[index]);
        }

        for (int row = 0; row < BoardSize; row++)
        {
            for (int column = 0; column < BoardSize; column++)
            {
                BoardPosition position = new(row, column);
                Button square = CreateSquare(position);

                squares[row, column] = square;
                Children.Add(square);
            }
        }

        SetRow(arrowCanvas, 0);
        SetColumn(arrowCanvas, 1);
        SetRowSpan(arrowCanvas, BoardSize);
        SetColumnSpan(arrowCanvas, BoardSize);
        Panel.SetZIndex(arrowCanvas, 10);
        Children.Add(arrowCanvas);
    }

    private static TextBlock CreateCoordinateLabel()
    {
        return new TextBlock
        {
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(200, 205, 213)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private Button CreateSquare(BoardPosition position)
    {
        Button square = new()
        {
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            FontFamily = new FontFamily("Segoe UI Symbol"),
            FontSize = 42,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(0),
            AllowDrop = true,
            Tag = position
        };

        square.Click += Square_Click;
        square.Drop += Square_Drop;
        square.MouseRightButtonDown += (_, _) => rightDragStart = position;
        square.MouseRightButtonUp += Square_MouseRightButtonUp;
        square.PreviewMouseLeftButtonDown += (_, e) => dragStartPoint = e.GetPosition(this);
        square.PreviewMouseMove += Square_PreviewMouseMove;
        square.MouseEnter += (_, _) => square.Opacity = 0.92;
        square.MouseLeave += (_, _) => square.Opacity = 1.0;
        square.GotKeyboardFocus += (_, _) => Refresh();
        square.LostKeyboardFocus += (_, _) => Refresh();
        square.ContextMenu = CreateSquareContextMenu(position);
        return square;
    }

    private ContextMenu CreateSquareContextMenu(BoardPosition position)
    {
        ContextMenu menu = new();
        MenuItem markItem = new() { Header = "Feld markieren" };
        markItem.Click += (_, _) =>
        {
            CycleManualMarker(position);
            Refresh();
        };

        MenuItem clearSquareItem = new() { Header = "Markierung entfernen" };
        clearSquareItem.Click += (_, _) =>
        {
            manualMarkers.Remove(position);
            manualArrows.RemoveWhere(arrow => arrow.From == position || arrow.To == position);
            Refresh();
        };

        MenuItem clearAllItem = new() { Header = "Alle Markierungen entfernen" };
        clearAllItem.Click += (_, _) =>
        {
            manualMarkers.Clear();
            manualArrows.Clear();
            Refresh();
        };

        MenuItem analysisItem = new() { Header = "Analyse fuer Feld starten" };
        analysisItem.Click += (_, _) =>
        {
            selectedSquare = position;
            selectedMoves = game.GetLegalMoves(position);
            Refresh();
            StatusChanged?.Invoke(game.GetStatusText($"Analyse fuer {position.ToAlgebraic()}"));
        };

        menu.Items.Add(markItem);
        menu.Items.Add(clearSquareItem);
        menu.Items.Add(clearAllItem);
        menu.Items.Add(analysisItem);
        return menu;
    }

    private void Square_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed ||
            sender is not Button { Tag: BoardPosition from } square ||
            InteractionLocked ||
            game.IsGameOver)
        {
            return;
        }

        Point current = e.GetPosition(this);
        if (Math.Abs(current.X - dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        ChessPiece? piece = game.GetPiece(from);
        if (piece is null || piece.Color != game.CurrentTurn)
        {
            return;
        }

        DragDrop.DoDragDrop(square, from.ToAlgebraic(), DragDropEffects.Move);
    }

    private void Square_Drop(object sender, DragEventArgs e)
    {
        Focus();
        if (sender is not Button { Tag: BoardPosition to } ||
            e.Data.GetData(DataFormats.Text) is not string fromText ||
            !BoardPosition.TryParse(fromText, out BoardPosition from))
        {
            return;
        }

        if (TryMoveFromTo(from, to))
        {
            e.Handled = true;
        }
    }

    private void Square_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        Focus();
        e.Handled = true;

        if (sender is not Button { Tag: BoardPosition position } square)
        {
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            square.ContextMenu.IsOpen = true;
            return;
        }

        if (rightDragStart is BoardPosition from && from != position)
        {
            ToggleManualArrow(from, position);
            rightDragStart = null;
            Refresh();
            StatusChanged?.Invoke(game.GetStatusText($"{from.ToAlgebraic()} -> {position.ToAlgebraic()} markiert"));
            return;
        }

        CycleManualMarker(position);
        Refresh();
        StatusChanged?.Invoke(game.GetStatusText($"{position.ToAlgebraic()} markiert"));
    }

    private void ToggleManualArrow(BoardPosition from, BoardPosition to)
    {
        (BoardPosition From, BoardPosition To) arrow = (from, to);
        if (!manualArrows.Add(arrow))
        {
            manualArrows.Remove(arrow);
        }
    }

    private void CycleManualMarker(BoardPosition position)
    {
        if (!manualMarkers.TryGetValue(position, out BoardMarkerColor markerColor))
        {
            manualMarkers[position] = BoardMarkerColor.Yellow;
            return;
        }

        if (markerColor == BoardMarkerColor.Yellow)
        {
            manualMarkers[position] = BoardMarkerColor.Red;
            return;
        }

        if (markerColor == BoardMarkerColor.Red)
        {
            manualMarkers[position] = BoardMarkerColor.Blue;
            return;
        }

        manualMarkers.Remove(position);
    }

    private void Square_Click(object sender, RoutedEventArgs e)
    {
        Focus();
        if (manualMarkers.Count > 0 || manualArrows.Count > 0)
        {
            manualMarkers.Clear();
            manualArrows.Clear();
        }

        if (InteractionLocked)
        {
            StatusChanged?.Invoke(game.GetStatusText("Bitte auf den Gegenzug warten"));
            Refresh();
            return;
        }

        if (game.IsGameOver)
        {
            StatusChanged?.Invoke(game.GetStatusText("Spiel ist beendet"));
            return;
        }

        if (sender is not Button { Tag: BoardPosition clicked })
        {
            return;
        }

        ChessPiece? clickedPiece = game.GetPiece(clicked);
        keyboardPosition = clicked;
        if (selectedSquare is null)
        {
            SelectSquare(clicked, clickedPiece);
            return;
        }

        BoardPosition from = selectedSquare.Value;
        if (TryMoveFromTo(from, clicked))
        {
            return;
        }

        if (clickedPiece is not null && clickedPiece.Color == game.CurrentTurn)
        {
            SelectSquare(clicked, clickedPiece);
            return;
        }

        selectedSquare = null;
        selectedMoves = [];
        Refresh();
        StatusChanged?.Invoke("Ungueltiger Zug");
    }

    private bool TryMoveFromTo(BoardPosition from, BoardPosition to)
    {
        IReadOnlyList<LegalMove> targetMoves = game.GetLegalMovesFrom(from)
            .Where(move => move.To == to)
            .ToList();
        PieceType promotionType = PieceType.Queen;
        if (targetMoves.Any(move => move.Kind == MoveKind.Promotion))
        {
            promotionType = PromotionRequested?.Invoke(game.CurrentTurn) ?? PieceType.Queen;
        }

        if (!game.TryMove(from, to, promotionType, out MoveResult? result) || result is null)
        {
            return false;
        }

        CompleteMove(result);
        return true;
    }

    private void CompleteMove(MoveResult result)
    {
        selectedSquare = null;
        selectedMoves = [];
        highlightedFrom = result.From;
        highlightedTo = result.To;
        Refresh();
        AnimateSquare(result.To, result.CapturedPiece is not null);
        MoveCompleted?.Invoke(result);
        StatusChanged?.Invoke(game.GetStatusText(result.Notation));
    }

    private void SelectSquare(BoardPosition position, ChessPiece? piece)
    {
        if (piece is null || piece.Color != game.CurrentTurn)
        {
            ClearSelection();
            return;
        }

        selectedSquare = position;
        selectedMoves = game.GetLegalMoves(position);
        Refresh();
        if (selectedMoves.Count == 0)
        {
            StatusChanged?.Invoke(game.GetStatusText($"{position.ToAlgebraic()} hat keine legalen Zuege"));
            return;
        }

        StatusChanged?.Invoke(game.GetStatusText($"{position.ToAlgebraic()} ausgewaehlt"));
    }

    private void ChessBoardView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            keyboardPosition = e.Key switch
            {
                Key.Left => new BoardPosition(keyboardPosition.Row, Math.Max(0, keyboardPosition.Column - 1)),
                Key.Right => new BoardPosition(keyboardPosition.Row, Math.Min(7, keyboardPosition.Column + 1)),
                Key.Up => new BoardPosition(Math.Max(0, keyboardPosition.Row - 1), keyboardPosition.Column),
                Key.Down => new BoardPosition(Math.Min(7, keyboardPosition.Row + 1), keyboardPosition.Column),
                _ => keyboardPosition
            };

            squares[keyboardPosition.Row, keyboardPosition.Column].Focus();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            Square_Click(squares[keyboardPosition.Row, keyboardPosition.Column], new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void Refresh()
    {
        RefreshCoordinates();

        for (int row = 0; row < BoardSize; row++)
        {
            for (int column = 0; column < BoardSize; column++)
            {
                BoardPosition position = new(row, column);
                ChessPiece? piece = game.GetPiece(position);
                Button square = squares[row, column];

                SetRow(square, GetViewRow(position));
                SetColumn(square, GetViewColumn(position) + 1);

                square.Content = piece?.Symbol ?? string.Empty;
                square.Foreground = piece is null
                    ? Brushes.Transparent
                    : theme.GetPieceBrush(piece.Color);
                square.Background = theme.GetSquareBrush(
                    position,
                    selectedSquare == position,
                    selectedMoves.Contains(position),
                    highlightedFrom == position || highlightedTo == position,
                    piece?.Type == PieceType.King && game.IsInCheck(piece.Color));
                ApplySquareBorder(square, position);
            }
        }

        RefreshArrows();
    }

    private void ApplySquareBorder(Button square, BoardPosition position)
    {
        if (manualMarkers.TryGetValue(position, out BoardMarkerColor markerColor))
        {
            square.BorderBrush = theme.GetMarkerBrush(markerColor);
            square.BorderThickness = new Thickness(5);
            return;
        }

        if (manualArrows.Any(arrow => arrow.From == position || arrow.To == position))
        {
            square.BorderBrush = theme.GetMarkerBrush(BoardMarkerColor.Blue);
            square.BorderThickness = new Thickness(4);
            return;
        }

        if (square.IsKeyboardFocused)
        {
            square.BorderBrush = theme.GetFocusBrush();
            square.BorderThickness = new Thickness(2);
            return;
        }

        square.BorderThickness = new Thickness(0);
    }

    private void RefreshArrows()
    {
        arrowCanvas.Children.Clear();
        double boardWidth = Math.Max(0, ActualWidth - CoordinateSize);
        double boardHeight = Math.Max(0, ActualHeight - CoordinateSize);
        double cellSize = Math.Min(boardWidth, boardHeight) / BoardSize;
        if (cellSize <= 0)
        {
            return;
        }

        foreach ((BoardPosition from, BoardPosition to) in manualArrows)
        {
            Point start = GetCanvasCenter(from, cellSize);
            Point end = GetCanvasCenter(to, cellSize);
            Vector direction = end - start;
            if (direction.Length <= 0)
            {
                continue;
            }

            direction.Normalize();
            Point shortenedEnd = end - direction * (cellSize * 0.22);
            Line line = new()
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = shortenedEnd.X,
                Y2 = shortenedEnd.Y,
                Stroke = theme.GetMarkerBrush(BoardMarkerColor.Blue),
                StrokeThickness = 7,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = 0.78
            };

            Polygon head = CreateArrowHead(shortenedEnd, direction, cellSize);
            arrowCanvas.Children.Add(line);
            arrowCanvas.Children.Add(head);
        }
    }

    private Point GetCanvasCenter(BoardPosition position, double cellSize)
    {
        return new Point(
            GetViewColumn(position) * cellSize + cellSize / 2,
            GetViewRow(position) * cellSize + cellSize / 2);
    }

    private Polygon CreateArrowHead(Point tip, Vector direction, double cellSize)
    {
        Vector perpendicular = new(-direction.Y, direction.X);
        double length = cellSize * 0.24;
        double width = cellSize * 0.16;
        return new Polygon
        {
            Fill = theme.GetMarkerBrush(BoardMarkerColor.Blue),
            Opacity = 0.82,
            Points =
            {
                tip,
                tip - direction * length + perpendicular * width,
                tip - direction * length - perpendicular * width
            }
        };
    }

    private void AnimateSquare(BoardPosition position, bool isCapture)
    {
        if (!AnimationsEnabled)
        {
            return;
        }

        Button square = squares[position.Row, position.Column];
        ScaleTransform scaleTransform = new(1, 1);
        square.RenderTransform = scaleTransform;
        square.RenderTransformOrigin = new Point(0.5, 0.5);

        double peak = isCapture ? 1.12 : 1.06;
        Duration duration = TimeSpan.FromMilliseconds(isCapture ? 190 : 140);
        DoubleAnimation grow = new(1, peak, duration) { AutoReverse = true };

        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, grow.Clone());
    }

    private void RefreshCoordinates()
    {
        for (int viewIndex = 0; viewIndex < BoardSize; viewIndex++)
        {
            int logicalRow = IsFlipped ? BoardSize - 1 - viewIndex : viewIndex;
            int logicalColumn = IsFlipped ? BoardSize - 1 - viewIndex : viewIndex;

            rankLabels[viewIndex].Text = (BoardSize - logicalRow).ToString();
            fileLabels[viewIndex].Text = ((char)('a' + logicalColumn)).ToString();
        }
    }

    private int GetViewRow(BoardPosition position)
    {
        return IsFlipped ? BoardSize - 1 - position.Row : position.Row;
    }

    private int GetViewColumn(BoardPosition position)
    {
        return IsFlipped ? BoardSize - 1 - position.Column : position.Column;
    }

}
