using System;
using System.Text.RegularExpressions;

namespace Excel
{
    class Program
    {
        static void Main(string[] args)
        {
            Regex regex = new Regex(@"(?<=[()+*/-])|(?=[()+*/-])");
            string[] parts = regex.Split(@"A1+AZ3/BH2*J4-XXDS5");
            ExcelTable e = new ExcelTable("./table.txt");
        }
    }
}
