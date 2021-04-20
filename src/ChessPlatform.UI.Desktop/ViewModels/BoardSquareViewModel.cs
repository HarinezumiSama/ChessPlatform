using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal sealed class BoardSquareViewModel : ViewModelBase
    {
        private readonly GameWindowViewModel _parentViewModel;

        private Brush _background;
        private Piece _piece;
        private Brush _foreground;
        private string _text;
        private Brush _borderBrush;

        internal BoardSquareViewModel(GameWindowViewModel parentViewModel, Square square)
        {
            if (parentViewModel == null)
            {
                throw new ArgumentNullException(nameof(parentViewModel));
            }

            _parentViewModel = parentViewModel;
            Square = square;

            SubscribeToChangeOf(() => Piece, OnPieceChanged);

            _parentViewModel.SubscribeToChangeOf(() => _parentViewModel.SelectionMode, OnSelectionModeChanged);

            _parentViewModel.SubscribeToChangeOf(
                () => _parentViewModel.CurrentGameBoard,
                OnCurrentGameBoardChanged);

            _parentViewModel.SubscribeToChangeOf(
                () => _parentViewModel.CurrentTargetSquare,
                OnCurrentTargetSquareChanged);

            _background = UIHelper.GetSquareBrush(square, SquareMode.Default);

            UpdatePiece(true);
            UpdateBorderBrush(true);
        }

        public Square Square
        {
            get;
        }

        public Brush Background
        {
            [DebuggerStepThrough]
            get
            {
                return _background;
            }

            private set
            {
                if (Equals(_background, value))
                {
                    return;
                }

                _background = value;
                RaisePropertyChanged(() => Background);
            }
        }

        public Brush Foreground
        {
            [DebuggerStepThrough]
            get
            {
                return _foreground;
            }

            private set
            {
                if (Equals(value, _foreground))
                {
                    return;
                }

                _foreground = value;
                RaisePropertyChanged(() => Foreground);
            }
        }

        public string Text
        {
            [DebuggerStepThrough]
            get
            {
                return _text;
            }

            private set
            {
                if (value == _text)
                {
                    return;
                }

                _text = value;
                RaisePropertyChanged(() => Text);
            }
        }

        public Piece Piece
        {
            [DebuggerStepThrough]
            get
            {
                return _piece;
            }

            set
            {
                SetPieceInternal(value, false);
            }
        }

        public Brush BorderBrush
        {
            [DebuggerStepThrough]
            get
            {
                return _borderBrush;
            }

            private set
            {
                if (Equals(_borderBrush, value))
                {
                    return;
                }

                _borderBrush = value;
                RaisePropertyChanged(() => BorderBrush);
            }
        }

        private void SetPieceInternal(Piece value, bool forceRaiseEvent)
        {
            if (!forceRaiseEvent && value == _piece)
            {
                return;
            }

            _piece = value;
            RaisePropertyChanged(() => Piece);
        }

        private SquareMode GetSquareMode()
        {
            if (_parentViewModel.ValidMoveTargetSquares.Contains(Square))
            {
                return _parentViewModel.CurrentTargetSquare.HasValue
                    && Square == _parentViewModel.CurrentTargetSquare.Value
                    ? SquareMode.CurrentMoveTarget
                    : SquareMode.ValidMoveTarget;
            }

            if (_parentViewModel.ValidMoveTargetSquares.Count != 0)
            {
                switch (_parentViewModel.SelectionMode)
                {
                    case GameWindowSelectionMode.DisplayValidMovesOnly:
                        if (_parentViewModel.CurrentSourceSquare.HasValue
                            && Square == _parentViewModel.CurrentSourceSquare.Value)
                        {
                            return SquareMode.ValidMoveSource;
                        }

                        break;

                    case GameWindowSelectionMode.MovingPieceSelected:
                        if (_parentViewModel.CurrentSourceSquare.HasValue
                            && Square == _parentViewModel.CurrentSourceSquare.Value)
                        {
                            return SquareMode.CurrentMoveSource;
                        }

                        break;
                }
            }

            return SquareMode.Default;
        }

        private void UpdateBackground()
        {
            var squareMode = GetSquareMode();
            Background = UIHelper.GetSquareBrush(Square, squareMode);
        }

        private void UpdatePiece(bool forceRaiseEvent)
        {
            if (_parentViewModel.CurrentGameBoard == null)
            {
                return;
            }

            var piece = _parentViewModel.CurrentGameBoard[Square];
            SetPieceInternal(piece, forceRaiseEvent);
        }

        private void UpdateBorderBrush(bool forceRaiseEvent)
        {
            var currentGameBoard = _parentViewModel.CurrentGameBoard;

            var lastMove = currentGameBoard.Morph(obj => obj.PreviousMove);
            var isLastMoveSquare = lastMove != null
                && (lastMove.From == Square || lastMove.To == Square);

            var isUnderCheck = currentGameBoard != null && currentGameBoard.State.IsAnyCheck()
                && Piece == currentGameBoard.ActiveSide.ToPiece(PieceType.King);

            //// TODO [HarinezumiSama] Move the choice of a color to UIHelper

            Brush borderBrush;
            if (isUnderCheck)
            {
                borderBrush = Brushes.Red;
            }
            else if (isLastMoveSquare)
            {
                borderBrush = Brushes.RoyalBlue;
            }
            else
            {
                var squareMode = GetSquareMode();
                borderBrush = UIHelper.GetSquareBrush(Square, squareMode);
            }

            if (!forceRaiseEvent && Equals(borderBrush, BorderBrush))
            {
                return;
            }

            BorderBrush = borderBrush;
            RaisePropertyChanged(() => BorderBrush);
        }

        private void OnPieceChanged(object sender, EventArgs e)
        {
            Foreground = UIHelper.GetPieceBrush(_piece.GetSide());
            Text = UIHelper.PieceToSymbolMap[_piece.GetPieceType()];
        }

        private void OnSelectionModeChanged(object sender, EventArgs e)
        {
            UpdateBackground();
        }

        private void OnCurrentGameBoardChanged(object sender, EventArgs e)
        {
            UpdatePiece(false);
            UpdateBorderBrush(false);
        }

        private void OnCurrentTargetSquareChanged(object sender, EventArgs e)
        {
            UpdateBackground();
        }
    }
}