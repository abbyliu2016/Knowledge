using System.Collections.Concurrent;

public class LatencyCalculator
{
    public ConcurrentQueue<(DateTime timestamp, double latency)> Latencies { get; private set; }
    public TimeSpan window { get; private set; }

    private ReaderWriterLockSlim rw;
    public LatencyCalculator(TimeSpan window)
    {
        this.window = window;
        Latencies = new ConcurrentQueue<(DateTime, double)>();
        rw = new ReaderWriterLockSlim();
    }

    public void AddLatency(int latency)
    {
        Latencies.Enqueue((DateTime.UtcNow, latency));
        rw.EnterWriteLock();
        try
        {
            while (Latencies.TryPeek(out var entry) && (DateTime.UtcNow - entry.timestamp) > window)
            {
                Latencies.TryDequeue(out _);
            }
        }
        finally
        {
            rw.ExitWriteLock();
        }
        
    }

    public double GetLatency(int k)
    {
        rw.EnterReadLock();
        try
        {
            var tmpList = Latencies.Select(x=>x.latency).Order().ToList();
            int idx = Math.Max(0, (int)Math.Ceiling(tmpList.Count * k / 100.0) - 1);
            return tmpList[idx];
        }
        finally
        {
            rw.ExitReadLock();
        }
    }
}