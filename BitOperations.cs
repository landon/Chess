using System;
using System.Collections.Generic;
using System.Text;

using BitBoard = System.Int64;

namespace Generator
{
    public static class BitOperations
    {
        const UInt64 DeBruijnMultiplier = 0x07EDD5E59A4E28C2;
        static int[] DeBruijnLookup = {
                                           63,  0, 58,  1, 59, 47, 53,  2,
                                           60, 39, 48, 27, 54, 33, 42,  3,
                                           61, 51, 37, 40, 49, 18, 28, 20,
                                           55, 30, 34, 11, 43, 14, 22,  4,
                                           62, 57, 46, 52, 38, 26, 32, 41,
                                           50, 36, 17, 19, 29, 10, 13, 21,
                                           56, 45, 25, 31, 35, 16,  9, 12,
                                           44, 24, 15,  8, 23,  7,  6,  5
                                       };

        public static int FindAndZeroLeastSignificantBit(ref BitBoard b)
        {
            int lsb = LeastSignificantBit(b);
            b &= Board.NotSquareMask[lsb];

            return lsb;
        }

        public static int LeastSignificantBit(BitBoard b)
        {
            return DeBruijnLookup[unchecked((UInt64)(b & -b) * DeBruijnMultiplier >> 58)];
        }
        
        public static int PopulationCount(BitBoard b)
        {
            int q = 0;
            while (b > 0)
            {
                q++;
                b &= b - 1;
            }
            return q;
        }

        public static void DrawBits(BitBoard n)
        {
            int i;
            for (i = 63; i >= 0; i--)
            {
                if ((Board.SquareMask[i] & n) != 0)
                {
                    Console.Write("1");
                }
                else
                {
                    Console.Write("0");
                }
                if ((i % 8) == 0)
                {
                    Console.WriteLine();
                }
            }
        }

        public static List<int> ListBits(BitBoard n)
        {
            List<int> bits = new List<int>();
            while (n != 0)
            {
                int bit = FindAndZeroLeastSignificantBit(ref n);
                bits.Add(bit);
            }

            return bits;
        }
    }
}
