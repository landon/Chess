using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;

using BitBoard = System.Int64;

namespace Generator
{
    public class Board
    {
        public const string StartingBoard = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
      
        public static Board FromFEN(string FEN)
        {
            return new Board(FEN);
        }

        public Board()
        {
        }

        Board(string FEN)
        {
            SetBoard(FEN);
            InitializeBitBoards();
            InitializeHashes();
        }

        public void CopyFrom(Board board)
        {
            Buffer.BlockCopy(board.Occupant, 0, Occupant, 0, board.Occupant.Length);
            Buffer.BlockCopy(board.BitBoard, 0, BitBoard, 0, board.BitBoard.Length * 8);

            WhiteKingSquare = board.WhiteKingSquare;
            BlackKingSquare = board.BlackKingSquare;
            _EnPassantSquare = board.EnPassantSquare;
            CastleFlags = board.CastleFlags;
            HashKey = board.HashKey;
            PawnHashKey = board.PawnHashKey;
            Side = board.Side;

            UpdateGameStage();
        }

        #region Setup
        void SetBoard(string FEN)
        {
            string[] parts = Utility.Split(FEN, " ");

            if (parts.Length > 0)
            {
                for (int sq = 0; sq < 64; sq++)
                {
                    Occupant[sq] = Pieces.None;
                }

                string[] ranks = Utility.Split(parts[0], "/");

                int r = 7;
                foreach (string rank in ranks)
                {
                    int f = 7;
                    foreach (char c in rank)
                    {
                        int b;
                        if (int.TryParse(c.ToString(), out b))
                        {
                            f -= b;
                        }
                        else
                        {
                            Occupant[f + 8 * r] = Pieces.FromFEN(c);
                            f--;
                        }
                    }

                    r--;
                }
            }

            Side = Sides.White;
            if (parts.Length > 1 && parts[1] == "b")
            {
                Side = Sides.Black;
            }

            CastleFlags castleFlags = CastleFlags.AllCastlingAvailable;
            if (parts.Length > 2)
            {
                if (!parts[2].Contains("K"))
                {
                    castleFlags &= ~CastleFlags.WhiteCanCastleKingSide;
                }
                if (!parts[2].Contains("Q"))
                {
                    castleFlags &= ~CastleFlags.WhiteCanCastleQueenSide;
                }
                if (!parts[2].Contains("k"))
                {
                    castleFlags &= ~CastleFlags.BlackCanCastleKingSide;
                }
                if (!parts[2].Contains("q"))
                {
                    castleFlags &= ~CastleFlags.BlackCanCastleQueenSide;
                }
            }

            EnPassantSquare = Squares.None;
            if (parts.Length > 3)
            {
                if (parts[3].Length == 2)
                {
                    byte[] bytes = ASCIIEncoding.ASCII.GetBytes(parts[3].ToUpper()[0].ToString());
                    int rank = int.Parse(parts[3][1].ToString());

                    EnPassantSquare = 7 - (bytes[0] - 65) + (rank - 1) * 8;
                }
            }
        }
        void InitializeBitBoards()
        {           
            BitBoard[Pieces.WhiteKnight] = 0;
            BitBoard[PieceCategories.AllWhite] = 0;
            BitBoard[Pieces.BlackKnight] = 0;
            BitBoard[PieceCategories.AllBlack] = 0;
            BitBoard[Pieces.WhitePawn] = 0;
            BitBoard[Pieces.BlackPawn] = 0;
            BitBoard[Pieces.WhiteRook] = 0;
            BitBoard[Pieces.WhiteBishop] = 0;
            BitBoard[Pieces.BlackRook] = 0;
            BitBoard[Pieces.BlackBishop] = 0;
            BitBoard[Pieces.WhiteQueen] = 0;
            BitBoard[Pieces.BlackQueen] = 0;
            BitBoard[PieceCategories.All] = 0;
            BitBoard[PieceCategories.AllRotated45] = 0;
            BitBoard[PieceCategories.AllRotated90] = 0;
            BitBoard[PieceCategories.AllRotated135] = 0;

            for (int i = 0; i < 64; i++)
            {
                if (Occupant[i] != Pieces.None)
                {
                    BitBoard[PieceCategories.All] |= SquareMask[Normal[i]];
                    BitBoard[PieceCategories.AllRotated45] |= SquareMask[Rotated45[i]];
                    BitBoard[PieceCategories.AllRotated90] |= SquareMask[Rotated90[i]];
                    BitBoard[PieceCategories.AllRotated135] |= SquareMask[Rotated135[i]];

                    BitBoard[Occupant[i]] |= SquareMask[i];
                    if (Pieces.IsWhite(Occupant[i]))
                    {
                        if (Occupant[i] == Pieces.WhiteKing)
                        {
                            WhiteKingSquare = (byte)i;
                        }

                        BitBoard[PieceCategories.AllWhite] |= SquareMask[i];
                    }
                    else
                    {
                        if (Occupant[i] == Pieces.BlackKing)
                        {
                            BlackKingSquare = (byte)i;
                        }

                        BitBoard[PieceCategories.AllBlack] |= SquareMask[i];
                    }
                }
            }

            UpdateGameStage();
        }
        void InitializeHashes()
        {
            HashKey = TranspositionTable.HashBoard(this);
        }
        #endregion

        public byte this[int square]
        {
            get
            {
                return Occupant[square];
            }
            set
            {
                Occupant[square] = value;
            }
        }

        byte[] Occupant = new byte[64];
        public int WhiteKingSquare;
        public int BlackKingSquare;
        public int EnPassantSquare
        {
            get
            {
                return _EnPassantSquare;
            }
            set
            {
                _EnPassantSquare = value;
            }
        }
        public CastleFlags CastleFlags;
        public Sides Side;
        public GameStages GameStage;
        public int PawnCount;
        public int PieceCount;

        int _EnPassantSquare;

        #region Attacks
        public int AttackerCount(int square, Sides attackerSide)
        {
            if (attackerSide == Sides.White)
            {
                return BitOperations.PopulationCount(WhiteAttacks(square));
            }
            else
            {
                return BitOperations.PopulationCount(BlackAttacks(square));
            }
        }

        public int KingAttackCount(Sides side)
        {
            return AttackerCount(side == Sides.White ? WhiteKingSquare : BlackKingSquare, OtherSide(side));
        }

        public bool InCheck(Sides side)
        {
            return KingAttackCount(side) > 0;
        }

        #endregion

        #region BitBoards

