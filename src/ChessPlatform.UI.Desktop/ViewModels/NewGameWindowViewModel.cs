using System;
using System.Diagnostics;
using ChessPlatform.Engine;
using ChessPlatform.Logging;
using Omnifactotum.Annotations;
using Omnifactotum.Validation;
using Omnifactotum.Validation.Constraints;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal sealed class NewGameWindowViewModel : ViewModelBase
    {
        private string _fen;

        public NewGameWindowViewModel([NotNull] ILogger logger, [NotNull] IOpeningBookProvider openingBookProvider)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (openingBookProvider is null)
            {
                throw new ArgumentNullException(nameof(openingBookProvider));
            }

            WhitePlayerViewModel = new PlayerChoiceControlViewModel(logger, openingBookProvider, GameSide.White);
            BlackPlayerViewModel = new PlayerChoiceControlViewModel(logger, openingBookProvider, GameSide.Black);
        }

        public NewGameWindowViewModel()
            : this(FakeLogger.Instance, FakeOpeningBookProvider.Instance)
        {
            // Nothing to do
        }

        [ValidatableMember]
        public PlayerChoiceControlViewModel WhitePlayerViewModel { get; }

        [ValidatableMember]
        public PlayerChoiceControlViewModel BlackPlayerViewModel { get; }

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