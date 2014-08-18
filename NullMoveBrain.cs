using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Generator
{
    public class NullMoveBrain : BasicAlphaBetaBrain
    {
        public NullMoveBrain(StaticEvaluator staticEvaluator, int memoryCapacity_MB)
            : base(staticEvaluator, memoryCapacity_MB)
        {
        }

        protected override bool CanPrune(int alpha, int beta, int remainingDepth, int currentPly, Sides currentSide, out int score)
        {
            score = 0;
            if (SkipNullMove(currentPly, currentSide)) return false;

            _ScratchBoard.Side = Board.OtherSide(_ScratchBoard.Side);
            _PlyInfo[currentPly + 1].SkipNullMove = true;

            int depth = NullMoveSearchDepth(remainingDepth);
            if (depth > 0)
                score = -AlphaBetaSearch(-beta, 1 - beta, depth, currentPly + 1, _ScratchBoard.Side);
            else
                score = -QuiescenceSearch(-beta, 1 - beta, currentPly + 1, _ScratchBoard.Side);

            _ScratchBoard.Side = Board.OtherSide(_ScratchBoard.Side);
            _PlyInfo[currentPly + 1].SkipNullMove = false;

            if (!_AreWeThinking)
                return false;

            if (score >= beta)
            {
                _TranspositionTable.AddPosition(_ScratchBoard, Move.Empty, score,
                        remainingDepth, currentPly, currentSide, HashEntryTypes.LowerBound);

                return true;
            }

            return false;
        }

        protected virtual bool SkipNullMove(int currentPly, Sides currentSide)
        {
            if (_PlyInfo[currentPly].SkipNullMove || _ScratchBoard.InCheck(currentSide))
                return true;

            return false;
        }

        protected virtual int NullMoveSearchDepth(int remainingDepth)
        {
            if (remainingDepth <= 6)
                return remainingDepth - 4;

            return (int)Math.Sqrt(remainingDepth);
        }
    }
}
