using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UptimeTracker
{
	class Program
	{
		static string defaultLogFileName = "uptimetracker.log";

		static readonly IReadOnlyList<HostInfo> defaultPingSites = new List<HostInfo>
		{
			new HostInfo("bing.com"),
			new HostInfo("youtube.com"),
			new HostInfo("github.com"),
			new HostInfo("google.com"),
			new HostInfo("yahoo.com"),
			new HostInfo("comcast.com")
		}.AsReadOnly();

		static void Main(string[] args)
		{
			var pingSites = new RoundRobinList<HostInfo>();
			var logFile = "";
			var interval = 10000;
			var showHelp = false;

			var options = new OptionSet
			{
				{
					"l|logfile=", "the path of the logfile", l =>
					{
						if (!string.IsNullOrWhiteSpace(l))
							logFile = l;
					}
				},
				{
					"s|server=",
					"a server to use in ping list (can be used more than once, if none are supplied it uses a default list).", s =>
					{
						if (!string.IsNullOrWhiteSpace(s))
							pingSites.Add(new HostInfo(s));
					}
				},
				{
					"i|interval=", $"delay (in seconds) between ping attempts (default is {interval / 1000} seconds)", i =>
					{
						if (!string.IsNullOrWhiteSpace(i))
							interval = int.Parse(i) * 1000;
					}
				},
				{"?|h|help", "show this message and exit", h => showHelp = h != null},
			};

			try
			{
				// parse the command line
				var extra = options.Parse(args);
			}
			catch (OptionException e)
			{
				Console.Write("UptimeTracker: ");
				Console.WriteLine(e.Message);
				Console.WriteLine("Try 'UptimeTracker --help' for more information.");
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
				logFile = Path.Combine(Path.GetTempPath(), defaultLogFileName);

			if (!pingSites.Any())
			{
				pingSites.AddRange(defaultPingSites);
			}

			void LogMesssageAction(string s)
			{
				try
				{
					File.AppendAllText(logFile, s);
				}
				catch (Exception ex)
				{
					//go on with life...
					Console.WriteLine($"Error writing to log file: {ex.Message}");
				}
			}

			var pingEngine = new PingEngine()
			{
				MessageLogger = LogMesssageAction,
				PingSites = pingSites,
				Interval = interval
			};

			pingEngine.StartPinging();

			Console.ReadKey(true);

			pingEngine.StopPinging();

		}
	}
}