using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ActionGameNetworking
{
	public class AgnServer : AgnNode
	{
		public AgnServer( uint protocolId, int port )
			: base( protocolId )
		{
			this.Socket.Bind( new IPEndPoint( IPAddress.Any, port ) );
		}
	}
}
