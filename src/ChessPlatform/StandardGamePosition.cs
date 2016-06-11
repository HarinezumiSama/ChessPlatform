using System;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform
{
    public sealed class StandardGamePosition : GamePosition
    {
        #region Constructors

        public StandardGamePosition()
        {
            throw new NotImplementedException();
        }

        private StandardGamePosition([NotNull] StandardGamePosition other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            throw new NotImplementedException();
        }

        #endregion

        #region Protected Properties

        protected override PiecePosition PiecePosition
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override long ZobristKey
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Public Methods

        public static bool TryCreate([NotNull] string fen, out StandardGamePosition gamePosition)
        {
            if (fen == null)
            {
                throw new ArgumentNullException(nameof(fen));
            }

            throw new NotImplementedException();
        }

        public static StandardGamePosition Create([NotNull] string fen)
        {
            StandardGamePosition gamePosition;
            if (!TryCreate(fen, out gamePosition) || gamePosition == null)
            {
                throw new ArgumentException($@"Invalid FEN for standard chess: '{fen}'.", nameof(fen));
            }

            return gamePosition;
        }

        public override GamePosition Copy()
        {
            return new StandardGamePosition(this);
        }

        public override bool IsSamePosition(GamePosition other)
        {
            //// ReSharper disable once ConstantConditionalAccessQualifier
            if (other?.GetType() != GetType())
            {
                return false;
            }

            throw new NotImplementedException();
        }

        public override GamePosition MakeMove(GameMove move)
        {
            if (move == null)
            {
                throw new ArgumentNullException(nameof(move));
            }

            throw new NotImplementedException();
        }

        #endregion
    }
}