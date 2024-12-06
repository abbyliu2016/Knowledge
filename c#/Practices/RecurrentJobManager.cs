
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.CoreFramework;

namespace Microsoft.PowerApps.CoreServices.Common
{
    /// <summary>
    /// Assists in regularly executing a set of simple background tasks.
    /// </summary>
    /// <remarks>
    /// This class is optimized for very simple background tasks. Different activities can be executed at the same time, but only
    /// one instance of a given activity is running at any point. If an activity is still running when its next scheduling comes up,
    /// the next scheduling is skipped -- skipping will continue until the previous one completes.
    /// </remarks>
    public class RecurrentJobManager : IRecurrentJobManager
    {
        private readonly object monitor = new object();
        private readonly ILogger logger;
        private readonly IWallClockTimer wallClockTimer;
        private readonly List<WorkItem> workItemList = new List<WorkItem>();

        // Make-shift condition variable for handling modification of the work list while sleeping.
        private volatile TaskCompletionSource<bool> wakeSource = new TaskCompletionSource<bool>();

        // Start / Stop fields.
        private Task backgroundPollingLoopTask;
        private CancellationTokenSource backgroundPollingLoopCancellation;

        /// <summary>
        /// Creates a new instance of <see cref="RecurrentJobManager"/>.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="wallClockTimer">Instance of <see cref="IWallClockTimer"/> to aid in testability of the class.</param>
        public RecurrentJobManager(ILogger logger, IWallClockTimer wallClockTimer)
        {
            Contracts.CheckValue(logger, nameof(logger));
            Contracts.CheckValue(wallClockTimer, nameof(wallClockTimer));

            this.logger = logger;
            this.wallClockTimer = wallClockTimer;
        }

        /// <inheritdoc />
        public TimeSpan SchedulingGranularity { get; set; } = TimeSpan.FromMinutes(1);

        /// <inheritdoc />
        public TimeSpan ShutdownGracePeriod { get; set; } = TimeSpan.FromMinutes(1);

        internal bool IsRunning => backgroundPollingLoopTask != null;


        /// <inheritdoc />
        public void Start()
        {
            Contracts.Check(!IsRunning, "StartPolling must not be called when already started.");

            // We don't expect the previous cancellation token to be still in use, but it would be preferable to not leak it.
            this.backgroundPollingLoopCancellation?.Dispose();

            var cancellationTokenSource = new CancellationTokenSource();
            this.backgroundPollingLoopCancellation = cancellationTokenSource;
            this.backgroundPollingLoopTask = ((Func<Task>)(async () =>
            {
                // Uses Task.Yield pattern instead of TaskScheduler.Current.Run in order to preserve ServiceContext.
                await Task.Yield();
                await StartImpl(cancellationTokenSource.Token);
            }))();
        }

        /// <inheritdoc />
        public async Task StopAsync()
        {
            Contracts.Check(IsRunning, "Start must be called before Stop is called.");
            try
            {
                this.backgroundPollingLoopCancellation.Cancel();
                await this.backgroundPollingLoopTask;
            }
            finally
            {
                this.backgroundPollingLoopCancellation.Dispose();
                this.backgroundPollingLoopCancellation = null;
                this.backgroundPollingLoopTask = null;
            }
        }

        /// <inheritdoc />
        public void AddWork(ActivityType activityType, RecurrentActivityOptions options, Func<CancellationToken, Task> func)
        {
            Contracts.Check(!IsRunning, "Does not support modifying the work list while running. Feature not implemented. Your work may fail to get scheduled.");

            options = options.Clone() ?? new RecurrentActivityOptions(); // defaults
            var currentTime = this.wallClockTimer.CurrentTime;
            var workItem = new WorkItem
            {
                Options = options,
                ActivityType = activityType,
                Func = func,
                LastExecution = null,
                NextExecution = options.DetermineNextExecution(null, currentTime)
            };
            lock (this.monitor)
            {
                this.workItemList.Add(workItem);
            }
        }

