using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Generator
{
    public class MTDfBrain : BasicAlphaBetaBrain
    {
        public MTDfBrain(StaticEvaluator staticEvaluator, int memoryCapacity_MB)
            : base(staticEvaluator, memoryCapacity_MB)
        {
        }

        protected override int Search(int depth, int scoreGuess)
        {
            var upperBound = int.MaxValue;
            var lowerBound = int.MinValue;

            int score = scoreGuess;
            int beta = 0;

            while (lowerBound < upperBound)
            {
                if (score == lowerBound)
                    beta = score + 1;
                else
                    beta = score;

                score = AlphaBetaSearch(beta - 1, beta, depth, 0, _OurSide);

                if (score < beta)
                    upperBound = score;
                else
                    lowerBound = score;
            }

            return score;
        }
    }
}
