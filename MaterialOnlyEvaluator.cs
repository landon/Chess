using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
    public class MaterialOnlyEvaluator : StaticEvaluator
    {
        protected override int EvaluateInternal(Board board)
        {
            int score = 0;

            score += PieceValue[Pieces.Pawn] * BitOperations.PopulationCount(board.BitBoard[Pieces.WhitePawn]);
            score += PieceValue[Pieces.Bishop] * BitOperations.PopulationCount(board.BitBoard[Pieces.WhiteBishop]);
            score += PieceValue[Pieces.Knight] * BitOperations.PopulationCount(board.BitBoard[Pieces.WhiteKnight]);
            score += PieceValue[Pieces.Rook] * BitOperations.PopulationCount(board.BitBoard[Pieces.WhiteRook]);
            score += PieceValue[Pieces.Queen] * BitOperations.PopulationCount(board.BitBoard[Pieces.WhiteQueen]);

            score -= PieceValue[Pieces.Pawn] * BitOperations.PopulationCount(board.BitBoard[Pieces.BlackPawn]);
            score -= PieceValue[Pieces.Bishop] * BitOperations.PopulationCount(board.BitBoard[Pieces.BlackBishop]);
            score -= PieceValue[Pieces.Knight] * BitOperations.PopulationCount(board.BitBoard[Pieces.BlackKnight]);
            score -= PieceValue[Pieces.Rook] * BitOperations.PopulationCount(board.BitBoard[Pieces.BlackRook]);
            score -= PieceValue[Pieces.Queen] * BitOperations.PopulationCount(board.BitBoard[Pieces.BlackQueen]);

            return score;
        }
    }
}