        public BitBoard WhiteAttacks(int sq)
        {
            return (((AttacksRotated135[sq, (int)((BitBoard[PieceCategories.AllRotated135] >> Rotated135Shift[sq]) & Rotated135SquareMask[sq])] |
                AttacksRotated45[sq, (int)((BitBoard[PieceCategories.AllRotated45] >> Rotated45Shift[sq]) & Rotated45SquareMask[sq])]) &
                (BitBoard[Pieces.WhiteBishop] | BitBoard[Pieces.WhiteQueen])) | ((RookAttacksHorizontal[sq, (int)((BitBoard[PieceCategories.All] >> HorizontalShift[sq]) & 0xff)] |
                RookAttacksVertical[sq, (int)((BitBoard[PieceCategories.AllRotated90] >> VerticalShift[sq]) & 0xff)]) & (BitBoard[Pieces.WhiteRook] | BitBoard[Pieces.WhiteQueen])) |
                (KnightMoves[sq] & BitBoard[Pieces.WhiteKnight]) | (((((SquareMask[sq]) & ZeroLeft) >> 7) | (((SquareMask[sq]) & ZeroRight) >> 9)) &
                BitBoard[Pieces.WhitePawn]) | (KingMoves[sq] & (SquareMask[(int)WhiteKingSquare])));
        }

        public BitBoard BlackAttacks(int sq)
        {
            return (((AttacksRotated135[sq, (int)((BitBoard[PieceCategories.AllRotated135] >> Rotated135Shift[sq]) & Rotated135SquareMask[sq])] |
                AttacksRotated45[sq, (int)((BitBoard[PieceCategories.AllRotated45] >> Rotated45Shift[sq]) & Rotated45SquareMask[sq])]) &
                (BitBoard[Pieces.BlackBishop] | BitBoard[Pieces.BlackQueen])) | ((RookAttacksHorizontal[sq, (int)((BitBoard[PieceCategories.All] >> HorizontalShift[sq]) & 0xff)] |
                RookAttacksVertical[sq, (int)((BitBoard[PieceCategories.AllRotated90] >> VerticalShift[sq]) & 0xff)]) & (BitBoard[Pieces.BlackRook] | BitBoard[Pieces.BlackQueen])) |
                (KnightMoves[sq] & BitBoard[Pieces.BlackKnight]) | (((((SquareMask[sq]) & ZeroRight) << 7) | (((SquareMask[sq]) & ZeroLeft) << 9)) &
                BitBoard[Pieces.BlackPawn]) | (KingMoves[sq] & (SquareMask[(int)BlackKingSquare])));
        }

        public BitBoard WhiteAttacksKingRemoved(int sq)
        {
            return (((AttacksRotated135[sq, (int)(((BitBoard[PieceCategories.AllRotated135] ^ SquareMask[Rotated135[(int)BlackKingSquare]]) >> Rotated135Shift[sq]) & Rotated135SquareMask[sq])] |
                AttacksRotated45[sq, (int)(((BitBoard[PieceCategories.AllRotated45] ^ SquareMask[Rotated45[(int)BlackKingSquare]]) >> Rotated45Shift[sq]) & Rotated45SquareMask[sq])]) &
                (BitBoard[Pieces.WhiteBishop] | BitBoard[Pieces.WhiteQueen])) |
                ((RookAttacksHorizontal[sq, (int)((((BitBoard[PieceCategories.All]) ^ SquareMask[(int)BlackKingSquare]) >> HorizontalShift[sq]) & 0xff)] |
                RookAttacksVertical[sq, (int)(((BitBoard[PieceCategories.AllRotated90] ^ SquareMask[Rotated90[(int)BlackKingSquare]]) >> VerticalShift[sq]) & 0xff)]) &
                (BitBoard[Pieces.WhiteRook] | BitBoard[Pieces.WhiteQueen])) |
                (KnightMoves[sq] & BitBoard[Pieces.WhiteKnight]) | (((((SquareMask[sq]) & ZeroLeft) >> 7) | (((SquareMask[sq]) & ZeroRight) >> 9)) & BitBoard[Pieces.WhitePawn]) |
                (KingMoves[sq] & (SquareMask[(int)WhiteKingSquare])));
        }

        public BitBoard BlackAttacksKingRemoved(int sq)
        {
            return (((AttacksRotated135[sq, (int)(((BitBoard[PieceCategories.AllRotated135] ^ SquareMask[Rotated135[(int)WhiteKingSquare]]) >> Rotated135Shift[sq]) & Rotated135SquareMask[sq])] |
                 AttacksRotated45[sq, (int)(((BitBoard[PieceCategories.AllRotated45] ^ SquareMask[Rotated45[(int)WhiteKingSquare]]) >> Rotated45Shift[sq]) & Rotated45SquareMask[sq])]) &
                 (BitBoard[Pieces.BlackBishop] | BitBoard[Pieces.BlackQueen])) |
                 ((RookAttacksHorizontal[sq, (int)((((BitBoard[PieceCategories.All]) ^ SquareMask[(int)WhiteKingSquare]) >> HorizontalShift[sq]) & 0xff)] |
                 RookAttacksVertical[sq, (int)(((BitBoard[PieceCategories.AllRotated90] ^ SquareMask[Rotated90[(int)WhiteKingSquare]]) >> VerticalShift[sq]) & 0xff)]) &
                 (BitBoard[Pieces.BlackRook] | BitBoard[Pieces.BlackQueen])) |
                 (KnightMoves[sq] & BitBoard[Pieces.BlackKnight]) | (((((SquareMask[sq]) & ZeroRight) << 7) | (((SquareMask[sq]) & ZeroLeft) << 9)) & BitBoard[Pieces.BlackPawn]) |
                 (KingMoves[sq] & (SquareMask[(int)BlackKingSquare])));
        }

        public BitBoard WhiteNonKingAttacks(int sq)
        {
            return (((AttacksRotated135[sq, (int)((BitBoard[PieceCategories.AllRotated135] >> Rotated135Shift[sq]) & Rotated135SquareMask[sq])] |
                AttacksRotated45[sq, (int)((BitBoard[PieceCategories.AllRotated45] >> Rotated45Shift[sq]) & Rotated45SquareMask[sq])]) &
                (BitBoard[Pieces.WhiteBishop] | BitBoard[Pieces.WhiteQueen])) | ((RookAttacksHorizontal[sq, (int)(((BitBoard[PieceCategories.All]) >> HorizontalShift[sq]) & 0xff)] |
                RookAttacksVertical[sq, (int)((BitBoard[PieceCategories.AllRotated90] >> VerticalShift[sq]) & 0xff)]) & (BitBoard[Pieces.WhiteRook] | BitBoard[Pieces.WhiteQueen])) | (KnightMoves[sq] & BitBoard[Pieces.WhiteKnight]) |
                (((((SquareMask[sq]) & ZeroLeft) >> 7) | (((SquareMask[sq]) & ZeroRight) >> 9)) & BitBoard[Pieces.WhitePawn]));
        }

