using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChessPlatform
{
    internal sealed class MoveHelper
    {
        #region Constants and Fields

        private PieceMove _move;
        private Piece _movingPiece;
        private Piece _capturedPiece;

        #endregion

        #region Constructors

        public MoveHelper(
            Piece[] originalPieces,
            ICollection<KeyValuePair<Piece, HashSet<byte>>> originalPieceOffsetMap)
        {
            #region Argument Check

            ChessHelper.ValidatePieces(originalPieces);

            if (originalPieceOffsetMap == null)
            {
                throw new ArgumentNullException("originalPieceOffsetMap");
            }

            #endregion

            this.Pieces = originalPieces.Copy();
            this.PieceOffsetMap = ChessHelper.CopyPieceOffsetMap(originalPieceOffsetMap);
        }

        #endregion

        #region Public Properties

        public Piece[] Pieces
        {
            get;
            private set;
        }

        public Dictionary<Piece, HashSet<byte>> PieceOffsetMap
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void MakeMove(PieceMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            if (_move != null)
            {
                throw new InvalidOperationException("The previous move must be undone first.");
            }

            _movingPiece = ChessHelper.SetPiece(this.Pieces, move.From, Piece.None);
            if (_movingPiece == Piece.None)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No piece at the source position '{0}'.",
                        move.From));
            }

            var movingPieceOffsets = this.PieceOffsetMap[_movingPiece];
            movingPieceOffsets.Remove(move.From.X88Value);

            _capturedPiece = ChessHelper.SetPiece(this.Pieces, move.To, _movingPiece);
            if (_capturedPiece.GetColor() == _movingPiece.GetColor())
            {
                throw new InvalidOperationException("Cannot capture a piece of the same color.");
            }

            if (_capturedPiece != Piece.None)
            {
                this.PieceOffsetMap[_capturedPiece].Remove(move.To.X88Value);
            }

            movingPieceOffsets.Add(move.To.X88Value);

            _move = move;
        }

        public void UndoMove()
        {
            if (_move == null)
            {
                throw new InvalidOperationException("No move has been made.");
            }

            ChessHelper.SetPiece(this.Pieces, _move.From, _movingPiece);
            var movingPieceOffsets = this.PieceOffsetMap[_movingPiece];
            movingPieceOffsets.Remove(_move.To.X88Value);
            movingPieceOffsets.Add(_move.From.X88Value);

            ChessHelper.SetPiece(this.Pieces, _move.To, _capturedPiece);
            if (_capturedPiece != Piece.None)
            {
                this.PieceOffsetMap[_capturedPiece].Add(_move.To.X88Value);
            }

            _movingPiece = Piece.None;
            _capturedPiece = Piece.None;
            _move = null;
        }

        #endregion
    }
}