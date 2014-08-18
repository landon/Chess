using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using System.Threading;

using BitBoard = System.Int64;

namespace Generator
{
    public abstract class Brain
    {
        public delegate void SearchInformationReportingDelegate(SearchInformation information);
        public event SearchInformationReportingDelegate SearchInformationReport;

        public Brain(StaticEvaluator staticEvaluator)
        {
            _StaticEvaluator = staticEvaluator;
            
            for (int i = 0; i < _PlyInfo.Length; i++)
            {
                _PlyInfo[i] = new PlyInfo();
            }
        }

        public void StartGame(Board initialBoard, Sides side, TimeControl timeControl)
        {
            _Board = initialBoard;
            _OurSide = side;
            _TimeControl = timeControl;
            _TimeControl.TimeToMove += _TimeControl_TimeToMove;

            _IsGameStopped = false;
            _ThinkingThread = new Thread(ThinkAboutGame);
            _ThinkingThread.Start();
        }

        public void StopGame()
        {
            _IsGameStopped = true;
            _AreWeThinking = false;
        }

        public void Analyze(Board board, TimeControl timeControl)
        {
            _Board = board;
            _OurSide = _Board.Side;
            _TimeControl = timeControl;
            _TimeControl.TimeToMove -= _TimeControl_TimeToMove;
            _TimeControl.TimeToMove += _TimeControl_TimeToMove;

            Thread t = new Thread(PonderOurBoard);
            t.Start();
        }

        void _TimeControl_TimeToMove()
        {
            _AreWeThinking = false;
        }

        void PonderOurBoard()
        {
            _AreWeThinking = true;
            _TimeControl.OurTurn();
            _ScratchBoard.CopyFrom(_Board);
            AnalyzeOurBoard();
            ForceMove();
        }

        void PonderOpponentBoard()
        {
            _IsOpponentThinking = true;
            _ScratchBoard.CopyFrom(_Board);
            AnalyzeOpponentBoard();
        }

        public Move ForceMove()
        {
            _AreWeThinking = false;

            _TimeControl.WeMoved(_BestMove);

            return _BestMove;
        }

        public void OpponentMoved()
        {
            _IsOpponentThinking = false;
        }

        protected abstract void AnalyzeOurBoard();

        protected virtual void AnalyzeOpponentBoard()
        {
            while (_IsOpponentThinking)
            {
                Thread.Sleep(100);
            }
        }

        void ThinkAboutGame()
        {
            while (!_IsGameStopped)
            {
                // Is it our turn to move?
                if (_Board.Side == _OurSide)
                {
                    PonderOurBoard();
                }
                else
                {
                    PonderOpponentBoard();
                }
            }
        }

        protected virtual void ReportSearchInformation(int depth)
        {
            if (SearchInformationReport != null)
            {
                SearchInformation searchInformation = new SearchInformation();
                searchInformation.Depth = depth;
                searchInformation.Score = _Score;
                searchInformation.NodesEvaluated = _NodesEvaluated;
                searchInformation.PrincipalVariation = new List<Move>(_PrincipalVariation);

                SearchInformationReport(searchInformation);
            }
        }

        protected void UpdatePrincipalVariation(Move move, int currentPly)
        {
            _PlyInfo[currentPly].PrincipalVariation[currentPly] = move;

            for (int i = currentPly + 1; i < _PlyInfo[currentPly + 1].PrincipalVariationLength; i++)
                _PlyInfo[currentPly].PrincipalVariation[i] = _PlyInfo[currentPly + 1].PrincipalVariation[i];

            _PlyInfo[currentPly].PrincipalVariationLength = _PlyInfo[currentPly + 1].PrincipalVariationLength;
        }

        protected void UndoMove(int currentPly)
        {
            _ScratchBoard.CopyFrom(_PlyInfo[currentPly].Board);
        }

        protected virtual MoveGenerationResults GenerateMoves(int currentPly, Sides currentSide, out List<Move> moves)
        {
            moves = _PlyInfo[currentPly].Moves;

            int kingAttackCount = _ScratchBoard.KingAttackCount(currentSide);
            if (kingAttackCount > 0)
            {
                MoveGenerator.GenerateCheckEscapes(_ScratchBoard, kingAttackCount, moves);

                if (moves.Count == 0)
                {
                    // We are mated.
                    return MoveGenerationResults.Mated;
                }
            }
            else
            {
                MoveGenerator.GenerateAll(_ScratchBoard, moves);
            }

            return MoveGenerationResults.NotMated;
        }

        protected virtual MoveGenerationResults GenerateQuiescenceMoves(int currentPly, Sides currentSide, out List<Move> moves)
        {
            moves = _PlyInfo[currentPly].Moves;

            MoveGenerator.GenerateQuiescenceCaptures(_ScratchBoard, moves);

            return MoveGenerationResults.NotMated;
        }

        protected void SetKiller(Move killer, int currentPly)
        {
            int holeIndex = _PlyInfo[currentPly].Killer.Length - 1;
            for (int i = 0; i < _PlyInfo[currentPly].Killer.Length; i++)
            {
                if (_PlyInfo[currentPly].Killer[i].Equals(killer))
                {
                    holeIndex = i;
                    break;
                }
            }

            for (int i = holeIndex; i >= 1; i--)
            {
                _PlyInfo[currentPly].Killer[i] = _PlyInfo[currentPly].Killer[i - 1];
            }

            _PlyInfo[currentPly].Killer[0] = killer;
        }

        public Move BestMove
        {
            get
            {
                return _BestMove;
            }
        }

        public int NodesEvaluated
        {
            get
            {
                return _NodesEvaluated;
            }
        }

        Board _Board;
        protected Board _ScratchBoard = new Board();
        protected TimeControl _TimeControl;
        protected Sides _OurSide;
        protected Move _BestMove;
        protected List<Move> _PrincipalVariation = new List<Move>();
        protected int _Score;
        protected int _NodesEvaluated;
        protected PlyInfo[] _PlyInfo = new PlyInfo[PlyInfo.MaxPly + 1];
        protected StaticEvaluator _StaticEvaluator;

        Thread _ThinkingThread;
        bool _IsGameStopped;

        bool _IsOpponentThinking;
        protected bool _AreWeThinking;

        #region Nested Classes
        public class SearchInformation
        {
            public int Depth;
            public int Score;
            public int NodesEvaluated;
            public List<Move> PrincipalVariation;

            public string DisplayScore
            {
                get
                {
                    if (StaticEvaluator.IsMate(Score))
                    {
                        string outcome = "Win in ";
                        if (Score < 0)
                        {
                            outcome = "Lose in ";
                        }

                        return outcome + StaticEvaluator.PlyFromMateScore(Score).ToString();
                    }
                    else
                    {
                        return Score.ToString();
                    }
                }
            }
        }
        #endregion
    }
}
