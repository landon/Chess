using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            var brain = new NullMoveBrain(new PieceSquareTablesEvaluator(), 200);
            brain.SearchInformationReport += brain_SearchInformationReport;

            var board = Board.FromFEN("6kr/p1p1qppp/5n2/1p6/PP6/K7/3r4/8 b - - 0 36");
            brain.Analyze(board, new TimeControl(TimeControl.TimeControlTypes.SecondsPerMove, 10));
        }

        static void brain_SearchInformationReport(Brain.SearchInformation information)
        {
            Console.Write(information.Depth + ".  " + information.DisplayScore + "  " + information.NodesEvaluated + " ");
            foreach (Move move in information.PrincipalVariation)
            {
                Console.Write(move.ToAlgebraic() + " ");
            }

            Console.WriteLine();
        }
    }
}
