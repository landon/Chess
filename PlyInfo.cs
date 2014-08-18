using System;
using System.Collections.Generic;
using System.Text;

using BitBoard = System.Int64;

namespace Generator
{
    public class PlyInfo
    {
        public const int MaxPly = 300;
        const int KillerCount = 4;

        public PlyInfo()
        {
            Board = new Board();
        }

        public Board Board;
        public Move[] PrincipalVariation = new Move[MaxPly];
        public int PrincipalVariationLength;
        public Move[] Killer = new Move[KillerCount];
        public bool SkipNullMove = false;
        public List<Move> Moves = new List<Move>(30);
    }
}
