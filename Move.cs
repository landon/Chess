using System;
using System.Collections.Generic;
using System.Text;

using BitBoard = System.Int64;

namespace Generator
{
    public struct Move
    {
        [Flags]
        public enum NotationModifiers
        {
            None = 0,
            Check = 1,
            Checkmate = 2,
            GoodMove = 4,
            ExcellentMove = 8,
            BadMove = 16,
            Blunder = 32,
            InterestingButMaybeNotBest = 64,
            DubiousMove = 128
        }

        static readonly Dictionary<NotationModifiers, string> NotationModifierMap;
        public static Move Empty = new Move();

        static Move()
        {
            NotationModifierMap = new Dictionary<NotationModifiers, string>()
            {
                {NotationModifiers.None, ""},
                {NotationModifiers.Check, "+"},
                {NotationModifiers.Checkmate, "#"},
                {NotationModifiers.GoodMove, "!"},
                {NotationModifiers.ExcellentMove, "!!"},
                {NotationModifiers.BadMove, "?"},
                {NotationModifiers.Blunder, "??"},
                {NotationModifiers.InterestingButMaybeNotBest, "!?"},
                {NotationModifiers.DubiousMove, "?!"}
            };
        }

        public Move(int fromSquare, int toSquare, byte movedPiece, byte capturedPiece = Pieces.None)
        {
            FromSquare = fromSquare;
            ToSquare = toSquare;
            MovedPiece = movedPiece;
            CapturedPiece = capturedPiece;
            Modifier = MoveModifiers.None;
            PromotionPiece = Pieces.None;
        }

        string SquareName(int square)
        {
            return FileName(square) + RankName(square);
        }

        string FileName(int square)
        {
            return Utility.EncodeASCII((byte)(104 - Board.File(square)));
        }

        string RankName(int square)
        {
            return (Board.Rank(square) + 1).ToString();
        }

        public string ToAlgebraic()
        {
            return ToAlgebraic(null, NotationModifiers.None);
        }

        public string ToAlgebraic(NotationModifiers notationModifier)
        {
            return ToAlgebraic(null, notationModifier);
        }

        public string ToAlgebraic(Board board)
        {
            return ToAlgebraic(board, NotationModifiers.None);
        }

        public string ToAlgebraic(Board board, NotationModifiers notationModifier)
        {
            string algebraic;
            if (Modifier == MoveModifiers.KingSideCastle)
            {
                algebraic = "O-O";
            }
            else if (Modifier == MoveModifiers.QueenSideCastle)
            {
                algebraic = "O-O-O";
            }
            else
            {
                string movedPieceName = MovedPieceName(board);

                algebraic = SquareName(ToSquare);
                if (CapturedPiece == Pieces.None)
                {
                    if (!Pieces.IsPawn(MovedPiece))
                    {
                        algebraic = movedPieceName + algebraic;
                    }
                }
                else
                {
                    algebraic = "x" + algebraic;
                    if (Pieces.IsPawn(MovedPiece))
                    {
                        algebraic = FileName(FromSquare) + algebraic;
                    }
                    else
                    {
                        algebraic = movedPieceName + algebraic;
                    }
                }

                if (Modifier == MoveModifiers.EnPassant)
                {
                    // This appended notation doesn't seem to be used by many people.
                    //algebraic += " e.p.";
                }

                if (PromotionPiece != Pieces.None)
                {
                    algebraic += "=" + Pieces.ToFEN(PromotionPiece).ToUpper();
                }
            }

            algebraic += NotationModifierMap[notationModifier];

            return algebraic;
        }

        string MovedPieceName(Board board)
        {
            string movedPieceName = Pieces.ToFEN(MovedPiece).ToUpper();

            // If we have no board, then we cannot disambiguate.
            if (board == null) return movedPieceName;

            // FIXME_lwr: Add disambiguation code.
            return movedPieceName;
        }

        public static Move FromAlgebraic(string algebraic)
        {
            return new Move();
        }

        public override bool Equals(object obj)
        {
            Move other = (Move)obj;

            return FromSquare == other.FromSquare &&
                   ToSquare == other.ToSquare &&
                   PromotionPiece == other.PromotionPiece &&
                   MovedPiece == other.MovedPiece &&
                   CapturedPiece == other.CapturedPiece &&
                   Modifier == other.Modifier;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool IsCapture
        {
            get { return CapturedPiece != Pieces.None; }
        }
        public bool IsPromotion
        {
            get { return PromotionPiece != Pieces.None; }
        }

        public int FromSquare;
        public int ToSquare;
        public byte MovedPiece;
        public byte CapturedPiece;
        public MoveModifiers Modifier;
        public byte PromotionPiece;
    }
}
