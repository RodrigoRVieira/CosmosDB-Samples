using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentDB.Samples.Shared.Util
{
    public class Log
    {
        public static void LogAndWait(string logMessage, double requestCharge)
        {
            // Display request charge from asynchronous response
            Console.Write(logMessage);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(requestCharge);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(Environment.NewLine + "Press enter key to continue...");
            Console.ReadKey();
            Console.WriteLine();
        }
    }
}