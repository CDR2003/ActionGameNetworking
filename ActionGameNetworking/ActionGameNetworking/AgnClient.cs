using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ActionGameNetworking
{
	public class AgnClient : AgnNode
	{
		public delegate void ServerDataReceiveDelegate( BinaryReader reader );

		public AgnConnection Connection { get; private set; }

		public float CurrentRtt
		{
			get
			{
				return this.Connection.CurrentRtt;
			}
		}

		public float CurrentDropRate
		{
			get
			{
				return this.Connection.CurrentDropRate;
			}
		}

		#region DEBUG ONLY

		public uint CurrentSequence
		{
			get
			{
				return this.Connection.CurrentSequence;
			}
		}

		public uint CurrentAck
		{
			get
			{
				return this.Connection.CurrentAck;
			}
		}

		#endregion

		public event ServerDataReceiveDelegate ServerDataReceive;

		public AgnClient( uint protocolId )
			: base( protocolId )
		{
		}

		public void Connect( string hostname, int port )
		{
			var remote = new IPEndPoint( IPAddress.Parse( hostname ), port );
			this.Connection = new AgnConnection( this, remote, this.ProtocolId );
			this.Connection.DataReceive += this.OnConnectionDataReceived;
		}

		public void SendTo( byte[] buffer )
		{
			this.SendTo( buffer, 0, buffer.Length );
		}

		public void SendTo( byte[] buffer, int size )
		{
			this.SendTo( buffer, 0, size );
		}

		public void SendTo( byte[] buffer, int offset, int size )
		{
			this.Connection.SendTo( buffer, offset, size );
		}

		protected override void ProcessReceivedData( MemoryStream data, IPEndPoint remote )
		{
			this.Connection.ProcessReceivedData( data, remote );
		}

		private void OnConnectionDataReceived( AgnConnection sender, BinaryReader reader )
		{
			if( this.ServerDataReceive != null )
			{
				this.ServerDataReceive( reader );
			}
		}
	}
}
