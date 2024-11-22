using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using static JsonFileIterator;


public interface IState<T>
{
  //  public int Index { get; set; }
    T Value { get; set; }
}

public class IdxState : IState<int>
{
    public int Value { get; set; }
}

public class FileState:IState<long>
{
    public long Value { get; set; }
}

public interface IIterator<T, R> 
{
  //  public bool HasNext();

    public T Next();

    public IState<R> SaveState();

    public void SetState(IState<R> idx);
}

public class Solution
{
    public static void Main()
    {
        // Test JsonFileIterator
        var filePaths = new List<string> { "D:\\interview\\.NET\\MultiFileIterator\\d.json", "D:\\interview\\.NET\\MultiFileIterator\\e.json" };
        var jsonFileIterator = new MultiJsonFileIterator(filePaths);
        var cur = jsonFileIterator.Next();
        var stateList = new List<IState<int[]>>();
        while (cur != null)
        {
            Console.WriteLine(cur.name);
            var state = jsonFileIterator.SaveState();
            //  stateList.Add(new MDIdxState() { Value = new int[2] { state.Value[0], state.Value[1] } });
            stateList.Add(state);
            cur = jsonFileIterator.Next();
        }

        for (int i = 0; i < stateList.Count; i++)
        {
            jsonFileIterator.SetState(stateList[i]);
            Console.WriteLine();
            Console.WriteLine($"state{i}: {stateList[i].Value[0]}, {stateList[i].Value[1]}");
            cur = jsonFileIterator.Next();
            while (cur != null)
            {
                Console.WriteLine(cur.name);
                cur = jsonFileIterator.Next();
            }
        }

        return;
        var filePaths1 = new List<string> { "D:\\interview\\.NET\\MultiFileIterator\\c.json", "D:\\interview\\.NET\\MultiFileIterator\\a.txt", "D:\\interview\\.NET\\MultiFileIterator\\b.txt" };
        var multiFileIterator = new MultiFileIterator1(filePaths1);
        while (multiFileIterator.MoveNext())
        {
            Console.WriteLine(multiFileIterator.Current);
        }


    }
}