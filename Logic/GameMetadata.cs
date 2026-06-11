namespace Schach.Logic;

public sealed record GameMetadata(
    string Event = "Casual Game",
    string Site = "Schach",
    string WhitePlayer = "Weiss",
    string BlackPlayer = "Schwarz",
    DateTime? Date = null,
    GameVariant Variant = GameVariant.Classic,
    string Result = "*");
