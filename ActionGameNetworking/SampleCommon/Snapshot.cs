using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class Snapshot
	{
		public DateTime Time { get; private set; }

		public Snapshot()
		{
			this.Time = DateTime.Now;
		}
	}
}
