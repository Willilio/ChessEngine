using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

// The structure that holds a move
public struct Move
{
    // Create the move
    public Move(int piece, int origin, int destination, bool isPromotion = false, 
        int promotionType = 0, int capturedPiece = 0, bool enPassant = false, 
        bool createsEnPassant = false, int enPassantSquare = 0, bool lostwkcastle = false, 
        bool lostwqcastle = false, bool lostbkcastle = false, bool lostbqcastle = false,
        bool castling = false)
    {
        // Assign all of the variables
        MovingPiece = piece;
        Origin = origin;
        Destination = destination;

        IsPromotion = isPromotion;
        PromotionType = promotionType;

        CapturedPiece = capturedPiece;
        IsEnPassant = enPassant;
        CreatesEnPassant = createsEnPassant;
        EnPassantSquare = enPassantSquare;
        LostWhiteKingsideCastleRights = lostwkcastle;
        LostWhiteQueensideCastleRights = lostwqcastle;
        LostBlackKingsideCastleRights = lostbkcastle;
        LostBlackQueensideCastleRights = lostbqcastle;
        IsCastling = castling;
    }

    // Create a deep copy
    public Move deepcopy()
    {
        return new Move()
        {
            MovingPiece = MovingPiece,
            Origin = Origin,
            Destination = Destination,
            PromotionType = PromotionType,
            IsCastling = IsCastling,
            IsEnPassant = IsEnPassant,
            IsPromotion = IsPromotion,
            CapturedPiece = CapturedPiece,
            CreatesEnPassant = CreatesEnPassant,
            LostBlackKingsideCastleRights = LostBlackKingsideCastleRights,
            LostBlackQueensideCastleRights = LostBlackQueensideCastleRights,
            LostWhiteKingsideCastleRights = LostWhiteKingsideCastleRights,
            LostWhiteQueensideCastleRights = LostWhiteQueensideCastleRights,
            EnPassantSquare = EnPassantSquare,
        };
    }

    // Turn the move into a string
    public override string ToString()
    {
        return $"{Board.SquareIndexToSquareName(Origin)}{Board.SquareIndexToSquareName(Destination)}";
    }

    // Turn the move into an algebraic notation move string
    public string ToAlgebraic()
    {
        // Initialize a string and get the destination square name
        string algebraic = "";
        string originSquareName = Board.SquareIndexToSquareName(Origin);
        string destinationSquareName = Board.SquareIndexToSquareName(Destination);

        // If the move is castling, it has special notation
        if (IsCastling)
        {
            algebraic += "O-O";
            if ((Destination % 8) == 2)
            {
                algebraic += "-O";
            }
            return algebraic;
        }

        // Unless the piece is a pawn, the move should start with the piece type
        // If the move is a capture, then add the file name
        if (Piece.Type(MovingPiece) != Piece.Pawn)
        {
            algebraic += Piece.ShortPieceNames[Piece.Type(MovingPiece)];
        }
        else if (Piece.Type(CapturedPiece) != Piece.None)
        {
            algebraic += originSquareName[0];
        }

        // Note: sometimes multiple of the same type of piece could move to the same space
        //  ^ Is there a simple solution to this?

        // If the move is a capture, then an 'x' is added
        if (Piece.Type(CapturedPiece) != Piece.None)
        {
            algebraic += "x";
        }

        // Then the piece destination square is appended
        algebraic += destinationSquareName;

        // Finally, if the move is a promotion, then the type of promotion is added after a '='
        if (IsPromotion)
        {
            algebraic += "=";
            algebraic += Piece.ShortPieceNames[Piece.Type(PromotionType)];
        }

        // Return the string
        return algebraic;
    }

    // The piece that is moving
    public int MovingPiece;

    // Starting and ending squares of the move
    public int Origin;
    public int Destination;

    // Promotion information
    public bool IsPromotion;
    public int PromotionType;

    // Captured pieces, castling, en passant
    public int CapturedPiece;
    public bool IsEnPassant;
    public bool CreatesEnPassant;
    public int EnPassantSquare;
    public bool LostWhiteKingsideCastleRights;
    public bool LostWhiteQueensideCastleRights;
    public bool LostBlackKingsideCastleRights;
    public bool LostBlackQueensideCastleRights;
    public bool IsCastling;
}

/* Here's a crazy idea that might work:
 * 
 * So right now I'm calculating the moves of a piece by looking at each piece and
 * asking which squares it can go to.  What if instead I looked at every square and
 * asked which pieces could go to it?  This would generate the attacked squares for
 * both sides as the move generation works.  It would also allow for my blindfolded
 * mod (which will be made eventually) to decipher when more specificity is needed
 * as to which piece is moving (e.g. Nbd2 rather than just Nd2 b/c both the knight
 * on b1 and f3 can move to d2)
 */

