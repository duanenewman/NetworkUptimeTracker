using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace UptimeTracker
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var config = PingEngineConfig.FromArgs(args);
			if (config == null) return;
			
			config.MessageLogger = (hostInfo, pingState, status) =>
			{
				if (pingState == PingState.Started)
				{
					Console.Write($"Pinging {hostInfo.Name}");
					return;
				}

				Console.WriteLine($" ({hostInfo.IPAddress?.ToString() ?? "?"})\t{status}");

				try
				{
					File.AppendAllText(config.LogFile, ($"{DateTime.Now:s}\t{hostInfo.Name} ({hostInfo.IPAddress})\t{status}\r\n"));
				}
				catch (Exception ex)
				{
					//go on with life...
					Console.WriteLine($"Error writing to log file: {ex.Message}");
				}
			};
			
			var pingEngine = new PingEngine(config);

			pingEngine.StartPinging();

			Console.ReadKey(true);

			pingEngine.StopPinging();

		}

	}
}