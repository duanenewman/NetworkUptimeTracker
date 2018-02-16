using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace UptimeTracker
{
	class Program
	{

		static void Main(string[] args)
		{
			var config = PingEngineConfig.FromArgs(args);
			if (config == null) return;

			void LogMesssageAction(string s)
			{
				try
				{
					File.AppendAllText(config.LogFile, s);
				}
				catch (Exception ex)
				{
					//go on with life...
					Console.WriteLine($"Error writing to log file: {ex.Message}");
				}
			}

			config.MessageLogger = LogMesssageAction;
			
			var pingEngine = new PingEngine()
			{
				Config = config
			};

			pingEngine.StartPinging();

			Console.ReadKey(true);

			pingEngine.StopPinging();

		}

	}
}