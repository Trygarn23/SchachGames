using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
    private readonly List<ChessPiece> capturedByWhite = [];
    private readonly List<ChessPiece> capturedByBlack = [];
    private AppSettings appSettings = new();
    private TimeSpan whiteTime = TimeSpan.FromMinutes(10);
    private TimeSpan blackTime = TimeSpan.FromMinutes(10);
    private int clockIncrementSeconds;
    private int aiGeneration;
    private bool isClockPaused;
    private bool isAiThinking;
    private bool isLoadingSettings = true;
    private bool isReplaying;
    private bool isSidebarCollapsed;
    private List<MoveRecord> analysisMoves = [];
    private int analysisIndex = -1;

    public MainWindow()
    {
        InitializeComponent();

        boardView = new ChessBoardView(game, new BoardTheme());
        boardView.PromotionRequested = RequestPromotion;
        boardView.StatusChanged += UpdateStatus;
        boardView.MoveCompleted += RecordMove;
        BoardHost.Child = boardView;
        MoveHistoryList.SelectionChanged += MoveHistoryList_SelectionChanged;

        clockTimer.Interval = TimeSpan.FromSeconds(1);
        clockTimer.Tick += ClockTimer_Tick;
        clockTimer.Start();

        appSettings = settingsService.Load();
        RestoreWindowBounds();
        SelectComboBoxItemByTag(GameModeComboBox, appSettings.GameMode);
        SelectComboBoxItemByTag(GameVariantComboBox, appSettings.GameVariant);
        SelectComboBoxItemByTag(AiDifficultyComboBox, appSettings.AiDifficulty);
        SelectComboBoxItemByTag(AiSideComboBox, appSettings.AiSide);
        SelectComboBoxItemByTag(AiPersonalityComboBox, appSettings.AiPersonality);
        SelectComboBoxItemByTag(BoardThemeComboBox, appSettings.BoardTheme);
        AnimationCheckBox.IsChecked = appSettings.AnimationsEnabled;
        SoundCheckBox.IsChecked = appSettings.SoundsEnabled;
        SelectClockPreset(appSettings.ClockMinutes, appSettings.ClockIncrementSeconds);
        isLoadingSettings = false;
        ApplySelectedBoardTheme(saveSettings: false);
        ApplyAnimationSetting(saveSettings: false);
        ApplyClockPreset(saveSettings: false);
        Closing += (_, _) => SaveWindowBounds();

        ResetUiState();
        UpdateStatus(game.GetStatusText("Spiel bereit"));
    }

    private void RecordMove(MoveResult move)
    {
        string color = move.MovedPiece.Color == PieceColor.White ? "Weiss" : "Schwarz";
        string notation = game.MoveHistory.LastOrDefault()?.Notation ?? move.Notation;
        string historyEntry = $"{move.MoveNumber}. {color}: {notation}{(IsAiSide(move.MovedPiece.Color) ? " (KI)" : string.Empty)}";
        if (move.CapturedPiece is not null)
        {
            historyEntry += $" x {move.CapturedPiece.Symbol}";
            AddCapturedPiece(move.CapturedPiece);
        }

        ApplyClockIncrement(move.MovedPiece.Color);
        LastMoveText.Text = historyEntry;
        MoveHistoryList.Items.Add(historyEntry);
        MoveHistoryList.ScrollIntoView(historyEntry);
        PlayMoveSound(move);
        UpdateSideInfo(GetCoachMessage(move));
        if (game.IsGameOver)
        {
            UpdateStatus(game.GetStatusText(notation));
        }

        ShowGameEndDialogIfNeeded();

        if (!isReplaying)
        {
            QueueAiMoveIfNeeded();
        }
    }

    private void AddCapturedPiece(ChessPiece capturedPiece)
    {
        if (capturedPiece.Color == PieceColor.Black)
        {
            capturedByWhite.Add(capturedPiece);
        }
        else
        {
            capturedByBlack.Add(capturedPiece);
        }

        CapturedByWhiteText.Text = FormatCapturedPieces(capturedByWhite);
        CapturedByBlackText.Text = FormatCapturedPieces(capturedByBlack);
    }

    private static string FormatCapturedPieces(IEnumerable<ChessPiece> pieces)
    {
        string text = string.Join(" ", pieces.OrderByDescending(piece => GetPieceValue(piece.Type)).Select(piece => piece.Symbol));
        return string.IsNullOrWhiteSpace(text) ? "-" : text;
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
        UpdateSideInfo();
        whiteTime = TimeSpan.FromMinutes(10);
        blackTime = TimeSpan.FromMinutes(appSettings.ClockMinutes);
        whiteTime = TimeSpan.FromMinutes(appSettings.ClockMinutes);
        boardView.IsEnabled = true;
        boardView.InteractionLocked = false;
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
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.K)
        {
            ShowCommandPalette();
            e.Handled = true;
            return;
        }

        if (isAiThinking && e.Key != Key.N)
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.N)
        {
            StartNewGame();
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

    private void AiSideComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isLoadingSettings)
        {
            return;
        }

        appSettings.AiSide = GetSelectedComboBoxTag(AiSideComboBox) ?? "Black";
        settingsService.Save(appSettings);
        QueueAiMoveIfNeeded();
    }

    private void GameModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isLoadingSettings)
        {
            return;
        }

        appSettings.GameMode = GetSelectedComboBoxTag(GameModeComboBox) ?? "HumanVsAi";
        settingsService.Save(appSettings);
        QueueAiMoveIfNeeded();
    }

    private void GameVariantComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isLoadingSettings)
        {
            return;
        }

        appSettings.GameVariant = GetSelectedComboBoxTag(GameVariantComboBox) ?? GameVariant.Classic.ToString();
        settingsService.Save(appSettings);
        StartNewGame();
    }

    private void AiPersonalityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isLoadingSettings)
        {
            return;
        }

        appSettings.AiPersonality = GetSelectedComboBoxTag(AiPersonalityComboBox) ?? AiPersonality.Balanced.ToString();
        settingsService.Save(appSettings);
        UpdateSideInfo();
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
        if (game.IsGameOver || isClockPaused)
        {
            return;
        }

        if (game.CurrentTurn == PieceColor.White)
        {
            whiteTime = whiteTime.Subtract(TimeSpan.FromSeconds(1));
            if (whiteTime <= TimeSpan.Zero)
            {
                whiteTime = TimeSpan.Zero;
                game.Timeout(PieceColor.White);
                UpdateStatus("Zeit abgelaufen. Schwarz gewinnt.");
                ShowGameEndDialogIfNeeded();
            }
        }
        else
        {
            blackTime = blackTime.Subtract(TimeSpan.FromSeconds(1));
            if (blackTime <= TimeSpan.Zero)
            {
                blackTime = TimeSpan.Zero;
                game.Timeout(PieceColor.Black);
                UpdateStatus("Zeit abgelaufen. Weiss gewinnt.");
                ShowGameEndDialogIfNeeded();
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
            appSettings.LastGameFile = dialog.FileName;
            settingsService.Save(appSettings);
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
            appSettings.LastGameFile = dialog.FileName;
            settingsService.Save(appSettings);
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

    private void ExportFenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new() { Filter = "FEN (*.fen)|*.fen|Text (*.txt)|*.txt", FileName = "stellung.fen" };
        if (dialog.ShowDialog(this) == true)
        {
            File.WriteAllText(dialog.FileName, game.ToFen());
            UpdateStatus(game.GetStatusText("FEN exportiert"));
        }
    }

    private void ImportFenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new() { Filter = "FEN (*.fen)|*.fen|Text (*.txt)|*.txt|Alle Dateien (*.*)|*.*" };
        if (dialog.ShowDialog(this) == true)
        {
            game.LoadFen(File.ReadAllText(dialog.FileName).Trim());
            ReloadGameFromModel("FEN importiert");
        }
    }

    private void ExportScreenshotMenuItem_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new() { Filter = "PNG (*.png)|*.png", FileName = "schach.png" };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        RenderTargetBitmap bitmap = new((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(this);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(dialog.FileName);
        encoder.Save(stream);
        UpdateStatus(game.GetStatusText("Screenshot exportiert"));
    }

    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Window dialog = new()
        {
            Title = "Einstellungen",
            Owner = this,
            Width = 360,
            Height = 260,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        TextBox minutesBox = new() { Text = appSettings.ClockMinutes.ToString(), Margin = new Thickness(0, 4, 0, 10) };
        TextBox incrementBox = new() { Text = appSettings.ClockIncrementSeconds.ToString(), Margin = new Thickness(0, 4, 0, 10) };
        CheckBox animationsBox = new() { Content = "Animationen", IsChecked = appSettings.AnimationsEnabled, Margin = new Thickness(0, 4, 0, 6) };
        CheckBox soundsBox = new() { Content = "Sounds", IsChecked = appSettings.SoundsEnabled, Margin = new Thickness(0, 4, 0, 12) };

        StackPanel panel = new() { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock { Text = "Minuten pro Seite" });
        panel.Children.Add(minutesBox);
        panel.Children.Add(new TextBlock { Text = "Inkrement in Sekunden" });
        panel.Children.Add(incrementBox);
        panel.Children.Add(animationsBox);
        panel.Children.Add(soundsBox);

        Button saveButton = new() { Content = "Speichern", Height = 32, MinWidth = 90, HorizontalAlignment = HorizontalAlignment.Right };
        saveButton.Click += (_, _) =>
        {
            if (int.TryParse(minutesBox.Text, out int minutes) && minutes > 0 &&
                int.TryParse(incrementBox.Text, out int increment) && increment >= 0)
            {
                appSettings.ClockMinutes = minutes;
                appSettings.ClockIncrementSeconds = increment;
                appSettings.AnimationsEnabled = animationsBox.IsChecked == true;
                appSettings.SoundsEnabled = soundsBox.IsChecked == true;
                settingsService.Save(appSettings);
                AnimationCheckBox.IsChecked = appSettings.AnimationsEnabled;
                SoundCheckBox.IsChecked = appSettings.SoundsEnabled;
                clockIncrementSeconds = increment;
                ResetUiState();
                dialog.DialogResult = true;
            }
        };
        panel.Children.Add(saveButton);
        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private void ResignMenuItem_Click(object sender, RoutedEventArgs e)
    {
        game.Resign(game.CurrentTurn);
        UpdateStatus(game.GetStatusText($"{(game.CurrentTurn == PieceColor.White ? "Weiss" : "Schwarz")} gibt auf"));
        ShowGameEndDialogIfNeeded();
    }

    private void DrawMenuItem_Click(object sender, RoutedEventArgs e)
    {
        game.AcceptDraw();
        UpdateStatus(game.GetStatusText("Remis vereinbart"));
        ShowGameEndDialogIfNeeded();
    }

    private void HelpMenuItem_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "Bedienung:\n" +
            "- Klick: Figur auswaehlen und ziehen.\n" +
            "- Drag-and-drop: Figur direkt ziehen.\n" +
            "- Rechtsklick: Feldmarkierung wechseln.\n" +
            "- N: neues Spiel, F: Brett drehen, Esc: Auswahl aufheben.\n\n" +
            "Fun Modes:\n" +
            "- Chess960: zufaellige Grundstellung mit Laeufern auf unterschiedlichen Farben.\n" +
            "- King of the Hill: Koenig gewinnt auf d4/e4/d5/e5.\n" +
            "- Drei-Schach: Das dritte gegebene Schach gewinnt.",
            "Kurzhilfe",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void BestMoveButton_Click(object sender, RoutedEventArgs e)
    {
        AiDifficulty difficulty = GetSelectedAiDifficulty() ?? AiDifficulty.Hard;
        IReadOnlyList<string> candidates = chessAi.GetCandidateSummaries(game, difficulty, GetSelectedAiPersonality(), maxCount: 3);
        AiCandidatesText.Text = candidates.Count == 0
            ? "Keine legalen Kandidaten."
            : string.Join("\n", candidates);
    }

    private void QuickStartButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag })
        {
            return;
        }

        isLoadingSettings = true;
        switch (tag)
        {
            case "Blitz":
                SelectComboBoxItemByTag(GameVariantComboBox, GameVariant.Classic.ToString());
                SelectComboBoxItemByTag(ClockPresetComboBox, "3|2");
                SelectComboBoxItemByTag(GameModeComboBox, "HumanVsAi");
                break;
            case "Fun":
                SelectComboBoxItemByTag(GameVariantComboBox, GameVariant.Chess960.ToString());
                SelectComboBoxItemByTag(GameModeComboBox, "HumanVsAi");
                break;
            case "Analysis":
                SelectComboBoxItemByTag(GameVariantComboBox, GameVariant.Classic.ToString());
                SelectComboBoxItemByTag(GameModeComboBox, "HumanVsHuman");
                SelectComboBoxItemByTag(AiDifficultyComboBox, "Off");
                break;
            default:
                SelectComboBoxItemByTag(GameVariantComboBox, GameVariant.Classic.ToString());
                SelectComboBoxItemByTag(GameModeComboBox, "HumanVsAi");
                break;
        }
        isLoadingSettings = false;

        ApplyClockPreset(saveSettings: true);
        appSettings.GameMode = GetSelectedComboBoxTag(GameModeComboBox) ?? appSettings.GameMode;
        appSettings.GameVariant = GetSelectedComboBoxTag(GameVariantComboBox) ?? appSettings.GameVariant;
        settingsService.Save(appSettings);
        StartNewGame();
    }

    private void CommandPaletteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ShowCommandPalette();
    }

    private void ToggleSidebarMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ToggleSidebar();
    }

    private void ShowCommandPalette()
    {
        Window dialog = new()
        {
            Title = "Command Palette",
            Owner = this,
            Width = 360,
            Height = 340,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        StackPanel panel = new() { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = "Aktion waehlen",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        });

        AddCommandButton(panel, "Neues Spiel", StartNewGame);
        AddCommandButton(panel, "Bester Zug", () => BestMoveButton_Click(this, new RoutedEventArgs()));
        AddCommandButton(panel, "Brett drehen", () => FlipBoardCheckBox.IsChecked = FlipBoardCheckBox.IsChecked != true);
        AddCommandButton(panel, "Sidebar umschalten", ToggleSidebar);
        AddCommandButton(panel, "Uhr pausieren/fortsetzen", () => PauseClockMenuItem_Click(this, new RoutedEventArgs()));
        AddCommandButton(panel, "Kurzhilfe", () => HelpMenuItem_Click(this, new RoutedEventArgs()));

        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private static void AddCommandButton(Panel panel, string label, Action action)
    {
        Button button = new()
        {
            Content = label,
            Height = 32,
            Margin = new Thickness(0, 0, 0, 8)
        };
        button.Click += (_, _) => action();
        panel.Children.Add(button);
    }

    private void ToggleSidebar()
    {
        isSidebarCollapsed = !isSidebarCollapsed;
        SidebarPanel.Visibility = isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        SideColumn.Width = isSidebarCollapsed ? new GridLength(0) : new GridLength(320);
        SideColumn.MinWidth = isSidebarCollapsed ? 0 : 320;
    }

    private void PauseClockMenuItem_Click(object sender, RoutedEventArgs e)
    {
        isClockPaused = !isClockPaused;
        UpdateStatus(game.GetStatusText(isClockPaused ? "Uhr pausiert" : "Uhr laeuft weiter"));
    }

    private void PreviousMoveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        NavigateHistory(-1);
    }

    private void NextMoveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        NavigateHistory(1);
    }

    private void PreviousMoveButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateHistory(-1);
    }

    private void NextMoveButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateHistory(1);
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void QueueAiMoveIfNeeded()
    {
        AiDifficulty? difficulty = GetSelectedAiDifficulty();
        if (difficulty is null || isAiThinking || game.IsGameOver || !IsAiSide(game.CurrentTurn))
        {
            return;
        }

        int generation = ++aiGeneration;
        isAiThinking = true;
        boardView.InteractionLocked = true;
        UpdateStatus("KI denkt...");

        try
        {
            await Task.Delay(GetAiDelay(difficulty.Value));
            if (generation != aiGeneration || game.IsGameOver || !IsAiSide(game.CurrentTurn))
            {
                return;
            }

            LegalMove? aiMove = chessAi.SelectMove(game, difficulty.Value, GetSelectedAiPersonality());
            if (aiMove is null)
            {
                UpdateStatus("KI hat keinen Zug");
                return;
            }

            boardView.TryPlayMove(aiMove);
        }
        finally
        {
            boardView.InteractionLocked = false;
            isAiThinking = false;
            boardView.Focus();
        }
    }

    private AiDifficulty? GetSelectedAiDifficulty()
    {
        if (AiDifficultyComboBox?.SelectedItem is not ComboBoxItem item ||
            item.Tag is not string tag ||
            tag == "Off" ||
            GetSelectedComboBoxTag(GameModeComboBox) == "HumanVsHuman")
        {
            return null;
        }

        return Enum.TryParse(tag, out AiDifficulty difficulty)
            ? difficulty
            : null;
    }

    private void StartNewGame()
    {
        aiGeneration++;
        analysisIndex = -1;
        analysisMoves.Clear();
        isClockPaused = false;
        ResetUiState();
        boardView.StartNewGame(GetSelectedGameVariant());
        UpdateStatus(game.GetStatusText(GetVariantStartMessage(game.Variant)));
        QueueAiMoveIfNeeded();
    }

    private void ReloadGameFromModel(string message)
    {
        aiGeneration++;
        ResetUiState();
        isLoadingSettings = true;
        SelectComboBoxItemByTag(GameVariantComboBox, game.Variant.ToString());
        isLoadingSettings = false;
        RebuildMoveUiFromHistory();
        boardView.ReloadFromGame(message);
        if (!isReplaying)
        {
            QueueAiMoveIfNeeded();
        }
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

        UpdateSideInfo();
    }

    private void MoveHistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int index = MoveHistoryList.SelectedIndex;
        if (index >= 0 && index < game.MoveHistory.Count)
        {
            boardView.HighlightMove(game.MoveHistory[index]);
        }
    }

    private PieceType RequestPromotion(PieceColor color)
    {
        Window dialog = new()
        {
            Title = $"{(color == PieceColor.White ? "Weiss" : "Schwarz")} wandelt um",
            Owner = this,
            Width = 330,
            Height = 150,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        PieceType selected = PieceType.Queen;
        StackPanel panel = new() { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = "Figur waehlen",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        });

        WrapPanel buttons = new();
        foreach ((string label, PieceType type) in new[]
        {
            ("Dame", PieceType.Queen),
            ("Turm", PieceType.Rook),
            ("Laeufer", PieceType.Bishop),
            ("Springer", PieceType.Knight)
        })
        {
            Button button = new()
            {
                Content = label,
                Tag = type,
                MinWidth = 68,
                Margin = new Thickness(0, 0, 8, 8)
            };
            button.Click += (_, _) =>
            {
                selected = (PieceType)button.Tag;
                dialog.DialogResult = true;
            };
            buttons.Children.Add(button);
        }

        panel.Children.Add(buttons);
        dialog.Content = panel;
        dialog.ShowDialog();
        return selected;
    }

    private void ClockPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyClockPreset(saveSettings: !isLoadingSettings);
    }

    private void ApplyClockPreset(bool saveSettings)
    {
        if (ClockPresetComboBox?.SelectedItem is not ComboBoxItem item ||
            item.Tag is not string tag)
        {
            return;
        }

        string[] parts = tag.Split('|');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out int minutes) ||
            !int.TryParse(parts[1], out int increment))
        {
            return;
        }

        appSettings.ClockMinutes = minutes;
        appSettings.ClockIncrementSeconds = increment;
        clockIncrementSeconds = increment;
        if (saveSettings)
        {
            settingsService.Save(appSettings);
            ResetUiState();
        }
    }

    private void AnimationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        ApplyAnimationSetting(saveSettings: !isLoadingSettings);
    }

    private void ApplyAnimationSetting(bool saveSettings)
    {
        if (boardView is null || AnimationCheckBox is null)
        {
            return;
        }

        boardView.AnimationsEnabled = AnimationCheckBox.IsChecked == true;
        if (saveSettings)
        {
            appSettings.AnimationsEnabled = boardView.AnimationsEnabled;
            settingsService.Save(appSettings);
        }
    }

    private void SoundCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (isLoadingSettings)
        {
            return;
        }

        appSettings.SoundsEnabled = SoundCheckBox.IsChecked == true;
        settingsService.Save(appSettings);
    }

    private void ApplyClockIncrement(PieceColor movedColor)
    {
        if (clockIncrementSeconds <= 0)
        {
            return;
        }

        if (movedColor == PieceColor.White)
        {
            whiteTime = whiteTime.Add(TimeSpan.FromSeconds(clockIncrementSeconds));
        }
        else
        {
            blackTime = blackTime.Add(TimeSpan.FromSeconds(clockIncrementSeconds));
        }

        UpdateClockText();
    }

    private void ShowGameEndDialogIfNeeded()
    {
        if (!game.IsGameOver)
        {
            return;
        }

        PlayGameEndSound();
        MessageBoxResult result = MessageBox.Show(
            this,
            StatusText.Text,
            "Spiel beendet",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information,
            MessageBoxResult.No);
        if (result == MessageBoxResult.Yes)
        {
            StartNewGame();
        }
    }

    private bool IsAiSide(PieceColor color)
    {
        return GetSelectedComboBoxTag(GameModeComboBox) == "HumanVsAi" &&
            GetSelectedAiDifficulty() is not null &&
            GetSelectedComboBoxTag(AiSideComboBox) == (color == PieceColor.White ? "White" : "Black");
    }

    private static int GetAiDelay(AiDifficulty difficulty)
    {
        return difficulty switch
        {
            AiDifficulty.Easy => 180,
            AiDifficulty.Medium => 260,
            AiDifficulty.Hard => 380,
            _ => 250
        };
    }

    private void PlayMoveSound(MoveResult move)
    {
        if (appSettings.SoundsEnabled)
        {
            (move.CapturedPiece is null ? SystemSounds.Asterisk : SystemSounds.Exclamation).Play();
        }
    }

    private void PlayGameEndSound()
    {
        if (appSettings.SoundsEnabled)
        {
            SystemSounds.Hand.Play();
        }
    }

    private void SelectClockPreset(int minutes, int increment)
    {
        SelectComboBoxItemByTag(ClockPresetComboBox, $"{minutes}|{increment}");
    }

    private void RestoreWindowBounds()
    {
        if (appSettings.WindowWidth is > 0)
        {
            Width = appSettings.WindowWidth.Value;
        }

        if (appSettings.WindowHeight is > 0)
        {
            Height = appSettings.WindowHeight.Value;
        }

        if (appSettings.WindowLeft is not null)
        {
            Left = appSettings.WindowLeft.Value;
        }

        if (appSettings.WindowTop is not null)
        {
            Top = appSettings.WindowTop.Value;
        }
    }

    private void SaveWindowBounds()
    {
        appSettings.WindowLeft = Left;
        appSettings.WindowTop = Top;
        appSettings.WindowWidth = Width;
        appSettings.WindowHeight = Height;
        settingsService.Save(appSettings);
    }

    private void NavigateHistory(int delta)
    {
        if (game.MoveHistory.Count == 0)
        {
            return;
        }

        if (analysisMoves.Count == 0)
        {
            analysisMoves = game.MoveHistory.ToList();
            analysisIndex = analysisMoves.Count;
        }

        analysisIndex = Math.Clamp(analysisIndex + delta, 0, analysisMoves.Count);
        IReadOnlyList<GameFileService.SavedMove> moves = analysisMoves
            .Take(analysisIndex)
            .Select(move => new GameFileService.SavedMove(move.From.ToAlgebraic(), move.To.ToAlgebraic()))
            .ToList();

        isReplaying = true;
        gameFileService.ReplayMoves(game, moves, game.Variant);
        ReloadGameFromModel($"Analysezug {analysisIndex}/{analysisMoves.Count}");
        isReplaying = false;
    }

    private void UpdateSideInfo(string? coachMessage = null)
    {
        int balance = GetMaterialBalanceForWhite();
        MaterialBalanceText.Text = balance switch
        {
            > 0 => $"+{balance} fuer Weiss",
            < 0 => $"{balance} fuer Schwarz",
            _ => "Ausgeglichen"
        };

        EvaluationText.Text = $"Bewertung: {balance:+#;-#;0}";
        EvaluationBar.Value = Math.Clamp(50 + balance * 4, 0, 100);

        AiDifficulty difficulty = GetSelectedAiDifficulty() ?? AiDifficulty.Medium;
        IReadOnlyList<string> candidates = chessAi.GetCandidateSummaries(game, difficulty, GetSelectedAiPersonality(), maxCount: 3);
        AiCandidatesText.Text = candidates.Count == 0
            ? "Noch keine Kandidaten."
            : string.Join("\n", candidates);

        if (!string.IsNullOrWhiteSpace(coachMessage))
        {
            CoachText.Text = coachMessage;
        }
    }

    private int GetMaterialBalanceForWhite()
    {
        int balance = 0;
        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                ChessPiece? piece = game.GetPiece(new BoardPosition(row, column));
                if (piece is null || piece.Type == PieceType.King)
                {
                    continue;
                }

                int value = GetPieceValue(piece.Type) / 100;
                balance += piece.Color == PieceColor.White ? value : -value;
            }
        }

        return balance;
    }

    private static string GetCoachMessage(MoveResult move)
    {
        if (move.Kind == MoveKind.Promotion)
        {
            return "Coach: Umwandlung erreicht. Das ist fast immer ein entscheidender Vorteil.";
        }

        if (move.CapturedPiece is not null)
        {
            return $"Coach: Schlagzug gesehen. Materialwert {GetPieceValue(move.CapturedPiece.Type) / 100}.";
        }

        if (move.Kind is MoveKind.CastlingKingSide or MoveKind.CastlingQueenSide)
        {
            return "Coach: Rochade bringt den Koenig meist sicherer ins Spiel.";
        }

        return "Coach: Pruefe nach jedem Zug, welche gegnerischen Schachs und Schlagzuege entstehen.";
    }

    private GameVariant GetSelectedGameVariant()
    {
        string tag = GetSelectedComboBoxTag(GameVariantComboBox) ?? GameVariant.Classic.ToString();
        return Enum.TryParse(tag, out GameVariant variant) ? variant : GameVariant.Classic;
    }

    private AiPersonality GetSelectedAiPersonality()
    {
        string tag = GetSelectedComboBoxTag(AiPersonalityComboBox) ?? AiPersonality.Balanced.ToString();
        return Enum.TryParse(tag, out AiPersonality personality) ? personality : AiPersonality.Balanced;
    }

    private static string GetVariantStartMessage(GameVariant variant)
    {
        return variant switch
        {
            GameVariant.Chess960 => "Chess960 gestartet",
            GameVariant.PawnsWar => "Bauernkrieg gestartet",
            GameVariant.KnightsDuel => "Springerduell gestartet",
            GameVariant.KingOfTheHill => "King of the Hill gestartet",
            GameVariant.ThreeCheck => "Drei-Schach gestartet",
            GameVariant.NoQueens => "Ohne-Damen-Modus gestartet",
            _ => "Neues Spiel gestartet"
        };
    }

    private static int GetPieceValue(PieceType type)
    {
        return type switch
        {
            PieceType.Pawn => 100,
            PieceType.Knight => 320,
            PieceType.Bishop => 330,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            PieceType.King => 20_000,
            _ => 0
        };
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