        public BitBoard BlackNonKingAttacks(int sq)
        {
            return (((AttacksRotated135[sq, (int)((BitBoard[PieceCategories.AllRotated135] >> Rotated135Shift[sq]) & Rotated135SquareMask[sq])] |
                AttacksRotated45[sq, (int)((BitBoard[PieceCategories.AllRotated45] >> Rotated45Shift[sq]) & Rotated45SquareMask[sq])]) &
                (BitBoard[Pieces.BlackBishop] | BitBoard[Pieces.BlackQueen])) | ((RookAttacksHorizontal[sq, (int)(((BitBoard[PieceCategories.All]) >> HorizontalShift[sq]) & 0xff)] |
                RookAttacksVertical[sq, (int)((BitBoard[PieceCategories.AllRotated90] >> VerticalShift[sq]) & 0xff)]) & (BitBoard[Pieces.BlackRook] | BitBoard[Pieces.BlackQueen])) | (KnightMoves[sq] & BitBoard[Pieces.BlackKnight]) |
                (((((SquareMask[sq]) & ZeroRight) << 7) | (((SquareMask[sq]) & ZeroLeft) << 9)) & BitBoard[Pieces.BlackPawn]));
        }

        public BitBoard WhiteNonKingOrPawnAttacks(int sq)
        {
            return (((AttacksRotated135[sq, (int)((BitBoard[PieceCategories.AllRotated135] >> Rotated135Shift[sq]) & Rotated135SquareMask[sq])] |
                AttacksRotated45[sq, (int)((BitBoard[PieceCategories.AllRotated45] >> Rotated45Shift[sq]) & Rotated45SquareMask[sq])]) & (BitBoard[Pieces.WhiteBishop] | BitBoard[Pieces.WhiteQueen])) |
                ((RookAttacksHorizontal[sq, (int)(((BitBoard[PieceCategories.All]) >> HorizontalShift[sq]) & 0xff)] |
                RookAttacksVertical[sq, (int)((BitBoard[PieceCategories.AllRotated90] >> VerticalShift[sq]) & 0xff)]) & (BitBoard[Pieces.WhiteRook] | BitBoard[Pieces.WhiteQueen])) | (KnightMoves[sq] & BitBoard[Pieces.WhiteKnight]));
        }

        public BitBoard BlackNonKingOrPawnAttacks(int sq)
        {
            return (((AttacksRotated135[sq, (int)((BitBoard[PieceCategories.AllRotated135] >> Rotated135Shift[sq]) & Rotated135SquareMask[sq])] |
                AttacksRotated45[sq, (int)((BitBoard[PieceCategories.AllRotated45] >> Rotated45Shift[sq]) & Rotated45SquareMask[sq])]) & (BitBoard[Pieces.BlackBishop] | BitBoard[Pieces.BlackQueen])) |
                ((RookAttacksHorizontal[sq, (int)(((BitBoard[PieceCategories.All]) >> HorizontalShift[sq]) & 0xff)] |
                RookAttacksVertical[sq, (int)((BitBoard[PieceCategories.AllRotated90] >> VerticalShift[sq]) & 0xff)]) & (BitBoard[Pieces.BlackRook] | BitBoard[Pieces.BlackQueen])) | (KnightMoves[sq] & BitBoard[Pieces.BlackKnight]));
        }

        public BitBoard[] BitBoard = new BitBoard[PieceCategories.MaxBitBoard];
        #endregion

        #region Hash Keys
        public UInt64 HashKey;
        public UInt64 PawnHashKey;
        #endregion

        #region Move Making
        void MovePiece(int from, int to, byte piece, byte promotionPiece)
        {
            Occupant[from] = Pieces.None;
            BitBoard[piece] &= Board.NotSquareMask[from];
            HashKey ^= TranspositionTable.SquarePieceHashModifier[from, piece];

            if (promotionPiece == Pieces.None)
            {
                Occupant[to] = piece;
                BitBoard[piece] |= Board.SquareMask[to];
                HashKey ^= TranspositionTable.SquarePieceHashModifier[to, piece];
            }
            else
            {
                Occupant[to] = promotionPiece;
                BitBoard[promotionPiece] |= Board.SquareMask[to];
                HashKey ^= TranspositionTable.SquarePieceHashModifier[to, promotionPiece];
            }

            BitBoard[PieceCategories.AllRotated90] &= Board.NotSquareMask[Board.Rotated90[from]];
            BitBoard[PieceCategories.AllRotated90] |= Board.SquareMask[Board.Rotated90[to]];
            BitBoard[PieceCategories.AllRotated135] &= Board.NotSquareMask[Board.Rotated135[from]];
            BitBoard[PieceCategories.AllRotated135] |= Board.SquareMask[Board.Rotated135[to]];
            BitBoard[PieceCategories.AllRotated45] &= Board.NotSquareMask[Board.Rotated45[from]];
            BitBoard[PieceCategories.AllRotated45] |= Board.SquareMask[Board.Rotated45[to]];

            BitBoard[Board.ColorCategory(piece)] ^= Board.SquareMask[from] | Board.SquareMask[to];
            BitBoard[Board.OtherColorCategory(piece)] &= Board.NotSquareMask[to];
        }

        public bool IsLegal(Move move)
        {
            if (move.Equals(Move.Empty))
                return false;

            if (!Squares.IsValid(move.FromSquare))
                return false;
            if (!Squares.IsValid(move.ToSquare))
                return false;

            if (move.MovedPiece == Pieces.None)
                return false;
            if (Pieces.GetSide(move.MovedPiece) != Side)
                return false;
            if (move.MovedPiece != this[move.FromSquare])
                return false;

            if (move.CapturedPiece != Pieces.None)
            {
                if (Pieces.GetSide(move.CapturedPiece) == Side)
                    return false;
                if (Pieces.IsKing(move.CapturedPiece))
                    return false;
                if (move.Modifier != MoveModifiers.EnPassant && move.CapturedPiece != this[move.ToSquare])
                    return false;
            }

            // TODO: Handle En Passant and Castling exeptions.

            return true;
        }

