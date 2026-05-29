using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using Schach.AI;
using Schach.Logic;
using Schach.Persistence;
using Schach.UI;
using Schach.Visuals;

namespace Schach;

public partial class MainWindow : Window
{
    private readonly ChessGame game = new();
    private readonly ChessAi chessAi = new();
    private readonly GameFileService gameFileService = new();
    private readonly PgnService pgnService = new();
    private readonly AppSettingsService settingsService = new();
    private readonly DispatcherTimer clockTimer = new();
    private readonly ChessBoardView boardView;
    private readonly List<string> capturedByWhite = [];
    private readonly List<string> capturedByBlack = [];
    private AppSettings appSettings = new();
    private TimeSpan whiteTime = TimeSpan.FromMinutes(10);
    private TimeSpan blackTime = TimeSpan.FromMinutes(10);
    private bool isAiThinking;
    private bool isLoadingSettings = true;

    public MainWindow()
    {
        InitializeComponent();

        boardView = new ChessBoardView(game, new BoardTheme());
        boardView.StatusChanged += UpdateStatus;
        boardView.MoveCompleted += RecordMove;
        BoardHost.Child = boardView;

        clockTimer.Interval = TimeSpan.FromSeconds(1);
        clockTimer.Tick += ClockTimer_Tick;
        clockTimer.Start();

        appSettings = settingsService.Load();
        SelectComboBoxItemByTag(AiDifficultyComboBox, appSettings.AiDifficulty);
        SelectComboBoxItemByTag(BoardThemeComboBox, appSettings.BoardTheme);
        isLoadingSettings = false;
        ApplySelectedBoardTheme(saveSettings: false);

        ResetUiState();
        UpdateStatus(game.GetStatusText("Spiel bereit"));
    }

    private void RecordMove(MoveResult move)
    {
        string color = move.MovedPiece.Color == PieceColor.White ? "Weiss" : "Schwarz";
        string notation = game.MoveHistory.LastOrDefault()?.Notation ?? move.Notation;
        string historyEntry = $"{move.MoveNumber}. {color}: {notation}";
        if (move.CapturedPiece is not null)
        {
            historyEntry += $" x {move.CapturedPiece.Symbol}";
            AddCapturedPiece(move.CapturedPiece);
        }

        LastMoveText.Text = historyEntry;
        MoveHistoryList.Items.Add(historyEntry);
        MoveHistoryList.ScrollIntoView(historyEntry);

        QueueAiMoveIfNeeded();
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
        StatusText.Text = message.Contains('\n') ||
            message.Contains("am Zug", StringComparison.Ordinal) ||
            message.Contains("Schachmatt", StringComparison.Ordinal) ||
            message.Contains("Patt", StringComparison.Ordinal)
            ? message
            : $"{message}\n{turn} am Zug";
    }

    private void ResetUiState()
    {
        capturedByWhite.Clear();
        capturedByBlack.Clear();
        MoveHistoryList.Items.Clear();
        LastMoveText.Text = "Noch kein Zug";
        CapturedByWhiteText.Text = "-";
        CapturedByBlackText.Text = "-";
        whiteTime = TimeSpan.FromMinutes(10);
        blackTime = TimeSpan.FromMinutes(10);
        boardView.IsEnabled = true;
        UpdateClockText();
    }

    private void NewGameButton_Click(object sender, RoutedEventArgs e)
    {
        StartNewGame();
    }