        /// <summary>
        /// Background execution loop.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task tracking execution.</returns>
        private async Task StartImpl(CancellationToken cancellationToken)
        {
            try
            {
                var wakeTime = this.wallClockTimer.CurrentTime;
                for (; ; )
                {
                    ExecuteEligibleWork(wakeTime);

                    // Note: this approach to scheduling assumes that all tasks are added
                    // before Start is called, otherwise tasks added after Start is called might
                    // not get their first run on time, running when the next pre-existing task
                    // was scheduled.
                    wakeTime = GetNextWakeTime();

                    if (wakeTime == DateTime.MaxValue)
                    {
                        return;
                    }

                    await wallClockTimer.When(wakeTime, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                await CancelRunningTasks();
            }
        }

        /// <summary>
        /// Executes all of the work items that are eligible to be scheduled.
        /// </summary>
        /// <param name="wakeTime">The time when execution was resumed.</param>
        private void ExecuteEligibleWork(DateTime wakeTime)
        {
            this.logger.Execute(
                ExecuteEligibleWorkActivity.Instance,
                () =>
                {
                    List<WorkItem> workToRun = null;
                    bool itemsToBeRemoved = false;
                    lock (this.monitor)
                    {
                        foreach (var item in this.workItemList)
                        {
                            if (item.NextExecution <= wakeTime)
                            {
                                // if currently executing, skip it and put it back to sleep. Use the expected execution
                                // as the last execution time for scheduling.
                                if (item.IsExecuting)
                                {
                                    item.NextExecution = item.Options.DetermineNextExecution(item.NextExecution, wakeTime);
                                    continue;
                                }

                                // otherwise, add it to the items to be executed.
                                (workToRun ?? (workToRun = new List<WorkItem>())).Add(item);
                                if (item.Options.OnlyOnce)
                                {
                                    // List iteration is not safe for removal during iteration. Mark
                                    // for removal after the iteration completes.
                                    item.Remove = true;
                                    itemsToBeRemoved = true;
                                }
                            }
                        }
                        if (itemsToBeRemoved)
                        {
                            this.workItemList.RemoveAll(item => item.Remove);
                        }
                    }
                    if (workToRun != null)
                    {
                        foreach (var item in workToRun)
                        {
                            item.Start(this.logger, this.wallClockTimer);
                        }
                    }
                });
        }

        /// <summary>
        /// Gets the <see cref="DateTime"/> when the task should resume execution based on
        /// the scheduling granularity of each work item.
        /// </summary>
        /// <returns>Next execution time.</returns>
        private DateTime GetNextWakeTime()
        {
            var currentTime = this.wallClockTimer.CurrentTime;
            lock (this.monitor)
            {
                if (this.workItemList.Count == 0)
                {
                    // No more work to execute, ever.
                    return DateTime.MaxValue;
                }

                var nextWake = this.workItemList.Min(item => item.NextExecution);

                // Round to scheduler granularity. Avoids very frequent scheduling in the case that something goes awry, e.g. if a user specifies zero
                // for scheduling frequency.
                nextWake = RoundToInterval(nextWake, SchedulingGranularity);
                if (nextWake <= currentTime)
                {
                    nextWake = currentTime + SchedulingGranularity;
                }

                return nextWake;
            }
        }

        /// <summary>
        /// Cancel any work items that are still running
        /// </summary>
        private async Task CancelRunningTasks()
        {
            var now = this.wallClockTimer.CurrentTime;
            List<WorkItem> stillExecuting = null;
            foreach (var workItem in this.workItemList)
            {
                if (workItem.IsExecuting)
                {
                    // Notify work that shutdown is requested
                    workItem.Cancel();
                    (stillExecuting ?? (stillExecuting = new List<WorkItem>())).Add(workItem);
                }
            }

            if (stillExecuting == null)
            {
                return;
            }

            // if some tasks were still executing, delay up to grace period, polling for completion.
            IEnumerable<TimeSpan> delayIntervals;
            if (this.ShutdownGracePeriod < TimeSpan.Zero)
            {
                // Delay indefinitely, with exponential back-off to prevent burning CPU unncessarily.
                delayIntervals = Enumerable.Range(0, int.MaxValue).Select(i => TimeSpan.FromSeconds(Math.Pow(2, i)));
            }
            else if (this.ShutdownGracePeriod == TimeSpan.Zero)
            {
                // No delay.
                return;
            }
            else
            {
                // Poll with exponential back off -- at 1/16, 1/8 ... 1/2, and 1 * the grace period.
                delayIntervals = Enumerable.Range(0, 5).Reverse().Select(i => new TimeSpan((int)(ShutdownGracePeriod.Ticks * Math.Pow(2, -i))));
            }

            foreach (var delayInterval in delayIntervals)
            {
                await this.wallClockTimer.When(now + delayInterval, CancellationToken.None);
                // Check if all are done executing. If so, exit grace period early
                if (!stillExecuting.Any(workItem => workItem.IsExecuting))
                {
                    return;
                }
            }
        }

        private static DateTime RoundToInterval(DateTime dateTime, TimeSpan interval)
        {
            if (interval.Ticks <= 1)
            {
                return dateTime;
            }

            var remainder = Mod(dateTime.TimeOfDay, interval);
            if (remainder != TimeSpan.Zero)
            {
                dateTime += (interval - remainder);
            }

            return dateTime;
        }

        private static TimeSpan Mod(TimeSpan numerator, TimeSpan denominator)
        {
            return new TimeSpan(ticks: numerator.Ticks % denominator.Ticks);
        }

        /// <summary>
        /// Class that is used to represent a unit of work.
        /// </summary>
        private class WorkItem
        {
            public RecurrentActivityOptions Options;
            public volatile bool IsExecuting;

            public DateTime? LastExecution;
            public DateTime NextExecution;

            public ActivityType ActivityType;
            public Func<CancellationToken, Task> Func;
            public volatile bool Remove = false;

            private CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

            /// <summary>
            /// Determines whether the work item can run given the current time.
            /// </summary>
            /// <param name="currentTime">Instance of <see cref="DateTime"/> representing current clock time.</param>
            /// <returns>Boolean indicating whether the task can run.</returns>
            internal bool CanRun(DateTime currentTime)
            {
                return !IsExecuting && currentTime >= NextExecution;
            }

            /// <summary>
            /// Start the execution of a work item.
            /// </summary>
            /// <param name="logger">Instance of <see cref="ILogger"/>.</param>
            /// <param name="wallClockTimer">Instance of <see cref="IWallClockTimer"/>.</param>
            internal async void Start(ILogger logger, IWallClockTimer wallClockTimer)
            {
                Contracts.Check(IsExecuting == false, "Expected work item was not executing");

                // Ensure CancellationTokenSource is initialized
                if (CancellationTokenSource.Token.IsCancellationRequested)
                {
                    CancellationTokenSource = new CancellationTokenSource();
                }

                // Note: capture the current CancellationTokenSource synchronously. This can be updated in cases of Start() after StopAsync()
                var ct = CancellationTokenSource.Token;

                IsExecuting = true;
                var currentTime = wallClockTimer.CurrentTime;
                LastExecution = currentTime;
                NextExecution = this.Options.DetermineNextExecution(LastExecution, currentTime);
                try
                {
                    await logger.ExecuteAsync(
                        this.ActivityType,
                        async () =>
                        {
                            try
                            {
                                await Func(ct);
                            }
                            catch (OperationCanceledException) when (ct.IsCancellationRequested)
                            {
                                ServiceContext.Activity.Current.AddCustomProperty("isCanceled", "true");
                            }
                            catch (Exception e) when (!e.IsFatal())
                            {
                                // Exceptions are not expected, but service must remain executing.

                                // Notify the service if interested
                                // The exception handler is invoked within the ExecuteAsync call to preserve the correlation context
                                if (this.Options.ExceptionHandler != null)
                                {
                                    RunFireAndForgetDeferred(() => this.Options.ExceptionHandler(e));
                                }

                                ServiceContext.Activity.Current.FailWith(e);
                            }
                        });
                }
                finally
                {
                    // Make eligible for execution again.
                    IsExecuting = false;
                }
            }

            private static async void RunFireAndForgetDeferred(Action func)
            {
                // continuation will be scheduled on the current TaskScheduler
                // This ensures that even the synchronous portion of `func` cannot block the caller
                await Task.Yield();

                try
                {
                    func();
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    // Swallow, the caller deliberately chose not to observe this task even if it faults....
                }
            }

            public void Cancel()
            {
                this.CancellationTokenSource.Cancel();
            }
        }

        private sealed class ExecuteEligibleWorkActivity : SingletonActivityType<ExecuteEligibleWorkActivity>
        {
            public ExecuteEligibleWorkActivity() : base("CoreServices.Common.ExecuteEligibleWork")
            {
            }
        }
    }
}
