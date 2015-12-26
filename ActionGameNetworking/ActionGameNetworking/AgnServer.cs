using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ActionGameNetworking
{
	public class AgnServer : AgnNode
	{
		public AgnServer( int port )
		{
			this.Socket.Bind( new IPEndPoint( IPAddress.Any, port ) );
		}

		public override void Update( TimeSpan elapsedTime )
		{
			IPEndPoint remote = null;
			var data = this.ReceiveFrom( ref remote );
			if( data != null )
			{
				Debug.WriteLine( data.Length );
			}
		}
	}
}
