using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Scripting.APIUpdating;

// Stay within the chess playing namespace
namespace ChessPlayer
{
    // The random player should play random legal moves
    // Its elo is practically 100
    // We'll name it "Aleatória"
    public class RandomPlayer : IPlayer
    {
        // Information about the player
        private string Name = "Aleatória";
        private int Elo = 100;
        private const int MillisTimeDelay = 200;
        private const bool PlayerIsComputer = true;

        // The player that plays next
        private IPlayer NextToPlay;

        // Constructor for the player
        public RandomPlayer(IPlayer nextPlayer)
        {
            NextToPlay = nextPlayer;
        }

        // Get information about the player
        public string GetName() { return Name; }
        public int GetElo() { return Elo; }
        public bool IsComputer() { return PlayerIsComputer; }

        // Set information about the player
        public void SetName(string newName) { Name = newName; }
        public void SetElo(int newElo) { Elo = newElo; }
        public void SetNextToMove(IPlayer nextMove) { NextToPlay = nextMove; }

        // Not implemented methods
        public void SubmitMove(SquareManager squareManager, Board board, Move move) { }

        // Get a move from the player
        public void OnPlayMoveRequest(SquareManager squareManager, MoveGeneration moveGenerator, Board board)
        {
            // Get the move
            moveGenerator.GenerateLegalMoves();

            // Play the move
            board.MakeMove(moveGenerator.GetRandomLegalMove());
            moveGenerator.GenerateLegalMoves();
            squareManager.UpdatePieceImagesByBoard(board);

            // Tell the other player to make a move
            if (!board.GameOver)
            {
                NextToPlay.OnPlayMoveRequest(squareManager, moveGenerator, board);
            }
        }
    }
}
