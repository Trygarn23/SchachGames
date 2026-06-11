namespace Schach.Persistence;

public sealed class AppSettings
{
    public string BoardTheme { get; set; } = "Dark";

    public string AiDifficulty { get; set; } = "Off";

    public string AiSide { get; set; } = "Black";

    public string GameMode { get; set; } = "HumanVsAi";

    public string GameVariant { get; set; } = "Classic";

    public string AiPersonality { get; set; } = "Balanced";

    public int ClockMinutes { get; set; } = 10;

    public int ClockIncrementSeconds { get; set; }

    public bool AnimationsEnabled { get; set; } = true;

    public bool SoundsEnabled { get; set; }

    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public double? WindowWidth { get; set; }

    public double? WindowHeight { get; set; }

    public string? LastGameFile { get; set; }
}
