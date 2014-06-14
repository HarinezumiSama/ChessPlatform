using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal sealed class BoardSquareViewModel : ViewModelBase
    {
        #region Constants and Fields

        private readonly GameWindowViewModel _parentViewModel;

        private Brush _background;
        private Piece _piece;
        private Brush _foreground;
        private string _text;
        private Brush _borderBrush;

        #endregion

        #region Constructors

        internal BoardSquareViewModel(GameWindowViewModel parentViewModel, Position position)
        {
            #region Argument Check

            if (parentViewModel == null)
            {
                throw new ArgumentNullException("parentViewModel");
            }

            #endregion

            _parentViewModel = parentViewModel;
            this.Position = position;

            SubscribeToChangeOf(() => this.Piece, this.OnPieceChanged);

            _parentViewModel.SubscribeToChangeOf(() => _parentViewModel.SelectionMode, this.OnSelectionModeChanged);

            _parentViewModel.SubscribeToChangeOf(
                () => _parentViewModel.CurrentGameBoard,
                this.OnCurrentGameBoardChanged);

            _parentViewModel.SubscribeToChangeOf(
                () => _parentViewModel.CurrentTargetPosition,
                this.OnCurrentTargetPositionChanged);

            _background = UIHelper.GetSquareBrush(position, SquareMode.Default);

            UpdatePiece(true);
            UpdateBorderBrush(true);
        }

        #endregion

        #region Public Properties

        public Position Position
        {
            get;
            private set;
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
                RaisePropertyChanged(() => this.Background);
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
                RaisePropertyChanged(() => this.Foreground);
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
                RaisePropertyChanged(() => this.Text);
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
                RaisePropertyChanged(() => this.BorderBrush);
            }
        }

        #endregion

        #region Private Methods

        private void SetPieceInternal(Piece value, bool forceRaiseEvent)
        {
            if (!forceRaiseEvent && value == _piece)
            {
                return;
            }

            _piece = value;
            RaisePropertyChanged(() => this.Piece);
        }

        private SquareMode GetSquareMode()
        {
            if (_parentViewModel.ValidMoveTargetPositions.Contains(this.Position))
            {
                return _parentViewModel.CurrentTargetPosition.HasValue
                    && this.Position == _parentViewModel.CurrentTargetPosition.Value
                    ? SquareMode.CurrentMoveTarget
                    : SquareMode.ValidMoveTarget;
            }

            if (_parentViewModel.ValidMoveTargetPositions.Count != 0)
            {
                switch (_parentViewModel.SelectionMode)
                {
                    case GameWindowSelectionMode.DisplayValidMovesOnly:
                        if (_parentViewModel.CurrentSourcePosition.HasValue
                            && this.Position == _parentViewModel.CurrentSourcePosition.Value)
                        {
                            return SquareMode.ValidMoveSource;
                        }

                        break;

                    case GameWindowSelectionMode.MovingPieceSelected:
                        if (_parentViewModel.CurrentSourcePosition.HasValue
                            && this.Position == _parentViewModel.CurrentSourcePosition.Value)
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
            this.Background = UIHelper.GetSquareBrush(this.Position, squareMode);
        }

        private void UpdatePiece(bool forceRaiseEvent)
        {
            if (_parentViewModel.CurrentGameBoard == null)
            {
                return;
            }

            var piece = _parentViewModel.CurrentGameBoard[this.Position];
            SetPieceInternal(piece, forceRaiseEvent);
        }

        private void UpdateBorderBrush(bool forceRaiseEvent)
        {
            var currentGameBoard = _parentViewModel.CurrentGameBoard;

            var lastMove = currentGameBoard.Morph(obj => obj.PreviousMove);
            var isLastMoveSquare = lastMove != null
                && (lastMove.From == this.Position || lastMove.To == this.Position);

            var isUnderCheck = currentGameBoard != null && currentGameBoard.State.IsAnyCheck()
                && this.Piece == PieceType.King.ToPiece(currentGameBoard.ActiveColor);

            //// TODO [vmcl] Move the choice of a color to UIHelper

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
                borderBrush = UIHelper.GetSquareBrush(this.Position, squareMode);
            }

            if (!forceRaiseEvent && Equals(borderBrush, this.BorderBrush))
            {
                return;
            }

            this.BorderBrush = borderBrush;
            RaisePropertyChanged(() => this.BorderBrush);
        }

        private void OnPieceChanged(object sender, EventArgs e)
        {
            var pieceInfo = _piece.GetPieceInfo();
            var ch = UIHelper.PieceToCharMap[pieceInfo.PieceType];

            this.Foreground = pieceInfo.Color == PieceColor.White ? Brushes.DarkKhaki : Brushes.Black;
            this.Text = ch.ToString(CultureInfo.InvariantCulture);
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

        private void OnCurrentTargetPositionChanged(object sender, EventArgs e)
        {
            UpdateBackground();
        }

        #endregion
    }
}