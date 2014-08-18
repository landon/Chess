using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Generator
{
    public abstract class TranspositionTableBrain : Brain
    {
        public TranspositionTableBrain(StaticEvaluator staticEvaluator, int memoryCapacity_MB)
            : base(staticEvaluator)
        {
            _TranspositionTable = new TranspositionTable(memoryCapacity_MB * 1024 * 1024);
        }

        protected override void AnalyzeOurBoard()
        {
            _NodesEvaluated = 0;

            _TranspositionTable.SetStale();

            int depth = 2;
            int score = 0;
            while (true)
            {
                score = Search(depth, score);

                if (!_AreWeThinking) break;

                _Score = score;
                _BestMove = _PlyInfo[0].PrincipalVariation[0];

                _PrincipalVariation.Clear();
                for (int i = 0; i < _PlyInfo[0].PrincipalVariationLength; i++)
                    _PrincipalVariation.Add(_PlyInfo[0].PrincipalVariation[i]);

                ReportSearchInformation(depth);

                if (StaticEvaluator.IsMate(_Score)) break;

                StuffPrincipalVariationIntoTranspositionTable(0, _OurSide, depth);

                depth++;
            }
        }

        void StuffPrincipalVariationIntoTranspositionTable(int currentPly, Sides currentSide, int searchDepth)
        {
            if (currentPly == _PlyInfo[0].PrincipalVariationLength)
                return;

            _TranspositionTable.AddPosition(_ScratchBoard, _PlyInfo[0].PrincipalVariation[currentPly], 0, searchDepth, currentPly, currentSide, HashEntryTypes.Junk);

            _ScratchBoard.MakeMove(_PlyInfo[0].PrincipalVariation[currentPly]);

            StuffPrincipalVariationIntoTranspositionTable(currentPly + 1, Board.OtherSide(currentSide), searchDepth - 1);

            UndoMove(currentPly);
        }

        protected abstract int Search(int depth, int scoreGuess);

        protected TranspositionTable _TranspositionTable;
    }
}
