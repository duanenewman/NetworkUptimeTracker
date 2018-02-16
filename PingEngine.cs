﻿using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace UptimeTracker
{
	public class PingEngine : IDisposable
	{
		public PingEngineConfig Config { get; set; }
		private CancellationTokenSource cancelSource = new CancellationTokenSource();

		public void StartPinging()
		{

			var cancelToken = cancelSource.Token;

			Task.Run(async () =>
			{
				while (true)
				{
					var site = Config.PingSites.GetNext();

					if (!CheckSite(site, Config.MessageLogger))
					{
						foreach (var s in Config.PingSites.GetAllButLast())
						{
							CheckSite(s, Config.MessageLogger);
						}
					}

					await Task.Delay(Config.Interval, cancelToken);
				}
			}, cancelToken);
		}

		public void StopPinging()
		{
			cancelSource.Cancel();
		}

		public void Dispose()
		{
			cancelSource?.Dispose();
		}

		private static bool CheckSite(HostInfo hostInfo, Action<string> logMessage)
		{
			var success = false;

			Console.Write($"Pinging {hostInfo.Name}");
			var status = "";
			var logAddress = hostInfo.IPAddress;

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
						r = p.Send(hostInfo.Name, 1000);
					}

					status = r.Status.ToString();
					success = r.Status == IPStatus.Success;
					if (success && logAddress == null)
					{
						logAddress = hostInfo.IPAddress = r.Address;
					}
					if (!success)
					{
						hostInfo.IPAddress = null;
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

			logMessage($"{DateTime.Now:s}\t{hostInfo.Name} ({logAddress})\t{status}\r\n");

			return success;
		}
	}
}