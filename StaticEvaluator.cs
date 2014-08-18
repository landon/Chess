using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
    public abstract class StaticEvaluator
    {
        public const int MinScore = int.MinValue + 1;
        public const int MaxScore = int.MaxValue;
        const int SmallestMateScore = MaxScore - 1050;
        public const int LargestMateScore = MaxScore - 50;

        public int[] PieceValue = new int[7];

        public StaticEvaluator()
        {
            PieceValue[Pieces.None] = 0;
            PieceValue[Pieces.Pawn] = 100;
            PieceValue[Pieces.Knight] = 300;
            PieceValue[Pieces.Bishop] = 300;
            PieceValue[Pieces.Rook] = 500;
            PieceValue[Pieces.Queen] = 900;
            PieceValue[Pieces.King] = int.MaxValue;
        }

        public static int MateScoreFromPly(int ply)
        {
            return LargestMateScore - ply;
        }

        public static int PlyFromMateScore(int score)
        {
            return LargestMateScore - Math.Abs(score);
        }

        public static bool IsMate(int score)
        {
            return Math.Abs(score) >= SmallestMateScore;
        }

        public static int RemovePlyDependence(int score)
        {
            if (Math.Abs(score) >= SmallestMateScore) return Math.Sign(score) * LargestMateScore;

            return score;
        }

        public static int AddPlyDependence(int score, int ply)
        {
            if (Math.Abs(score) == LargestMateScore) return Math.Sign(score) * (LargestMateScore - ply);

            return score;
        }

        public virtual int DrawScore(int ply, Sides fromWhosePerspective)
        {
            return 0;
        }

        public int Evaluate(Board board, Sides fromWhosePerspective)
        {
            var score = EvaluateInternal(board);

            return fromWhosePerspective == Sides.White ? score : -score;
        }

        protected abstract int EvaluateInternal(Board board);
    }
}
