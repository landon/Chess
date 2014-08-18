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

            var testSuite = new TestSuite(@"..\..\wac.epd");

            testSuite.Run(brain, 1);

            //Console.ReadKey();
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
