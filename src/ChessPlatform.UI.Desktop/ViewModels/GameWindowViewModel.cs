using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Omnifactotum;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal sealed class GameWindowViewModel : ViewModelBase
    {
        #region Constants and Fields

        private readonly HashSet<Position> _validMoveTargetPositionsInternal;
        private readonly Stack<GameBoard> _previousGameBoards;
        private GameBoard _currentGameBoard;
        private GameWindowSelectionMode _selectionMode;
        private Position? _currentTargetPosition;

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameWindowViewModel"/> class.
        /// </summary>
        public GameWindowViewModel()
        {
            _validMoveTargetPositionsInternal = new HashSet<Position>();
            _previousGameBoards = new Stack<GameBoard>();
            _currentGameBoard = new GameBoard();

            _selectionMode = GameWindowSelectionMode.None;
            this.ValidMoveTargetPositions = _validMoveTargetPositionsInternal.AsReadOnly();

            this.SquareViewModels = ChessHelper.AllPositions
                .ToDictionary(Factotum.Identity, position => new BoardSquareViewModel(this, position))
                .AsReadOnly();
        }

        #endregion

        #region Public Properties

        public ReadOnlyDictionary<Position, BoardSquareViewModel> SquareViewModels
        {
            get;
            private set;
        }

        public GameWindowSelectionMode SelectionMode
        {
            [DebuggerStepThrough]
            get
            {
                return _selectionMode;
            }

            private set
            {
                if (value == _selectionMode)
                {
                    return;
                }

                _selectionMode = value;
                RaisePropertyChanged(() => this.SelectionMode);
            }
        }

        public GameBoard CurrentGameBoard
        {
            [DebuggerStepThrough]
            get
            {
                return _currentGameBoard;
            }

            private set
            {
                if (ReferenceEquals(value, _currentGameBoard))
                {
                    return;
                }

                _currentGameBoard = value;
                RaisePropertyChanged(() => this.CurrentGameBoard);
            }
        }

        public Position? CurrentTargetPosition
        {
            [DebuggerStepThrough]
            get
            {
                return _currentTargetPosition;
            }

            set
            {
                if (value == _currentTargetPosition)
                {
                    return;
                }

                _currentTargetPosition = value;
                RaisePropertyChanged(() => this.CurrentTargetPosition);
            }
        }

        #endregion

        #region Internal Properties

        internal ReadOnlySet<Position> ValidMoveTargetPositions
        {
            get;
            private set;
        }

        internal Position? CurrentSourcePosition
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void ResetSelectionMode()
        {
            _validMoveTargetPositionsInternal.Clear();
            this.CurrentSourcePosition = null;

            this.SelectionMode = GameWindowSelectionMode.None;
        }

        public void SetMovingPieceSelectionMode(Position currentSourcePosition)
        {
            SetModeInternal(currentSourcePosition, GameWindowSelectionMode.MovingPieceSelected);
        }

        public void SetValidMovesOnlySelectionMode(Position currentSourcePosition)
        {
            SetModeInternal(currentSourcePosition, GameWindowSelectionMode.DisplayValidMovesOnly);
        }

        public void StartNewGame()
        {
            var gameBoard = new GameBoard();

            _previousGameBoards.Clear();
            this.CurrentGameBoard = gameBoard;
        }

        public void StartNewGameFromFen(string fen)
        {
            #region Argument Check

            if (string.IsNullOrWhiteSpace(fen))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    "fen");
            }

            #endregion

            var gameBoard = new GameBoard(fen);

            _previousGameBoards.Clear();
            this.CurrentGameBoard = gameBoard;
        }

        public void MakeMove(PieceMove move)
        {
            #region Argument Check

            if (move == null)
            {
                throw new ArgumentNullException("move");
            }

            #endregion

            var newGameBoard = this.CurrentGameBoard.MakeMove(move).EnsureNotNull();

            ResetSelectionMode();

            _previousGameBoards.Push(this.CurrentGameBoard);
            this.CurrentGameBoard = newGameBoard;
        }

        public bool CanUndoLastMove()
        {
            return _previousGameBoards.Count != 0;
        }

        public void UndoLastMove()
        {
            if (_previousGameBoards.Count == 0)
            {
                throw new InvalidOperationException("No moves to undo.");
            }

            var gameBoard = _previousGameBoards.Pop();
            this.CurrentGameBoard = gameBoard;
        }

        #endregion

        #region Private Methods

        private void SetModeInternal(Position currentSourcePosition, GameWindowSelectionMode selectionMode)
        {
            var validMoves = this.CurrentGameBoard.GetValidMovesBySource(currentSourcePosition);
            if (validMoves.Length == 0)
            {
                return;
            }

            _validMoveTargetPositionsInternal.Clear();
            validMoves.DoForEach(move => _validMoveTargetPositionsInternal.Add(move.To));

            this.CurrentSourcePosition = currentSourcePosition;
            this.SelectionMode = selectionMode;
        }

        #endregion
    }
}