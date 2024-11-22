using System.Text;

using System;
using System.Collections.Generic;

using System;
using System.Collections.Generic;





public class FilePathMerger
{
    public string? Cd(string currentPath, string newPath)
    {
        var curPaths = currentPath.Split('/' ,StringSplitOptions.RemoveEmptyEntries);
        var newPaths = newPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var paths = new List<string>();
        if (!SanitizePath(curPaths, paths) || 
            !SanitizePath(newPaths, paths))
        {
            return null;
        }

        if (paths.Count == 0) return "/";

        StringBuilder ans = new StringBuilder(string.Join('/', paths));
       if (ans[0] != '/') ans.Insert(0, '/');
       // if (ans.Length != 1 && ans[ans.Length - 1] == '/') ans.Length--;
        return ans.ToString();
    }

    public string? Cd(string currentPath, string newPath, Dictionary<string, string> SoftLink)
    {
        var path = Cd(currentPath, newPath);
        var visited = new HashSet<string>();
        return ResolveSoftLink(path, SoftLink, visited);

    }

    private string? ResolveSoftLink(string path, Dictionary<string, string> SoftLink, HashSet<string> visited)
    {
        int longestKey = 0;
        string? longestValue = null;
        if (visited.Contains(path))
        {
            return null;
        }
        visited.Add(path);
        foreach (var key in SoftLink.Keys)
        {
            if (path.StartsWith(key) && key.Length > longestKey)
            {
                longestKey = key.Length;
                longestValue = SoftLink[key];
            }
        }

        if (longestValue == null)
        {
            return path;
        }

     

        string newPath = longestValue+path.Substring(longestKey);
        return ResolveSoftLink(newPath, SoftLink, visited);
    }



    private bool SanitizePath(string[] paths,  List<string> sanitizedPaths)
    {
        int count = sanitizedPaths.Count;
        for (int i = 0; i < paths.Length;i++)
        {
            var path = paths[i];
            if (path == "..")
            {
                if (sanitizedPaths.Count > count)
                {
                    sanitizedPaths.RemoveAt(sanitizedPaths.Count - 1);
                }
                else
                {
                    return false;
                }
            }
            else if (path != ".")
            {
                sanitizedPaths.Add(path);
            }
        }

        return true;
    }

    public static void AssertEqual(string message, string? expected, string actual)
    {
        if (expected != actual)
        {
            Console.WriteLine($"{message}: Expected: {expected} Actual: {actual}");
            return;
        }

        Console.WriteLine($"{message} succeeded!");
    }

    //  public static void Main()
    public static void Main()
    {
        var inputCur = "/";
        var inputNew = "";
        var filePathMerger = new FilePathMerger();
        AssertEqual("Both root dir", "/", filePathMerger.Cd(inputCur, inputNew));

        inputCur = "/";
        inputNew = "a";
        AssertEqual("firstone is root", "/a", filePathMerger.Cd(inputCur, inputNew));

        inputCur = "/a";
        inputNew = "./";
        AssertEqual("second one is .", "/a", filePathMerger.Cd(inputCur, inputNew));

        inputCur = "/a";
        inputNew = "bcd";
        AssertEqual("no root", "/a/bcd", filePathMerger.Cd(inputCur, inputNew));

        inputCur = "/a/./";
        inputNew = "bcd";
        AssertEqual("first has .", "/a/bcd", filePathMerger.Cd(inputCur, inputNew));

        inputCur = "/a/./";
        inputNew = "./bcd";
        AssertEqual("both has .", "/a/bcd", filePathMerger.Cd(inputCur, inputNew));

        inputCur = "/a/../";
        inputNew = "bcd";
        AssertEqual("first has ..", "/bcd", filePathMerger.Cd(inputCur, inputNew));

        inputCur = "/a/../../";
        inputNew = "bcd";
        AssertEqual("first has 2 ..", null, filePathMerger.Cd(inputCur, inputNew));

        inputCur = "/c";
        inputNew = "../a/../bcd";
        AssertEqual("second has 2 .. not vailid", null, filePathMerger.Cd(inputCur, inputNew));


        inputCur = "/c";
        inputNew = "a/bcd/../..";
        AssertEqual("second has 2 .. vailid", "/c", filePathMerger.Cd(inputCur, inputNew));

        inputCur = "/foo/bar";
        inputNew = "baz";
        AssertEqual("foo bar normal", "/foo/bar/baz", filePathMerger.Cd(inputCur, inputNew));

        inputCur = "/foo/../";
        inputNew = "./baz";
        AssertEqual("foo bar ../", "/baz", filePathMerger.Cd(inputCur, inputNew));

        inputCur = "/";
        inputNew = "foo/bar/../../baz";
        AssertEqual("new 2..", "/baz", filePathMerger.Cd(inputCur, inputNew));

        inputCur = "/";
        inputNew = "..";
        AssertEqual("new ..", null, filePathMerger.Cd(inputCur, inputNew));

        var softLink = new Dictionary<string, string>
        {
            {"/foo/bar", "/abc" }
        };

        inputCur = "/foo/bar";
        inputNew = "baz";
        AssertEqual("new 1", "/abc/baz", filePathMerger.Cd(inputCur, inputNew, softLink));

        softLink = new Dictionary<string, string>
        {
            {"/foo/bar", "/abc" },
            {"/abc", "/bcd" },
            {"/bcd/baz", "/xyz" }
        };

        inputCur = "/foo/bar";
        inputNew = "baz";
        AssertEqual("new 2", "/xyz", filePathMerger.Cd(inputCur, inputNew, softLink));

        softLink = new Dictionary<string, string>
        {
             { "/foo/bar", "/abc" },
             { "/abc", "/foo/bar" }
        };

        inputCur = "/foo/bar";
        inputNew = "baz";
        AssertEqual("new 3", null, filePathMerger.Cd(inputCur, inputNew, softLink));
    }
}