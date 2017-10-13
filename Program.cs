using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
                "yahoo.com",
                "comcast.com"
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
                                    var status = "";
                                    try
                                    {
                                        PingReply r = null;
                                        r = p.Send(s, 1000);
                                        status = IPStatus.Success.ToString();
                                    }
                                    catch (Exception ex)
                                    {
                                        status = ex.Message;
                                        var se = ex.InnerException as SocketException;
                                        if (se != null)
                                        {
                                            status = se.SocketErrorCode.ToString();
                                        }
                                    }
                                    Console.WriteLine($"\t{status}");
                                    System.IO.File.AppendAllText(logFile, $"{DateTime.Now:s}\t{s}\t{status}\r\n");
                                }
                            }
                        }
                        await Task.Delay(100);
                    }
                }));
        }
    }
}