        public MakeMoveResults MakeMove(Move move)
        {
            int from = move.FromSquare;
            int to = move.ToSquare;
            byte piece = move.MovedPiece;
            byte captured = move.CapturedPiece;
            byte promotionPiece = move.PromotionPiece;

            HashKey ^= TranspositionTable.GetEPHash(this);
            HashKey ^= TranspositionTable.GetCastleHash(this);

            MovePiece(from, to, piece, promotionPiece);
            
            EnPassantSquare = Squares.None;

            if (Pieces.IsWhite(piece))
            {
                switch (piece)
                {
                    case Pieces.WhitePawn:
                        if (to < Squares.H8)
                        {
                            // Set the en passant square.
                            if (to - from == 16 && (Board.File(to) > 0 && Occupant[to - 1] == Pieces.BlackPawn ||
                                                    Board.File(to) < 7 && Occupant[to + 1] == Pieces.BlackPawn))
                            {
                                EnPassantSquare = from + 8;
                            }
                        }
                        break;
                    case Pieces.WhiteKnight:
                        break;
                    case Pieces.WhiteBishop:
                        break;
                    case Pieces.WhiteRook:
                        if (from == Squares.H1 && (CastleFlags & CastleFlags.WhiteCanCastleKingSide) != 0) //lost kingside castling rights
                        {
                            CastleFlags &= ~CastleFlags.WhiteCanCastleKingSide;
                        }
                        else if (from == Squares.A1 && (CastleFlags & CastleFlags.WhiteCanCastleQueenSide) != 0) //lost queenside castling rights
                        {
                            CastleFlags &= ~CastleFlags.WhiteCanCastleQueenSide;
                        }
                        break;
                    case Pieces.WhiteQueen:
                        break;
                    case Pieces.WhiteKing:
                        if (move.Modifier == MoveModifiers.KingSideCastle)
                        {
                            CastleFlags |= CastleFlags.WhiteHasCastled;

                            MovePiece(Squares.H1, Squares.F1, Pieces.WhiteRook, Pieces.None);
                        }
                        else if (move.Modifier == MoveModifiers.QueenSideCastle)
                        {
                            CastleFlags |= CastleFlags.WhiteHasCastled;

                            MovePiece(Squares.A1, Squares.D1, Pieces.WhiteRook, Pieces.None);
                        }

                        WhiteKingSquare = to;

                        CastleFlags &= ~(CastleFlags.WhiteCanCastleKingSide | CastleFlags.WhiteCanCastleQueenSide);
                        break;
                }

                if (captured != Pieces.None)
                {
                    if (move.Modifier != MoveModifiers.EnPassant)
                    {
                        BitBoard[captured] &= Board.NotSquareMask[to];
                        HashKey ^= TranspositionTable.SquarePieceHashModifier[to, captured];
                    }
                }

                switch (captured)
                {
                    case Pieces.None:
                        break;
                    case Pieces.BlackPawn:
                        if (move.Modifier == MoveModifiers.EnPassant)
                        {
                            int capSQ = to - 8;
                            Occupant[capSQ] = Pieces.None;
                            BitBoard[Pieces.BlackPawn] &= Board.NotSquareMask[capSQ];
                            BitBoard[PieceCategories.AllBlack] &= Board.NotSquareMask[capSQ];
                            BitBoard[PieceCategories.AllRotated90] &= Board.NotSquareMask[Board.Rotated90[capSQ]];
                            BitBoard[PieceCategories.AllRotated135] &= Board.NotSquareMask[Board.Rotated135[capSQ]];
                            BitBoard[PieceCategories.AllRotated45] &= Board.NotSquareMask[Board.Rotated45[capSQ]];

                            HashKey ^= TranspositionTable.SquarePieceHashModifier[capSQ, captured];
                        }
                        break;
                    case Pieces.BlackKnight:
                        break;
                    case Pieces.BlackBishop:
                        break;
                    case Pieces.BlackRook:
                        if (to == Squares.H8 && (CastleFlags & CastleFlags.BlackCanCastleKingSide) != 0) //lost kingside castling rights
                        {
                            CastleFlags &= ~CastleFlags.BlackCanCastleKingSide;
                        }
                        else if (to == Squares.A8 && (CastleFlags & CastleFlags.BlackCanCastleQueenSide) != 0) //lost queenside castling rights
                        {
                            CastleFlags &= ~CastleFlags.BlackCanCastleQueenSide;
                        }
                        break;
                    case Pieces.BlackQueen:
                        break;
                }
            }
            else
            {
                switch (piece)
                {
                    case Pieces.BlackPawn:
                        if (to > Squares.A1)
                        {
                            // Set the en passant square.
                            if (from - to == 16 && (Board.File(to) > 0 && Occupant[to - 1] == Pieces.WhitePawn ||
                                                    Board.File(to) < 7 && Occupant[to + 1] == Pieces.WhitePawn))
                            {
                                EnPassantSquare = from - 8;
                            }
                        }
                        break;
                    case Pieces.BlackKnight:
                        break;
                    case Pieces.BlackBishop:
                        break;
                    case Pieces.BlackRook:
                        if (from == Squares.H8 && (CastleFlags & CastleFlags.BlackCanCastleKingSide) != 0) //lost kingside castling rights
                        {
                            CastleFlags &= ~CastleFlags.BlackCanCastleKingSide;
                        }
                        else if (from == Squares.A8 && (CastleFlags & CastleFlags.BlackCanCastleQueenSide) != 0) //lost queenside castling rights
                        {
                            CastleFlags &= ~CastleFlags.BlackCanCastleQueenSide;
                        }
                        break;
                    case Pieces.BlackQueen:
                        break;
                    case Pieces.BlackKing:
                        if (move.Modifier == MoveModifiers.KingSideCastle)
                        {
                            CastleFlags |= CastleFlags.BlackHasCastled;

                            MovePiece(Squares.H8, Squares.F8, Pieces.BlackRook, Pieces.None);
                        }
                        else if (move.Modifier == MoveModifiers.QueenSideCastle)
                        {
                            CastleFlags |= CastleFlags.BlackHasCastled;

                            MovePiece(Squares.A8, Squares.D8, Pieces.BlackRook, Pieces.None);
                        }

                        BlackKingSquare = to;

                        CastleFlags &= ~(CastleFlags.BlackCanCastleKingSide | CastleFlags.BlackCanCastleQueenSide);
                        break;
                }

                if (captured != Pieces.None)
                {
                    if (move.Modifier != MoveModifiers.EnPassant)
                    {
                        BitBoard[captured] &= Board.NotSquareMask[to];
                        HashKey ^= TranspositionTable.SquarePieceHashModifier[to, captured];
                    }
                }

                switch (captured)
                {
                    case Pieces.None:
                        break;
                    case Pieces.WhitePawn:
                        if (move.Modifier == MoveModifiers.EnPassant)
                        {
                            int capSQ = to + 8;
                            Occupant[capSQ] = Pieces.None;
                            BitBoard[Pieces.WhitePawn] &= Board.NotSquareMask[capSQ];
                            BitBoard[PieceCategories.AllWhite] &= Board.NotSquareMask[capSQ];
                            BitBoard[PieceCategories.AllRotated90] &= Board.NotSquareMask[Board.Rotated90[capSQ]];
                            BitBoard[PieceCategories.AllRotated135] &= Board.NotSquareMask[Board.Rotated135[capSQ]];
                            BitBoard[PieceCategories.AllRotated45] &= Board.NotSquareMask[Board.Rotated45[capSQ]];

                            HashKey ^= TranspositionTable.SquarePieceHashModifier[capSQ, captured];
                        }
                        break;
                    case Pieces.WhiteKnight:
                        break;
                    case Pieces.WhiteBishop:
                        break;
                    case Pieces.WhiteRook:
                        if (to == Squares.H1 && (CastleFlags & CastleFlags.WhiteCanCastleKingSide) != 0) //lost kingside castling rights
                        {
                            CastleFlags &= ~CastleFlags.WhiteCanCastleKingSide;
                        }
                        else if (to == Squares.A1 && (CastleFlags & CastleFlags.WhiteCanCastleQueenSide) != 0) //lost queenside castling rights
                        {
                            CastleFlags &= ~CastleFlags.WhiteCanCastleQueenSide;
                        }
                        break;
                    case Pieces.WhiteQueen:
                        break;
                }
            }

            BitBoard[PieceCategories.All] = BitBoard[PieceCategories.AllWhite] | BitBoard[PieceCategories.AllBlack];

            HashKey ^= TranspositionTable.GetEPHash(this);
            HashKey ^= TranspositionTable.GetCastleHash(this);

            Side = OtherSide(Side);

            UpdateGameStage();

            if (InCheck(OtherSide(Side)))
                return MakeMoveResults.UndoMove;

            return MakeMoveResults.AllGood;
        }

