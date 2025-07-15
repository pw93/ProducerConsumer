using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProfitWin;
namespace QuoteEngine
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var server = new QuoteServer();
            server.Start();

            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("CTRL+C detected. Shutting down...");
                server.Stop();
            };

            server.WaitUntilExit();
        }
    }
}
