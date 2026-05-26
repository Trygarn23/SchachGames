namespace Schach.Logic;

public readonly record struct BoardPosition(int Row, int Column)
{
    public bool IsValid => Row >= 0 && Row < 8 && Column >= 0 && Column < 8;

    public string ToAlgebraic()
    {
        char file = (char)('a' + Column);
        int rank = 8 - Row;
        return $"{file}{rank}";
    }
}
