using System.Windows.Media;
using Schach.Logic;

namespace Schach.Visuals;

public sealed class BoardTheme
{
    private static readonly Brush WhitePiece = CreateBrush(244, 241, 232);
    private static readonly Brush BlackPiece = CreateBrush(35, 38, 44);
    private static readonly Brush YellowMarker = CreateBrush(241, 196, 75);
    private static readonly Brush RedMarker = CreateBrush(217, 95, 95);
    private static readonly Brush BlueMarker = CreateBrush(89, 151, 213);
    private static readonly Brush FocusBrush = CreateBrush(245, 245, 245);

    private BoardThemeMode mode = BoardThemeMode.Dark;

    public BoardThemeMode Mode
    {
        get => mode;
        set => mode = value;
    }

    public Brush GetSquareBrush(
        BoardPosition position,
        bool isSelected,
        bool isLegalMove,
        bool isLastMove = false,
        bool isKingInCheck = false)
    {
        if (isKingInCheck)
        {
            return CreateBrush(197, 65, 65);
        }

        if (isSelected)
        {
            return CreateBrush(232, 193, 106);
        }

        bool isLightSquare = (position.Row + position.Column) % 2 == 0;
        if (isLegalMove)
        {
            return isLightSquare
                ? CreateBrush(175, 188, 124)
                : CreateBrush(126, 145, 84);
        }

        if (isLastMove)
        {
            return CreateBrush(198, 176, 86);
        }

        return Mode == BoardThemeMode.Light
            ? GetLightThemeSquare(isLightSquare)
            : GetDarkThemeSquare(isLightSquare);
    }

    public Brush GetPieceBrush(PieceColor color)
    {
        return color == PieceColor.White ? WhitePiece : BlackPiece;
    }

    public Brush GetMarkerBrush(BoardMarkerColor markerColor)
    {
        return markerColor switch
        {
            BoardMarkerColor.Red => RedMarker,
            BoardMarkerColor.Blue => BlueMarker,
            _ => YellowMarker
        };
    }

    public Brush GetFocusBrush()
    {
        return FocusBrush;
    }

    private static Brush GetDarkThemeSquare(bool isLightSquare)
    {
        return isLightSquare
            ? CreateBrush(232, 218, 186)
            : CreateBrush(100, 126, 92);
    }

    private static Brush GetLightThemeSquare(bool isLightSquare)
    {
        return isLightSquare
            ? CreateBrush(238, 238, 228)
            : CreateBrush(128, 156, 122);
    }

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue)
    {
        SolidColorBrush brush = new(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
