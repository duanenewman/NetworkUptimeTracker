using System.Collections.Generic;
using System.Linq;

namespace UptimeTracker
{
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
}