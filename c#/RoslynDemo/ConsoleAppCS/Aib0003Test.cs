namespace ConsoleApp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using DT = System.DateTime;

    public class Aib0003Test
    {
        public DateTime Now => DateTime.Now;

        public void M1()
        {
            Console.WriteLine(DT.Now);
            this.M2(DateTime.Now);
        }

        public void M2(in DateTime dateTime) { }
    }
}
