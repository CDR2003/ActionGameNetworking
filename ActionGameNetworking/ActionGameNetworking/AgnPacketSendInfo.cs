using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionGameNetworking
{
	internal class AgnPacketSendInfo
	{
		internal uint Sequence { get; private set; }

		internal DateTime Time { get; private set; }

		internal bool Acked { get; private set; }
		
		internal AgnPacketSendInfo( uint sequence, DateTime time )
		{
			this.Sequence = sequence;
			this.Time = time;
			this.Acked = false;
		}

		internal void Ack()
		{
			this.Acked = true;
		}

		public override string ToString()
		{
			return "#" + this.Sequence + ": " + this.Time.ToShortTimeString();
		}
	}
}
