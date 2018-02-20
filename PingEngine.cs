using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace UptimeTracker
{
	public class PingEngine : IDisposable
	{
		public PingEngineConfig Config { get; }
		private CancellationTokenSource cancelSource;

		private readonly object startLock = new object();
		private bool isStarted = false;

		public PingEngine(PingEngineConfig config)
		{
			Config = config;
		}

		public void StartPinging()
		{
			lock (startLock)
			{
				if (isStarted) return;
				isStarted = true;
			}

			cancelSource = new CancellationTokenSource();
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
			lock (startLock)
			{
				if (!isStarted) return;
			}
			cancelSource.Cancel();
			lock (startLock)
			{
				isStarted = false;
			}
		}

		public void Dispose()
		{
			cancelSource?.Dispose();
		}

		private static bool CheckSite(HostInfo hostInfo, Action<HostInfo, PingState, string> updateStatus)
		{
			var success = false;

			updateStatus?.Invoke(hostInfo, PingState.Started, string.Empty);

			var status = "";
			var logAddress = hostInfo.IPAddress;
			var pingState = PingState.Failure;
			try
			{
				using (var p = new Ping())
				{
					var r = logAddress != null
						? p.Send(logAddress, 1000)
						: p.Send(hostInfo.Name, 1000);

					status = r.Status.ToString();
					success = r.Status == IPStatus.Success;
					if (!success)
					{
						hostInfo.IPAddress = null;
					}
					else
					{
						pingState = PingState.Success;
						if (logAddress == null)
						{
							hostInfo.IPAddress = r.Address;
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (ex.InnerException is SocketException se)
				{
					status = se.SocketErrorCode.ToString();
				}
				else
				{
					status = ex.Message;
				}
			}

			updateStatus?.Invoke(hostInfo, pingState, status);

			return success;
		}
	}
}