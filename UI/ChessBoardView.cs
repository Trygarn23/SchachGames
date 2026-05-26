using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Schach.Logic;
using Schach.Visuals;

namespace Schach.UI;

public sealed class ChessBoardView : Grid
{
    private readonly ChessGame game;
    private readonly BoardTheme theme;
    private readonly Button[,] squares = new Button[8, 8];
    private BoardPosition? selectedSquare;
    private IReadOnlyList<BoardPosition> selectedMoves = [];

    public ChessBoardView(ChessGame game, BoardTheme theme)
    {
        this.game = game;
        this.theme = theme;

        Width = 640;
        Height = 640;

        BuildBoard();
        Refresh();
    }

    public event Action<string>? StatusChanged;

    public void StartNewGame()
    {
        game.Reset();
        selectedSquare = null;
        selectedMoves = [];
        Refresh();
        StatusChanged?.Invoke("Neues Spiel gestartet");
    }

    private void BuildBoard()
    {
        RowDefinitions.Clear();
        ColumnDefinitions.Clear();
        Children.Clear();

        for (int index = 0; index < 8; index++)
        {
            RowDefinitions.Add(new RowDefinition());
            ColumnDefinitions.Add(new ColumnDefinition());
        }

        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                BoardPosition position = new(row, column);
                Button square = CreateSquare(position);

                squares[row, column] = square;
                Children.Add(square);
            }
        }
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
        SetRow(square, position.Row);
        SetColumn(square, position.Column);
        return square;
    }

    private void Square_Click(object sender, RoutedEventArgs e)
    {
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
        if (game.TryMove(from, clicked))
        {
            selectedSquare = null;
            selectedMoves = [];
            Refresh();
            StatusChanged?.Invoke($"{from.ToAlgebraic()} - {clicked.ToAlgebraic()}");
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

    private void SelectSquare(BoardPosition position, ChessPiece? piece)
    {
        if (piece is null || piece.Color != game.CurrentTurn)
        {
            selectedSquare = null;
            selectedMoves = [];
            Refresh();
            StatusChanged?.Invoke(GetTurnText());
            return;
        }

        selectedSquare = position;
        selectedMoves = game.GetLegalMoves(position);
        Refresh();
        StatusChanged?.Invoke($"{position.ToAlgebraic()} ausgewaehlt");
    }

    private void Refresh()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                BoardPosition position = new(row, column);
                ChessPiece? piece = game.GetPiece(position);
                Button square = squares[row, column];

                square.Content = piece?.Symbol ?? string.Empty;
                square.Foreground = piece is null
                    ? Brushes.Transparent
                    : theme.GetPieceBrush(piece.Color);
                square.Background = theme.GetSquareBrush(
                    position,
                    selectedSquare == position,
                    selectedMoves.Contains(position));
            }
        }
    }

    private string GetTurnText()
    {
        return game.CurrentTurn == PieceColor.White ? "Weiss am Zug" : "Schwarz am Zug";
    }
}
