using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UptimeTracker
{
	public class HostInfo
	{
		public string Name { get; set; }
		public IPAddress IPAddress { get; set; }

		public HostInfo(string name)
		{
			Name = name;
			//UpdateIPAddress();
		}

		private void UpdateIPAddress()
		{
			IPAddress = Dns.GetHostAddresses(Name).First();
		}
	}

	public class RoundRobinList<T> : List<T>
	{
		int lastEntryIndex = -1;

		public T GetNext()
		{
			if (Count == 0) return default(T);

			if (lastEntryIndex < Count) lastEntryIndex++;
			if (lastEntryIndex >= Count) lastEntryIndex = 0;

			return this[lastEntryIndex];
		}

		public IEnumerable<T> GetAllButLast()
		{
			if (Count == 0) return new T[0];

			var lastItem = default(T);
			if (lastEntryIndex >= 0 && lastEntryIndex < Count)
			{
				lastItem = this[lastEntryIndex];
			}

			return this.Where(i => !i.Equals(lastItem));
		}
	}

	class Program
	{
		static string logFileName = "uptimetracker.log";
		static string logFile = "";
		static int interval = 10000;

		static RoundRobinList<HostInfo> pingSites = new RoundRobinList<HostInfo>();

		static readonly IReadOnlyList<HostInfo> defaultPingSites = new List<HostInfo>
		{
			new HostInfo("bing.com"),
			new HostInfo("youtube.com"),
			new HostInfo("github.com"),
			new HostInfo("google.com"),
			new HostInfo("yahoo.com"),
			new HostInfo("comcast.com")
		}.AsReadOnly();

		static DateTime lastRun = DateTime.MinValue;

		static void Main(string[] args)
		{
			var showHelp = false;

			var options = new OptionSet { 
				{ "l|logfile=", "the path of the logfile", l => {
					if (!string.IsNullOrWhiteSpace(l))
						logFile = l;
					}
				},
				{ "s|server=", "a server to use in ping list (can be used more than once, if none are supplied it uses a default list).", s => {
					if (!string.IsNullOrWhiteSpace(s))
						pingSites.Add(new HostInfo(s));
					}
				},
				{ "i|interval=", $"delay (in seconds) between ping attempts (default is {interval/1000} seconds)", i => {
					if (!string.IsNullOrWhiteSpace(i))
						interval = int.Parse(i) * 1000;
					}
				},
				{ "?|h|help", "show this message and exit", h => showHelp = h != null },
			};

			List<string> extra;
			try
			{
				// parse the command line
				extra = options.Parse(args);
			}
			catch (OptionException e)
			{
				Console.Write("UptimeTracker: ");
				Console.WriteLine(e.Message);
				Console.WriteLine("Try `UptimeTracker --help' for more information.");
				return;
			}

			if (showHelp)
			{
				Console.WriteLine("Usage: UptimeTracker.exe [OPTIONS]+");
				Console.WriteLine("Periodically pings hosts in a round-robin fashion to track network connectivity.");
				Console.WriteLine();
				Console.WriteLine("Options:");
				options.WriteOptionDescriptions(Console.Out);
				return;
			}

			if (string.IsNullOrWhiteSpace(logFile))
				logFile = Path.Combine(Path.GetTempPath(), logFileName);

			if (pingSites.Count() == 0)
			{
				pingSites.AddRange(defaultPingSites);
			}

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
							var site = pingSites.GetNext();

							if (!CheckSite(site))
							{
								foreach (var s in pingSites.GetAllButLast())
								{
									CheckSite(s);
								}
							}
							lastRun = DateTime.Now;
						}
						await Task.Delay(100);
					}
				}));
		}

		private static bool CheckSite(HostInfo site)
		{
			var success = false;

			Console.Write($"Pinging {site.Name}");
			var status = "";
			var logAddress = site.IPAddress;

			try
			{
				using (var p = new Ping())
				{
					PingReply r = null;

					if (logAddress != null)
					{
						r = p.Send(logAddress, 1000);
					}
					else
					{
						r = p.Send(site.Name, 1000);
					}

					status = r.Status.ToString();
					success = r.Status == IPStatus.Success;
					if (success && logAddress == null)
					{
						logAddress = site.IPAddress = r.Address;
					}
					if (!success)
					{
						site.IPAddress = null;
					} 
				}
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

			Console.WriteLine($" ({logAddress?.ToString() ?? "?"})\t{status}");

			try
			{
				File.AppendAllText(logFile, $"{DateTime.Now:s}\t{site.Name} ({logAddress})\t{status}\r\n");
			}
			catch (Exception ex)
			{
				//go on with life...
				Console.WriteLine($"Error writing to log file: {ex.Message}");
			}

			return success;
		}


	}
}