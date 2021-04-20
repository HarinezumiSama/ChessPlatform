using System.Diagnostics;
using Omnifactotum.Validation;
using Omnifactotum.Validation.Constraints;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal sealed class NewGameWindowViewModel : ViewModelBase
    {
        private string _fen;

        /// <summary>
        ///     Initializes a new instance of the <see cref="NewGameWindowViewModel"/> class.
        /// </summary>
        public NewGameWindowViewModel()
        {
            WhitePlayerViewModel = new PlayerChoiceControlViewModel(GameSide.White);
            BlackPlayerViewModel = new PlayerChoiceControlViewModel(GameSide.Black);
        }

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
            get => _fen;

            set
            {
                if (value == _fen)
                {
                    return;
                }

                _fen = value;
                RaisePropertyChanged(() => Fen);
                RaisePropertyChanged(() => IsFenValid);
                RaisePropertyChanged(() => IsFenDefault);
            }
        }

        public bool IsFenValid => GameBoard.IsValidFen(Fen);

        public bool IsFenDefault => Fen == ChessConstants.DefaultInitialFen;

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
                        "The specified FEN has invalid format or represents an invalid position.");
                }
            }
        }
    }
}