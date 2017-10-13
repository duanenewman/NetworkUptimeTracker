using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UptimeTracker
{
    class Program
    {
        static void Main(string[] args)
        {
            var logFile = "uptimetracker.log";
            var pingSites = new List<string>
            {
                "google.com",
                "yahoo.com"
            };
            var interval = 30000;

            var lastRun = DateTime.MinValue;
            var keepRunning = true;

            Task.WaitAll(
                Task.Run(() => 
                {
                    Console.ReadKey();
                    keepRunning = false;
                }),
                Task.Run(async () =>
                {
                    while (keepRunning)
                    {
                        if ((DateTime.Now - lastRun).TotalMilliseconds >= interval)
                        {
                            lastRun = DateTime.Now;
                            using (var p = new Ping())
                            {
                                foreach (var s in pingSites)
                                {
                                    Console.Write($"Pinging {s}");
                                    var r = p.Send(s);
                                    //if (r.Status != IPStatus.Success)
                                    Console.WriteLine($"\t{r.Status}");
                                    System.IO.File.AppendAllText(logFile, $"{DateTime.Now:s}\t{s}\t{r.Status}\r\n");
                                }
                            }
                        }
                        await Task.Delay(100);
                    }
                }));
        }
    }
}
