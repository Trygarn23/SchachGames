using System.Windows;
using System.Windows.Input;
using Schach.Logic;
using Schach.UI;
using Schach.Visuals;

namespace Schach;

public partial class MainWindow : Window
{
    private readonly ChessGame game = new();
    private readonly ChessBoardView boardView;
    private readonly List<string> capturedByWhite = [];
    private readonly List<string> capturedByBlack = [];
    private int moveCount;

    public MainWindow()
    {
        InitializeComponent();

        boardView = new ChessBoardView(game, new BoardTheme());
        boardView.StatusChanged += UpdateStatus;
        boardView.MoveCompleted += RecordMove;
        BoardHost.Child = boardView;

        ResetUiState();
        UpdateStatus("Weiss am Zug");
    }

    private void RecordMove(MoveResult move)
    {
        moveCount++;

        string color = move.MovedPiece.Color == PieceColor.White ? "Weiss" : "Schwarz";
        string historyEntry = $"{moveCount}. {color}: {move.Notation}";
        if (move.CapturedPiece is not null)
        {
            historyEntry += $" x {move.CapturedPiece.Symbol}";
            AddCapturedPiece(move.CapturedPiece);
        }

        LastMoveText.Text = historyEntry;
        MoveHistoryList.Items.Add(historyEntry);
        MoveHistoryList.ScrollIntoView(historyEntry);
    }

    private void AddCapturedPiece(ChessPiece capturedPiece)
    {
        if (capturedPiece.Color == PieceColor.Black)
        {
            capturedByWhite.Add(capturedPiece.Symbol);
        }
        else
        {
            capturedByBlack.Add(capturedPiece.Symbol);
        }

        CapturedByWhiteText.Text = capturedByWhite.Count == 0 ? "-" : string.Join(" ", capturedByWhite);
        CapturedByBlackText.Text = capturedByBlack.Count == 0 ? "-" : string.Join(" ", capturedByBlack);
    }

    private void UpdateStatus(string message)
    {
        string turn = game.CurrentTurn == PieceColor.White ? "Weiss" : "Schwarz";
        StatusText.Text = message.Contains("am Zug", StringComparison.Ordinal)
            ? message
            : $"{message}\n{turn} am Zug";
    }

    private void ResetUiState()
    {
        moveCount = 0;
        capturedByWhite.Clear();
        capturedByBlack.Clear();
        MoveHistoryList.Items.Clear();
        LastMoveText.Text = "Noch kein Zug";
        CapturedByWhiteText.Text = "-";
        CapturedByBlackText.Text = "-";
    }

    private void NewGameButton_Click(object sender, RoutedEventArgs e)
    {
        ResetUiState();
        boardView.StartNewGame();
    }

    private void FlipBoardCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        boardView.SetFlipped(FlipBoardCheckBox.IsChecked == true);
        boardView.Focus();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.N)
        {
            ResetUiState();
            boardView.StartNewGame();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F)
        {
            FlipBoardCheckBox.IsChecked = FlipBoardCheckBox.IsChecked != true;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            boardView.ClearSelection();
            e.Handled = true;
        }
    }
}
