public class Solution
{

    public static bool AssertEqual(string message, int value, int expectedValue)
    {
        if (value != expectedValue)
        {
            Console.WriteLine($"{message} - Expected: {expectedValue}, but was: {value}");
            return false;
        }

        Console.WriteLine($"{message} - Passed");
        return true;
    }

    public static bool AssertNotNull(string message, int? value)
    {
        if (value == null)
        {
            Console.WriteLine($"{message} - value is null");
            return false;
        }

        return true;
    }

    public static void Test()
    {
        SpreadSheet sheet = new SpreadSheet();
        sheet.SetCell("A1", "1");
        sheet.SetCell("A2", "2");
        sheet.SetCell("A3", "=A1+A2");

        // Test 1
        int? a1 = sheet.GetCell("A1");

        if (AssertNotNull("A1", a1))
        {
            AssertEqual("Test 1", a1.Value, 1);
        }

        // Test null
        var a4 = sheet.GetCell("A4");
        AssertNotNull("A4 should be null", a4);

        // Test a4
        var a3 = sheet.GetCell("A3");
        if (AssertNotNull("A3", a3))
        {
            AssertEqual("Test a3", a3.Value, 3);
        }

        sheet.SetCell("A5", "=A3+A2");

        var a5 = sheet.GetCell("A5");
        if (AssertNotNull("A5", a5))
        {
            AssertEqual("Test a5", a5.Value, 5);
        }

     

        // Update A5
        sheet.SetCell("A5", "=A1+A2");

        var na5 = sheet.GetCell("A5");
        if (AssertNotNull("nA5", na5))
        {
            AssertEqual("Test na5", na5.Value, 3);
        }

        // Update A3
        sheet.SetCell("A5", "=A3+A2");
        sheet.SetCell("A3", "=A2+A2");

        var na3 = sheet.GetCell("A3");
        if (AssertNotNull("nA3", na3))
        {
            AssertEqual("Test na3", na3.Value, 4);
        }

        // Update A5 after A3
        sheet.SetCell("A5", "=A3+A2");

        var nna5 = sheet.GetCell("A5");
        if (AssertNotNull("nnA5", nna5))
        {
            AssertEqual("Test nna5", nna5.Value, 6);
        }
    }

    public static void Main1(string[] args)
    {
        Test();

        
    }
}