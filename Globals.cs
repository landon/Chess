using System;
using System.Collections.Generic;
using System.Text;

using BitBoard = System.Int64;

namespace Generator
{
    #region Enums
    public static class Squares
    {
        public const int None = 255;
        public const int H1 = 0;
        public const int G1 = 1;
        public const int F1 = 2;
        public const int E1 = 3;
        public const int D1 = 4;
        public const int C1 = 5;
        public const int B1 = 6;
        public const int A1 = 7;
        public const int H2 = 8;
        public const int G2 = 9;
        public const int F2 = 10;
        public const int E2 = 11;
        public const int D2 = 12;
        public const int C2 = 13;
        public const int B2 = 14;
        public const int A2 = 15;
        public const int H3 = 16;
        public const int G3 = 17;
        public const int F3 = 18;
        public const int E3 = 19;
        public const int D3 = 20;
        public const int C3 = 21;
        public const int B3 = 22;
        public const int A3 = 23;
        public const int H4 = 24;
        public const int G4 = 25;
        public const int F4 = 26;
        public const int E4 = 27;
        public const int D4 = 28;
        public const int C4 = 29;
        public const int B4 = 30;
        public const int A4 = 31;
        public const int H5 = 32;
        public const int G5 = 33;
        public const int F5 = 34;
        public const int E5 = 35;
        public const int D5 = 36;
        public const int C5 = 37;
        public const int B5 = 38;
        public const int A5 = 39;
        public const int H6 = 40;
        public const int G6 = 41;
        public const int F6 = 42;
        public const int E6 = 43;
        public const int D6 = 44;
        public const int C6 = 45;
        public const int B6 = 46;
        public const int A6 = 47;
        public const int H7 = 48;
        public const int G7 = 49;
        public const int F7 = 50;
        public const int E7 = 51;
        public const int D7 = 52;
        public const int C7 = 53;
        public const int B7 = 54;
        public const int A7 = 55;
        public const int H8 = 56;
        public const int G8 = 57;
        public const int F8 = 58;
        public const int E8 = 59;
        public const int D8 = 60;
        public const int C8 = 61;
        public const int B8 = 62;
        public const int A8 = 63;

        public static bool IsValid(int square)
        {
            return H1 <= square && square <= A8;
        }
    }

    public static class Pieces
    {
        public const byte None = 0;
        public const byte BlackPawn = 1;
        public const byte BlackKnight = 2;
        public const byte BlackBishop = 3;
        public const byte BlackRook = 4;
        public const byte BlackQueen = 5;
        public const byte BlackKing = 6;

        public const byte WhitePawn = 7;
        public const byte WhiteKnight = 8;
        public const byte WhiteBishop = 9;
        public const byte WhiteRook = 10;
        public const byte WhiteQueen = 11;
        public const byte WhiteKing = 12;

        public const byte Pawn = 1;
        public const byte Knight = 2;
        public const byte Bishop = 3;
        public const byte Rook = 4;
        public const byte Queen = 5;
        public const byte King = 6;

        public static byte GetKind(byte piece)
        {
            if (piece > 6)
                piece -= 6;

            return piece;
        }

        public static bool IsWhite(byte piece)
        {
            return 7 <= piece && piece <= 12;
        }

        public static bool IsBlack(byte piece)
        {
            return 1 <= piece && piece <= 6;
        }
        public static Sides GetSide(byte piece)
        {
            return IsBlack(piece) ? Sides.Black : Sides.White;
        }
        public static bool IsPawn(byte piece)
        {
            return piece == WhitePawn || piece == BlackPawn;
        }
        public static bool IsKnight(byte piece)
        {
            return piece == WhiteKnight || piece == BlackKnight;
        }
        public static bool IsBishop(byte piece)
        {
            return piece == WhiteBishop || piece == BlackBishop;
        }
        public static bool IsRook(byte piece)
        {
            return piece == WhiteRook || piece == BlackRook;
        }
        public static bool IsQueen(byte piece)
        {
            return piece == WhiteQueen || piece == BlackQueen;
        }
        public static bool IsKing(byte piece)
        {
            return piece == WhiteKing || piece == BlackKing;
        }

        public static string ToFEN(byte b)
        {
            return (string)_Map[b];
        }

        public static byte FromFEN(string s)
        {
            return (byte)_Map[s];
        }

        public static byte FromFEN(char c)
        {
            return FromFEN(c.ToString());
        }

        static Pieces()
        {
            _Map = new Dictionary<object, object>() 
            { { None, "?"},
              { BlackPawn, "p" }, { BlackKnight, "n" }, { BlackBishop, "b" },
              { BlackRook, "r" }, { BlackQueen, "q" }, { BlackKing, "k" }, 
              { WhitePawn, "P" }, { WhiteKnight, "N" }, { WhiteBishop, "B" },
              { WhiteRook, "R" }, { WhiteQueen, "Q" }, { WhiteKing, "K" }};

            foreach (object key in new List<object>(_Map.Keys))
            {
                _Map[_Map[key]] = key;
            }
        }

        static Dictionary<object, object> _Map;
    }

    public static class PieceCategories
    {
        public const byte All = 13;
        public const byte AllWhite = 14;
        public const byte AllBlack = 15;
        public const byte AllRotated45 = 16;
        public const byte AllRotated90 = 17;
        public const byte AllRotated135 = 18;
        public const byte MaxBitBoard = 19;
    }

    public enum MoveModifiers : byte
    {
        None,
        KingSideCastle,
        QueenSideCastle,
        EnPassant
    }

    [Flags]
    public enum CastleFlags : byte
    {
        BlackHasCastled = 1,
        BlackCanCastleKingSide = 2,
        BlackCanCastleQueenSide = 4,
        WhiteHasCastled = 8,
        WhiteCanCastleKingSide = 16,
        WhiteCanCastleQueenSide = 32,
        AllCastlingAvailable = BlackCanCastleKingSide | BlackCanCastleQueenSide | WhiteCanCastleKingSide | WhiteCanCastleQueenSide
    }

    public enum Sides
    {
        Black,
        White
    }

    public enum MakeMoveResults : byte
    {
        AllGood,
        SkipMove,
        UndoMove
    }

    public enum MoveGenerationResults : byte
    {
        NotMated,
        Mated
    }

    public enum GameStages : byte
    {
        Opening,
        EarlyMiddleGame,
        LateMiddleGame,
        EndGame
    }

    #endregion
}