        public void Check(byte piece)
        {
            var kind = Pieces.GetKind(piece);

            if (kind == Pieces.King)
            {
                int sq = Pieces.IsWhite(piece) ? WhiteKingSquare : BlackKingSquare;
            }
            else
            {
                var squares = BitBoard[piece];

                while (squares != 0)
                {
                    int sq = BitOperations.FindAndZeroLeastSignificantBit(ref squares);
                }
            }
        }


        void UpdateGameStage()
        {
            PawnCount = BitOperations.PopulationCount(BitBoard[Pieces.WhitePawn] | BitBoard[Pieces.BlackPawn]);
            PieceCount = BitOperations.PopulationCount(BitBoard[Pieces.WhiteKnight] |
                                                       BitBoard[Pieces.BlackKnight] |
                                                       BitBoard[Pieces.WhiteBishop] |
                                                       BitBoard[Pieces.BlackBishop] |
                                                       BitBoard[Pieces.WhiteRook] |
                                                       BitBoard[Pieces.BlackRook] |
                                                       BitBoard[Pieces.WhiteQueen] |
                                                       BitBoard[Pieces.BlackQueen]);

            if (PawnCount >= 14 && PieceCount >= 12)
                GameStage = GameStages.Opening;
            else if (PawnCount >= 8 && PieceCount >= 7)
                GameStage = GameStages.EarlyMiddleGame;
            else if (PawnCount >= 6 && PieceCount >= 5)
                GameStage = GameStages.LateMiddleGame;
            else
                GameStage = GameStages.EndGame;
        }

        #endregion

        #region Static Members