// In the future I could keep track of what type of piece attacks each square
public class MoveGeneration
{
    // The move generation works with a board
    private Board GameBoard;

    // It has a list of moves and of attacked squares
    public List<Move> PseudoLegalMoves;
    public List<Move> LegalMoves;
    public List<int> WhiteAttackedSquares;
    public List<int> BlackAttackedSquares;

    // State of the class
    private bool BorderInitialized = false;
    private bool KnightMovesInitialized = false;
    private bool KingMovesInitialized = false;

    // Direction indicies for sliding pieces and number of squares to edge
    public readonly static int[] DirectionOffsets = new int[8] { 7, 9, -7, -9, 8, 1, -8, -1 };
    public readonly static int[] KnightOffsets = new int[8] { 6, 15, 17, 10, -6, -15, -17, -10 };
    public readonly static int[] KingOffsets = new int[8] { 7, 8, 9, 1, -7, -8, -9, -1 };
    public readonly static int[] WhitePawnCaptures = new int[2] { 7, 9 };
    public readonly static int[] BlackPawnCaptures = new int[2] { -7, -9 };
    public static int[][] DistanceToBorder = new int[64][];
    public static int[][] KnightMoves = new int[64][];
    public static int[][] KingMoves = new int[64][];

    // Castling squares (required to be empty to castle)
    public static int[] WhiteKingsideCastlingSquares =  new int[2] { 5, 6 };
    public static int[] BlackKingsideCastlingSquares = new int[2] { 61, 62 };
    public static int[] WhiteKingsideCheckCastlingSquares = new int[3] { 4, 5, 6 };
    public static int[] BlackKingsideCheckCastlingSquares = new int[3] { 60, 61, 62 };
    public static int[] WhiteQueensideCastlingSquares = new int[3] { 3, 2, 1 };
    public static int[] BlackQueensideCastlingSquares = new int[3] { 59, 58, 57 };
    public static int[] WhiteQueensideCheckCastlingSquares = new int[3] { 4, 3, 2 };
    public static int[] BlackQueensideCheckCastlingSquares = new int[3] { 60, 59, 58 };

    // Board values hosted through the move generation
    public bool KingInCheck = false;
    public bool KingCheckmated = false;
    public bool KingStalemated = false;

    // Initalize the move generation with a board
    public MoveGeneration(Board board, bool optimize = true)
    {
        // Set the game board
        GameBoard = board;

        // Initialize all of the lists
        PseudoLegalMoves = new List<Move>();
        LegalMoves = new List<Move>();
        WhiteAttackedSquares = new List<int>();
        BlackAttackedSquares = new List<int>();

        // Start the optimization
        if (optimize)
        {
            InitializeBorderOptimization();
            InitializeKnightOptimization();
            InitializeKingOptimization();
            if (BorderInitialized && KnightMovesInitialized && KingMovesInitialized)
            {
                MonoBehaviour.print("Board optimized and move generation ready");
            }
        }
    }

    // Get a random pseudo legal move
    public Move GetRandomPseudoLegalMove()
    {
        if (PseudoLegalMoves.Count <= 0) { return new Move(); }
        System.Random randomGenerator = new System.Random();
        int index = randomGenerator.Next() % PseudoLegalMoves.Count;
        return PseudoLegalMoves[index];
    }

    // Get a random legal move
    public Move GetRandomLegalMove()
    {
        if (LegalMoves.Count <= 0) { return new Move(); }
        System.Random randomGenerator = new System.Random();
        int index = randomGenerator.Next() % LegalMoves.Count;
        return LegalMoves[index];
    }

    // Get all of the pseudo legal moves as a string
    public string PseudoLegalMovesAsString()
    {
        if (PseudoLegalMoves.Count <= 0)
        {
            return "";
        }
        string moves = "";
        foreach (Move move in PseudoLegalMoves)
        {
            moves += move.ToAlgebraic();
            moves += ", ";
        }
        return moves[0 .. ^2];
    }

    // Get all of the legal moves as a string
    public string LegalMovesAsString()
    {
        if (LegalMoves.Count <= 0)
        {
            return "";
        }
        string moves = "";
        foreach (Move move in LegalMoves)
        {
            moves += move.ToAlgebraic();
            moves += ", ";
        }
        return moves[0..^2];
    }

