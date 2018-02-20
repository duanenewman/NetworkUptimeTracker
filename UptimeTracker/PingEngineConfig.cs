using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Options;

namespace UptimeTracker
{
	public class PingEngineConfig
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

		public Action<HostInfo, PingState, string> MessageLogger { get; set; }
		public int Interval { get; set; } = 10000;
		public string LogFile { get; set; }

		public RoundRobinList<HostInfo> PingSites = new RoundRobinList<HostInfo>();

		public static PingEngineConfig FromArgs(string[] args)
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
				return null;
			}

			if (showHelp)
			{
				Console.WriteLine("Usage: UptimeTracker.exe [OPTIONS]+");
				Console.WriteLine("Periodically pings hosts in a round-robin fashion to track network connectivity.");
				Console.WriteLine();
				Console.WriteLine("Options:");
				options.WriteOptionDescriptions(Console.Out);
				return null;
			}

			if (string.IsNullOrWhiteSpace(logFile))
				logFile = Path.Combine(Path.GetTempPath(), defaultLogFileName);

			if (!pingSites.Any())
			{
				pingSites.AddRange(defaultPingSites);
			}

			var config = new PingEngineConfig()
			{
				PingSites = pingSites,
				Interval = interval,
				LogFile = logFile
			};
			return config;
		}
	}
}