using System;
using System.Diagnostics;
using System.Linq;
using Omnifactotum.Validation;
using Omnifactotum.Validation.Constraints;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal sealed class NewGameWindowViewModel : ViewModelBase
    {
        #region Constants and Fields

        private string _fen;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="NewGameWindowViewModel"/> class.
        /// </summary>
        public NewGameWindowViewModel()
        {
            this.WhitePlayerViewModel = new PlayerChoiceControlViewModel(PieceColor.White);
            this.BlackPlayerViewModel = new PlayerChoiceControlViewModel(PieceColor.Black);
        }

        #endregion

        #region Public Properties

        [ValidatableMember]
        public PlayerChoiceControlViewModel WhitePlayerViewModel
        {
            get;
            private set;
        }

        [ValidatableMember]
        public PlayerChoiceControlViewModel BlackPlayerViewModel
        {
            get;
            private set;
        }

        [MemberConstraint(typeof(FenConstraint))]
        public string Fen
        {
            [DebuggerStepThrough]
            get
            {
                return _fen;
            }

            set
            {
                if (value == _fen)
                {
                    return;
                }

                _fen = value;
                RaisePropertyChanged(() => this.Fen);
            }
        }

        #endregion

        #region FenConstraint Class

        private sealed class FenConstraint : TypedMemberConstraintBase<string>
        {
            protected override void ValidateTypedValue(
                ObjectValidatorContext validatorContext,
                MemberConstraintValidationContext memberContext,
                string value)
            {
                if (!GameBoard.IsValidFen(value))
                {
                    AddError(
                        validatorContext,
                        memberContext,
                        "The specified FEN has invalid format or represents an invalid board position.");
                }
            }
        }

        #endregion
    }
}