using System;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.Build.Content;
using UnityEngine;

// The board class holds information about a singular game
public class Board
{
    // Board variables
    public int[] spaces;
    public int ColorToMove;
    public int HalfMoveClock;
    public int FullMoveCounter;
    public int EnPassantSquare;
    public bool WhiteCanKingsideCastle;
    public bool WhiteCanQueensideCastle;
    public bool BlackCanKingsideCastle;
    public bool BlackCanQueensideCastle;

    public List<int> whitePieces;
    public List<int> blackPieces;

    public bool GameOver;

    private GameRunner GameManager;

    // File names
    public static char[] FileNames = new char[8] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
    public static char[] RankNames = new char[8] { '1', '2', '3', '4', '5', '6', '7', '8' };

    // FEN translation
    public static Dictionary<char, int> FENToPiece;

    // Common FEN strings
    public const string ClassicStartingFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public const string EnPassantTestFEN = "rnbqkbnr/p1ppppp1/7p/Pp6/8/8/1PPPPPPP/RNBQKBNR w KQkq b6 0 3";
    public const string CastlingTestFEN = "r3k2r/pppbnppp/2np1q2/2b1p3/2B1P3/1PN2N2/PBPPQPPP/R3K2R w KQkq - 4 8";

    /* Not necessary for now
    // Use a stack for the previous board positions
    //                   en passant [ ep ][  ] 
    static uint examplePosition = 0b0000001111;
    //                    castling rights KQkq

    Stack<uint> previousBoardPositions; */

    // Create a list of the en passant oppurutunities for past moves
    private Stack<int> PastEnPassantOppurutunites;

    // The constructor for the board
    public Board(GameRunner gameRunner)
    {
        // Initialize the lists of pieces
        whitePieces = new List<int>();
        blackPieces = new List<int>();

        // Set the game manager
        GameManager = gameRunner;

        // Initialize the list of en passant oppurutunites
        PastEnPassantOppurutunites = new Stack<int>();

        // The game doesn't start dead
        GameOver = false;
    }

    // Initialize the board
    public void Initialize()
    {
        // Setup the board
        ResetBoard();

        // Some setup methods
        SetupFENDictionary();
    }

    // Clear the board and reset all the values
    public void ResetBoard()
    {
        // Reset the board to a bunch of empty spaces
        spaces = new int[64];
        for (int i = 0; i < 64; i++) { spaces[i] = 0; }

        // Reset the game info
        ColorToMove = Piece.White;
        HalfMoveClock = 0;
        FullMoveCounter = 0;
        EnPassantSquare = -1;
        WhiteCanKingsideCastle = true;
        WhiteCanQueensideCastle = true;
        BlackCanKingsideCastle = true;
        BlackCanQueensideCastle = true;

        // Reset the lists of pieces
        whitePieces.Clear();
        blackPieces.Clear();
    }

    // Initialize the fen dictionary
    public void SetupFENDictionary()
    {
        // Add the values and define the dictionary
        FENToPiece = new Dictionary<char, int>()
        {
            ['P'] = Piece.White | Piece.Pawn,
            ['N'] = Piece.White | Piece.Knight,
            ['B'] = Piece.White | Piece.Bishop,
            ['Q'] = Piece.White | Piece.Queen,
            ['K'] = Piece.White | Piece.King,
            ['R'] = Piece.White | Piece.Rook,
            ['p'] = Piece.Black | Piece.Pawn,
            ['n'] = Piece.Black | Piece.Knight,
            ['b'] = Piece.Black | Piece.Bishop,
            ['q'] = Piece.Black | Piece.Queen,
            ['k'] = Piece.Black | Piece.King,
            ['r'] = Piece.Black | Piece.Rook,
        };
    }

    // Setup the board from a FEN string
    // Thanks https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
    public void SetupFromFEN(string fen)
    {
        // Clear the game board
        ResetBoard();

        // Dissect the fen string
        string[] fenSlices = fen.Split(' ');
        string piecePositions = fenSlices[0];

        // Setup all of the pieces
        // Starting on the seventh rank
        int skippingSquares = 0;
        int stringPosition = 0;
        for (int rank = 7; rank >= 0; rank--)
        {
            // From the a file to the h file
            for (int file = 0; file < 8; file++)
            {
                // Skip squares if necessary
                if (skippingSquares > 0)
                {
                    skippingSquares--;
                    continue;
                }

                // Skip over the forward slashes '/'
                if (piecePositions[stringPosition] == '/')
                {
                    stringPosition++;
                }

                // Get the character at the current position in the string
                char currentChar = piecePositions[stringPosition];

                // Place a piece if required
                if (FENToPiece.ContainsKey(currentChar)) {
                    spaces[file + rank * 8] = FENToPiece[currentChar];
                }

                // Or skip some spaces
                else if (char.IsDigit(currentChar))
                {
                    skippingSquares = (currentChar - '0') - 1; // Using ascii wizardry
                }

                // Increment the stringPosition
                stringPosition++;
            }
        }

        // Color to move
        ColorToMove = fenSlices[1][0] == 'w' ? Piece.White : Piece.Black;

        // Castling rights
        WhiteCanKingsideCastle = fenSlices[2].Contains('K');
        WhiteCanQueensideCastle = fenSlices[2].Contains('Q');
        BlackCanKingsideCastle = fenSlices[2].Contains('k');
        BlackCanQueensideCastle = fenSlices[2].Contains('q');

        // En passant target square
        if (!fenSlices[3].Contains('-'))
        {
            EnPassantSquare = SquareNameToSquareIndex(fenSlices[3]);
        }
        else
        {
            EnPassantSquare = -1;
        }

        // Halfmove and fullmove clocks
        HalfMoveClock = int.Parse(fenSlices[4]);
        FullMoveCounter = int.Parse(fenSlices[5]);
    }