    private void FlipBoardCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        boardView.SetFlipped(FlipBoardCheckBox.IsChecked == true);
        boardView.Focus();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (isAiThinking && e.Key != Key.N)
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.N)
        {
            ResetUiState();
            boardView.StartNewGame();
            QueueAiMoveIfNeeded();
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

    private void AiDifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isLoadingSettings)
        {
            return;
        }

        appSettings.AiDifficulty = GetSelectedComboBoxTag(AiDifficultyComboBox) ?? "Off";
        settingsService.Save(appSettings);
        QueueAiMoveIfNeeded();
    }

    private void BoardThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplySelectedBoardTheme(saveSettings: !isLoadingSettings);
    }

    private void ApplySelectedBoardTheme(bool saveSettings)
    {
        if (boardView is null)
        {
            return;
        }

        if (BoardThemeComboBox?.SelectedItem is not ComboBoxItem item ||
            item.Tag is not string tag ||
            !Enum.TryParse(tag, out BoardThemeMode themeMode))
        {
            return;
        }

        boardView.SetTheme(themeMode);
        if (saveSettings)
        {
            appSettings.BoardTheme = tag;
            settingsService.Save(appSettings);
        }
    }

    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        if (game.IsGameOver || !boardView.IsEnabled)
        {
            return;
        }

        if (game.CurrentTurn == PieceColor.White)
        {
            whiteTime = whiteTime.Subtract(TimeSpan.FromSeconds(1));
            if (whiteTime <= TimeSpan.Zero)
            {
                whiteTime = TimeSpan.Zero;
                boardView.IsEnabled = false;
                UpdateStatus("Zeit abgelaufen. Schwarz gewinnt.");
            }
        }
        else
        {
            blackTime = blackTime.Subtract(TimeSpan.FromSeconds(1));
            if (blackTime <= TimeSpan.Zero)
            {
                blackTime = TimeSpan.Zero;
                boardView.IsEnabled = false;
                UpdateStatus("Zeit abgelaufen. Weiss gewinnt.");
            }
        }

        UpdateClockText();
    }

    private void UpdateClockText()
    {
        WhiteClockText.Text = FormatClock(whiteTime);
        BlackClockText.Text = FormatClock(blackTime);
    }

    private static string FormatClock(TimeSpan time)
    {
        return $"{Math.Max(0, (int)time.TotalMinutes):00}:{Math.Max(0, time.Seconds):00}";
    }

    private void NewGameMenuItem_Click(object sender, RoutedEventArgs e)
    {
        StartNewGame();
    }

    private void SaveGameMenuItem_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new()
        {
            Filter = "Schach Spielstand (*.schach)|*.schach|JSON (*.json)|*.json",
            FileName = "spiel.schach"
        };

        if (dialog.ShowDialog(this) == true)
        {
            gameFileService.Save(game, dialog.FileName);
            UpdateStatus(game.GetStatusText("Spiel gespeichert"));
        }
    }

    private void LoadGameMenuItem_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "Schach Spielstand (*.schach;*.json)|*.schach;*.json|Alle Dateien (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            gameFileService.Load(game, dialog.FileName);
            ReloadGameFromModel("Spiel geladen");
        }
    }

    private void ExportPgnMenuItem_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new()
        {
            Filter = "PGN (*.pgn)|*.pgn|Text (*.txt)|*.txt",
            FileName = "spiel.pgn"
        };

        if (dialog.ShowDialog(this) == true)
        {
            File.WriteAllText(dialog.FileName, pgnService.Export(game));
            UpdateStatus(game.GetStatusText("PGN exportiert"));
        }
    }

    private void ImportPgnMenuItem_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "PGN (*.pgn)|*.pgn|Text (*.txt)|*.txt|Alle Dateien (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            IReadOnlyList<GameFileService.SavedMove> moves = pgnService.ImportMoves(File.ReadAllText(dialog.FileName));
            gameFileService.ReplayMoves(game, moves);
            ReloadGameFromModel("PGN importiert");
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void QueueAiMoveIfNeeded()
    {
        AiDifficulty? difficulty = GetSelectedAiDifficulty();
        if (difficulty is null || isAiThinking || game.IsGameOver || game.CurrentTurn != PieceColor.Black)
        {
            return;
        }

        isAiThinking = true;
        boardView.IsEnabled = false;
        UpdateStatus("KI denkt...");

        try
        {
            await Task.Delay(300);
            if (game.IsGameOver || game.CurrentTurn != PieceColor.Black)
            {
                return;
            }

            LegalMove? aiMove = chessAi.SelectMove(game, difficulty.Value);
            if (aiMove is null)
            {
                UpdateStatus("KI hat keinen Zug");
                return;
            }

            boardView.TryPlayMove(aiMove);
        }
        finally
        {
            boardView.IsEnabled = true;
            isAiThinking = false;
            boardView.Focus();
        }
    }

    private AiDifficulty? GetSelectedAiDifficulty()
    {
        if (AiDifficultyComboBox?.SelectedItem is not ComboBoxItem item ||
            item.Tag is not string tag ||
            tag == "Off")
        {
            return null;
        }

        return Enum.TryParse(tag, out AiDifficulty difficulty)
            ? difficulty
            : null;
    }

    private void StartNewGame()
    {
        ResetUiState();
        boardView.StartNewGame();
        QueueAiMoveIfNeeded();
    }

    private void ReloadGameFromModel(string message)
    {
        ResetUiState();
        RebuildMoveUiFromHistory();
        boardView.ReloadFromGame(message);
        QueueAiMoveIfNeeded();
    }

    private void RebuildMoveUiFromHistory()
    {
        foreach (MoveRecord move in game.MoveHistory)
        {
            string color = move.Color == PieceColor.White ? "Weiss" : "Schwarz";
            string historyEntry = $"{move.MoveNumber}. {color}: {move.Notation}";
            if (move.CapturedPiece is not null)
            {
                historyEntry += $" x {move.CapturedPiece.Symbol}";
                AddCapturedPiece(move.CapturedPiece);
            }

            LastMoveText.Text = historyEntry;
            MoveHistoryList.Items.Add(historyEntry);
        }

        if (MoveHistoryList.Items.Count > 0)
        {
            MoveHistoryList.ScrollIntoView(MoveHistoryList.Items[^1]);
        }
    }

    private static void SelectComboBoxItemByTag(ComboBox comboBox, string tag)
    {
        foreach (ComboBoxItem item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
    }

    private static string? GetSelectedComboBoxTag(ComboBox comboBox)
    {
        return comboBox.SelectedItem is ComboBoxItem item ? item.Tag?.ToString() : null;
    }
}