        static Board()
        {
            int i, k, l, q;

            BitBoard j = 1;
            ZeroRight = 1;
            ZeroLeft = j << 63;
            for (i = 8; i < 57; i += 8)
            {
                ZeroRight |= j << i;
                ZeroLeft |= j << (i - 1);
            }
            ZeroRight = ~ZeroRight;
            ZeroLeft = ~ZeroLeft;

            j = 1;
            //setup square bitSquareMasks and complement of square bitSquareMasks
            for (i = 0; i < 64; i++)
            {
                SquareMask[i] = j << i;
                NotSquareMask[i] = ~SquareMask[i];
                BlackPawnAttacks[i] = ((SquareMask[i] & ZeroRight) << 7) | ((SquareMask[i] & ZeroLeft) << 9);
                WhitePawnAttacks[i] = ((SquareMask[i] & ZeroLeft) >> 7) | ((SquareMask[i] & ZeroRight) >> 9);
            }

            //setup knightMoves boards
            int[] knightInc = { -17, 17, -15, 15, -10, 10, -6, 6 };

            for (i = 0; i < 64; i++)
            {
                j = 0;
                for (k = 0; k < 8; k++)
                    if (i + knightInc[k] >= 0 && i + knightInc[k] < 64)
                        if (Math.Abs((i + knightInc[k]) % 8 - i % 8) +
                            Math.Abs((i + knightInc[k]) / 8 - i / 8) == 3)
                        {
                            j |= SquareMask[i + knightInc[k]];
                        }
                KnightMoves[i] = j;
            }
            //setup kingMoves move bitBoards
            int[] kingInc = { -1, 1, -8, 8, 7, -7, 9, -9 };
            for (i = 0; i < 64; i++)
            {
                j = 0;
                for (k = 0; k < 8; k++)
                    if (i + kingInc[k] >= 0 && i + kingInc[k] < 64)
                        if (Math.Abs((i + kingInc[k]) % 8 - i % 8) +
                            Math.Abs((i + kingInc[k]) / 8 - i / 8) < 3)
                        {
                            j |= SquareMask[i + kingInc[k]];
                        }
                KingMoves[i] = j;
            }


            //setup horizShift and vertShift tables
            for (i = 0; i < 64; i++)
            {
                HorizontalShift[i] = (i >> 3) << 3;
                VerticalShift[i] = (i % 8) << 3;
            }
            //setup rookAttacksHoriz bitBoards
            for (i = 0; i < 64; i++)
                for (q = 0; q < 256; q++)
                {
                    RookAttacksHorizontal[i, q] = 0;
                }
            j = 1;
            for (i = 0; i < 64; i++)
            {
                for (q = 0; q < 256; q++)
                {
                    for (k = i + 1; k < i + 8 - i % 8 && ((1 << (k % 8)) & q) == 0; k++)
                    {
                        RookAttacksHorizontal[i, q] |= j << k;
                    }
                    if (k < i + 8 - i % 8)
                        RookAttacksHorizontal[i, q] |= j << k;
                    for (k = i - 1; k >= i - i % 8 && ((1 << (k % 8)) & q) == 0; k--)
                    {
                        RookAttacksHorizontal[i, q] |= j << k;
                    }
                    if (k >= i - i % 8)
                        RookAttacksHorizontal[i, q] |= j << k;
                }
            }
            //make vert rook/queen boards
            for (i = 0; i < 64; i++)
                for (q = 0; q < 256; q++)
                {
                    RookAttacksVertical[i, q] = 0;
                }
            j = 1;
            for (i = 0; i < 64; i++)
            {
                for (q = 0; q < 256; q++)
                {
                    for (k = i + 8; k < 64 && ((1 << (7 - k / 8)) & q) == 0; k += 8)
                    {
                        RookAttacksVertical[i, q] |= j << k;
                    }
                    if (k < 64)
                        RookAttacksVertical[i, q] |= j << k;
                    for (k = i - 8; k >= 0 && ((1 << (7 - k / 8)) & q) == 0; k -= 8)
                    {
                        RookAttacksVertical[i, q] |= j << k;
                    }
                    if (k >= 0)
                        RookAttacksVertical[i, q] |= j << k;
                }
            }

            //setup rot135 direction diagonal moves and attacks
            for (i = 0; i < 64; i++)
                for (q = 0; q < 256; q++)
                {
                    AttacksRotated135[i, q] = 0;
                }
            j = 1;
            for (i = 0; i < 64; i++)
            {
                for (q = 0; q < (1 << LengthRotated135[i]); q++)
                {
                    l = UpLeft[i];
                    for (k = i + 9; l < LengthRotated135[i] && ((1 << l) & q) == 0; k += 9)
                    {
                        AttacksRotated135[i, q] |= j << k;
                        l++;
                    }
                    if (l < LengthRotated135[i])
                        AttacksRotated135[i, q] |= j << k;
                    l = DownRight[i];
                    for (k = i - 9; l < LengthRotated135[i] && k >= 0 && ((1 << (LengthRotated135[i] - l - 1)) & q) == 0; k -= 9)
                    {
                        AttacksRotated135[i, q] |= j << k;
                        l++;
                    }
                    if (l < LengthRotated135[i] && k >= 0)
                        AttacksRotated135[i, q] |= j << k;
                }
            }

            //setup rot45 direction diagonal moves and attacks
            for (i = 0; i < 64; i++)
                for (q = 0; q < 256; q++)
                {
                    AttacksRotated45[i, q] = 0;
                }
            j = 1;
            for (i = 0; i < 64; i++)
            {
                for (q = 0; q < (1 << LengthRotated45[i]); q++)
                {
                    l = UpRight[i];
                    for (k = i + 7; l < LengthRotated45[i] && ((1 << l) & q) == 0; k += 7)
                    {
                        AttacksRotated45[i, q] |= j << k;
                        l++;
                    }
                    if (l < LengthRotated45[i])
                        AttacksRotated45[i, q] |= j << k;
                    l = DownLeft[i];
                    for (k = i - 7; l < LengthRotated45[i] && k >= 0 && ((1 << (LengthRotated45[i] - l - 1)) & q) == 0; k -= 7)
                    {
                        AttacksRotated45[i, q] |= j << k;
                        l++;
                    }
                    if (l < LengthRotated45[i] && k >= 0)
                        AttacksRotated45[i, q] |= j << k;
                }

            }

            for (i = 0; i < 64; i++)
                for (l = 0; l < 64; l++)
                {
                    Distance[i, l] = Math.Abs(File(i) - File(l)) > Math.Abs(Rank(i) - Rank(l)) ? Math.Abs(File(i) - File(l)) : Math.Abs(Rank(i) - Rank(l));
                }
            for (i = 0; i < 64; i++)
                for (l = 0; l < 64; l++)
                {
                    DistanceShort[i, l] = Math.Abs(File(i) - File(l)) < Math.Abs(Rank(i) - Rank(l)) ? Math.Abs(File(i) - File(l)) : Math.Abs(Rank(i) - Rank(l));
                }

            for (i = 0; i < 64; i++)
                for (l = 0; l < 64; l++)
                {
                    if (i == l)
                        DirectionIncrement[i, l] = 0;
                    else if (DistanceShort[i, l] == 0) //vertical or horizontal
                    {
                        if (Math.Abs(l - i) < 8) //horizontal
                        {
                            if (l > i) //left
                                DirectionIncrement[i, l] = 1;
                            else	//right
                                DirectionIncrement[i, l] = -1;
                        }
                        else //vertical
                        {
                            if (l > i) //up
                                DirectionIncrement[i, l] = 8;
                            else //down
                                DirectionIncrement[i, l] = -8;
                        }
                    }
                    else if (DistanceShort[i, l] == Distance[i, l]) //diagonal
                    {
                        if (File(l) > File(i))
                        {
                            if (Rank(l) > Rank(i))
                                DirectionIncrement[i, l] = 9;
                            else
                                DirectionIncrement[i, l] = -7;
                        }
                        else
                        {
                            if (Rank(l) > Rank(i))
                                DirectionIncrement[i, l] = 7;
                            else
                                DirectionIncrement[i, l] = -9;
                        }
                    }
                    else
                        DirectionIncrement[i, l] = 0;
                }

            //setup obstructed bitBoards
            j = 1;
            for (i = 0; i < 64; i++)
                for (q = 0; q < 64; q++)
                {
                    Obstructed[i, q] = 0;
                    if (i / 8 == q / 8)
                    {
                        if (i > q)
                        {
                            for (k = q + 1; k < i; k++)
                                Obstructed[i, q] |= (j << k);
                        }
                        else
                        {
                            for (k = i + 1; k < q; k++)
                                Obstructed[i, q] |= (j << k);
                        }
                    }
                    else if (i % 8 == q % 8)
                    {
                        if (i > q)
                        {
                            for (k = q + 8; k < i; k += 8)
                                Obstructed[i, q] |= (j << k);
                        }
                        else
                        {
                            for (k = i + 8; k < q; k += 8)
                                Obstructed[i, q] |= (j << k);
                        }
                    }
                    else if (DirectionIncrement[i, q] != 0)
                    {
                        if (i % 8 < q % 8 && i / 8 > q / 8)
                        {
                            for (k = q + 7; k < i; k += 7)
                                Obstructed[i, q] |= (j << k);
                        }
                        else if (i % 8 > q % 8 && i / 8 > q / 8)
                        {
                            for (k = q + 9; k < i; k += 9)
                                Obstructed[i, q] |= (j << k);
                        }
                        else if (i % 8 > q % 8 && i / 8 < q / 8)
                        {
                            for (k = q - 7; k > i; k -= 7)
                                Obstructed[i, q] |= (j << k);
                        }
                        else if (i % 8 < q % 8 && i / 8 < q / 8)
                        {
                            for (k = q - 9; k > i; k -= 9)
                                Obstructed[i, q] |= (j << k);
                        }
                    }
                }
            for (i = 0; i < 8; i++)
                CharMask[i] = (byte)(1 << i);

            for (i = 0; i < 8; i++)
                FileMask[i] = 0;

            j = 1;
            for (i = 0; i < 8; i++)
            {
                for (k = 0; k < 8; k++)
                    FileMask[k] |= j << (k + i * 8);
            }
            for (i = 0; i < 8; i++)
                RankMask[i] = 0;
            j = 1;
            for (i = 0; i < 8; i++)
            {
                for (k = 0; k < 8; k++)
                    RankMask[k] |= j << (k * 8 + i);
            }
            for (i = 0; i < 8; i++)
                for (l = 0; l < 8; l++)
                {
                    RankBlockMask[i, l] = 0;
                    if (i < l)
                    {
                        for (k = i; k <= l; k++)
                            RankBlockMask[i, l] |= RankMask[k];
                    }
                    else
                    {
                        for (k = l; k <= i; k++)
                            RankBlockMask[i, l] |= RankMask[k];
                    }
                }

            j = 1;
            for (i = 0; i < 64; i++)
            {
                ToEdgeMaskRight[i] = 0;
                for (l = i; File(l) > 0; l--)
                    ToEdgeMaskRight[i] |= j << l;
                ToEdgeMaskRight[i] |= j << l;

                ToEdgeMaskLeft[i] = 0;
                for (l = i; File(l) < 7; l++)
                    ToEdgeMaskLeft[i] |= j << l;
                ToEdgeMaskLeft[i] |= j << l;

                ToEdgeMaskUp[i] = 0;
                for (l = i; Rank(l) < 7; l += 8)
                    ToEdgeMaskUp[i] |= j << l;
                ToEdgeMaskUp[i] |= j << l;

                ToEdgeMaskDown[i] = 0;
                for (l = i; Rank(l) > 0; l -= 8)
                    ToEdgeMaskDown[i] |= j << l;
                ToEdgeMaskDown[i] |= j << l;

                ToEdgeMaskUpRight[i] = 0;
                for (l = i; File(l) > 0 && Rank(l) < 7; l += 7)
                    ToEdgeMaskUpRight[i] |= j << l;
                ToEdgeMaskUpRight[i] |= j << l;

                ToEdgeMaskDownRight[i] = 0;
                for (l = i; File(l) > 0 && Rank(l) > 0; l -= 9)
                    ToEdgeMaskDownRight[i] |= j << l;
                ToEdgeMaskDownRight[i] |= j << l;

                ToEdgeMaskUpLeft[i] = 0;
                for (l = i; File(l) < 7 && Rank(l) < 7; l += 9)
                    ToEdgeMaskUpLeft[i] |= j << l;
                ToEdgeMaskUpLeft[i] |= j << l;

                ToEdgeMaskDownLeft[i] = 0;
                for (l = i; File(l) < 7 && Rank(l) > 0; l -= 7)
                    ToEdgeMaskDownLeft[i] |= j << l;
                ToEdgeMaskDownLeft[i] |= j << l;
            }

            //setup darkSquares and lightSquares bitBoards
            for (i = 0; i < 64; i++)
            {
                if (((File(i) + Rank(i)) & 1) != 0)
                    BlackSquares |= SquareMask[i];
            }
            WhiteSquares = ~BlackSquares;

            //setup the upperHalfPlane and lowerHalfPlane
            LowerHalfPlane = RankMask[0] | RankMask[1] | RankMask[2] | RankMask[3];
            UpperHalfPlane = RankMask[4] | RankMask[5] | RankMask[6] | RankMask[7];
        }

