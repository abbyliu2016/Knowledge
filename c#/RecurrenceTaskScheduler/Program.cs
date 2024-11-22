using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

public enum JobType
{
    OnlyOnce = 0,
    Repeat = 1
}


public class Option
{
    public int scheduleIntervalInSeconds { get; set; }
    public JobType jobType { get; set; }


}

public class JobItem
{
    public string jobName { get; set; }
    public Option option { get; set; }
    public Func<CancellationToken, Task> action { get; set; }
    public bool IsRunning { get; set; }
    public bool IsCancelled { get; set; }

    public DateTime? lastRun { get; set; }
    public DateTime nextRun { get; set; }

    public void AdjustNextRunTime(DateTime time)
    {
        nextRun = time;
    }

    public JobItem(string jobName, Option option, Func<CancellationToken, Task> action)
    {
        this.jobName = jobName;
        this.option = option;
        this.action = action;
    }
}

public class RecurrenceTaskScheduler
{
    private List<JobItem> jobs;

    private int TaskRunnerIntervalInSeconds = 100;

    private ManualResetEventSlim TaskComing;

    private CancellationTokenSource cancelationTokenSource;

    private Object lockObject;

    public RecurrenceTaskScheduler()
    {
        jobs = new List<JobItem>();
        cancelationTokenSource = new CancellationTokenSource();
        TaskComing = new ManualResetEventSlim(false);
        lockObject = new Object();
    }

    public void AddJob(string jobName, Option option, Func<CancellationToken, Task> action)
    {
        if (option.scheduleIntervalInSeconds <= 0)
        {
            throw new ArgumentException("scheduleIntervalInSeconds must be greater than zero.");
        }

        lock (lockObject)
        {
            jobs.Add(new JobItem(jobName, option, action));
            if (!TaskComing.IsSet)
            {
                TaskComing.Set();
            }
        }

    }

    public void CancelledJob(string jobName)
    {
        lock (lockObject)
        {
            var job = jobs.FirstOrDefault(x => x.jobName == jobName);
            if (job != null)
            {
                job.IsCancelled = true;
            }
        }
    }

    public void Stop()
    {
        cancelationTokenSource.Cancel();
    }

    public DateTime GetWakeTime()
    {
        var curTime = DateTime.UtcNow;
        lock (this.lockObject)
        {
            if (this.jobs.Count == 0)
            {
                return DateTime.MaxValue;
            }

            var nextWake = this.jobs.Min(x => x.nextRun);

            return nextWake;
        }
    }

    private void CleanupCanceledJobs()
    {
        lock (lockObject)
        {
            jobs.RemoveAll(x => x.IsCancelled);
        }
    }


    public async Task RunningAsync()
    {
        var toRemoveList = new List<JobItem>();

        while (true)
        {
            if (cancelationTokenSource.Token.IsCancellationRequested)
            {
                break;
            }

            bool hasJobs;

            lock (lockObject)
            {
                hasJobs = jobs.Count > 0;
            }

            // If no jobs are present, wait for a signal
            if (!hasJobs)
            {
                TaskComing.Wait(cancelationTokenSource.Token);
                lock (lockObject)
                {
                    if (jobs.Count == 0) TaskComing.Reset();  // Reset only if no jobs are added
                }
            }

            var jobToRun = new List<JobItem>();
            lock (lockObject)
            {
                foreach (var job in jobs)
                {
                    if (job.IsCancelled) continue;

                    if (job.IsRunning)
                    {
                        job.AdjustNextRunTime(DateTime.UtcNow.AddSeconds(job.option.scheduleIntervalInSeconds));
                        continue;
                    }

                    if (job.option.jobType == JobType.OnlyOnce)
                    {
                        if (job.lastRun == null)
                        {
                            jobToRun.Add(job);
                            job.lastRun = DateTime.UtcNow;
                        }

                        job.IsCancelled = true;
                    }
                    else
                    {
                        if (job.nextRun == default || DateTime.UtcNow >= job.nextRun)
                        {
                            jobToRun.Add(job);
                            job.lastRun = DateTime.UtcNow;
                            job.nextRun = job.lastRun.Value.AddSeconds(job.option.scheduleIntervalInSeconds);
                        }
                    }
                }

            }

            foreach (var job in jobToRun)
            {
                job.IsRunning = true;
                try
                {
                    await job.action(cancelationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                
                job.IsRunning = false;
            }


            CleanupCanceledJobs();

            var wakeTime = GetWakeTime();
            var delay = Math.Max(0, (wakeTime - DateTime.UtcNow).TotalMilliseconds);
            await Task.Delay((int)delay, cancelationTokenSource.Token);
        }
    }
}