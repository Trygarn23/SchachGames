namespace Schach.Logic;

public readonly record struct BoardPosition(int Row, int Column)
{
    public bool IsValid => Row >= 0 && Row < 8 && Column >= 0 && Column < 8;

    public static bool TryParse(string value, out BoardPosition position)
    {
        position = default;
        if (value.Length != 2)
        {
            return false;
        }

        char file = char.ToLowerInvariant(value[0]);
        char rank = value[1];
        if (file is < 'a' or > 'h' || rank is < '1' or > '8')
        {
            return false;
        }

        position = new BoardPosition(8 - (rank - '0'), file - 'a');
        return true;
    }

    public string ToAlgebraic()
    {
        char file = (char)('a' + Column);
        int rank = 8 - Row;
        return $"{file}{rank}";
    }
}