    // Create the distance to border list to speed up move generation
    public void InitializeBorderOptimization()
    {
        // Go over every square
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                // The square's index
                int squareNumber = file + rank * 8;
                DistanceToBorder[squareNumber] = new int[8];

                // In each direction calculate the distance to the border
                DistanceToBorder[squareNumber][0] = Mathf.Min(file, 7 - rank); // diagonal to top right
                DistanceToBorder[squareNumber][1] = Mathf.Min(7 - file, 7 - rank); // diagonal to bottom right
                DistanceToBorder[squareNumber][2] = Mathf.Min(7 - file, rank); // diagonal to bottom left
                DistanceToBorder[squareNumber][3] = Mathf.Min(file, rank); // diagonal to top left
                DistanceToBorder[squareNumber][4] = 7 - rank; // orthogonal to top
                DistanceToBorder[squareNumber][5] = 7 - file; // orthogonal to right
                DistanceToBorder[squareNumber][6] = rank; // orthogonal to bottom
                DistanceToBorder[squareNumber][7] = file; // orthogonal to left
            }
        }
        
        // Tell the class that the border is created
        BorderInitialized = true;
    }

    // Create the list of knight moves from every square on the board
    // This speeds up move generation slightly
    public void InitializeKnightOptimization()
    {
        // Go over every square
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                // The square's index
                int squareNumber = file + rank * 8;
                KnightMoves[squareNumber] = new int[8];

                // Represent legal moves by their destinations
                // Illegal moves are indicated by a negative number
                KnightMoves[squareNumber][0] = (file >= 2 && rank <= 6) ? squareNumber + KnightOffsets[0] : -1; // left 2 up 1
                KnightMoves[squareNumber][1] = (file >= 1 && rank <= 5) ? squareNumber + KnightOffsets[1] : -1; // left 1 up 2
                KnightMoves[squareNumber][2] = (file <= 6 && rank <= 5) ? squareNumber + KnightOffsets[2] : -1; // right 1 up 2
                KnightMoves[squareNumber][3] = (file <= 5 && rank <= 6) ? squareNumber + KnightOffsets[3] : -1; // right 2 up 1
                KnightMoves[squareNumber][4] = (file <= 5 && rank >= 1) ? squareNumber + KnightOffsets[4] : -1; // right 2 down 1
                KnightMoves[squareNumber][5] = (file <= 6 && rank >= 2) ? squareNumber + KnightOffsets[5] : -1; // right 1 down 2
                KnightMoves[squareNumber][6] = (file >= 1 && rank >= 2) ? squareNumber + KnightOffsets[6] : -1; // left 1 down 2
                KnightMoves[squareNumber][7] = (file >= 2 && rank >= 1) ? squareNumber + KnightOffsets[7] : -1; // left 2 down 1
            }
        }

        // The knight moves have been initialized
        KnightMovesInitialized = true;
    }

    // Create the list of king moves from every square on the board
    // This speeds up move generation slightly
    public void InitializeKingOptimization()
    {
        // Go over every square
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                // The square's index
                int squareNumber = file + rank * 8;
                KingMoves[squareNumber] = new int[8];

                // Represent legal moves by their destinations
                // Illegal moves are indicated by a negative number
                KingMoves[squareNumber][0] = (file >= 1 && rank <= 6) ? squareNumber + KingOffsets[0] : -1; // left 1 up 1
                KingMoves[squareNumber][1] = (rank <= 6) ? squareNumber + KingOffsets[1] : -1; // up 1
                KingMoves[squareNumber][2] = (file <= 6 && rank <= 6) ? squareNumber + KingOffsets[2] : -1; // right 1 up 1
                KingMoves[squareNumber][3] = (file <= 6) ? squareNumber + KingOffsets[3] : -1; // right 1
                KingMoves[squareNumber][4] = (file <= 6 && rank >= 1) ? squareNumber + KingOffsets[4] : -1; // right 1 down 1
                KingMoves[squareNumber][5] = (rank >= 1) ? squareNumber + KingOffsets[5] : -1; // down 1
                KingMoves[squareNumber][6] = (file >= 1 && rank >= 1) ? squareNumber + KingOffsets[6] : -1; // left 1 down 1
                KingMoves[squareNumber][7] = (file >= 1) ? squareNumber + KingOffsets[7] : -1; // left 1
            }
        }

        // The king moves have been initialized
        KingMovesInitialized = true;
    }

    // Generate all of the sliding moves for the pieces
    // Note that pieces is actually a list of piece locations
    public List<Move> GenerateSlidingMoves(List<int> pieces)
    {
        // Create a list of sliding moves
        List<Move> slidingMoves = new List<Move>();
        List<int> attackedSquares = GameBoard.ColorToMove == Piece.White ? WhiteAttackedSquares : BlackAttackedSquares;

        // Loop over the pieces
        foreach (int i in pieces)
        {
            // What kind of moves can this piece do?
            int pieceType = Piece.Type( GameBoard.spaces[ i ] );
            int startOffset = Piece.IsDiagonalPiece(pieceType) ? 0 : 4;
            int endOffset = Piece.IsOrthogonalPiece(pieceType) ? 8 : 4;

            // If the piece is none, then end the generation
            if (pieceType == Piece.None)
            {
                continue;
            }

            // Keep track of the index of the current offset
            int offsetIndex = startOffset;

            // Loop over the piece's directions
            foreach (int offset in DirectionOffsets[ startOffset .. endOffset ])
            {

                // As far as the piece can move in this direction
                for (int j = 1; j <= DistanceToBorder[ i ][offsetIndex]; j++)
                {

                    // The square the piece is trying to move to
                    int currentSquare = i + offset * j;

                    // Square is empty
                    if (Piece.Type(GameBoard.spaces[currentSquare]) == Piece.None)
                    {
                        // The move is at least pseudo legal
                        attackedSquares.Add(currentSquare);
                        slidingMoves.Add(new Move()
                        {
                            MovingPiece = pieceType,
                            Origin = i,
                            Destination = currentSquare,
                            CapturedPiece = Piece.None,
                            LostWhiteKingsideCastleRights = GameBoard.WhiteCanKingsideCastle && (i == 7 || currentSquare == 7),
                            LostWhiteQueensideCastleRights = GameBoard.WhiteCanQueensideCastle && (i == 0 || currentSquare == 0),
                            LostBlackKingsideCastleRights = GameBoard.BlackCanKingsideCastle && (i == 63 || currentSquare == 63),
                            LostBlackQueensideCastleRights = GameBoard.BlackCanQueensideCastle && (i == 55 || currentSquare == 55),
                        });
                    }

                    // Square contains a piece
                    else
                    {
                        // The piece is an enemy piece
                        if (Piece.Color(GameBoard.spaces[currentSquare]) != GameBoard.ColorToMove)
                        {
                            // The square is a valid capture for the piece
                            slidingMoves.Add(new Move()
                            {
                                MovingPiece = pieceType,
                                Origin = i,
                                Destination = currentSquare,
                                CapturedPiece = GameBoard.spaces[currentSquare],
                                LostWhiteKingsideCastleRights = GameBoard.WhiteCanKingsideCastle && (i == 7 || currentSquare == 7),
                                LostWhiteQueensideCastleRights = GameBoard.WhiteCanQueensideCastle && (i == 0 || currentSquare == 0),
                                LostBlackKingsideCastleRights = GameBoard.BlackCanKingsideCastle && (i == 63 || currentSquare == 63),
                                LostBlackQueensideCastleRights = GameBoard.BlackCanQueensideCastle && (i == 55 || currentSquare == 55),
                            });
                        }

                        // No more movement in this direction
                        attackedSquares.Add(currentSquare);
                        break;
                    }

                }

                // Increment the offset index
                offsetIndex += 1;
            }
        }

        // Return the list of moves
        PseudoLegalMoves.AddRange(slidingMoves);
        return slidingMoves;
    }

    // Generate the moves for a knight
    public List<Move> GenerateKnightMoves(int piece)
    {
        // Create a list of the knight's moves
        List<Move> knightMoves = new List<Move>();

        // Go through the list of knight moves from that square
        for (int i = 0; i < 8; i++)
        {
            // If the value is positive, add it
            // Also make sure the piece there is not a friendly piece
            if (KnightMoves[piece][i] >= 0 && Piece.Color(GameBoard.spaces[KnightMoves[piece][i]]) != GameBoard.ColorToMove)
            {
                (GameBoard.ColorToMove == Piece.White ? WhiteAttackedSquares : BlackAttackedSquares).Add(KnightMoves[piece][i]);
                knightMoves.Add(new Move()
                {
                    MovingPiece = Piece.Knight,
                    Origin = piece,
                    Destination = KnightMoves[piece][i],
                    CapturedPiece = GameBoard.spaces[KnightMoves[piece][i]],
                    LostWhiteKingsideCastleRights = GameBoard.WhiteCanKingsideCastle && (KnightMoves[piece][i] == 7),
                    LostWhiteQueensideCastleRights = GameBoard.WhiteCanQueensideCastle && (KnightMoves[piece][i] == 0),
                    LostBlackKingsideCastleRights = GameBoard.BlackCanKingsideCastle && (KnightMoves[piece][i] == 63),
                    LostBlackQueensideCastleRights = GameBoard.BlackCanQueensideCastle && (KnightMoves[piece][i] == 55),
                });
            }
        }

        // Return the list of moves
        PseudoLegalMoves.AddRange(knightMoves);
        return knightMoves;
    }

    public List<Move> GenerateCastlingMoves(bool whiteToMove, int piece)
    {
        // Create a list of moves
        List<Move> castlingMoves = new List<Move>();

        // Castling important squares
        int[] kingsideCastleSquares = whiteToMove ? WhiteKingsideCastlingSquares : BlackKingsideCastlingSquares;
        int[] queensideCastleSquares = whiteToMove ? WhiteQueensideCastlingSquares : BlackQueensideCastlingSquares;
        int[] queensideCheckSquares = whiteToMove ? WhiteQueensideCheckCastlingSquares : BlackQueensideCheckCastlingSquares;

        // Kingside castling
        bool isKingsideCastlingLegal = whiteToMove ? GameBoard.WhiteCanKingsideCastle : GameBoard.BlackCanKingsideCastle;
        foreach (int square in kingsideCastleSquares)
        {
            if (Piece.Type(GameBoard.spaces[square]) != Piece.None)
            {
                isKingsideCastlingLegal = false;
                break;
            }
        }

        // Add the move if it is deemed legal
        if (isKingsideCastlingLegal)
        {
            castlingMoves.Add(new Move()
            {
                MovingPiece = Piece.King,
                Origin = piece,
                Destination = kingsideCastleSquares[^1],
                CapturedPiece = Piece.None,
                IsCastling = true,
                LostWhiteKingsideCastleRights = whiteToMove,
                LostWhiteQueensideCastleRights = whiteToMove,
                LostBlackKingsideCastleRights = !whiteToMove,
                LostBlackQueensideCastleRights = !whiteToMove,
            });
        }

        // Queenside castling
        bool isQueensideCastlingLegal = whiteToMove ? GameBoard.WhiteCanQueensideCastle : GameBoard.BlackCanQueensideCastle;
        foreach (int square in queensideCastleSquares)
        {
            if (Piece.Type(GameBoard.spaces[square]) != Piece.None)
            {
                isQueensideCastlingLegal = false;
                break;
            }
        }

        // Add the move if it is deemed legal
        if (isQueensideCastlingLegal)
        {
            castlingMoves.Add(new Move()
            {
                MovingPiece = Piece.King,
                Origin = piece,
                Destination = queensideCheckSquares[^1],
                CapturedPiece = Piece.None,
                IsCastling = true,
                LostWhiteKingsideCastleRights = whiteToMove,
                LostWhiteQueensideCastleRights = whiteToMove,
                LostBlackKingsideCastleRights = !whiteToMove,
                LostBlackQueensideCastleRights = !whiteToMove,
            });
        }

        // Return the list
        return castlingMoves;
    }

    // Generate the moves for a king
    public List<Move> GenerateKingMoves(int piece)
    {
        // Create a list of the king's moves
        List<Move> kingMoves = new List<Move>();

        // Create a white to move boolean
        bool whiteToMove = GameBoard.ColorToMove == Piece.White;

        // Go through the list of king moves from that square
        for (int i = 0; i < 8; i++)
        {
            // If the value is positive, add it
            // Also make sure the piece there is not a friendly piece
            if (KingMoves[piece][i] >= 0 && Piece.Color(GameBoard.spaces[KingMoves[piece][i]]) != GameBoard.ColorToMove)
            {
                (whiteToMove ? WhiteAttackedSquares : BlackAttackedSquares).Add(KingMoves[piece][i]);
                kingMoves.Add(new Move()
                {
                    MovingPiece = Piece.King,
                    Origin = piece,
                    Destination = KingMoves[piece][i],
                    CapturedPiece = GameBoard.spaces[KingMoves[piece][i]],
                    LostWhiteKingsideCastleRights = GameBoard.WhiteCanKingsideCastle && ((GameBoard.ColorToMove == Piece.White) ? true : (KingMoves[piece][i] == 7)),
                    LostWhiteQueensideCastleRights = GameBoard.WhiteCanQueensideCastle && ((GameBoard.ColorToMove == Piece.White) ? true : (KingMoves[piece][i] == 0)),
                    LostBlackKingsideCastleRights = GameBoard.BlackCanKingsideCastle && ((GameBoard.ColorToMove == Piece.Black) ? true : (KingMoves[piece][i] == 55)),
                    LostBlackQueensideCastleRights = GameBoard.BlackCanQueensideCastle && ((GameBoard.ColorToMove == Piece.Black) ? true : (KingMoves[piece][i] == 63)),
                });
            }
        }

        // Add castling moves (do not count as attacked squares)
        // For now castling is deemed illegal
        bool whiteCanCastle = GameBoard.WhiteCanKingsideCastle || GameBoard.WhiteCanQueensideCastle;
        bool blackCanCastle = GameBoard.BlackCanKingsideCastle || GameBoard.BlackCanQueensideCastle;
        if (whiteCanCastle || blackCanCastle) { kingMoves.AddRange(GenerateCastlingMoves(whiteToMove, piece)); }

        // Return the list of moves
        PseudoLegalMoves.AddRange(kingMoves);
        return kingMoves;
    }

    // Generate the moves for a white pawn
    public List<Move> GenerateWhitePawnMoves(int piece)
    {
        // Create a list of the pawn's moves
        List<Move> pawnMoves = new List<Move>();
        List<int> attackedSquares = WhiteAttackedSquares;

        // Check if the pawn is on the seventh rank (promotions possible)
        int rank = (int) Mathf.Floor(piece / 8);
        bool secondLastRank = rank == 6;
        if (rank == 7)
        {
            // No moves are possible
            return pawnMoves;
        }

        // If the square in front of the pawn is empty, then the pawn can move forward one square
        if (Piece.Type(GameBoard.spaces[piece + 8]) == Piece.None)
        {
            attackedSquares.Add(piece + 8);
            pawnMoves.Add(new Move()
            {
                MovingPiece = Piece.Pawn,
                Origin = piece,
                Destination = piece + 8,
                CapturedPiece = Piece.None,
                LostWhiteKingsideCastleRights = false,
                LostWhiteQueensideCastleRights = false,
                LostBlackKingsideCastleRights = false,
                LostBlackQueensideCastleRights = false,
                IsPromotion = secondLastRank,
            });

            // If the pawn is on the second rank and the squares two ahead of the pawn are empty
            // then it can move forward two squares at a time
            if (rank == 1 && Piece.Type(GameBoard.spaces[piece + 16]) == Piece.None)
            {
                attackedSquares.Add(piece + 16);
                pawnMoves.Add(new Move()
                {
                    MovingPiece = Piece.Pawn,
                    Origin = piece,
                    Destination = piece + 16,
                    CapturedPiece = Piece.None,
                    CreatesEnPassant = true,
                    EnPassantSquare = piece + 8,
                    LostWhiteKingsideCastleRights = false,
                    LostWhiteQueensideCastleRights = false,
                    LostBlackKingsideCastleRights = false,
                    LostBlackQueensideCastleRights = false,
                });
            }
        }

        // If there are pieces up 1 and left or right 1 from the pawn (or if these are en passant
        // squares), then it can capture those pieces
        // Adding precomputed pawn moves would speed this up
        foreach (int captureSquareOffset in WhitePawnCaptures)
        {
            // Disallow captures off the board
            if (piece % 8 == 7 && captureSquareOffset == 9) { continue; }
            if (piece % 8 == 0 && captureSquareOffset == 7) { continue; }

            // Calculate the capture space
            int captureSquare = piece + captureSquareOffset;
            int pieceAtCaptureSquare = GameBoard.spaces[captureSquare];

            // If there is a piece at the capture square, then we can capture it
            if (Piece.Color(pieceAtCaptureSquare) == Piece.Black && Piece.Type(pieceAtCaptureSquare) != Piece.None)
            {
                WhiteAttackedSquares.Add(captureSquare);
                pawnMoves.Add(new Move()
                {
                    MovingPiece = Piece.Pawn,
                    Origin = piece,
                    Destination = captureSquare,
                    CapturedPiece = pieceAtCaptureSquare,
                    LostWhiteKingsideCastleRights = false,
                    LostWhiteQueensideCastleRights = false,
                    LostBlackKingsideCastleRights = GameBoard.BlackCanKingsideCastle && (captureSquare == 63),
                    LostBlackQueensideCastleRights = GameBoard.BlackCanQueensideCastle && (captureSquare == 55),
                    IsPromotion = secondLastRank,
                });
            }

            // Handle en passant
            else if (Piece.Type(pieceAtCaptureSquare) == Piece.None && GameBoard.EnPassantSquare == captureSquare)
            {
                WhiteAttackedSquares.Add(captureSquare);
                pawnMoves.Add(new Move()
                {
                    MovingPiece = Piece.Pawn,
                    Origin = piece,
                    Destination = captureSquare,
                    CapturedPiece = GameBoard.spaces[captureSquare - 8],
                    IsEnPassant = true,
                    LostWhiteKingsideCastleRights = false,
                    LostWhiteQueensideCastleRights = false,
                    LostBlackKingsideCastleRights = false,
                    LostBlackQueensideCastleRights = false,
                    IsPromotion = false,
                });
            }
        }

        // Return the list of moves
        PseudoLegalMoves.AddRange(pawnMoves);
        return pawnMoves;
    }

    // Generate the moves for a black pawn
    public List<Move> GenerateBlackPawnMoves(int piece)
    {
        // Create a list of the pawn's moves
        List<Move> pawnMoves = new List<Move>();
        List<int> attackedSquares = BlackAttackedSquares;

        // Check if the pawn is on the seventh rank (promotions possible)
        int rank = (int)Mathf.Floor(piece / 8);
        bool secondLastRank = rank == 1;
        if (rank == 0)
        {
            // No moves are possible
            return pawnMoves;
        }

        // If the square in front of the pawn is empty, then the pawn can move forward one square
        if (Piece.Type(GameBoard.spaces[piece - 8]) == Piece.None)
        {
            attackedSquares.Add(piece - 8);
            pawnMoves.Add(new Move()
            {
                MovingPiece = Piece.Pawn,
                Origin = piece,
                Destination = piece - 8,
                CapturedPiece = Piece.None,
                LostWhiteKingsideCastleRights = false,
                LostWhiteQueensideCastleRights = false,
                LostBlackKingsideCastleRights = false,
                LostBlackQueensideCastleRights = false,
                IsPromotion = secondLastRank,
            });

            // If the pawn is on the second rank and the squares two ahead of the pawn are empty
            // then it can move forward two squares at a time
            if (rank == 6 && Piece.Type(GameBoard.spaces[piece - 16]) == Piece.None)
            {
                attackedSquares.Add(piece - 16);
                pawnMoves.Add(new Move()
                {
                    MovingPiece = Piece.Pawn,
                    Origin = piece,
                    Destination = piece - 16,
                    CapturedPiece = Piece.None,
                    CreatesEnPassant = true,
                    EnPassantSquare = piece - 8,
                    LostWhiteKingsideCastleRights = false,
                    LostWhiteQueensideCastleRights = false,
                    LostBlackKingsideCastleRights = false,
                    LostBlackQueensideCastleRights = false,
                });
            }
        }

        // If there are pieces up 1 and left or right 1 from the pawn (or if these are en passant
        // squares), then it can capture those pieces
        // Adding precomputed pawn moves would speed this up
        foreach (int captureSquareOffset in BlackPawnCaptures)
        {
            // Disallow captures off the board
            if (piece % 8 == 0 && captureSquareOffset == -9) { continue; }
            if (piece % 8 == 7 && captureSquareOffset == -7) { continue; }

            // Calculate the capture space
            int captureSquare = piece + captureSquareOffset;
            int pieceAtCaptureSquare = GameBoard.spaces[captureSquare];

            // If there is a piece at the capture square, then we can capture it
            if (Piece.Color(pieceAtCaptureSquare) == Piece.White && Piece.Type(pieceAtCaptureSquare) != Piece.None)
            {
                BlackAttackedSquares.Add(captureSquare);
                pawnMoves.Add(new Move()
                {
                    MovingPiece = Piece.Pawn,
                    Origin = piece,
                    Destination = captureSquare,
                    CapturedPiece = pieceAtCaptureSquare,
                    LostWhiteKingsideCastleRights = GameBoard.WhiteCanKingsideCastle && (captureSquare == 7),
                    LostWhiteQueensideCastleRights = GameBoard.WhiteCanQueensideCastle && (captureSquare == 0),
                    LostBlackKingsideCastleRights = false,
                    LostBlackQueensideCastleRights = false,
                    IsPromotion = secondLastRank,
                });
            }

            // Handle en passant
            else if (Piece.Type(pieceAtCaptureSquare) == Piece.None && GameBoard.EnPassantSquare == captureSquare)
            {
                BlackAttackedSquares.Add(captureSquare);
                pawnMoves.Add(new Move()
                {
                    MovingPiece = Piece.Pawn,
                    Origin = piece,
                    Destination = captureSquare,
                    CapturedPiece = GameBoard.spaces[captureSquare + 8],
                    IsEnPassant = true,
                    LostWhiteKingsideCastleRights = false,
                    LostWhiteQueensideCastleRights = false,
                    LostBlackKingsideCastleRights = false,
                    LostBlackQueensideCastleRights = false,
                    IsPromotion = false,
                });
            }
        }

        // Return the list of moves
        PseudoLegalMoves.AddRange(pawnMoves);
        return pawnMoves;
    }

    // Generate all of the pseudo legal moves for the color to move
    public List<Move> GeneratePseudoLegalMoves()
    {
        // Empty the previous list
        PseudoLegalMoves = new List<Move>();

        // Find all of the pieces
        List<int> slidingPieces = new List<int>();
        for (int i = 0; i < 64; i++)
        {
            // No need to calculate moves for anything other than friendly pieces
            if (Piece.Color(GameBoard.spaces[i]) != GameBoard.ColorToMove)
            {
                continue;
            }

            // The piece is a sliding piece
            if (Piece.IsSlidingPiece(GameBoard.spaces[i]))
            {
                slidingPieces.Add(i);
            }

            // The piece is a knight
            if (Piece.Type(GameBoard.spaces[i]) == Piece.Knight)
            {
                GenerateKnightMoves(i);
            }

            // The piece is a king
            if (Piece.Type(GameBoard.spaces[i]) == Piece.King)
            {
                GenerateKingMoves(i);
            }

            // The piece is a pawn
            if (Piece.Type(GameBoard.spaces[i]) == Piece.Pawn)
            {
                if (GameBoard.ColorToMove == Piece.White)
                {
                    GenerateWhitePawnMoves(i);
                }
                else
                {
                    GenerateBlackPawnMoves(i);
                }
            }
        }

        // Generate all of the sliding move
        GenerateSlidingMoves(slidingPieces);

        // Return all of the pseudo legal moves
        return PseudoLegalMoves;
    }

    // Weed out illegal moves
    public List<Move> WeedOutIllegalMoves()
    {
        // Create a list of legal moves and backup the pseudo legal moves
        List<Move> safeMoves = new List<Move>();
        List<Move> pseudoLegalMovesBackup = new List<Move>();

        // Is it white's turn
        bool whiteToMove = GameBoard.ColorToMove == Piece.White;

        // Track whether the king is in check, checkmated, or stalemated
        KingInCheck = false;
        KingCheckmated = false;
        KingStalemated = true;

        // Create the copy of the pseudo legal moves
        foreach (Move m in PseudoLegalMoves)
        {
            pseudoLegalMovesBackup.Add(m.deepcopy());
        }

        // See if the king is in check
        GameBoard.ColorToMove = Piece.OppositeColor(GameBoard.ColorToMove);
        foreach (Move move in GeneratePseudoLegalMoves())
        {
            if (Piece.Type(move.CapturedPiece) == Piece.King)
            {
                KingInCheck = true;
                break;
            }
        }
        GameBoard.ColorToMove = Piece.OppositeColor(GameBoard.ColorToMove);

        // Go over each pseudo legal move
        foreach (Move pseudoLegalMove in pseudoLegalMovesBackup)
        {
            // Make the move
            GameBoard.MakeMove(pseudoLegalMove);

            // Is the king stalemated?
            if (Piece.Type(pseudoLegalMove.MovingPiece) == Piece.King)
            {
                KingStalemated = false;
            }

            // Generate every possible response
            List<Move> opponentResponses = GeneratePseudoLegalMoves();

            // Castling important squares
            bool isQueenside = pseudoLegalMove.Destination % 8 == 2;
            int[] kingsideCheckSquares = whiteToMove ? WhiteKingsideCheckCastlingSquares : BlackKingsideCheckCastlingSquares;
            int[] queensideCheckSquares = whiteToMove ? WhiteQueensideCheckCastlingSquares : BlackQueensideCheckCastlingSquares;
            int[] importantSquares = isQueenside ? queensideCheckSquares : kingsideCheckSquares;

            // Is the king taken?
            bool isLegal = true;
            foreach (Move opponentResponse in opponentResponses)
            {
                // Can the opponent take my king?
                if (Piece.Type(opponentResponse.CapturedPiece) == Piece.King)
                {
                    isLegal = false;
                    break;
                }

                // Can the opponent attack any castling squares?
                if (pseudoLegalMove.IsCastling)
                {
                    // If the opponent attacks any important squares then castling is illegal
                    foreach (int square in importantSquares)
                    {
                        if (opponentResponse.Destination == square)
                        {
                            isLegal = false;
                            break;
                        }
                    }

                    if (!isLegal) { break; }
                }
            }

            // Unmake the move
            GameBoard.UnmakeMove(pseudoLegalMove);

            // Add the move
            if (isLegal)
            {
                safeMoves.Add(pseudoLegalMove);
            }
        }

        /* If there are no safe moves
        if (safeMoves.Count == 0)
        {
            if (KingInCheck) { KingCheckmated = true; GameBoard.EndGame(GameBoard.ColorToMove == Piece.Black ? 1f : 0f); }
            else if (KingStalemated) { GameBoard.EndGame(0.5f); }
        }*/

        // Restore the pseudo legal moves and return the list of safe moves
        PseudoLegalMoves = pseudoLegalMovesBackup;
        return safeMoves;
    }

    // Generate all the legal moves in a position
    public List<Move> GenerateLegalMoves()
    {
        // Empty the previous list
        LegalMoves = new List<Move>();

        // Get all the pseudo legal moves
        GeneratePseudoLegalMoves();

        // Make sure all of the moves are legal
        LegalMoves.AddRange(WeedOutIllegalMoves());

        // Return all of the legal moves
        return LegalMoves;
    }
}
