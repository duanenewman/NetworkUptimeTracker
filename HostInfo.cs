using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace UptimeTracker
{
	public class HostInfo
	{
		public string Name { get; set; }
		public IPAddress IPAddress { get; set; }

		public HostInfo(string name)
		{
			Name = name;
		}
	}
}