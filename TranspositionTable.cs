using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;

namespace Generator
{
    public enum HashEntryTypes : byte
    {
        Junk,
        LowerBound,
        Exact,
        UpperBound
    }

    public class TranspositionTable
    {
        public TranspositionTable(int size)
        {
            int entrySize = Marshal.SizeOf(typeof(HashEntry));
            int count = Math.Max(2, size / entrySize);

            _GlobalTable = new HashEntry[count / 2];
            _LocalTable = new HashEntry[count / 2];
        }

        public void SetStale()
        {
            for(int i = 0; i < _GlobalTable.Length; i++)
            {
                _GlobalTable[i].IsStale = true;
            }
        }

        public void AddPosition(Board board, Move bestMove, int score, int searchDepth, int currentPly, Sides currentSide, HashEntryTypes type)
        {
            score = StaticEvaluator.RemovePlyDependence(score);

            UInt64 hashKey = currentSide == Sides.White ? board.HashKey : ~board.HashKey;

            UInt64 globalTableIndex = hashKey % (UInt64)_GlobalTable.Length;
            UInt64 localTableIndex = hashKey % (UInt64)_LocalTable.Length;

            HashEntry globalEntry = _GlobalTable[globalTableIndex];

            if (globalEntry.IsStale)
            {
                _GlobalTable[globalTableIndex] = new HashEntry(hashKey, bestMove, score, searchDepth, type);
            }
            else
            {
                bool isMoreValuable = searchDepth > globalEntry.SearchDepth ||
                                      searchDepth == globalEntry.SearchDepth && type == HashEntryTypes.Exact;

                if (globalEntry.HashKey == hashKey)
                {
                    bool improvesBound = searchDepth == globalEntry.SearchDepth &&
                        (type == HashEntryTypes.LowerBound && globalEntry.Type == HashEntryTypes.LowerBound && score > globalEntry.Score ||
                        type == HashEntryTypes.UpperBound && globalEntry.Type == HashEntryTypes.LowerBound && score < globalEntry.Score);

                    if (isMoreValuable || improvesBound)
                    {
                        _GlobalTable[globalTableIndex] = new HashEntry(hashKey, bestMove, score, searchDepth, type);
                    }
                }
                else
                {
                    if (isMoreValuable)
                    {
                        _GlobalTable[globalTableIndex] = new HashEntry(hashKey, bestMove, score, searchDepth, type);
                        _LocalTable[localTableIndex] = globalEntry;
                    }
                    else
                    {
                        _LocalTable[localTableIndex] = new HashEntry(hashKey, bestMove, score, searchDepth, type);
                    }
                }
            }
        }

        public HashEntryTypes LookupPosition(Board board, int desiredDepth, int currentPly, Sides currentSide, ref int alpha, ref int beta)
        {
            UInt64 hashKey = currentSide == Sides.White ? board.HashKey : ~board.HashKey;

            HashEntryTypes result = LookupPosition(_GlobalTable, hashKey, desiredDepth, currentPly, currentSide, ref alpha, ref beta);
            if (result == HashEntryTypes.Junk)
            {
                result = LookupPosition(_LocalTable, hashKey, desiredDepth, currentPly, currentSide, ref alpha, ref beta);
            }

            return result;
        }

        HashEntryTypes LookupPosition(HashEntry[] table, UInt64 hashKey, int desiredDepth, int currentPly, Sides currentSide, ref int alpha, ref int beta)
        {
            UInt64 tableIndex = hashKey % (UInt64)table.Length;
            if (table[tableIndex].HashKey == hashKey)
            {
                table[tableIndex].IsStale = false;

                int score = StaticEvaluator.AddPlyDependence(table[tableIndex].Score, currentPly);

                if (table[tableIndex].SearchDepth < desiredDepth) return HashEntryTypes.Junk;

                switch (table[tableIndex].Type)
                {
                    case HashEntryTypes.LowerBound:
                        if (score >= beta)
                        {
                            beta = score;
                            return HashEntryTypes.LowerBound;
                        }
                        break;
                    case HashEntryTypes.Exact:
                        alpha = score;
                        return HashEntryTypes.Exact;
                    case HashEntryTypes.UpperBound:
                        if (score <= alpha)
                        {
                            alpha = score;
                            return HashEntryTypes.UpperBound;
                        }
                        break;
                }
            }

            return HashEntryTypes.Junk;
        }

