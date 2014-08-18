using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using BitBoard = System.Int64;

namespace Generator
{
    public static class MoveEnumerator
    {
        /// <summary>
        /// Enumerate all moves for the current board.  Note that
        /// this Enumerates illegal moves that leave the moving side's king in check.
        /// </summary>
        public static IEnumerable<Move> EnumerateAll(Board board)
        {
            if (board.Side == Sides.White)
            {
                if ((board.CastleFlags & CastleFlags.WhiteCanCastleKingSide) != 0 &&
                     board[Squares.F1] == Pieces.None && board[Squares.G1] == Pieces.None)
                {
                    if ((board.BlackAttacks(Squares.E1) | board.BlackAttacks(Squares.F1) | board.BlackAttacks(Squares.G1)) == 0)
                    {
                        Move move = new Move(Squares.E1, Squares.G1, Pieces.WhiteKing);
                        move.Modifier = MoveModifiers.KingSideCastle;

                        yield return move;
                    }
                }

                if ((board.CastleFlags & CastleFlags.WhiteCanCastleQueenSide) != 0 &&
                     board[Squares.B1] == Pieces.None && board[Squares.D1] == Pieces.None && board[Squares.C1] == Pieces.None)
                {
                    if ((board.BlackAttacks(Squares.E1) | board.BlackAttacks(Squares.D1) | board.BlackAttacks(Squares.C1)) == 0)
                    {
                        Move move = new Move(Squares.E1, Squares.C1, Pieces.WhiteKing);
                        move.Modifier = MoveModifiers.QueenSideCastle;

                        yield return move;
                    }
                }

                foreach (var m in EnumerateWhiteKnightCaptures(board).Union(
                                  EnumerateWhitePawnCaptures(board)).Union(
                                  EnumerateWhitePawnEnPassantCaptures(board)).Union(
                                  EnumerateWhiteRookQueenCaptures(board)).Union(
                                  EnumerateWhiteBishopQueenCaptures(board)).Union(
                                  EnumerateWhiteKingCaptures(board)).Union(
                                  EnumerateWhiteKnightNonCaptures(board)).Union(
                                  EnumerateWhiteKingNonCaptures(board)).Union(
                                  EnumerateWhitePawnNonCaptures(board)).Union(
                                  EnumerateWhiteRookQueenNonCaptures(board)).Union(
                                  EnumerateWhiteBishopQueenNonCaptures(board)))
                {
                    yield return m;
                }
            }
            else
            {
                if ((board.CastleFlags & CastleFlags.BlackCanCastleKingSide) != 0 &&
                 board[Squares.F8] == Pieces.None && board[Squares.G8] == Pieces.None)
                {
                    if ((board.WhiteAttacks(Squares.E8) | board.WhiteAttacks(Squares.F8) | board.WhiteAttacks(Squares.G8)) == 0)
                    {
                        Move move = new Move(Squares.E8, Squares.G8, Pieces.BlackKing);
                        move.Modifier = MoveModifiers.KingSideCastle;

                        yield return move;
                    }
                }

                if ((board.CastleFlags & CastleFlags.BlackCanCastleQueenSide) != 0 &&
                     board[Squares.B8] == Pieces.None && board[Squares.D8] == Pieces.None && board[Squares.C8] == Pieces.None)
                {
                    if ((board.WhiteAttacks(Squares.E8) | board.WhiteAttacks(Squares.D8) | board.WhiteAttacks(Squares.C8)) == 0)
                    {
                        Move move = new Move(Squares.E8, Squares.C8, Pieces.BlackKing);
                        move.Modifier = MoveModifiers.QueenSideCastle;

                        yield return move;
                    }
                }

                foreach (var m in EnumerateBlackKnightCaptures(board).Union(
                                  EnumerateBlackPawnCaptures(board)).Union(
                                  EnumerateBlackPawnEnPassantCaptures(board)).Union(
                                  EnumerateBlackRookQueenCaptures(board)).Union(
                                  EnumerateBlackBishopQueenCaptures(board)).Union(
                                  EnumerateBlackKingCaptures(board)).Union(
                                  EnumerateBlackKnightNonCaptures(board)).Union(
                                  EnumerateBlackKingNonCaptures(board)).Union(
                                  EnumerateBlackPawnNonCaptures(board)).Union(
                                  EnumerateBlackRookQueenNonCaptures(board)).Union(
                                  EnumerateBlackBishopQueenNonCaptures(board)))
                {
                    yield return m;
                }
            }
        }

        public static IEnumerable<Move> EnumerateCaptures(Board board)
        {
            if (board.Side == Sides.White)
            {
                foreach (var m in EnumerateWhiteKnightCaptures(board).Union(
                                  EnumerateWhitePawnCaptures(board)).Union(
                                  EnumerateWhitePawnEnPassantCaptures(board)).Union(
                                  EnumerateWhiteRookQueenCaptures(board)).Union(
                                  EnumerateWhiteBishopQueenCaptures(board)).Union(
                                  EnumerateWhiteKingCaptures(board)))
                {
                    yield return m;
                }
            }
            else
            {
                foreach (var m in EnumerateBlackKnightCaptures(board).Union(
                                  EnumerateBlackPawnCaptures(board)).Union(
                                  EnumerateBlackPawnEnPassantCaptures(board)).Union(
                                  EnumerateBlackRookQueenCaptures(board)).Union(
                                  EnumerateBlackBishopQueenCaptures(board)).Union(
                                  EnumerateBlackKingCaptures(board)))
                {
                    yield return m;
                }
            }
        }