        public static BitBoard[,] Obstructed = new BitBoard[64, 64];
        public static BitBoard[] SquareMask = new BitBoard[64];
        public static BitBoard[] NotSquareMask = new BitBoard[64];
        public static BitBoard[] KnightMoves = new BitBoard[64];
        public static BitBoard[] KingMoves = new BitBoard[64];
        public static BitBoard[] BlackPawnAttacks = new BitBoard[64];
        public static BitBoard[] WhitePawnAttacks = new BitBoard[64];

        public static BitBoard[,] RookAttacksHorizontal = new BitBoard[64, 256];
        public static BitBoard[,] RookAttacksVertical = new BitBoard[64, 256];
        public static BitBoard[,] AttacksRotated135 = new BitBoard[64, 256];
        public static BitBoard[,] AttacksRotated45 = new BitBoard[64, 256];

        public static BitBoard ZeroRight;
        public static BitBoard ZeroLeft;
        public static BitBoard WhiteSquares;
        public static BitBoard BlackSquares;
        public static BitBoard UpperHalfPlane;
        public static BitBoard LowerHalfPlane;

        public static BitBoard[] FileMask = new BitBoard[8];
        public static BitBoard[] RankMask = new BitBoard[8];

        // All bits from first element's rank to second elements rank are set.
        public static BitBoard[,] RankBlockMask = new BitBoard[8, 8];

        // All bits set from the input square to edge of board in given direction.
        public static BitBoard[] ToEdgeMaskRight = new BitBoard[64];
        public static BitBoard[] ToEdgeMaskLeft = new BitBoard[64];
        public static BitBoard[] ToEdgeMaskUp = new BitBoard[64];
        public static BitBoard[] ToEdgeMaskDown = new BitBoard[64];
        public static BitBoard[] ToEdgeMaskUpRight = new BitBoard[64];
        public static BitBoard[] ToEdgeMaskDownRight = new BitBoard[64];
        public static BitBoard[] ToEdgeMaskUpLeft = new BitBoard[64];
        public static BitBoard[] ToEdgeMaskDownLeft = new BitBoard[64];

        #region Setup Tables
        public static int[] HorizontalShift = new int[64];
        public static int[] VerticalShift = new int[64];
        public static int[,] Distance = new int[64, 64];
        public static int[,] DistanceShort = new int[64, 64];
        public static int[,] DirectionIncrement = new int[64, 64];
        public static byte[] CharMask = new byte[8];

