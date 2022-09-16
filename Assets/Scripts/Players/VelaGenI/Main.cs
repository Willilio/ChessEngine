using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// Using the vela namespace
namespace ChessPlayer.Vela
{
    // Genereation 1 of my vela chess engine
    public class VelaGenI : IPlayer
    {
        // Information about the player
        private string Name = "Vela Gen1";
        private int Elo = 100;
        private const bool PlayerIsComputer = true;
        private int depth = 3;

        // Has the player been set up?
        public GameRunner GameManager = null;
        public IPlayer NextToPlay = null;

        // Latest variables
        private MoveGeneration LatestMoveGeneration;
        private Board LatestBoard;
        private SquareManager LatestSquareManager;

        // Constructor for the player
        public VelaGenI(IPlayer nextPlayer, GameRunner gameRunner)
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

        // Not implemented methods
        public void SubmitMove(SquareManager squareManager, Board board, Move move) { }

        // Get a move from the player
        public void OnPlayMoveRequest(SquareManager squareManager, MoveGeneration moveGenerator, Board board)
        {
            // Get the move
            Move toPlay = GetBestMove(moveGenerator, board);

            // Play the move
            board.MakeMove(toPlay);
            moveGenerator.GenerateLegalMoves();
            squareManager.UpdatePieceImagesByBoard(board);

            // Tell the other player to make a move
            if (!board.GameOver)
            {
                NextToPlay.OnPlayMoveRequest(squareManager, moveGenerator, board);
            }
        }

        // Get the best move
        public Move GetBestMove(MoveGeneration moveGenerator, Board board)
        {
            // Initialize class values
            List<Move> AllMoves = moveGenerator.GenerateLegalMoves();
            Move bestMove = AllMoves[0];
            float bestEvaluation = float.NegativeInfinity;

            // Go through all the moves
            foreach (Move move in AllMoves)
            {
                board.MakeMove(move);
                float evaluation = -Search(moveGenerator, board, depth);
                MonoBehaviour.print($"Evaluation for {move.ToAlgebraic()} is {evaluation}");
                if (evaluation > bestEvaluation) { bestEvaluation = evaluation; bestMove = move; }
                board.UnmakeMove(move);
            }

            // Return the best move
            return bestMove;
        }

        // Search is a better evaluation function that looks into the future
        public float Search(MoveGeneration moveGeneration, Board board, int depth)
        {
            // When the depth is complete
            if (depth == 0) { return Evaluate(board); }

            // Get all the legal moves
            List<Move> legalMoves = moveGeneration.GenerateLegalMoves();
            if (legalMoves.Count == 0)
            {
                // ATTENTION: THIS SHOULD BE NEGATIVE INFINITY IF IT IS CHECKMATE
                return 0;
            }

            // Keep track of the best evaluation
            float bestEvaluation = float.NegativeInfinity;

            // Go through all opponent moves
            foreach (Move response in legalMoves)
            {
                board.MakeMove(response);
                float newEval = -Search(moveGeneration, board, depth - 1);
                bestEvaluation = MathF.Max(newEval, bestEvaluation);
                board.UnmakeMove(response);
            }

            // Search for a deeper evaluation
            return bestEvaluation;
        }

        // Get the value of a piece
        public float GetPieceValue(int piece)
        {
            switch (Piece.Type(piece))
            {
                case Piece.Queen: 
                    return 900;
                case Piece.Rook:
                    return 500;
                case Piece.Bishop:
                    return 300;
                case Piece.Knight:
                    return 250;
                case Piece.Pawn:
                    return 100;
                default:
                    return 0;
            }
        }

        // Get the evaluation of a position
        public float Evaluate(Board board)
        {
            // Define the evaluation
            float evaluation = 0f;

            // Count material
            for (int i = 0; i < 64; i++)
            {
                if (Piece.Type(board.spaces[i]) != Piece.None)
                {
                    evaluation += (Piece.Color(board.spaces[i]) == board.ColorToMove ? 1 : -1) * GetPieceValue(board.spaces[i]);
                }
            }

            // Return the evaluation
            return evaluation;
        }
    }
}