        #region White Moves
        public static IEnumerable<Move> EnumerateWhiteKnightCaptures(Board board)
        {
            BitBoard fromMap = board.BitBoard[Pieces.WhiteKnight];
            while (fromMap != 0)
            {
                int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);

                BitBoard toMap = Board.KnightMoves[fromSquare] & board.BitBoard[PieceCategories.AllBlack];
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(fromSquare, toSquare, Pieces.WhiteKnight, board[toSquare]);
                    yield return move;
                }
            }
        }
        public static IEnumerable<Move> EnumerateWhitePawnCaptures(Board board)
        {
            //shift all pawns up one and to the left one
            BitBoard toMap = ((board.BitBoard[Pieces.WhitePawn] & Board.ZeroLeft) << 9) & board.BitBoard[PieceCategories.AllBlack];
            while (toMap != 0)
            {
                int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                if (toSquare >= Squares.H8) //gets promoted
                {
                    Move move = new Move(toSquare - 9, toSquare, Pieces.WhitePawn, board[toSquare]);
                    move.PromotionPiece = Pieces.WhiteQueen;
                    yield return move;
                    move = new Move(toSquare - 9, toSquare, Pieces.WhitePawn, board[toSquare]);
                    move.PromotionPiece = Pieces.WhiteRook;
                    yield return move;
                    move = new Move(toSquare - 9, toSquare, Pieces.WhitePawn, board[toSquare]);
                    move.PromotionPiece = Pieces.WhiteBishop;
                    yield return move;
                    move = new Move(toSquare - 9, toSquare, Pieces.WhitePawn, board[toSquare]);
                    move.PromotionPiece = Pieces.WhiteKnight;
                    yield return move;
                }
                else
                {
                    Move move = new Move(toSquare - 9, toSquare, Pieces.WhitePawn, board[toSquare]);
                    yield return move;
                }
            }
            toMap = ((board.BitBoard[Pieces.WhitePawn] & Board.ZeroRight) << 7) & board.BitBoard[PieceCategories.AllBlack];
            while (toMap != 0)
            {
                int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                if (toSquare >= Squares.H8) //gets promoted
                {
                    Move move = new Move(toSquare - 7, toSquare, Pieces.WhitePawn, board[toSquare]);
                    move.PromotionPiece = Pieces.WhiteQueen;
                    yield return move;
                    move = new Move(toSquare - 7, toSquare, Pieces.WhitePawn, board[toSquare]);
                    move.PromotionPiece = Pieces.WhiteRook;
                    yield return move;
                    move = new Move(toSquare - 7, toSquare, Pieces.WhitePawn, board[toSquare]);
                    move.PromotionPiece = Pieces.WhiteBishop;
                    yield return move;
                    move = new Move(toSquare - 7, toSquare, Pieces.WhitePawn, board[toSquare]);
                    move.PromotionPiece = Pieces.WhiteKnight;
                    yield return move;
                }
                else
                {
                    Move move = new Move(toSquare - 7, toSquare, Pieces.WhitePawn, board[toSquare]);
                    yield return move;
                }
            }
            //Enumerate promotions
            toMap = (board.BitBoard[Pieces.WhitePawn] << 8) & ~board.BitBoard[PieceCategories.All] & Board.RankMask[7];
            while (toMap != 0)
            {
                int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                Move move = new Move(toSquare - 8, toSquare, Pieces.WhitePawn, board[toSquare]);
                move.PromotionPiece = Pieces.WhiteQueen;
                yield return move;
                move = new Move(toSquare - 8, toSquare, Pieces.WhitePawn, board[toSquare]);
                move.PromotionPiece = Pieces.WhiteRook;
                yield return move;
                move = new Move(toSquare - 8, toSquare, Pieces.WhitePawn, board[toSquare]);
                move.PromotionPiece = Pieces.WhiteBishop;
                yield return move;
                move = new Move(toSquare - 8, toSquare, Pieces.WhitePawn, board[toSquare]);
                move.PromotionPiece = Pieces.WhiteKnight;
                yield return move;
            }
        }
        public static IEnumerable<Move> EnumerateWhiteRookQueenCaptures(Board board)
        {
            BitBoard fromMap = board.BitBoard[Pieces.WhiteRook] | board.BitBoard[Pieces.WhiteQueen];
            while (fromMap != 0)
            {
                int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);

                BitBoard toMap = (Board.RookAttacksHorizontal[fromSquare, (int)((board.BitBoard[PieceCategories.All] >> Board.HorizontalShift[fromSquare]) & 0xff)] |
                                  Board.RookAttacksVertical[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated90] >> Board.VerticalShift[fromSquare]) & 0xff)]) & board.BitBoard[PieceCategories.AllBlack];
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(fromSquare, toSquare, board[fromSquare], board[toSquare]);
                    yield return move;
                }
            }

            
        }
        public static IEnumerable<Move> EnumerateWhiteBishopQueenCaptures(Board board)
        {
            BitBoard fromMap = board.BitBoard[Pieces.WhiteBishop] | board.BitBoard[Pieces.WhiteQueen];
            while (fromMap != 0)
            {
                int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);


                BitBoard toMap = (Board.AttacksRotated135[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated135] >> Board.Rotated135Shift[fromSquare]) & Board.Rotated135SquareMask[fromSquare])] |
                                  Board.AttacksRotated45[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated45] >> Board.Rotated45Shift[fromSquare]) & Board.Rotated45SquareMask[fromSquare])]) & board.BitBoard[PieceCategories.AllBlack];
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(fromSquare, toSquare, board[fromSquare], board[toSquare]);
                    yield return move;
                }
            }
        }
        public static IEnumerable<Move> EnumerateWhiteKingCaptures(Board board)
        {
            int fromSquare = board.WhiteKingSquare;

            BitBoard toMap = Board.KingMoves[fromSquare] & board.BitBoard[PieceCategories.AllBlack];
            while (toMap != 0)
            {
                int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                Move move = new Move(fromSquare, toSquare, Pieces.WhiteKing, board[toSquare]);
                yield return move;
            }
        }
        public static IEnumerable<Move> EnumerateWhitePawnEnPassantCaptures(Board board)
        {
            if (board.EnPassantSquare != Squares.None)
            {
                if ((((board.BitBoard[Pieces.WhitePawn] & Board.ZeroLeft) << 9) & Board.SquareMask[board.EnPassantSquare]) != 0)
                {
                    Move move = new Move(board.EnPassantSquare - 9, board.EnPassantSquare, Pieces.WhitePawn, Pieces.BlackPawn);
                    move.Modifier = MoveModifiers.EnPassant;

                    yield return move;
                }
                if ((((board.BitBoard[Pieces.WhitePawn] & Board.ZeroRight) << 7) & Board.SquareMask[board.EnPassantSquare]) != 0)
                {
                    Move move = new Move(board.EnPassantSquare - 7, board.EnPassantSquare, Pieces.WhitePawn, Pieces.BlackPawn);
                    move.Modifier = MoveModifiers.EnPassant;

                    yield return move;
                }
            }
        }

        public static IEnumerable<Move> EnumerateWhiteKnightNonCaptures(Board board)
        {
            BitBoard fromMap = board.BitBoard[Pieces.WhiteKnight];
            while (fromMap != 0)
            {
                int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);

                BitBoard toMap = Board.KnightMoves[fromSquare] & ~board.BitBoard[PieceCategories.All];
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(fromSquare, toSquare, Pieces.WhiteKnight);
                    yield return move;
                }
            }
        }
        public static IEnumerable<Move> EnumerateWhiteKingNonCaptures(Board board)
        {
            int fromSquare = board.WhiteKingSquare;

            BitBoard toMap = Board.KingMoves[fromSquare] & ~board.BitBoard[PieceCategories.All];
            while (toMap != 0)
            {
                int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                Move move = new Move(fromSquare, toSquare, Pieces.WhiteKing);
                yield return move;
            }
        }
        public static IEnumerable<Move> EnumerateWhitePawnNonCaptures(Board board)
        {
            //shift all pawns up one and to the left one
            BitBoard toMap = (board.BitBoard[Pieces.WhitePawn] << 8) & ~board.BitBoard[PieceCategories.All] & ~Board.RankMask[7];
            while (toMap != 0)
            {
                int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);
                Move move = new Move(toSquare - 8, toSquare, Pieces.WhitePawn);
                yield return move;

                if (toSquare < 24 && board[toSquare + 8] == Pieces.None)
                {
                    move = new Move(toSquare - 8, toSquare + 8, Pieces.WhitePawn);
                    yield return move;
                }
            }
        }
        public static IEnumerable<Move> EnumerateWhiteRookQueenNonCaptures(Board board)
        {
            BitBoard fromMap = board.BitBoard[Pieces.WhiteRook] | board.BitBoard[Pieces.WhiteQueen];
            while (fromMap != 0)
            {
                int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);

                BitBoard toMap = (Board.RookAttacksHorizontal[fromSquare, (int)((board.BitBoard[PieceCategories.All] >> Board.HorizontalShift[fromSquare]) & 0xff)] |
                                  Board.RookAttacksVertical[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated90] >> Board.VerticalShift[fromSquare]) & 0xff)]) &
                                  ~board.BitBoard[PieceCategories.All];

                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(fromSquare, toSquare, board[fromSquare]);
                    yield return move;
                }
            }
        }
        public static IEnumerable<Move> EnumerateWhiteBishopQueenNonCaptures(Board board)
        {
            BitBoard fromMap = board.BitBoard[Pieces.WhiteBishop] | board.BitBoard[Pieces.WhiteQueen];
            while (fromMap != 0)
            {
                int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);


                BitBoard toMap = (Board.AttacksRotated135[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated135] >> Board.Rotated135Shift[fromSquare]) & Board.Rotated135SquareMask[fromSquare])] |
                                  Board.AttacksRotated45[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated45] >> Board.Rotated45Shift[fromSquare]) & Board.Rotated45SquareMask[fromSquare])]) &
                                  ~board.BitBoard[PieceCategories.All];
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(fromSquare, toSquare, board[fromSquare]);
                    yield return move;
                }
            }
        }
        #endregion

        #region Black Moves
        public static IEnumerable<Move> EnumerateBlackKnightCaptures(Board board)
        {
            BitBoard fromMap = board.BitBoard[Pieces.BlackKnight];
            while (fromMap != 0)
            {
                int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);

                BitBoard toMap = Board.KnightMoves[fromSquare] & board.BitBoard[PieceCategories.AllWhite];
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(fromSquare, toSquare, Pieces.BlackKnight, board[toSquare]);
                    yield return move;
                }
            }
        }
        public static IEnumerable<Move> EnumerateBlackPawnCaptures(Board board)
        {
            //shift all pawns up one and to the left one
            BitBoard toMap = ((board.BitBoard[Pieces.BlackPawn] & Board.ZeroLeft) >> 7) & board.BitBoard[PieceCategories.AllWhite];
            while (toMap != 0)
            {
                int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                if (toSquare <= Squares.A1) //gets promoted
                {
                    Move move = new Move(toSquare + 7, toSquare, Pieces.BlackPawn, board[toSquare]);
                    move.PromotionPiece = Pieces.BlackQueen;
                    yield return move;
                    move = new Move(toSquare + 7, toSquare, Pieces.BlackPawn, board[toSquare]);
                    move.PromotionPiece = Pieces.BlackRook;
                    yield return move;
                    move = new Move(toSquare + 7, toSquare, Pieces.BlackPawn, board[toSquare]);
                    move.PromotionPiece = Pieces.BlackBishop;
                    yield return move;
                    move = new Move(toSquare + 7, toSquare, Pieces.BlackPawn, board[toSquare]);
                    move.PromotionPiece = Pieces.BlackKnight;
                    yield return move;
                }
                else
                {
                    Move move = new Move(toSquare + 7, toSquare, Pieces.BlackPawn, board[toSquare]);
                    yield return move;
                }
            }
            toMap = ((board.BitBoard[Pieces.BlackPawn] & Board.ZeroRight) >> 9) & board.BitBoard[PieceCategories.AllWhite];
            while (toMap != 0)
            {
                int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                if (toSquare <= Squares.A1) //gets promoted
                {
                    Move move = new Move(toSquare + 9, toSquare, Pieces.BlackPawn, board[toSquare]);
                    move.PromotionPiece = Pieces.BlackQueen;
                    yield return move;
                    move = new Move(toSquare + 9, toSquare, Pieces.BlackPawn, board[toSquare]);
                    move.PromotionPiece = Pieces.BlackRook;
                    yield return move;
                    move = new Move(toSquare + 9, toSquare, Pieces.BlackPawn, board[toSquare]);
                    move.PromotionPiece = Pieces.BlackBishop;
                    yield return move;
                    move = new Move(toSquare + 9, toSquare, Pieces.BlackPawn, board[toSquare]);
                    move.PromotionPiece = Pieces.BlackKnight;
                    yield return move;
                }
                else
                {
                    Move move = new Move(toSquare + 9, toSquare, Pieces.BlackPawn, board[toSquare]);
                    yield return move;
                }
            }
            //Enumerate promotions
            toMap = (board.BitBoard[Pieces.BlackPawn] >> 8) & ~board.BitBoard[PieceCategories.All] & Board.RankMask[0];
            while (toMap != 0)
            {
                int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                Move move = new Move(toSquare + 8, toSquare, Pieces.BlackPawn, board[toSquare]);
                move.PromotionPiece = Pieces.BlackQueen;
                yield return move;
                move = new Move(toSquare + 8, toSquare, Pieces.BlackPawn, board[toSquare]);
                move.PromotionPiece = Pieces.BlackRook;
                yield return move;
                move = new Move(toSquare + 8, toSquare, Pieces.BlackPawn, board[toSquare]);
                move.PromotionPiece = Pieces.BlackBishop;
                yield return move;
                move = new Move(toSquare + 8, toSquare, Pieces.BlackPawn, board[toSquare]);
                move.PromotionPiece = Pieces.BlackKnight;
                yield return move;
            }
        }
        public static IEnumerable<Move> EnumerateBlackRookQueenCaptures(Board board)
        {
            BitBoard fromMap = board.BitBoard[Pieces.BlackRook] | board.BitBoard[Pieces.BlackQueen];
            while (fromMap != 0)
            {
                int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);

                BitBoard toMap = (Board.RookAttacksHorizontal[fromSquare, (int)((board.BitBoard[PieceCategories.All] >> Board.HorizontalShift[fromSquare]) & 0xff)] |
                                  Board.RookAttacksVertical[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated90] >> Board.VerticalShift[fromSquare]) & 0xff)]) & board.BitBoard[PieceCategories.AllWhite];
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(fromSquare, toSquare, board[fromSquare], board[toSquare]);
                    yield return move;
                }
            }
        }
        public static IEnumerable<Move> EnumerateBlackBishopQueenCaptures(Board board)
        {
            BitBoard fromMap = board.BitBoard[Pieces.BlackBishop] | board.BitBoard[Pieces.BlackQueen];
            while (fromMap != 0)
            {
                int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);


                BitBoard toMap = (Board.AttacksRotated135[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated135] >> Board.Rotated135Shift[fromSquare]) & Board.Rotated135SquareMask[fromSquare])] |
                                  Board.AttacksRotated45[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated45] >> Board.Rotated45Shift[fromSquare]) & Board.Rotated45SquareMask[fromSquare])]) & board.BitBoard[PieceCategories.AllWhite];
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(fromSquare, toSquare, board[fromSquare], board[toSquare]);
                    yield return move;
                }
            }
        }
        public static IEnumerable<Move> EnumerateBlackKingCaptures(Board board)
        {
            int fromSquare = board.BlackKingSquare;

            BitBoard toMap = Board.KingMoves[fromSquare] & board.BitBoard[PieceCategories.AllWhite];
            while (toMap != 0)
            {
                int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                Move move = new Move(fromSquare, toSquare, Pieces.BlackKing, board[toSquare]);
                yield return move;
            }
        }
        public static IEnumerable<Move> EnumerateBlackPawnEnPassantCaptures(Board board)
        {
            if (board.EnPassantSquare != Squares.None)
            {
                if ((((board.BitBoard[Pieces.BlackPawn] & Board.ZeroLeft) >> 7) & Board.SquareMask[board.EnPassantSquare]) != 0)
                {
                    Move move = new Move(board.EnPassantSquare + 7, board.EnPassantSquare, Pieces.BlackPawn, Pieces.WhitePawn);
                    move.Modifier = MoveModifiers.EnPassant;

                    yield return move;
                }
                if ((((board.BitBoard[Pieces.BlackPawn] & Board.ZeroRight) >> 9) & Board.SquareMask[board.EnPassantSquare]) != 0)
                {
                    Move move = new Move(board.EnPassantSquare + 9, board.EnPassantSquare, Pieces.BlackPawn, Pieces.WhitePawn);
                    move.Modifier = MoveModifiers.EnPassant;

                    yield return move;
                }
            }
        }

        public static IEnumerable<Move> EnumerateBlackKnightNonCaptures(Board board)
        {
            BitBoard fromMap = board.BitBoard[Pieces.BlackKnight];
            while (fromMap != 0)
            {
                int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);

                BitBoard toMap = Board.KnightMoves[fromSquare] & ~board.BitBoard[PieceCategories.All];
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(fromSquare, toSquare, Pieces.BlackKnight);
                    yield return move;
                }
            }
        }
        public static IEnumerable<Move> EnumerateBlackKingNonCaptures(Board board)
        {
            int fromSquare = board.BlackKingSquare;

            BitBoard toMap = Board.KingMoves[fromSquare] & ~board.BitBoard[PieceCategories.All];
            while (toMap != 0)
            {
                int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                Move move = new Move(fromSquare, toSquare, Pieces.BlackKing);
                yield return move;
            }
        }
        public static IEnumerable<Move> EnumerateBlackPawnNonCaptures(Board board)
        {
            //shift all pawns up one and to the left one
            BitBoard toMap = (board.BitBoard[Pieces.BlackPawn] >> 8) & ~board.BitBoard[PieceCategories.All] & ~Board.RankMask[0];
            while (toMap != 0)
            {
                int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);
                Move move = new Move(toSquare + 8, toSquare, Pieces.BlackPawn);
                yield return move;

                if (toSquare > 39 && board[toSquare - 8] == Pieces.None)
                {
                    move = new Move(toSquare + 8, toSquare - 8, Pieces.BlackPawn);
                    yield return move;
                }
            }
        }
        public static IEnumerable<Move> EnumerateBlackRookQueenNonCaptures(Board board)
        {
            BitBoard fromMap = board.BitBoard[Pieces.BlackRook] | board.BitBoard[Pieces.BlackQueen];
            while (fromMap != 0)
            {
                int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);

                BitBoard toMap = (Board.RookAttacksHorizontal[fromSquare, (int)((board.BitBoard[PieceCategories.All] >> Board.HorizontalShift[fromSquare]) & 0xff)] |
                                  Board.RookAttacksVertical[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated90] >> Board.VerticalShift[fromSquare]) & 0xff)]) &
                                  ~board.BitBoard[PieceCategories.All];
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(fromSquare, toSquare, board[fromSquare]);
                    yield return move;
                }
            }
        }
        public static IEnumerable<Move> EnumerateBlackBishopQueenNonCaptures(Board board)
        {
            BitBoard fromMap = board.BitBoard[Pieces.BlackBishop] | board.BitBoard[Pieces.BlackQueen];
            while (fromMap != 0)
            {
                int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);


                BitBoard toMap = (Board.AttacksRotated135[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated135] >> Board.Rotated135Shift[fromSquare]) & Board.Rotated135SquareMask[fromSquare])] |
                                  Board.AttacksRotated45[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated45] >> Board.Rotated45Shift[fromSquare]) & Board.Rotated45SquareMask[fromSquare])]) &
                                  ~board.BitBoard[PieceCategories.All];
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(fromSquare, toSquare, board[fromSquare]);
                    yield return move;
                }
            }
        }
        #endregion

        #region Check Escapes
        public static IEnumerable<Move> EnumerateCheckEscapes(Board board, int numberOfAttackers)
        {
            if (board.Side == Sides.White)
            {
                BitBoard possibleEscapeSquares = Board.KingMoves[board.WhiteKingSquare] & ~board.BitBoard[PieceCategories.AllWhite];

                // Find which of the escape squares are attacked.
                BitBoard attacked = 0;
                BitBoard toMap = possibleEscapeSquares;

                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    if (board.BlackAttacksKingRemoved(toSquare) != 0)
                    {
                        attacked |= Board.SquareMask[toSquare];
                    }
                }

                toMap = possibleEscapeSquares & ~attacked;
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(board.WhiteKingSquare, toSquare, Pieces.WhiteKing, board[toSquare]);
                    yield return move;
                }

                // If there was more than one attacker, 
                // then moving the king is our only option, so we are done.
                if (numberOfAttackers > 1)
                    yield break;

                // Otherwise we need to try capturing the piece or blocking it.
                int attackerSquare = BitOperations.LeastSignificantBit(board.BlackAttacks(board.WhiteKingSquare));

                // Get all our pieces(except king done already) that can attack this square
                BitBoard fromMap = board.WhiteNonKingAttacks(attackerSquare);

                // Add in en passant attacks if they exist.
                if (board.EnPassantSquare - 8 == attackerSquare)
                {
                    if (Board.File(board.EnPassantSquare) > 0 && board[board.EnPassantSquare - 9] == Pieces.WhitePawn)
                    {
                        fromMap |= Board.SquareMask[board.EnPassantSquare - 9];
                    }
                    if (Board.File(board.EnPassantSquare) < 7 && board[board.EnPassantSquare - 7] == Pieces.WhitePawn)
                    {
                        fromMap |= Board.SquareMask[board.EnPassantSquare - 7];
                    }
                }

                while (fromMap != 0)
                {
                    int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);

                    // We need to make sure this capture did not expose another check.
                    if (ExposedCheckWhite(board, fromSquare, attackerSquare))
                    {
                        continue;
                    }

                    // If the capture didn't expose a check then lets add it to the list.

                    // We need to do the case of pawn captures seperately 
                    // to deal with promotion and en passant issues.
                    if (board[fromSquare] == Pieces.WhitePawn)
                    {
                        // Was the move a promotion?
                        if (attackerSquare >= Squares.H8)
                        {
                            Move move = new Move(fromSquare, attackerSquare, Pieces.WhitePawn, board[attackerSquare]);
                            move.PromotionPiece = Pieces.WhiteQueen;
                            yield return move;
                            move = new Move(fromSquare, attackerSquare, Pieces.WhitePawn, board[attackerSquare]);
                            move.PromotionPiece = Pieces.WhiteRook;
                            yield return move;
                            move = new Move(fromSquare, attackerSquare, Pieces.WhitePawn, board[attackerSquare]);
                            move.PromotionPiece = Pieces.WhiteBishop;
                            yield return move;
                            move = new Move(fromSquare, attackerSquare, Pieces.WhitePawn, board[attackerSquare]);
                            move.PromotionPiece = Pieces.WhiteKnight;
                            yield return move;
                        }
                        else if (Board.Rank(attackerSquare) == Board.Rank(fromSquare)) // Was en passant capture.
                        {
                            // An en passant capture cannot expose check, for otherwise we were already in check
                            // on the previous move.
                            Move move = new Move(fromSquare, attackerSquare + 8, Pieces.WhitePawn, Pieces.BlackPawn);
                            move.Modifier = MoveModifiers.EnPassant;
                            yield return move;
                        }
                        else //just a normal pawn capture
                        {
                            Move move = new Move(fromSquare, attackerSquare, Pieces.WhitePawn, board[attackerSquare]);
                            yield return move;
                        }
                    }
                    else //a non-pawn capture
                    {
                        Move move = new Move(fromSquare, attackerSquare, board[fromSquare], board[attackerSquare]);
                        yield return move;
                    }
                }

                // If the attacker as a pawn or a knight we can't interpose a piece, so we are done.
                if (board[attackerSquare] == Pieces.BlackPawn || board[attackerSquare] == Pieces.BlackKnight)
                    

                // Otherwise it is a slider, so try interpositions.

                // Get possible block squares.
                toMap = Board.Obstructed[board.WhiteKingSquare, attackerSquare];

                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    fromMap = board.WhiteNonKingOrPawnAttacks(toSquare);

                    //need to add in pawn moves to this square.
                    fromMap |= (Board.SquareMask[toSquare] >> 8) & board.BitBoard[Pieces.WhitePawn];
                    fromMap |= (Board.SquareMask[toSquare] >> 16) & board.BitBoard[Pieces.WhitePawn] & Board.RankMask[1] & ((~board.BitBoard[PieceCategories.All]) >> 8);

                    while (fromMap != 0)
                    {
                        int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);

                        // We need to make sure this block did not expose another check.
                        if (ExposedCheckWhite(board, fromSquare, toSquare))
                        {
                            continue;
                        }

                        // We need to do the case of pawn captures seperately 
                        // to deal with promotion and en passant issues.
                        if (board[fromSquare] == Pieces.WhitePawn)
                        {
                            // Was the move a promotion?
                            if (attackerSquare >= Squares.H8)
                            {
                                Move move = new Move(fromSquare, toSquare, Pieces.WhitePawn);
                                move.PromotionPiece = Pieces.WhiteQueen;
                                yield return move;
                                move = new Move(fromSquare, toSquare, Pieces.WhitePawn);
                                move.PromotionPiece = Pieces.WhiteRook;
                                yield return move;
                                move = new Move(fromSquare, toSquare, Pieces.WhitePawn);
                                move.PromotionPiece = Pieces.WhiteBishop;
                                yield return move;
                                move = new Move(fromSquare, toSquare, Pieces.WhitePawn);
                                move.PromotionPiece = Pieces.WhiteKnight;
                                yield return move;
                            }
                            else //just a normal pawn block
                            {
                                Move move = new Move(fromSquare, toSquare, Pieces.WhitePawn);
                                yield return move;
                            }
                        }
                        else //a non-pawn block
                        {
                            Move move = new Move(fromSquare, toSquare, board[fromSquare]);
                            yield return move;
                        }
                    }
                }
            }
            else
            {
                BitBoard possibleEscapeSquares = Board.KingMoves[board.BlackKingSquare] & ~board.BitBoard[PieceCategories.AllBlack];

                // Find which of the escape squares are attacked.
                BitBoard attacked = 0;
                BitBoard toMap = possibleEscapeSquares;

                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    if (board.WhiteAttacksKingRemoved(toSquare) != 0)
                    {
                        attacked |= Board.SquareMask[toSquare];
                    }
                }

                toMap = possibleEscapeSquares & ~attacked;
                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    Move move = new Move(board.BlackKingSquare, toSquare, Pieces.BlackKing, board[toSquare]);
                    yield return move;
                }

                // If there was more than one attacker, 
                // then moving the king is our only option, so we are done.
                if (numberOfAttackers > 1)
                    yield break;

                // Otherwise we need to try capturing the piece or blocking it.
                int attackerSquare = BitOperations.LeastSignificantBit(board.WhiteAttacks(board.BlackKingSquare));

                // Get all our pieces(except king done already) that can attack this square
                BitBoard fromMap = board.BlackNonKingAttacks(attackerSquare);

                // Add in en passant attacks if they exist.
                if (board.EnPassantSquare + 8 == attackerSquare)
                {
                    if (Board.File(board.EnPassantSquare) > 0 && board[board.EnPassantSquare + 7] == Pieces.BlackPawn)
                    {
                        fromMap |= Board.SquareMask[board.EnPassantSquare + 7];
                    }
                    if (Board.File(board.EnPassantSquare) < 7 && board[board.EnPassantSquare + 9] == Pieces.BlackPawn)
                    {
                        fromMap |= Board.SquareMask[board.EnPassantSquare + 9];
                    }
                }

                while (fromMap != 0)
                {
                    int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);

                    // We need to make sure this capture did not expose another check.
                    if (ExposedCheckBlack(board, fromSquare, attackerSquare))
                    {
                        continue;
                    }

                    // If the capture didn't expose a check then lets add it to the list.

                    // We need to do the case of pawn captures seperately 
                    // to deal with promotion and en passant issues.
                    if (board[fromSquare] == Pieces.BlackPawn)
                    {
                        // Was the move a promotion?
                        if (attackerSquare >= Squares.H8)
                        {
                            Move move = new Move(fromSquare, attackerSquare, Pieces.BlackPawn, board[attackerSquare]);
                            move.PromotionPiece = Pieces.BlackQueen;
                            yield return move;
                            move = new Move(fromSquare, attackerSquare, Pieces.BlackPawn, board[attackerSquare]);
                            move.PromotionPiece = Pieces.BlackRook;
                            yield return move;
                            move = new Move(fromSquare, attackerSquare, Pieces.BlackPawn, board[attackerSquare]);
                            move.PromotionPiece = Pieces.BlackBishop;
                            yield return move;
                            move = new Move(fromSquare, attackerSquare, Pieces.BlackPawn, board[attackerSquare]);
                            move.PromotionPiece = Pieces.BlackKnight;
                            yield return move;
                        }
                        else if (Board.Rank(attackerSquare) == Board.Rank(fromSquare)) // Was en passant capture.
                        {
                            // An en passant capture cannot expose a check.
                            Move move = new Move(fromSquare, attackerSquare - 8, Pieces.BlackPawn, Pieces.WhitePawn);
                            move.Modifier = MoveModifiers.EnPassant;
                            yield return move;
                        }
                        else //just a normal pawn capture
                        {
                            Move move = new Move(fromSquare, attackerSquare, Pieces.BlackPawn, board[attackerSquare]);
                            yield return move;
                        }
                    }
                    else //a non-pawn capture
                    {
                        Move move = new Move(fromSquare, attackerSquare, board[fromSquare], board[attackerSquare]);
                        yield return move;
                    }
                }

                // If the attacker as a pawn or a knight we can't interpose a piece, so we are done.
                if (board[attackerSquare] == Pieces.WhitePawn || board[attackerSquare] == Pieces.WhiteKnight)
                    

                // Otherwise it is a slider, so try interpositions.

                // Get possible block squares.
                toMap = Board.Obstructed[board.BlackKingSquare, attackerSquare];

                while (toMap != 0)
                {
                    int toSquare = BitOperations.FindAndZeroLeastSignificantBit(ref toMap);

                    fromMap = board.BlackNonKingOrPawnAttacks(toSquare);

                    //need to add in pawn moves to this square.
                    fromMap |= (Board.SquareMask[toSquare] << 8) & board.BitBoard[Pieces.BlackPawn];
                    fromMap |= (Board.SquareMask[toSquare] << 16) & board.BitBoard[Pieces.BlackPawn] & Board.RankMask[6] & ((~board.BitBoard[PieceCategories.All]) << 8);

                    while (fromMap != 0)
                    {
                        int fromSquare = BitOperations.FindAndZeroLeastSignificantBit(ref fromMap);

                        // We need to make sure this block did not expose another check.
                        if (ExposedCheckBlack(board, fromSquare, toSquare))
                        {
                            continue;
                        }

                        // We need to do the case of pawn captures seperately 
                        // to deal with promotion and en passant issues.
                        if (board[fromSquare] == Pieces.BlackPawn)
                        {
                            // Was the move a promotion?
                            if (attackerSquare >= Squares.H8)
                            {
                                Move move = new Move(fromSquare, toSquare, Pieces.BlackPawn);
                                move.PromotionPiece = Pieces.BlackQueen;
                                yield return move;
                                move = new Move(fromSquare, toSquare, Pieces.BlackPawn);
                                move.PromotionPiece = Pieces.BlackRook;
                                yield return move;
                                move = new Move(fromSquare, toSquare, Pieces.BlackPawn);
                                move.PromotionPiece = Pieces.BlackBishop;
                                yield return move;
                                move = new Move(fromSquare, toSquare, Pieces.BlackPawn);
                                move.PromotionPiece = Pieces.BlackKnight;
                                yield return move;
                            }
                            else //just a normal pawn block
                            {
                                Move move = new Move(fromSquare, toSquare, Pieces.BlackPawn);
                                yield return move;
                            }
                        }
                        else //a non-pawn block
                        {
                            Move move = new Move(fromSquare, toSquare, board[fromSquare]);
                            yield return move;
                        }
                    }
                }
            }
        }

        static bool ExposedCheckWhite(Board board, int fromSquare, int toSquare)
        {
            int increment = Board.DirectionIncrement[fromSquare, board.WhiteKingSquare];

            // Are we on a file, rank or diagonal of the king?
            // If we moved in line with the king, then could not have exposed check.
            // If there were pieces between us and king, we could not have exposed check.

            if (increment == 0 ||
                Math.Abs(increment) == Math.Abs(Board.DirectionIncrement[toSquare, fromSquare]) ||
               (Board.Obstructed[fromSquare, board.WhiteKingSquare] & board.BitBoard[PieceCategories.All]) != 0)
            {
                return false;
            }

            // We moved off the line of the king we were on and there are no pieces between
            // us and the king, is there an enemy piece on the line?

            switch (increment)
            {
                case 1: //king was horizontally left, check to the right of fromSquare
                    if ((Board.ToEdgeMaskRight[fromSquare] &
                         Board.RookAttacksHorizontal[fromSquare, (int)((board.BitBoard[PieceCategories.All] >> Board.HorizontalShift[fromSquare]) & 0xff)] &
                         (board.BitBoard[Pieces.BlackRook] | board.BitBoard[Pieces.BlackQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case -1: //king was horizontally right, check to the left of fromSquare
                    if ((Board.ToEdgeMaskLeft[fromSquare] &
                                 Board.RookAttacksHorizontal[fromSquare,
                                 (int)((board.BitBoard[PieceCategories.All] >> Board.HorizontalShift[fromSquare]) & 0xff)] &
                                 (board.BitBoard[Pieces.BlackRook] | board.BitBoard[Pieces.BlackQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case 8:  //king was vertically up, check down from fromSquare
                    if ((Board.ToEdgeMaskDown[fromSquare] &
                                 Board.RookAttacksVertical[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated90] >> Board.VerticalShift[fromSquare]) & 0xff)] &
                                 (board.BitBoard[Pieces.BlackRook] | board.BitBoard[Pieces.BlackQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case -8: //king was vertically down, check up from fromSquare
                    if ((Board.ToEdgeMaskUp[fromSquare] &
                                 Board.RookAttacksVertical[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated90] >> Board.VerticalShift[fromSquare]) & 0xff)] &
                                 (board.BitBoard[Pieces.BlackRook] | board.BitBoard[Pieces.BlackQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case 9: //king was diagonal, up left, check down right
                    if ((Board.ToEdgeMaskDownRight[fromSquare] &
                                 Board.AttacksRotated135[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated135] >> Board.Rotated135Shift[fromSquare]) & Board.Rotated135SquareMask[fromSquare])] &
                                 (board.BitBoard[Pieces.BlackBishop] | board.BitBoard[Pieces.BlackQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case -9: //king was diagonal, down right, check up left
                    if ((Board.ToEdgeMaskUpLeft[fromSquare] &
                                 Board.AttacksRotated135[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated135] >> Board.Rotated135Shift[fromSquare]) & Board.Rotated135SquareMask[fromSquare])] &
                                 (board.BitBoard[Pieces.BlackBishop] | board.BitBoard[Pieces.BlackQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case 7: //king was diagonal, up right, check down left
                    if ((Board.ToEdgeMaskDownLeft[fromSquare] &
                                 Board.AttacksRotated45[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated45] >> Board.Rotated45Shift[fromSquare]) & Board.Rotated45SquareMask[fromSquare])] &
                                 (board.BitBoard[Pieces.BlackBishop] | board.BitBoard[Pieces.BlackQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case -7: //king was diagonal, down left, check up right
                    if ((Board.ToEdgeMaskUpRight[fromSquare] &
                                 Board.AttacksRotated45[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated45] >> Board.Rotated45Shift[fromSquare]) & Board.Rotated45SquareMask[fromSquare])] &
                                 (board.BitBoard[Pieces.BlackBishop] | board.BitBoard[Pieces.BlackQueen])) != 0)
                    {
                        return true;
                    }
                    break;

            }

            return false;
        }
        static bool ExposedCheckBlack(Board board, int fromSquare, int toSquare)
        {
            int increment = Board.DirectionIncrement[fromSquare, board.BlackKingSquare];

            // Are we on a file, rank or diagonal of the king?
            // If we moved in line with the king, then could not have exposed check.
            // If there were pieces between us and king, we could not have exposed check.

            if (increment == 0 ||
                Math.Abs(increment) == Math.Abs(Board.DirectionIncrement[toSquare, fromSquare]) ||
               (Board.Obstructed[fromSquare, board.BlackKingSquare] & board.BitBoard[PieceCategories.All]) != 0)
            {
                return false;
            }

            // We moved off the line of the king we were on and there are no pieces between
            // us and the king, is there an enemy piece on the line?

            switch (increment)
            {
                case 1: //king was horizontally left, check to the right of fromSquare
                    if ((Board.ToEdgeMaskRight[fromSquare] &
                         Board.RookAttacksHorizontal[fromSquare, (int)((board.BitBoard[PieceCategories.All] >> Board.HorizontalShift[fromSquare]) & 0xff)] &
                         (board.BitBoard[Pieces.WhiteRook] | board.BitBoard[Pieces.WhiteQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case -1: //king was horizontally right, check to the left of fromSquare
                    if ((Board.ToEdgeMaskLeft[fromSquare] &
                                 Board.RookAttacksHorizontal[fromSquare,
                                 (int)((board.BitBoard[PieceCategories.All] >> Board.HorizontalShift[fromSquare]) & 0xff)] &
                                 (board.BitBoard[Pieces.WhiteRook] | board.BitBoard[Pieces.WhiteQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case 8:  //king was vertically up, check down from fromSquare
                    if ((Board.ToEdgeMaskDown[fromSquare] &
                                 Board.RookAttacksVertical[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated90] >> Board.VerticalShift[fromSquare]) & 0xff)] &
                                 (board.BitBoard[Pieces.WhiteRook] | board.BitBoard[Pieces.WhiteQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case -8: //king was vertically down, check up from fromSquare
                    if ((Board.ToEdgeMaskUp[fromSquare] &
                                 Board.RookAttacksVertical[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated90] >> Board.VerticalShift[fromSquare]) & 0xff)] &
                                 (board.BitBoard[Pieces.WhiteRook] | board.BitBoard[Pieces.WhiteQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case 9: //king was diagonal, up left, check down right
                    if ((Board.ToEdgeMaskDownRight[fromSquare] &
                                 Board.AttacksRotated135[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated135] >> Board.Rotated135Shift[fromSquare]) & Board.Rotated135SquareMask[fromSquare])] &
                                 (board.BitBoard[Pieces.WhiteBishop] | board.BitBoard[Pieces.WhiteQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case -9: //king was diagonal, down right, check up left
                    if ((Board.ToEdgeMaskUpLeft[fromSquare] &
                                 Board.AttacksRotated135[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated135] >> Board.Rotated135Shift[fromSquare]) & Board.Rotated135SquareMask[fromSquare])] &
                                 (board.BitBoard[Pieces.WhiteBishop] | board.BitBoard[Pieces.WhiteQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case 7: //king was diagonal, up right, check down left
                    if ((Board.ToEdgeMaskDownLeft[fromSquare] &
                                 Board.AttacksRotated45[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated45] >> Board.Rotated45Shift[fromSquare]) & Board.Rotated45SquareMask[fromSquare])] &
                                 (board.BitBoard[Pieces.WhiteBishop] | board.BitBoard[Pieces.WhiteQueen])) != 0)
                    {
                        return true;
                    }
                    break;
                case -7: //king was diagonal, down left, check up right
                    if ((Board.ToEdgeMaskUpRight[fromSquare] &
                                 Board.AttacksRotated45[fromSquare, (int)((board.BitBoard[PieceCategories.AllRotated45] >> Board.Rotated45Shift[fromSquare]) & Board.Rotated45SquareMask[fromSquare])] &
                                 (board.BitBoard[Pieces.WhiteBishop] | board.BitBoard[Pieces.WhiteQueen])) != 0)
                    {
                        return true;
                    }
                    break;

            }

            return false;
        }
        #endregion
    }
}
