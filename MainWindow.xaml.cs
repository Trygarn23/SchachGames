using System.Windows;
using Schach.Logic;
using Schach.UI;
using Schach.Visuals;

namespace Schach;

public partial class MainWindow : Window
{
    private readonly ChessGame game = new();
    private readonly ChessBoardView boardView;

    public MainWindow()
    {
        InitializeComponent();

        boardView = new ChessBoardView(game, new BoardTheme());
        boardView.StatusChanged += UpdateStatus;
        BoardHost.Children.Add(boardView);

        UpdateStatus("Weiss am Zug");
    }

    private void UpdateStatus(string message)
    {
        string turn = game.CurrentTurn == PieceColor.White ? "Weiss" : "Schwarz";
        StatusText.Text = message.Contains("am Zug", StringComparison.Ordinal)
            ? message
            : $"{message}\n{turn} am Zug";
    }

    private void NewGameButton_Click(object sender, RoutedEventArgs e)
    {
        boardView.StartNewGame();
    }
}
