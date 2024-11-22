using System.Collections.Concurrent;

public class RateLimiter
{
    private TimeSpan TimeWindow { get; set; }
    private int Limit { get; set; }

    private ConcurrentDictionary<string, ConcurrentQueue<DateTime>> requests { get; set; }
    public RateLimiter(TimeSpan timeWindow, int limit)
    {
        TimeWindow = timeWindow;
        Limit = limit;
        requests = new ConcurrentDictionary<string, ConcurrentQueue<DateTime>>();
    }

    public bool IsAllowed(string userId)
    {
        var requestsList = requests.GetOrAdd(userId, new ConcurrentQueue<DateTime>());
        while (requestsList.TryDequeue(out var item))
        {
            if (DateTime.UtcNow - item < TimeWindow)
            {
                requestsList.Enqueue(item);
                break;
            }
        }

        if (requestsList.Count < Limit)
        {
            requestsList.Enqueue(DateTime.UtcNow);
            return true;
        }

        return false;
    }
}