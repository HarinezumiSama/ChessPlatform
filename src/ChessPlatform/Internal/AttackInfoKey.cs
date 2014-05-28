using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ChessPlatform.Internal
{
    internal struct AttackInfoKey : IEquatable<AttackInfoKey>
    {
        #region Constants and Fields

        private readonly Position _targetPosition;
        private readonly PieceColor _attackingColor;
        private readonly int _hashCode;

        #endregion

        #region Constructors

        internal AttackInfoKey(Position targetPosition, PieceColor attackingColor)
        {
            _targetPosition = targetPosition;
            _attackingColor = attackingColor;

            _hashCode = (byte)_attackingColor << 8 | _targetPosition.X88Value;
        }

        #endregion

        #region Public Properties

        public Position TargetPosition
        {
            [DebuggerStepThrough]
            get
            {
                return _targetPosition;
            }
        }

        public PieceColor AttackingColor
        {
            [DebuggerStepThrough]
            get
            {
                return _attackingColor;
            }
        }

        #endregion

        #region Public Methods

        public override bool Equals(object obj)
        {
            return obj is AttackInfoKey && Equals((AttackInfoKey)obj);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{{{0} <- {1}}}", _targetPosition, _attackingColor);
        }

        #endregion

        #region IEquatable<AttackInfoKey> Members

        public bool Equals(AttackInfoKey other)
        {
            return other._targetPosition == _targetPosition && other._attackingColor == _attackingColor;
        }

        #endregion
    }
}