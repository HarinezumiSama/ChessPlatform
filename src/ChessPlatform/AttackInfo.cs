using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform
{
    internal struct AttackInfo
    {
        #region Constants and Fields

        private readonly ReadOnlyDictionary<PieceType, PieceAttackInfo> _attacks;

        #endregion

        #region Constructors

        internal AttackInfo(IDictionary<PieceType, PieceAttackInfo> attacks)
        {
            #region Argument Check

            if (attacks == null)
            {
                throw new ArgumentNullException("attacks");
            }

            #endregion

            _attacks = new Dictionary<PieceType, PieceAttackInfo>(attacks).AsReadOnly();
        }

        #endregion

        #region Public Properties

        public ReadOnlyDictionary<PieceType, PieceAttackInfo> Attacks
        {
            [DebuggerStepThrough]
            get
            {
                return _attacks;
            }
        }

        #endregion
    }
}