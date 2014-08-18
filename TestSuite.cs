using System;
using System.Collections.Generic;
using System.Text;

using System.Linq;
using System.IO;
using System.Threading;

namespace Generator
{
    public class TestSuite
    {
        public TestSuite(string fileName)
        {
            using (StreamReader sr = new StreamReader(fileName))
            {
                ParseEPDs(sr);
            }
        }

        public TestSuite(byte[] fileBytes)
        {
            using (MemoryStream m = new MemoryStream(fileBytes))
            using (StreamReader sr = new StreamReader(m))
            {
                ParseEPDs(sr);
            }
        }

        void ParseEPDs(StreamReader sr)
        {
            _EPDs = new Dictionary<string, List<string>>();
            string[] lines = Utility.Split(sr.ReadToEnd(), "\n");
            foreach (string line in lines)
            {
                int i = line.IndexOf(" bm ");
                if (i < 0) continue;

                string fen = line.Substring(0, i);

                int j = line.IndexOf(";", i);
                if (j < 0)
                {
                    j = line.Length - 1;
                }

                string bestMoves = line.Substring(i + 4, j - i - 4).Trim();

                _EPDs[fen] = new List<string>(Utility.Split(bestMoves, " "));
            }
        }

        public double Run(Brain brain, double secondsPerMove)
        {
            TimeControl timeControl = new TimeControl(TimeControl.TimeControlTypes.SecondsPerMove, secondsPerMove);
            timeControl.MoveMade += new TimeControl.MoveDelegate(timeControl_MoveMade);

            int totalTests = 0;
            int passedTests = 0;
            foreach (KeyValuePair<string, List<string>> epd in _EPDs)
            {
                Board board = Board.FromFEN(epd.Key);

                brain.Analyze(board, timeControl);

                _WaitForMove.WaitOne();

                string algebraic = brain.BestMove.ToAlgebraic(board);
                bool passed = epd.Value.Exists(bm => bm.StartsWith(algebraic));

                totalTests++;
                if (passed)
                {
                    passedTests++;
                }

                string bestMoves = string.Join(", ", epd.Value);
                
                Console.WriteLine(string.Format("Nodes Per Second: {0}", (int)(brain.NodesEvaluated / timeControl.LastMoveTimeUsed)));
                Console.WriteLine(string.Format("  Suggested Move: {0}", algebraic));
                Console.WriteLine(string.Format("       Best Move: {0}", bestMoves.Trim().Trim(',')));
                Console.WriteLine(string.Format("           Score: {0}/{1}", passedTests, totalTests));
                Console.WriteLine();
            }

            double percent = 100 * (double)passedTests / totalTests;
            Console.WriteLine();
            Console.WriteLine(string.Format("Passed {0:0.0}% of tests.", percent));

            return percent;
        }

        void timeControl_MoveMade(Move move)
        {
            _WaitForMove.Set();
        }

        Dictionary<string, List<string>> _EPDs;
        AutoResetEvent _WaitForMove = new AutoResetEvent(false);
    }
}
