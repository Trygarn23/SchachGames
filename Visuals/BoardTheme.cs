using System.Windows.Media;
using Schach.Logic;

namespace Schach.Visuals;

public sealed class BoardTheme
{
    private static readonly Brush LightSquare = CreateBrush(232, 218, 186);
    private static readonly Brush DarkSquare = CreateBrush(100, 126, 92);
    private static readonly Brush SelectedSquare = CreateBrush(232, 193, 106);
    private static readonly Brush LightMoveSquare = CreateBrush(175, 188, 124);
    private static readonly Brush DarkMoveSquare = CreateBrush(126, 145, 84);
    private static readonly Brush WhitePiece = CreateBrush(244, 241, 232);
    private static readonly Brush BlackPiece = CreateBrush(35, 38, 44);

    public Brush GetSquareBrush(BoardPosition position, bool isSelected, bool isLegalMove)
    {
        if (isSelected)
        {
            return SelectedSquare;
        }

        bool isLightSquare = (position.Row + position.Column) % 2 == 0;
        if (isLegalMove)
        {
            return isLightSquare ? LightMoveSquare : DarkMoveSquare;
        }

        return isLightSquare ? LightSquare : DarkSquare;
    }

    public Brush GetPieceBrush(PieceColor color)
    {
        return color == PieceColor.White ? WhitePiece : BlackPiece;
    }

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue)
    {
        SolidColorBrush brush = new(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
