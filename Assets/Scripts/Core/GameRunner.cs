using ChessPlayer;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

/* My MASTER TODO LIST
 * 
 * Add pawn choice of promotion
 * Propper winning
 * Double check winning moves
 * 
 * Not as important
 * -----------------
 * Fifty move rule
 * Threefold repitition
 * Condense move generation and organize
 */

// An enum for the piece dragging
public enum PieceDragState
{
    DISABLED,
    NOTHING_GRABBED,
    DRAGGING,
}

// The chess playing namespace
namespace ChessPlayer
{
    // The generic player class
    public interface IPlayer
    {
        // Methods to get player information
        public string GetName();
        public int GetElo();
        public bool IsComputer();

        // Methods to set player information
        public void SetName(string newName);
        public void SetElo(int newElo);
        public void SetNextToMove(IPlayer nextMove);

        // Human player methods
        public void SubmitMove(SquareManager squareManager, Board board, Move move);

        // Get a move from the player
        public void OnPlayMoveRequest(SquareManager squareManager, MoveGeneration moveGenerator, Board board);
    }

    // The human player class
    // We'll give him a generic name like "Human Player" and an elo of 1000
    public class HumanPlayer : IPlayer
    {
        // Information about the player
        private string Name = "Human Player";
        private int Elo = 1000;
        private const bool PlayerIsComputer = false;
        private bool WaitingForInput = false;

        // Has the player been set up?
        public GameRunner GameManager = null;
        public IPlayer NextToPlay = null;

        // Latest variables
        private MoveGeneration LatestMoveGeneration;
        private Board LatestBoard;
        private SquareManager LatestSquareManager;

        // Constructor for the player
        public HumanPlayer(IPlayer nextPlayer, GameRunner gameRunner)
        {
            NextToPlay = nextPlayer;
            GameManager = gameRunner;
        }

        // Get information about the player
        public string GetName() { return Name; }
        public int GetElo() { return Elo; }
        public bool IsComputer() { return PlayerIsComputer; }

        // Set information about the player
        public void SetName(string newName) { Name = newName; }
        public void SetElo(int newElo) { Elo = newElo; }
        public void SetGameManager(GameRunner gameRunner) { GameManager = gameRunner; }
        public void SetNextToMove(IPlayer nextMove) { NextToPlay = nextMove; }

        // Ask the player to play a move
        public void OnPlayMoveRequest(SquareManager squareManager, MoveGeneration moveGenerator, Board board)
        {
            // Make sure that the game manager and player are not null
            if (GameManager == null) { throw new System.NullReferenceException("The game runner is not set"); }
            if (NextToPlay == null) { throw new System.NullReferenceException("The next player is not set"); }

            // Set the latest variables
            LatestMoveGeneration = moveGenerator;
            LatestBoard = board;
            LatestSquareManager = squareManager;

            // Allow the player to drag pieces
            GameManager.SetPieceDragState(PieceDragState.NOTHING_GRABBED);

            // The player is now primed for input
            WaitingForInput = true;
        }

        private int GetPromotionPiece(Board board)
        {
            // Alternate underpromotions
            if (Input.GetKey(KeyCode.B)) { return board.ColorToMove | Piece.Bishop; }
            if (Input.GetKey(KeyCode.N)) { return board.ColorToMove | Piece.Knight; }
            if (Input.GetKey(KeyCode.R)) { return board.ColorToMove | Piece.Rook; }

            // The backup is promoting to a queen
            return board.ColorToMove | Piece.Queen;
        }

        // Submit a move to the player
        public void SubmitMove(SquareManager squareManager, Board board, Move move)
        {
            // If not waiting for input then end the function
            if (!WaitingForInput) { return; }

            // Stop the player from drag pieces
            GameManager.SetPieceDragState(PieceDragState.DISABLED);

            // No longer need input
            WaitingForInput = false;

            // Promotions
            if (move.IsPromotion) { move.PromotionType = GetPromotionPiece(board); }

            // Play the move
            board.MakeMove(move);
            LatestMoveGeneration.GenerateLegalMoves();
            squareManager.UpdatePieceImagesByBoard(board);

            // Make the other player play a move
            NextToPlay.OnPlayMoveRequest(squareManager, LatestMoveGeneration, board);
        }
    }
}

// The game runner organizes the game and calls other functions
// It is basically the __main__ functionality
public class GameRunner : MonoBehaviour
{
    // Game objects
    private Board GameBoard;
    private SquareManager GameSquareManager;
    private MoveGeneration MoveGenerator;
    private BoardCanvasPlacement BoardPlacement;

    // The state of the piece dragging
    private PieceDragState PlayerPieceDragState;
    private const int PieceDragButton = 0;
    private const double MAX_DRAG_TIME = 0.4;
    private int DragStartSquare;
    private double DragStartTime;