        static int[] WhiteConvert = {
                               63, 62, 61, 60, 59, 58, 57, 56,
                               55, 54, 53, 52, 51, 50, 49, 48,
                               47, 46, 45, 44, 43, 42, 41, 40,
                               39, 38, 37, 36, 35, 34, 33, 32,
                               31, 30, 29, 28, 27, 26, 25, 24,
                               23, 22, 21, 20, 19, 18, 17, 16,
                               15, 14, 13, 12, 11, 10,  9,  8,
                                7,  6,  5,  4,  3,  2,  1,  0};
        static int[] BlackConvert = {
                                7,  6,  5,  4,  3,  2,  1,  0,
                               15, 14, 13, 12, 11, 10,  9,  8,
                               23, 22, 21, 20, 19, 18, 17, 16,
                               31, 30, 29, 28, 27, 26, 25, 24,
                               39, 38, 37, 36, 35, 34, 33, 32,
                               47, 46, 45, 44, 43, 42, 41, 40,
                               55, 54, 53, 52, 51, 50, 49, 48,
                               63, 62, 61, 60, 59, 58, 57, 56};
        public static int[] Normal = {0,1,2,3,4,5,6,7,
                        8,9,10,11,12,13,14,15,
                        16,17,18,19,20,21,22,23,
                        24,25,26,27,28,29,30,31,
                        32,33,34,35,36,37,38,39,
                        40,41,42,43,44,45,46,47,
                        48,49,50,51,52,53,54,55,
                        56,57,58,59,60,61,62,63};
        public static int[] Rotated45 = { 48,40,32,24,16,8,0,56,
                            41,33,25,17,9,1,57,49,
                            34,26,18,10,2,58,50,42, 
                            27,19,11,3,59,51,43,35,
                            20,12,4,60,52,44,36,28,
                            13,5,61,53,45,37,29,21,
                            6,62,54,46,38,30,22,14,
                            63,55,47,39,31,23,15,7};
        public static int[] Rotated90 = {   7,15,23,31,39,47,55,63,
                              6,14,22,30,38,46,54,62,
                              5,13,21,29,37,45,53,61,
                              4,12,20,28,36,44,52,60,
                              3,11,19,27,35,43,51,59,
                              2,10,18,26,34,42,50,58,
                              1,9,17,25,33,41,49,57,
                              0,8,16,24,32,40,48,56};
        public static int[] Rotated135 = {   0,56,48,40,32,24,16,8,
                              9,1,57,49,41,33,25,17,
                              18,10,2,58,50,42,34,26, 
                              27,19,11,3,59,51,43,35,
                              36,28,20,12,4,60,52,44,
                              45,37,29,21,13,5,61,53,
                              54,46,38,30,22,14,6,62,
                              63,55,47,39,31,23,15,7};
        public static int[] Rotated45Shift = {  48,40,32,24,16,8 ,0 ,56,
                                         40,32,24,16,8 ,0 ,56,49,
                                         32,24,16,8 ,0 ,56,49,42,
                                         24,16,8 ,0 ,56,49,42,35,
                                         16,8 ,0 ,56,49,42,35,28,
                                         8 ,0 ,56,49,42,35,28,21,
                                         0 ,56,49,42,35,28,21,14,
                                         56,49,42,35,28,21,14,7 };
        public static int[] Rotated135Shift = {  0 ,56,48,40,32,24,16,8,
                                          9 ,0 ,56,48,40,32,24,16,
                                          18,9 ,0 ,56,48,40,32,24,
                                          27,18,9 ,0 ,56,48,40,32,
                                          36,27,18,9 ,0 ,56,48,40,
                                          45,36,27,18,9 ,0 ,56,48,
                                          54,45,36,27,18,9 ,0 ,56,
                                          63,54,45,36,27,18,9 ,0 };
        public static BitBoard[] Rotated45SquareMask = {     1  ,3  ,7  ,15 ,31 ,63 ,127,255,
                                                 3  ,7  ,15 ,31 ,63 ,127,255,127,
                                                 7  ,15 ,31 ,63 ,127,255,127,63 ,
                                                 15 ,31 ,63 ,127,255,127,63 ,31 ,
                                                 31 ,63 ,127,255,127,63 ,31 ,15 ,
                                                 63 ,127,255,127,63 ,31 ,15 ,7  ,
                                                 127,255,127,63 ,31 ,15 ,7  ,3  ,
                                                 255,127,63 ,31 ,15 ,7  ,3  ,1  };
        public static BitBoard[] Rotated135SquareMask = {    255,127,63 ,31 ,15 ,7  ,3  ,1  ,
                                                 127,255,127,63 ,31 ,15 ,7  ,3  ,
                                                 63 ,127,255,127,63 ,31 ,15 ,7  ,
                                                 31 ,63 ,127,255,127,63 ,31 ,15 ,
                                                 15 ,31 ,63 ,127,255,127,63 ,31 ,
                                                 7  ,15 ,31 ,63 ,127,255,127,63 ,
                                                 3  ,7  ,15 ,31 ,63 ,127,255,127,
                                                 1  ,3  ,7  ,15 ,31 ,63 ,127,255};
        static int[] LengthRotated45 = {1,2,3,4,5,6,7,8,
                                 2,3,4,5,6,7,8,7,
                                 3,4,5,6,7,8,7,6,
                                 4,5,6,7,8,7,6,5,
                                 5,6,7,8,7,6,5,4,
                                 6,7,8,7,6,5,4,3,
                                 7,8,7,6,5,4,3,2,
                                 8,7,6,5,4,3,2,1};
        static int[] LengthRotated135 = {8,7,6,5,4,3,2,1,
                                  7,8,7,6,5,4,3,2,
                                  6,7,8,7,6,5,4,3,
                                  5,6,7,8,7,6,5,4,
                                  4,5,6,7,8,7,6,5,
                                  3,4,5,6,7,8,7,6,
                                  2,3,4,5,6,7,8,7,
                                  1,2,3,4,5,6,7,8};
        static int[] DownRight = {  8,7,6,5,4,3,2,1,
						     7,7,6,5,4,3,2,1,
						     6,6,6,5,4,3,2,1,
						     5,5,5,5,4,3,2,1,
						     4,4,4,4,4,3,2,1,
						     3,3,3,3,3,3,2,1,
						     2,2,2,2,2,2,2,1,
						     1,1,1,1,1,1,1,1};
        static int[] DownLeft = {1,2,3,4,5,6,7,8,
                          1,2,3,4,5,6,7,7,
                          1,2,3,4,5,6,6,6,
                          1,2,3,4,5,5,5,5,
                          1,2,3,4,4,4,4,4,
                          1,2,3,3,3,3,3,3,
                          1,2,2,2,2,2,2,2,
                          1,1,1,1,1,1,1,1};
        static int[] UpLeft = {   1,1,1,1,1,1,1,1,
                           1,2,2,2,2,2,2,2,
                           1,2,3,3,3,3,3,3,
                           1,2,3,4,4,4,4,4,
                           1,2,3,4,5,5,5,5,
                           1,2,3,4,5,6,6,6,
                           1,2,3,4,5,6,7,7,
                           1,2,3,4,5,6,7,8};
        static int[] UpRight = { 1,1,1,1,1,1,1,1,
                          2,2,2,2,2,2,2,1,
                          3,3,3,3,3,3,2,1,
                          4,4,4,4,4,3,2,1,
                          5,5,5,5,4,3,2,1,
                          6,6,6,5,4,3,2,1,
                          7,7,6,5,4,3,2,1,
                          8,7,6,5,4,3,2,1};
        #endregion

        public static byte ColorCategory(byte piece)
        {
            return Pieces.IsWhite(piece) ? PieceCategories.AllWhite : PieceCategories.AllBlack;
        }

        public static byte OtherColorCategory(byte piece)
        {
            return Pieces.IsWhite(piece) ? PieceCategories.AllBlack : PieceCategories.AllWhite;
        }

        public static byte Rank(int square)
        {
            return (byte)(square >> 3);
        }
        public static byte File(int square)
        {
            return (byte)(square & 7);
        }

        public static Sides OtherSide(Sides side)
        {
            return side == Sides.White ? Sides.Black : Sides.White;
        }

        #endregion
    }
}