        public Move GetBestMove(Board board, Sides currentSide)
        {
            UInt64 hashKey = currentSide == Sides.White ? board.HashKey : ~board.HashKey;

            UInt64 globalTableIndex = hashKey % (UInt64)_GlobalTable.Length;
            UInt64 localTableIndex = hashKey % (UInt64)_LocalTable.Length;

            Move move = Move.Empty;
            if (_GlobalTable[globalTableIndex].HashKey == hashKey)
            {
                _GlobalTable[globalTableIndex].IsStale = false;

                move = _GlobalTable[globalTableIndex].Move;
            }

            if (_LocalTable[localTableIndex].HashKey == hashKey)
            {
                if (!_LocalTable[localTableIndex].Move.Equals(Move.Empty) &&
                    _LocalTable[localTableIndex].SearchDepth > _GlobalTable[globalTableIndex].SearchDepth)
                {
                    move = _LocalTable[localTableIndex].Move;
                }
            }

            return move;
        }

        #region Members
        HashEntry[] _GlobalTable;
        HashEntry[] _LocalTable;
        #endregion
        #region Nested Classes

        struct HashEntry
        {
            public HashEntry(UInt64 key, Move move, int score, int searchDepth, HashEntryTypes type)
            {
                HashKey = key;
                Move = move;
                Score = score;
                SearchDepth = searchDepth;
                Type = type;
                IsStale = false;
            }

            public UInt64 HashKey;
            public Move Move;
            public int Score;
            public int SearchDepth;
            public HashEntryTypes Type;
            public bool IsStale;
        }
        #endregion
        #region Static Data
        static TranspositionTable()
        {
            Random RNG = new Random();
            byte[] bytes = new byte[8];
            for (int sq = Squares.H1; sq <= Squares.A8; sq++)
            {
                SquarePieceHashModifier[sq, Pieces.None] = 0;

                for (int piece = Pieces.BlackPawn; piece <= Pieces.WhiteKing; piece++)
                {
                    RNG.NextBytes(bytes);

                    SquarePieceHashModifier[sq, piece] = BitConverter.ToUInt64(bytes, 0);
                }
            }

            for (int sq = Squares.H1; sq <= Squares.A8; sq++)
            {
                RNG.NextBytes(bytes);

                EPModifier[sq] = BitConverter.ToUInt64(bytes, 0);
            }
        }

        public static UInt64 HashBoard(Board board)
        {
            UInt64 hash = 0;
            for (int sq = Squares.H1; sq <= Squares.A8; sq++)
            {
                hash ^= SquarePieceHashModifier[sq, board[sq]];
            }

            hash ^= GetEPHash(board);
            hash ^= GetCastleHash(board);

            return hash;
        }

        public static UInt64 GetEPHash(Board board)
        {
            UInt64 hash = 0;

            if (board.EnPassantSquare != Squares.None)
            {
                hash ^= EPModifier[board.EnPassantSquare];
            }

            return hash;
        }

        public static UInt64 GetCastleHash(Board board)
        {
            UInt64 hash = 0;

            if (((int)board.CastleFlags & 2) != 0)
            {
                hash ^= CastleModifier[0];
            }
            if (((int)board.CastleFlags & 4) != 0)
            {
                hash ^= CastleModifier[1];
            }
            if (((int)board.CastleFlags & 16) != 0)
            {
                hash ^= CastleModifier[2];
            }
            if (((int)board.CastleFlags & 32) != 0)
            {
                hash ^= CastleModifier[3];
            }

            return hash;
        }

        public static readonly UInt64[,] SquarePieceHashModifier= new UInt64[64, 13];
        static readonly UInt64[] EPModifier = new UInt64[64];
        static readonly UInt64[] CastleModifier = new UInt64[4];
        #endregion
    }
}
