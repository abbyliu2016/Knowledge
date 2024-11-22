using Practices;
using System.Collections.Concurrent;

namespace Practices
{
    public class Item
    {
        public int version { get; set; }
        public string value { get; set; }
        public long timestamp { get; set; }
    }


    public class ThreadSafeItemList
    {
        private List<Item> items;
        private ReaderWriterLockSlim rwLock;
        public ThreadSafeItemList()
        {
            items = new List<Item>();
            rwLock = new ReaderWriterLockSlim();
        }

        public void Add(string value)
        {
            var item = new Item()
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                value = value
            };
            rwLock.EnterWriteLock();
            try
            {
                item.version = items.LastOrDefault()?.version + 1 ?? 1;
                items.Add(item);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public Item GetItem(long timestamp)
        {
            rwLock.EnterReadLock();
            try
            {
                int idx = BinarySearch(timestamp);
                return idx == -1 ? default : items[idx];
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        public List<Item> GetItems()
        {
            rwLock.EnterReadLock();
            try
            {
                return items;
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }

        private int BinarySearch(long timeStamp)
        {
            int i = 0;
            int j = items.Count - 1;
            int ans = -1;
            while (i <= j)
            {
                int mid = i + (j - i) / 2;
                if (items[mid].timestamp <= timeStamp)
                {
                    ans = mid;
                    i = mid + 1;
                }
                else
                {
                    j = mid - 1;
                }
            }

            return ans;
        }
    }


    public class KVStore
    {
        private ConcurrentDictionary<string, ThreadSafeItemList> store;

        public KVStore()
        {
            store = new ConcurrentDictionary<string, ThreadSafeItemList>();
        }

        public void CreateItem(string key, string value)
        {
            var tmpList = store.GetOrAdd(key, _ => new ThreadSafeItemList());
            tmpList.Add(value);
        }

        public Item GetItem(string key, long timestamp)
        {
            if (store.TryGetValue(key, out var itemList))
            {
                return itemList.GetItem(timestamp);
            }

            return default;
        }

    }
}