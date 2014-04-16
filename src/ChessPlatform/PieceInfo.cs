﻿using System;
using System.Linq;

namespace ChessPlatform
{
    public struct PieceInfo
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PieceInfo"/> class.
        /// </summary>
        public PieceInfo(Piece piece)
            : this()
        {
            #region Argument Check

            piece.EnsureDefined();

            #endregion

            this.Piece = piece;
            this.PieceType = piece.GetPieceType();
            this.Color = piece.GetColor();
        }

        #endregion

        #region Public Properties

        public Piece Piece
        {
            get;
            private set;
        }

        public PieceType PieceType
        {
            get;
            private set;
        }

        public PieceColor? Color
        {
            get;
            private set;
        }

        #endregion
    }
}