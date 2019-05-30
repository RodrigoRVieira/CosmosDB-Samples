using Microsoft.Azure.Documents;
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

        /// <summary>
        /// Log exception error message to the console
        /// </summary>
        /// <param name="e">The caught exception.</param>
        public static void LogException(Exception e)
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            Exception baseException = e.GetBaseException();
            if (e is DocumentClientException)
            {
                DocumentClientException de = (DocumentClientException)e;
                Console.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
            }
            else
            {
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }

            Console.ForegroundColor = color;
        }
    }
}