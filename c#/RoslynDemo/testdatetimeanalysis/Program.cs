using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using DT = System.DateTime;
namespace testdatetimeanalysis
{
    public class Aib0003Test
    { 
        public DateTime Now => DateTime.Now;


        public async Task func1()
        {
            await Task.Delay(1);
            throw new ArgumentNullException("haha");
        }

        public async Task func2()
        {
            await func1();
        }

        public void M1()
        {
            Console.WriteLine(DT.Now);
            this.M2(DateTime.Now);
        }

        public void M2(in DateTime dateTime) { }
    }

    public static class Program
    {
        public static void Main()
        {
            try
            {
                Aib0003Test tp = new Aib0003Test();
                tp.func2().GetAwaiter().GetResult();
            }
            catch(Exception ex)
            {
                var tmp = ex.GetType();
            }
        }
            }
}
