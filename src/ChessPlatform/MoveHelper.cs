using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

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

        public MoveHelper(PieceData pieceData)
        {
            #region Argument Check

            if (pieceData == null)
            {
                throw new ArgumentNullException("pieceData");
            }

            #endregion

            this.PieceData = pieceData;
        }

        #endregion

        #region Public Properties

        public PieceData PieceData
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
                throw new ChessPlatformException("The previous move must be undone first.");
            }

            _movingPiece = this.PieceData.SetPiece(move.From, Piece.None);
            if (_movingPiece == Piece.None)
            {
                throw new ChessPlatformException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No piece at the source position '{0}'.",
                        move.From));
            }

            var movingPieceOffsets = this.PieceData.PieceOffsetMap[_movingPiece];
            movingPieceOffsets.Remove(move.From.X88Value);

            _capturedPiece = this.PieceData.SetPiece(move.To, _movingPiece);
            if (_capturedPiece.GetColor() == _movingPiece.GetColor())
            {
                throw new ChessPlatformException("Cannot capture a piece of the same color.");
            }

            if (_capturedPiece != Piece.None)
            {
                this.PieceData.PieceOffsetMap[_capturedPiece].Remove(move.To.X88Value);
            }

            movingPieceOffsets.Add(move.To.X88Value);

            _move = move;

            Trace.TraceInformation(
                "Executed {0}: {1}",
                MethodBase.GetCurrentMethod().GetQualifiedName(),
                this.PieceData.GetFenSnippet());
        }

        public void UndoMove()
        {
            if (_move == null)
            {
                throw new ChessPlatformException("No move has been made.");
            }

            this.PieceData.SetPiece(_move.From, _movingPiece);
            var movingPieceOffsets = this.PieceData.PieceOffsetMap[_movingPiece];
            movingPieceOffsets.Remove(_move.To.X88Value);
            movingPieceOffsets.Add(_move.From.X88Value);

            this.PieceData.SetPiece(_move.To, _capturedPiece);
            if (_capturedPiece != Piece.None)
            {
                this.PieceData.PieceOffsetMap[_capturedPiece].Add(_move.To.X88Value);
            }

            _movingPiece = Piece.None;
            _capturedPiece = Piece.None;
            _move = null;

            Trace.TraceInformation(
                "Executed {0}: {1}",
                MethodBase.GetCurrentMethod().GetQualifiedName(),
                this.PieceData.GetFenSnippet());
        }

        #endregion
    }
}