    // The players
    public ChessPlayer.IPlayer WhitePlayer;
    public ChessPlayer.IPlayer BlackPlayer;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the players
        WhitePlayer = new ChessPlayer.HumanPlayer(null, this);
        BlackPlayer = new ChessPlayer.Vela.VelaGenI(null, this);
        WhitePlayer.SetNextToMove(BlackPlayer);
        BlackPlayer.SetNextToMove(WhitePlayer);

        // The player's piece drag state defaults to disabled
        PlayerPieceDragState = PieceDragState.DISABLED;

        // Get the board placement component
        BoardPlacement = GetComponent<BoardCanvasPlacement>();

        // Start the game
        PlayGame();
    }

    // Play the game
    public void PlayGame()
    {
        // Create a board and get the square manager
        GameBoard = new Board(this);
        GameSquareManager = GetComponent<SquareManager>();

        // Start the board and square manager
        GameBoard.Initialize();
        GameSquareManager.Initialize();

        // Setup the pieces in the classical way and setup move generation
        GameBoard.SetupFromFEN(Board.ClassicStartingFEN);
        GameSquareManager.UpdatePieceImagesByBoard(GameBoard);
        MoveGenerator = new MoveGeneration(GameBoard);

        // Play the first move
        WhitePlayer.OnPlayMoveRequest(GameSquareManager, MoveGenerator, GameBoard);
    }

    // Move generation testing code
    public void TestMoveGeneration(string startingFen = Board.ClassicStartingFEN)
    {
        // Create a test board
        Board TestBoard = new Board(this);
        TestBoard.Initialize();
        TestBoard.SetupFromFEN(startingFen);

        // Create a move generation object
        MoveGeneration TestGeneration = new MoveGeneration(TestBoard);

        // Print out the fen and the number of moves in the position
        print($"Testing a board setup {startingFen}\nNumber of moves in position: {CountMovesInPosition(TestBoard, TestGeneration, 5, 5)}");
    }

    // Get the number of moves in a position
    public int CountMovesInPosition(Board board, MoveGeneration moveGeneration, int depth, int idepth)
    {
        // Return 1 if the depth is 0
        if (depth == 0) { return 1; }

        // Keep a count of the moves
        int count = 0;
        
        // For every move
        foreach (Move move in moveGeneration.GenerateLegalMoves())
        {
            board.MakeMove(move);
            int pcount = CountMovesInPosition(board, moveGeneration, depth - 1, idepth);
            if (depth == idepth) { print($"Moves after {move.ToAlgebraic()}: {pcount}"); }
            count += pcount;
            board.UnmakeMove(move);
        }

        // Return the count
        return count;
    }

    // Search for a move
    // Get all moves with a certain start index
    public List<Move> SearchForMove(List<Move> moves, int startSquare)
    {
        // Create a list with all the matching moves
        List<Move> matchingMoves = new List<Move>();

        // Go over all the moves
        foreach (Move move in moves)
        {
            if (move.Origin == startSquare) { matchingMoves.Add(move); }
        }

        // Return the list of matching moves
        return matchingMoves;
    }

    // Search for a move
    // Returns the index of the first move to have the same start and end squares
    // It returns -1 if no move could be found
    public int SearchForMove(List<Move> moves, int startSquare, int endSquare)
    {
        // Go over all the moves
        int index = 0;
        foreach(Move move in moves) 
        {
            if (move.Origin == startSquare && move.Destination == endSquare) {  return index; }
            index++;
        }

        // Return -1 if no move is found
        return -1;
    }

    // Attempt a move (called when a possibly legal move is dragged on the board)
    public void AttemptMove(int startSquare, int endSquare)
    {
        // Get all the legal moves in the position
        List<Move> legalMoves = MoveGenerator.GenerateLegalMoves();
        int MatchingMoveIndex = SearchForMove(legalMoves, startSquare, endSquare);

        // If this move is a legal move
        if (MatchingMoveIndex != -1)
        {
            // Attempt the move
            Move toAttempt = legalMoves[MatchingMoveIndex];

            // Play the move
            IPlayer toMove = GameBoard.ColorToMove == Piece.White ? WhitePlayer : BlackPlayer;
            toMove.SubmitMove(GameSquareManager, GameBoard, toAttempt);
            MoveGenerator.GenerateLegalMoves();
            GameSquareManager.UpdatePieceImagesByBoard(GameBoard);
        }
    }

    // When a square is clicked
    // This means the player dragged the piece for less than MaxDragTime seconds
    // NOTE: Only called if there is a piece on the square
    public void OnMouseSquareClick(int square, double timeClicked)
    {
        print($"Player clicked on square with index {square}");
        print($"Piece on square is {GameBoard.spaces[square]}");
    }

    // When the mouse is dragged from one square to another
    // NOTE: Only called if there is a piece on the start square
    public void OnMouseSquareDrag(int startSquare, int endSquare, double timeDragged)
    {
        // Logic for square clicking
        if (startSquare == endSquare && timeDragged < MAX_DRAG_TIME) { OnMouseSquareClick(startSquare, timeDragged); return; }
        else if (startSquare == endSquare) { return; }

        // Attempt a move
        int pieceAtStartSquare = GameBoard.spaces[startSquare];
        int pieceAtEndSquare = GameBoard.spaces[endSquare];
        if (Piece.Color(pieceAtStartSquare) == GameBoard.ColorToMove && Piece.Color(pieceAtEndSquare) != GameBoard.ColorToMove)
        {
            AttemptMove(startSquare, endSquare);
        }
    }

    // Handle dragging
    public void HandleDragging()
    {
        // Get mouse info
        bool FrameMouseButtonDown = Input.GetMouseButtonDown(PieceDragButton);
        bool FrameMouseButtonUp = Input.GetMouseButtonUp(PieceDragButton);
        Vector2 ScaledMousePosition = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
        Vector2 BoardScaledMousePosition = new Vector2(
            ((ScaledMousePosition.x - BoardPlacement.TLX) / (BoardPlacement.BRX - BoardPlacement.TLX)), 
            ((ScaledMousePosition.y - BoardPlacement.BRY) / (BoardPlacement.TLY - BoardPlacement.BRY)));
        bool MouseOverBoard = (0 <= BoardScaledMousePosition.x && BoardScaledMousePosition.x <= 1 &&
            BoardScaledMousePosition.y >= 0 && 1 >= BoardScaledMousePosition.y);

        // If a piece is currently being dragged
        if (PlayerPieceDragState == PieceDragState.DRAGGING)
        {
            // Update the piece renderer positions
            GameSquareManager.SetPieceRendererSquarePosition(SquareManager.DragPieceRenderer,
                    BoardScaledMousePosition.x * 8 - 0.5f, BoardScaledMousePosition.y * 8 - 0.5f);
        }

        // When the mouse is pushed down over the board
        if (MouseOverBoard && FrameMouseButtonDown && PlayerPieceDragState == PieceDragState.NOTHING_GRABBED)
        {
            // Get info about the square clicked
            int rank = (int) (BoardScaledMousePosition.y / 0.125f);
            int file = (int) (BoardScaledMousePosition.x / 0.125f);
            int squareIndex = file + rank * 8;
            int pieceAtIndex = GameBoard.spaces[squareIndex];
            bool isPieceAtIndex = Piece.Type(pieceAtIndex) != Piece.None;

            // If there is a piece at that index
            if (isPieceAtIndex)
            {
                // Setup dragging stuff
                PlayerPieceDragState = PieceDragState.DRAGGING;
                DragStartSquare = squareIndex;
                GameSquareManager.SetPieceDragOrigin(squareIndex);
                GameSquareManager.SetPieceRendererImage(SquareManager.DragPieceRenderer, GameSquareManager.GetPieceTexture(pieceAtIndex));
                GameSquareManager.SetPieceRendererSquarePosition(SquareManager.DragPieceRenderer,
                    BoardScaledMousePosition.x * 8 - 0.5f, BoardScaledMousePosition.y * 8 - 0.5f);
                DragStartTime = Time.realtimeSinceStartupAsDouble;

                // Square highlights
                foreach (Move move in SearchForMove(MoveGenerator.GenerateLegalMoves(), squareIndex))
                {
                    GameSquareManager.ChangeSquareHighlight(move.Destination, Square.BlueHighlight);
                }
            }
        }

        // When the mouse is released over the board
        else if (MouseOverBoard && FrameMouseButtonUp && PlayerPieceDragState == PieceDragState.DRAGGING)
        {
            // Get info about the square the mouse was released on
            int rank = (int)(BoardScaledMousePosition.y / 0.125f);
            int file = (int)(BoardScaledMousePosition.x / 0.125f);
            int squareIndex = file + rank * 8;

            // End dragging stuff
            PlayerPieceDragState = PieceDragState.NOTHING_GRABBED;
            GameSquareManager.SetPieceDragOrigin(-1);
            OnMouseSquareDrag(DragStartSquare, squareIndex, Time.realtimeSinceStartupAsDouble - DragStartTime);
            GameSquareManager.ChangeAllSquareHighlights(Square.NoHighlight);
        }

        // When the mouse is released but isn't over the board
        else if (!MouseOverBoard && FrameMouseButtonUp && PlayerPieceDragState == PieceDragState.DRAGGING)
        {
            // End dragging stuff
            PlayerPieceDragState = PieceDragState.NOTHING_GRABBED;
            GameSquareManager.SetPieceDragOrigin(-1);
            GameSquareManager.ChangeAllSquareHighlights(Square.NoHighlight);
        }
    }

    // Change the availability of piece dragging
    public void SetPieceDragState(PieceDragState newPieceDragState)
    {
        PlayerPieceDragState = newPieceDragState;
    }

    // Update every frame
    public void Update()
    {
        // If the mouse dragging is allowed, then do it
        if (PlayerPieceDragState != PieceDragState.DISABLED) { HandleDragging(); }
    }
}
