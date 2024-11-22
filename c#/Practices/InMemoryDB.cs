using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Practices
{
    public class Row
    {
        private Dictionary<string, string> cols;
        private ReaderWriterLockSlim rw;
        public Row()
        {
            cols = new Dictionary<string, string>();
            rw = new ReaderWriterLockSlim();
        }

        public Row(Dictionary<string, string> col)
        {
            cols = col;
            rw = new ReaderWriterLockSlim();
        }

        public void Update(Dictionary<string, string> cols)
        {

            rw.EnterWriteLock();
            this.cols = cols;
            rw.ExitWriteLock();
        }

        public Dictionary<string, string> QueryCols(Dictionary<string, string> where)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            rw.EnterReadLock();
           
            try
            {
                var tmpCols = cols.All(pair => where.ContainsKey(pair.Key) && where[pair.Key] == pair.Value) ? cols : null;
                if (tmpCols != null)
                {
                    result = new Dictionary<string, string>(tmpCols);
                }

            }
            finally
            {
                rw.ExitReadLock();
            }

            return result;
        }

    }
        public class InMemoryDB
    {
        private ConcurrentDictionary<int, Row> rows;
        private int id;
        public InMemoryDB()
        {
            rows = new ConcurrentDictionary<int, Row>();
            id = 0;
        }

        public void Insert(Dictionary<string, string> cols)
        {
            int newId = Interlocked.Increment(ref id);

            rows.TryAdd(newId, new Row(cols));

        }

        public void Update(int id, Dictionary<string, string> cols)
        {
           if (rows.TryGetValue(id, out var row))
            {
                row.Update(cols);
            }

        }

        public List<Dictionary<string, string>> Query(Dictionary<string, string> where)
        {
            var keys = rows.Keys.ToList();

            var results = new List<Dictionary<string, string>>();

            foreach (int key in keys)
            {
                if (rows.TryGetValue(key, out var row))
                {
                    var cols = row.QueryCols(where);
                    if (cols != null && cols.Count > 0)
                    {
                        results.Add(cols);
                    }
                }

            }

            return results; ;
        }

        public List<Dictionary<string, string>> QueryAndOrderBy(Dictionary<string, string> where, List<(string colName, bool ascending)> orderby)
        {
            var results = Query(where);
            if (orderby == null || orderby.Count == 0)
            {
                return results;
            }

            // Apply multi-column ordering without using IOrderedEnumerable
            for (int i = orderby.Count - 1; i >= 0; i--)
            {
                var (column, ascending) = orderby[i];

                results = ascending
                    ? results.OrderBy(row => row.ContainsKey(column) ? row[column] : null).ToList()
                    : results.OrderByDescending(row => row.ContainsKey(column) ? row[column] : null).ToList();
            }


            return results;

        }
    }



    public partial class Solution
    {

        public static bool Assert(string message, Dictionary<string, string> actual, Dictionary<string, string> expected)
        {
            if (actual.Count != expected.Count)
            {
                Console.WriteLine($"{message} - count is not equal");
                return false;
            }

            foreach (var pair in actual)
            {
                if (!expected.ContainsKey(pair.Key) || expected[pair.Key] != pair.Value)
                {
                    Console.WriteLine($"{message} - Expected: {}, but was: {pair.Value}");
                    return false;
                }
            }

            Console.WriteLine($"{message} - Passed");
            return true;
        }

            public static void TestDB()
        {
            var row1 = new Dictionary<string, string>
            {
                { "name", "John" },
                { "age", "20" }
            };

            var db = new InMemoryDB();
            db.Insert(row1);
            var re = db.Query(new Dictionary<string, string> { { "name", "John" } });
            
        }
    }
