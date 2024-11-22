using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Practices
{
    public interface IState<T>
    {
        public T value { get; set; }
    }

    public class IdState : IState<int>
    {
        public int value { get; set; }
    }

    public class MDState : IState<int[]>
    {
        public int[] value { get; set; }
    }


    public class NoItemException : Exception
    {
        public NoItemException(string message) { }
    }

    public interface IIterator<T, R>
    {
        public T Next();
        public IState<R> GetState();
        public void SetState(IState<R> state);
    }

    public class ListIterator<T> : IIterator<T, int>
    {
        public IState<int> state;
        private List<T> list;
        public ListIterator(List<T> list)
        {
            this.list = list;
            state = new IdState();
            state.value = 0;
        }

        public IState<int> GetState()
        {
            return state;
        }

        public void SetState(IState<int> state)
        {
            this.state = state;
        }

        private bool HasNext()
        {
            if (state.value < list.Count)
            {
                return true;
            }

            return false;
        }

        public T Next()
        {
            if (HasNext())
            {
                return list[state.value++];
            }

            throw new NoItemException("No item exists");
        }
    }

    public class Person
    {
        public int id { get; set; }
        public string name { get; set; }
        public int age { get; set; }

        public bool Equals(Person b)
        {
            return id == b.id && name == b.name && age == b.age;
        }
    }

    public partial class JsonFileIterator1<T> : IIterator<T, int>
    {
        public IState<int> state;
        public int position;
        private ListIterator<T> listIterator;
        List<JsonElement> itemList;
        public JsonFileIterator1(string filePath)
        {
            state = new IdState();
            state.value = 0;

            var jsonContent = File.ReadAllText(filePath);
            var document = JsonDocument.Parse(jsonContent);
            itemList = document.RootElement.EnumerateArray().ToList();
        }

        public IState<int> GetState()
        {
            return new IdState { value = position };
        }

        public void SetState(IState<int> idx)
        {
            position = idx.value;
        }

        private bool HasNext()
        {
            return position < itemList.Count;
        }

        public T Next()
        {
            if (HasNext())
            {
                return JsonSerializer.Deserialize<T>(itemList[position++].GetRawText());
            }

            throw new NoItemException("No item exists");
        }
    }

    public class JsonFileIterator<T> : IIterator<T, long>
    {
        private readonly FileStream _fileStream;
        private long _position;
        private bool _hasMoreItems;

        public JsonFileIterator(string filePath)
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _position = 0;
            _hasMoreItems = true;
        }

        public T Next()
        {
            if (!_hasMoreItems) throw new InvalidOperationException("No more items.");

            _fileStream.Seek(_position, SeekOrigin.Begin);
            using (var reader = new StreamReader(_fileStream, leaveOpen: true))
            {
                var buffer = new char[1024];
                int bytesRead = reader.Read(buffer, 0, buffer.Length);
                var json = new string(buffer, 0, bytesRead).Trim();


                var jsonReader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), isFinalBlock: true, state: default);

                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonTokenType.StartObject)
                    {
                        var jsonElement = JsonDocument.ParseValue(ref jsonReader).RootElement;
                        _position += jsonReader.BytesConsumed + Environment.NewLine.Length;
                        return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                    }
                }
            }

            _hasMoreItems = false;
            throw new InvalidOperationException("No more items.");
        }

        public IState<long> GetState()
        {
            return new FileState { value = _position };
        }

        public void SetState(IState<long> state)
        {
            _position = state.value;
            _hasMoreItems = true;
        }

        public void Dispose()
        {
            _fileStream.Dispose();
        }
    }

    public class FileState : IState<long>
    {
        public long value { get; set; }
    }


    public class MultiJsonFileIterator<T> : IIterator<T, int[]>
    {
        public IState<int[]> state;
        private List<JsonFileIterator1<T>> list;

        public MultiJsonFileIterator(List<string> filePath)
        {
            list = new List<JsonFileIterator1<T>>();
            for (int i = 0; i < filePath.Count; i++)
            {
                list.Add(new JsonFileIterator1<T>(filePath[i]));
            }
            state = new MDState() { value = new int[2] };
        }

        public IState<int[]> GetState()
        {
            return new MDState { value = (int[])state.value.Clone() };
        }

        public void SetState(IState<int[]> idx)
        {
            state = idx;
        }

        public T Next()
        {
            while (state.value[0] < list.Count)
            {
                int listIdx = state.value[0];
                list[listIdx].SetState(new IdState { value = state.value[1] });
                try
                {
                    var value = list[listIdx].Next();
                    state.value[1] = list[listIdx].GetState().value;
                    return value;
                }
                catch (NoItemException)
                {
                    state.value[0]++;
                    state.value[1] = 0;
                    return Next();
                }
            }

            throw new NoItemException("No item exists");
        }
    }

    public partial class Solution
    {
        public static bool Assert(string message, int actual, int expected)
        {
            if (actual != expected)
            {
                Console.WriteLine($"{message}: Expected {expected} but got {actual}");
                return false;
            }

            Console.WriteLine($"{message}: passed");
            return true;
        }


        public static bool Assert(string message, Person actual, Person expected)
        {
            if (!actual.Equals(expected))
            {
                Console.WriteLine($"{message}: Expected {JsonSerializer.Serialize(expected)} but got {JsonSerializer.Serialize(actual)}");
                return false;
            }

            Console.WriteLine($"{message}: passed");
            return true;
        }

        public static void Test()
        {
            var input = new List<int> { 1, 2, 3 };
            var stateList = new List<int>();
            var iterator = new ListIterator<int>(input);
            try
            {
                while (true)
                {
                    int idx = iterator.GetState().value;
                    stateList.Add(idx);
                    int actualValue = iterator.Next();
                    Assert($"Test current idx:{idx}", actualValue, input[idx]);
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(NoItemException))
                {
                    int actualIdx = iterator.GetState().value;
                    Assert($"No item exists, index:{actualIdx}", actualIdx, input.Count);
                }
            }

            for (int i = stateList.Count - 1; i >= 0; i--)
            {
                try
                {
                    int idx = stateList[i];
                    iterator.SetState(new IdState { value = idx });
                    int actualValue = iterator.Next();
                    Assert($"Test current idx:{idx}", actualValue, input[idx]);
                }
                catch (Exception ex)
                {
                    if (ex.GetType() == typeof(NoItemException))
                    {
                        int actualIdx = stateList[i];
                        Assert($"No item exists, index:{stateList[i]}", actualIdx, input.Count);
                    }
                }
            }
        }


        public static void TestJsonFileIterator()
        {
            var filePaths = "D:\\interview\\.NET\\MultiFileIterator\\d.json";
            var iterator = new JsonFileIterator<Person>(filePaths);
            while (true)
            {
                try
                {
                    var person = iterator.Next();
                    Console.WriteLine($"Person: {person.id}, {person.name}, {person.age}");
                }
                catch (InvalidOperationException)
                {
                    break;
                }
            }
        }

        public static void TestMultiFileIterator()
        {
            var filePaths = new List<string> { "D:\\interview\\.NET\\MultiFileIterator\\d.json", "D:\\interview\\.NET\\MultiFileIterator\\e.json" };
            var iterator = new MultiJsonFileIterator<Person>(filePaths);
            var stateList = new List<IState<int[]>>();
            var inputList = new List<Person>();
            inputList.Add(new Person() { name = "Alice", age = 30, id = 1 });
            inputList.Add(new Person() { name = "Bob", age = 25, id = 2 });
            inputList.Add(new Person() { name = "Charlie", age = 35, id = 3 });
            inputList.Add(new Person() { name = "Abby", age = 30, id = 1 });
            inputList.Add(new Person() { name = "Seth", age = 25, id = 2 });
            inputList.Add(new Person() { name = "Owen", age = 35, id = 3 });
            int i = 0;
            while (true)
            {
                try
                {
                    var person = iterator.Next();
                    Assert("Test multi file iterator", person, inputList[i++]);
                    stateList.Add(new MDState { value = (int[])iterator.GetState().value.Clone() });
                    // Console.WriteLine($"Person: {person.id}, {person.name}, {person.age}");
                }
                catch (NoItemException)
                {
                    break;
                }
            }

            for (i = stateList.Count - 1; i >= 0; i--)
            {
                try
                {
                    var idx = (int[])stateList[i].value.Clone();
                    iterator.SetState(new MDState() { value = (int[])idx.Clone() });
                    var actualValue = iterator.Next();
                    Assert($"Test current idx:{JsonSerializer.Serialize(idx)}", actualValue, inputList[i+1]);
                }
                catch (Exception ex)
                {
                    if (ex.GetType() == typeof(NoItemException))
                    {
                        var actualIdx = stateList[i];
                      //  Assert($"No item exists, index:{stateList[i]}", actualIdx, inputList.Count);
                    }
                }
            }
        }


        public static void Main(string[] args)
        {
           // Test();
           TestMultiFileIterator();

        }
    }
}
