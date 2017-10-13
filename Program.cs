using System;
using System.Collections.Generic;
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

		static string logFile = "uptimetracker.log";
		static int interval = 10000;

		static RoundRobinList<HostInfo> pingSites = new RoundRobinList<HostInfo>
		{
			new HostInfo("bing.com"),
			new HostInfo("youtube.com"),
			new HostInfo("github.com"),
			new HostInfo("google.com"),
			new HostInfo("yahoo.com"),
			new HostInfo("comcast.com")
		};

		static DateTime lastRun = DateTime.MinValue;

		static void Main(string[] args)
		{
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

			Console.Write($"Pinging {site.Name} ({site.IPAddress?.ToString() ?? "?"})");
			var status = "";
			var logAddress = default(IPAddress);

			try
			{
				using (var p = new Ping())
				{
					PingReply r = null;

					if (site.IPAddress != null)
					{
						r = p.Send(site.IPAddress, 1000);
					}
					else
					{
						logAddress = site.IPAddress;
						r = p.Send(site.Name, 1000);
					}

					status = r.Status.ToString();
					success = r.Status == IPStatus.Success;
					if (success && site.IPAddress == null)
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

			Console.WriteLine($"\t{status}");
			System.IO.File.AppendAllText(logFile, $"{DateTime.Now:s}\t{site.Name} ({logAddress})\t{status}\r\n");

			return success;
		}
	}
}