    // Make a move on the board
    public void MakeMove(Move move)
    {
        // Castling has its own set of changes
        if (move.IsCastling)
        {
            // Calculate castling coordinates
            bool isQueenside = move.Destination % 8 == 2;
            int rankOffset = (ColorToMove == Piece.White) ? 0 : 56;
            int kingsideRookOffset = isQueenside ? 0 : 7;
            int newRookPositionOffset = isQueenside ? 3 : 5;

            // Move the rook
            spaces[rankOffset + kingsideRookOffset] = Piece.None;
            spaces[rankOffset + newRookPositionOffset] = ColorToMove | Piece.Rook;
        }

        // Replace the pieces on the squares and handle promotions
        spaces[move.Origin] = Piece.None;
        spaces[move.Destination] = ColorToMove | Piece.Type(move.IsPromotion ? move.PromotionType : move.MovingPiece);

        // Castling rights
        WhiteCanKingsideCastle = WhiteCanKingsideCastle && !move.LostWhiteKingsideCastleRights;
        WhiteCanQueensideCastle = WhiteCanQueensideCastle && !move.LostWhiteQueensideCastleRights;
        BlackCanKingsideCastle = BlackCanKingsideCastle && !move.LostBlackKingsideCastleRights;
        BlackCanQueensideCastle = BlackCanQueensideCastle && !move.LostBlackQueensideCastleRights;

        // En passant
        // Add the last en passant square
        PastEnPassantOppurutunites.Push(EnPassantSquare);
        if (move.IsEnPassant)
        {
            // Remove the pawn in passing
            spaces[move.Destination + (ColorToMove == Piece.White ? -8 : 8)] = Piece.None;
        }

        // Is there an en passant oppurtunity created?
        if (move.CreatesEnPassant)
        {
            // Set the new en passant square
            EnPassantSquare = move.EnPassantSquare;
        }
        else
        {
            // Set to zero to avoid confusion when unmaking moves
            EnPassantSquare = -1;
        }

        // The color to move switches
        ColorToMove = ColorToMove == Piece.White ? Piece.Black : Piece.White;
    }

    // Unmake a move on the board
    public void UnmakeMove(Move move)
    {
        // The color to move switches
        ColorToMove = ColorToMove == Piece.White ? Piece.Black : Piece.White;

        // Castling has its own set of changes
        if (move.IsCastling)
        {
            // Calculate castling coordinates
            bool isQueenside = move.Destination % 8 == 2;
            int rankOffset = (ColorToMove == Piece.White) ? 0 : 56;
            int kingsideRookOffset = isQueenside ? 0 : 7;
            int newRookPositionOffset = isQueenside ? 3 : 5;

            // Move the rook
            spaces[rankOffset + kingsideRookOffset] = ColorToMove | Piece.Rook;
            spaces[rankOffset + newRookPositionOffset] = Piece.None;
        }

        // Replace the pieces on the squares and handle promotions
        spaces[move.Origin] = ColorToMove | Piece.Type(move.IsPromotion ? Piece.Pawn : move.MovingPiece);
        if (!move.IsEnPassant) { spaces[move.Destination] = (Piece.Type(move.CapturedPiece) == Piece.None) ? Piece.None : (ColorToMove == Piece.White ? Piece.Black : Piece.White) | Piece.Type(move.CapturedPiece); }

        // Castling rights
        WhiteCanKingsideCastle = WhiteCanKingsideCastle || move.LostWhiteKingsideCastleRights;
        WhiteCanQueensideCastle = WhiteCanQueensideCastle || move.LostWhiteQueensideCastleRights;
        BlackCanKingsideCastle = BlackCanKingsideCastle || move.LostBlackKingsideCastleRights;
        BlackCanQueensideCastle = BlackCanQueensideCastle || move.LostBlackQueensideCastleRights;

        // En passant
        EnPassantSquare = PastEnPassantOppurutunites.Pop();
        if (move.IsEnPassant)
        {
            // Return the piece to its square
            spaces[move.Destination + (ColorToMove == Piece.White ? -8 : 8)] = move.CapturedPiece;
            spaces[move.Destination] = Piece.None;
        }
    }

    // End the game
    public void EndGame(float score)
    {
        if (!GameOver)
        {
            GameOver = true;
            MonoBehaviour.print($"The game has concluded score={score}");
            GameManager.SetPieceDragState(PieceDragState.DISABLED);
        }
    }

    // Convert a 6-bit square index into a file and rank
    public static int[] SquareIndexToFileAndRank(int index)
    {
        int file = index % 8;
        int rank = (int) Mathf.Floor(index / 8);
        return new int[2] { file, rank };
    }

    // Convert a 6-bit square index to a square name
    public static string SquareIndexToSquareName(int index)
    {
        int[] fileAndRank = Board.SquareIndexToFileAndRank(index);
        return $"{Board.FileNames[fileAndRank[0]]}{Board.RankNames[fileAndRank[1]]}";
    }

    // Convert a square name into a 6-bit square index
    public static int SquareNameToSquareIndex(string name)
    {
        int file = Array.IndexOf(FileNames, name[0]);
        int rank = Array.IndexOf(RankNames, name[1]);
        return file + rank * 8;
    }
}
