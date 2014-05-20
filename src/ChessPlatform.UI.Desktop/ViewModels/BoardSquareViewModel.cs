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
                if (ReferenceEquals(_background, value))
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
                if (ReferenceEquals(value, _foreground))
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
            var piece = _parentViewModel.CurrentGameBoard[this.Position];
            SetPieceInternal(piece, forceRaiseEvent);
        }

        private void OnPieceChanged(object sender, EventArgs eventArgs)
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
        }

        private void OnCurrentTargetPositionChanged(object sender, EventArgs e)
        {
            UpdateBackground();
        }

        #endregion
    }
}