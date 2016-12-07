using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ActionGameNetworking
{
	internal struct AgnLaggedBuffer
	{
		internal DateTime SendTime;

		internal byte[] Buffer;

		internal int Offset;

		internal int Size;

		internal IPEndPoint Remote;

		internal AgnLaggedBuffer( DateTime sendTime, byte[] buffer, int offset, int size, IPEndPoint remote )
		{
			this.SendTime = sendTime;
			this.Buffer = buffer;
			this.Offset = offset;
			this.Size = size;
			this.Remote = remote;
		}
	}
}
