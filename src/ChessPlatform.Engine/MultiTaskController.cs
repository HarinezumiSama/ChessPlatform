using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    internal sealed class MultiTaskController<TResult>
    {
        private const string ThisTypeName = nameof(MultiTaskController<TResult>);

        private readonly object _syncLock;
        private readonly GameControlInfo _gameControlInfo;
        private readonly TaskSlot[] _taskSlots;
        private readonly Thread[] _threads;
        private readonly List<Exception> _exceptions;
        private InternalState _state;
        private int _taskIndex;

        public MultiTaskController(
            [NotNull] GameControlInfo gameControlInfo,
            int threadCount,
            [NotNull] ICollection<Func<TResult>> tasks)
        {
            if (gameControlInfo is null)
            {
                throw new ArgumentNullException(nameof(gameControlInfo));
            }

            if (threadCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(threadCount),
                    threadCount,
                    @"The value must be positive.");
            }

            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            if (tasks.Any(item => item is null))
            {
                throw new ArgumentException(@"The collection contains a null element.", nameof(tasks));
            }

            _syncLock = new object();
            _gameControlInfo = gameControlInfo;
            _taskSlots = tasks.Select(obj => new TaskSlot(obj)).ToArray();
            _threads = Enumerable.Range(1, threadCount).Select(index => CreateThread(index, threadCount)).ToArray();
            _exceptions = new List<Exception>();

            _state = InternalState.Ready;
            _taskIndex = 0;
        }

        public TResult[] GetResults()
        {
            const InternalState ValidState = InternalState.Ready;

            lock (_syncLock)
            {
                if (_state != ValidState)
                {
                    throw new InvalidOperationException(
                        $@"{ThisTypeName} cannot be started since it is not in the {ValidState
                            } state. Current state: {_state}.");
                }

                _exceptions.Clear();
                _state = InternalState.Running;
                _threads.DoForEach(thread => thread.Start());
            }

            var waiting = true;
            while (waiting)
            {
                var errorFlag = false;
                lock (_syncLock)
                {
                    if (_exceptions.Count != 0)
                    {
                        _state = InternalState.Finished;
                        errorFlag = true;
                    }
                }

                if (errorFlag)
                {
                    var allJoined = _threads.All(thread => thread.Join(100));
                    if (!allJoined)
                    {
                        _threads.DoForEach(
                            thread =>
                            {
                                if (thread.ThreadState == ThreadState.Running)
                                {
                                    thread.Abort();
                                }
                            });
                    }

                    throw new AggregateException(_exceptions);
                }

                waiting = _threads.Any(thread => !thread.Join(0));
                Thread.Sleep(1);
            }

            lock (_syncLock)
            {
                _state = InternalState.Finished;

                if (_exceptions.Count != 0)
                {
                    throw new AggregateException(_exceptions);
                }
            }

            if (_taskSlots.Any(slot => !slot.IsCompleted))
            {
                throw new InvalidOperationException("Internal error: non-completed slot after successful execution.");
            }

            var result = _taskSlots.Select(slot => slot.Result).ToArray();
            return result;
        }

        private Thread CreateThread(int index, int threadCount)
        {
            var thread = new Thread(ExecuteThreadWork)
            {
                Name = $@"{ThisTypeName}:WorkerThread #{index}/{threadCount}",
                CurrentCulture = CultureInfo.InvariantCulture,
                CurrentUICulture = CultureInfo.InvariantCulture,
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };

            return thread;
        }

        private void ExecuteThreadWork()
        {
            lock (_syncLock)
            {
                if (_state == InternalState.Ready)
                {
                    throw new InvalidOperationException("The thread should not have been started yet.");
                }
            }

            while (true)
            {
                TaskSlot taskSlot;
                lock (_syncLock)
                {
                    if (_exceptions.Count != 0 || _state != InternalState.Running)
                    {
                        return;
                    }

                    if (_taskIndex >= _taskSlots.Length)
                    {
                        return;
                    }

                    taskSlot = _taskSlots[_taskIndex].EnsureNotNull();
                    _taskIndex++;
                }

                try
                {
                    _gameControlInfo.CheckInterruptions();

                    var taskResult = taskSlot.Task();
                    taskSlot.Result = taskResult;
                    taskSlot.IsCompleted = true;
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (MultiTaskControllerStopException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    lock (_syncLock)
                    {
                        _exceptions.Add(ex);
                    }

                    _gameControlInfo.RequestCustomInterruption<MultiTaskControllerStopException>();
                    return;
                }
            }
        }

        private enum InternalState
        {
            Ready,
            Running,
            Finished
        }

        private sealed class TaskSlot
        {
            public TaskSlot(Func<TResult> task)
            {
                Task = task.EnsureNotNull();
            }

            public Func<TResult> Task
            {
                get;
            }

            public TResult Result
            {
                get;
                set;
            }

            public bool IsCompleted
            {
                get;
                set;
            }
        }
    }
}