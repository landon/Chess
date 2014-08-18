using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using BitBoard = System.Int64;

namespace Generator
{
    public class BasicAlphaBetaBrain : TranspositionTableBrain
    {
        public BasicAlphaBetaBrain(StaticEvaluator staticEvaluator, int memoryCapacity_MB)
            : base(staticEvaluator, memoryCapacity_MB)
        {
        }

        protected override int Search(int depth, int scoreGuess)
        {
            int alpha = StaticEvaluator.MinScore;
            int beta = StaticEvaluator.MaxScore;

            return AlphaBetaSearch(alpha, beta, depth, 0, _OurSide);
        }

        protected virtual bool CanPrune(int alpha, int beta, int remainingDepth, int currentPly, Sides currentSide, out int score)
        {
            score = 0;
            return false;
        }

        protected int AlphaBetaSearch(int alpha, int beta, int remainingDepth, int currentPly, Sides currentSide)
        {
            _NodesEvaluated++;

            _PlyInfo[currentPly].PrincipalVariationLength = currentPly;

            if (currentPly >= PlyInfo.MaxPly) return beta;

            switch (_TranspositionTable.LookupPosition(_ScratchBoard, remainingDepth, currentPly, currentSide, ref alpha, ref beta))
            {
                case HashEntryTypes.LowerBound:
                    return beta;
                case HashEntryTypes.Exact:
                    Move transpositionTableMove = _TranspositionTable.GetBestMove(_ScratchBoard, currentSide);
                    if (!transpositionTableMove.Equals(Move.Empty))
                        UpdatePrincipalVariation(transpositionTableMove, currentPly);

                    return alpha;
                case HashEntryTypes.UpperBound:
                    return alpha;
            }

            if (remainingDepth <= 0) return QuiescenceSearch(alpha, beta, currentPly, currentSide);

            int pruneScore;
            if (CanPrune(alpha, beta, remainingDepth, currentPly, currentSide, out pruneScore)) return pruneScore;

            List<Move> moves;
            if (GenerateMoves(currentPly, currentSide, out moves) == MoveGenerationResults.Mated)
            {
                int mateScore = -StaticEvaluator.MateScoreFromPly(currentPly);
                _TranspositionTable.AddPosition(_ScratchBoard, Move.Empty,
                    mateScore, remainingDepth, currentPly, currentSide, HashEntryTypes.Exact);

                return mateScore;
            }
          
            _PlyInfo[currentPly].Board.CopyFrom(_ScratchBoard);

            int originalAlpha = alpha;

            foreach (Move move in moves)
            {
                if (!_AreWeThinking)
                    return alpha;

                if (_ScratchBoard.MakeMove(move) == MakeMoveResults.UndoMove)
                {
                    UndoMove(currentPly);
                    continue;
                }

                int score = -AlphaBetaSearch(-beta, -alpha, remainingDepth - 1, currentPly + 1, Board.OtherSide(currentSide));

                UndoMove(currentPly);

                if (score >= beta)
                {
                    SetKiller(move, currentPly);

                    _TranspositionTable.AddPosition(_ScratchBoard, move, score,
                        remainingDepth, currentPly, currentSide, HashEntryTypes.LowerBound);

                    return score;
                }

                if (score > alpha)
                {
                    alpha = score;

                    UpdatePrincipalVariation(move, currentPly);
                }
            }

            if (alpha == originalAlpha)
            {
                _TranspositionTable.AddPosition(_ScratchBoard, Move.Empty, 
                    alpha, remainingDepth, currentPly, currentSide, HashEntryTypes.UpperBound);
            }
            else
            {
                SetKiller(_PlyInfo[currentPly].PrincipalVariation[currentPly], currentPly);

                _TranspositionTable.AddPosition(_ScratchBoard, _PlyInfo[currentPly].PrincipalVariation[currentPly], 
                    alpha, remainingDepth, currentPly, currentSide, HashEntryTypes.Exact);
            }

            return alpha;
        }

        protected virtual int QuiescenceSearch(int alpha, int beta, int currentPly, Sides currentSide)
        {
            _NodesEvaluated++;

            _PlyInfo[currentPly].PrincipalVariationLength = currentPly;

            if (currentPly >= PlyInfo.MaxPly) return beta;

            int staticScore = _StaticEvaluator.Evaluate(_ScratchBoard, currentSide);
            if (staticScore >= beta) return beta;

            if (staticScore > alpha)
            {
                alpha = staticScore;
            }

            List<Move> moves;
            GenerateQuiescenceMoves(currentPly, currentSide, out moves);

            _PlyInfo[currentPly].Board.CopyFrom(_ScratchBoard);

            foreach (Move move in moves)
            {
                if (move.IsCapture)
                {
                    var value = _StaticEvaluator.PieceValue[Pieces.GetKind(move.CapturedPiece)];
                    if (move.IsPromotion)
                        value += _StaticEvaluator.PieceValue[Pieces.GetKind(move.PromotionPiece)];

                    value += _StaticEvaluator.PieceValue[Pieces.Pawn];

                    if (staticScore + value < alpha)
                        continue;
                }

                if (_ScratchBoard.MakeMove(move) == MakeMoveResults.UndoMove)
                {
                    UndoMove(currentPly);
                    continue;
                }

                int score = -QuiescenceSearch(-beta, -alpha, currentPly + 1, Board.OtherSide(currentSide));

                UndoMove(currentPly);

                if (score >= beta) return score;

                if (score > alpha)
                {
                    alpha = score;

                    UpdatePrincipalVariation(move, currentPly);
                }
            }

            return alpha;
        }

        #region Move Ordering
        protected override MoveGenerationResults GenerateMoves(int currentPly, Sides currentSide, out List<Move> moves)
        {
            if (base.GenerateMoves(currentPly, currentSide, out moves) == MoveGenerationResults.Mated)
                return MoveGenerationResults.Mated;

            for (int i = _PlyInfo[currentPly].Killer.Length - 1; i >= 0; i--)
            {
                SwapInIfLegal(moves, _PlyInfo[currentPly].Killer[i]);
            }

            var move = _TranspositionTable.GetBestMove(_ScratchBoard, currentSide);
            SwapInIfLegal(moves, move);

            return MoveGenerationResults.NotMated;
        }

        protected override MoveGenerationResults GenerateQuiescenceMoves(int currentPly, Sides currentSide, out List<Move> moves)
        {
            base.GenerateQuiescenceMoves(currentPly, currentSide, out moves);

            return MoveGenerationResults.NotMated;
        }

        void SwapInIfLegal(List<Move> moves, Move move)
        {
            if (moves.Remove(move))
            {
                moves.Insert(0, move);
            }
        }
        #endregion
    }
}
