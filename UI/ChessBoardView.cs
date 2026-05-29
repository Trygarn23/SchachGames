using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
    private BoardPosition? selectedSquare;
    private IReadOnlyList<BoardPosition> selectedMoves = [];

    public ChessBoardView(ChessGame game, BoardTheme theme)
    {
        this.game = game;
        this.theme = theme;

        Width = 668;
        Height = 668;
        Focusable = true;

        BuildBoard();
        Refresh();
    }

    public event Action<string>? StatusChanged;

    public event Action<MoveResult>? MoveCompleted;

    public bool IsFlipped { get; private set; }

    public void StartNewGame()
    {
        game.Reset();
        selectedSquare = null;
        selectedMoves = [];
        manualMarkers.Clear();
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
        Refresh();
        StatusChanged?.Invoke(game.GetStatusText(message));
    }

    public bool TryPlayMove(LegalMove move)
    {
        if (!game.TryMove(move.From, move.To, out MoveResult? result) || result is null)
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
            Tag = position
        };

        square.Click += Square_Click;
        square.MouseRightButtonUp += Square_MouseRightButtonUp;
        square.MouseEnter += (_, _) => square.Opacity = 0.92;
        square.MouseLeave += (_, _) => square.Opacity = 1.0;
        square.GotKeyboardFocus += (_, _) => Refresh();
        square.LostKeyboardFocus += (_, _) => Refresh();
        return square;
    }

    private void Square_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        Focus();
        e.Handled = true;

        if (sender is not Button { Tag: BoardPosition position })
        {
            return;
        }

        CycleManualMarker(position);
        Refresh();
        StatusChanged?.Invoke(game.GetStatusText($"{position.ToAlgebraic()} markiert"));
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
        if (selectedSquare is null)
        {
            SelectSquare(clicked, clickedPiece);
            return;
        }

        BoardPosition from = selectedSquare.Value;
        if (game.TryMove(from, clicked, out MoveResult? result) && result is not null)
        {
            CompleteMove(result);
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

    private void CompleteMove(MoveResult result)
    {
        selectedSquare = null;
        selectedMoves = [];
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
        StatusChanged?.Invoke(game.GetStatusText($"{position.ToAlgebraic()} ausgewaehlt"));
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
                    selectedMoves.Contains(position));
                ApplySquareBorder(square, position);
            }
        }
    }

    private void ApplySquareBorder(Button square, BoardPosition position)
    {
        if (manualMarkers.TryGetValue(position, out BoardMarkerColor markerColor))
        {
            square.BorderBrush = theme.GetMarkerBrush(markerColor);
            square.BorderThickness = new Thickness(5);
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

    private void AnimateSquare(BoardPosition position, bool isCapture)
    {
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
