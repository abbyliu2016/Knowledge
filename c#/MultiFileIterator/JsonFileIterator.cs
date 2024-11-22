using System.ComponentModel;
using System.IO;
using System.Text.Json;



public class Person
{
    public int id { get; set; }
    public string name { get; set; }
    public int age { get; set; }
}

public class MDIdxState : IState<int[]>
{
    public int[] Value { get; set; }
}

public class MultiJsonFileIterator : IIterator<Person, int[]>
{
    public MDIdxState idxState;
    public List<JsonFileIterator> jsonFileIterators;
    private Person current;
    

    public MultiJsonFileIterator(List<string> filePaths)
    {
        idxState = new MDIdxState();
        idxState.Value = new int[2];
        jsonFileIterators = new List<JsonFileIterator>();
        foreach (var filePath in filePaths)
        {
            jsonFileIterators.Add(new JsonFileIterator(filePath));
        }
    }

    public IState<int[]> SaveState()
    {
        return new MDIdxState() { Value = (int[])idxState.Value.Clone() };
    }

    public void SetState(IState<int[]> idx)
    {
        idxState = (MDIdxState)idx;
    }

    private bool HasNext()
    {
        while (idxState.Value[0] < jsonFileIterators.Count)
        {
            jsonFileIterators[idxState.Value[0]].SetState(new IdxState { Value = idxState.Value[1] });
            current = jsonFileIterators[idxState.Value[0]].Next();

            if (current == null)
            {
                idxState.Value[0]++;
                idxState.Value[1] = 0;
            }
            else
            {
                var tmpState = jsonFileIterators[idxState.Value[0]].SaveState();
                idxState.Value[1] = tmpState.Value;
                return true;
            }
        }

        return false;
    }

    public Person Next()
    {
        if (HasNext())
        {
            return current;
        }

        return null;
    }

}



public partial class JsonFileIterator : IIterator<Person, int>
{
    public IState<int> state;
  //  public FileStream fileStream;
    public int position; // Use a long to store the position instead of Utf8JsonReader
                          //  private JsonReaderState _jsonReaderState; // Add this field to store the state of the Utf8JsonReader

    private ListIterator<Person> listIterator;
    List<JsonElement> itemList;
    public JsonFileIterator(string filePath)
    {
        state = new IdxState();
        state.Value = 0;


        var jsonContent = File.ReadAllText(filePath);
        var document = JsonDocument.Parse(jsonContent);
        itemList = document.RootElement.EnumerateArray().ToList();

       // listIterator = new ListIterator<Person>(tmpList);
    }

    public IState<int> SaveState()
    {
      //  position = fileStream.Position;
        return new IdxState { Value = position };
    }

    public void SetState(IState<int> idx)
    {
        position = idx.Value;
       // fileStream.Seek(position, SeekOrigin.Begin);
    }

    private bool HasNext()
    {
        return position < itemList.Count;
    }

    public Person Next()
    {
        if (HasNext())
        {
            return JsonSerializer.Deserialize<Person>(itemList[position++].GetRawText());
        }

        return null;
    }
}
