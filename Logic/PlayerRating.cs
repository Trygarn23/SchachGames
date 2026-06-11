namespace Schach.Logic;

public sealed record PlayerRating(
    string PlayerName,
    int Elo = 1200,
    int GamesPlayed = 0,
    int Wins = 0,
    int Draws = 0,
    int Losses = 0)
{
    public double ScoreRate => GamesPlayed == 0 ? 0 : (Wins + Draws * 0.5) / GamesPlayed;
}
