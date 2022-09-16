using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Possible optimization chances with replacing variables with hardcoded values (masks etc.)
public static class Piece
{
    // Piece values are in bits
    public const byte None = 0; // 00000
    public const byte Pawn = 4; // 00100
    public const byte Knight = 2; // 00010
    public const byte Bishop = 5; // 00101
    public const byte Rook = 3; // 00011
    public const byte Queen = 1; // 00001
    public const byte King = 6; // 00110

    public const byte White = 8; // 01000
    public const byte Black = 16; // 10000

    // Masks for the class
    private const byte PieceColorMask = 24; // 11000
    private const byte PieceTypeMask = 7; // 00111
    private const byte SlidingPieceMask = 1; // 00001
    private const byte DiagonalPieceMask = 3; // 00011
    private const byte OrthogonalPieceMask = 5; // 00101

    // Names for the colors and pieces
    public static char[] ShortColorNames = new char[2] { 'w', 'b' };
    public static string[] ColorNames = new string[2] { "white", "black" };
    public static char[] ShortPieceNames = new char[7] { '?', 'Q', 'N', 'R', 'P', 'B', 'K' };
    public static string[] PieceNames = new string[7] { "None", "Queen", "Knight", "Rook", "Pawn", "Bishop", "King" };

    // Get the color of the piece
    public static int Color(int piece)
    {
        return piece & PieceColorMask;
    }

    // Get the opposite color
    public static int OppositeColor(int color)
    {
        return 24 - color;
    }

    // Get the type of the piece
    public static int Type(int piece)
    {
        return piece & PieceTypeMask;
    }

    // Can the piece move in direct paths?
    public static bool IsSlidingPiece(int piece)
    {
        return (piece & SlidingPieceMask) == 1;
    }

    // Can the piece move on diagonals?
    public static bool IsDiagonalPiece(int piece)
    {
        return (piece & DiagonalPieceMask) == 1;
    }

    // Can the piece move along ranks and files?
    public static bool IsOrthogonalPiece(int piece)
    {
        return (piece & OrthogonalPieceMask) == 1;
    }
}
