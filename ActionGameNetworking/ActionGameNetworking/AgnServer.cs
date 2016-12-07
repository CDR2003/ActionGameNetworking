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
		public delegate void ClientDataReceiveDelegate( BinaryReader reader, AgnConnection connection );

		public TimeSpan ConnectionTimeout { get; set; }

		public int Port { get; private set; }

		public Dictionary<IPEndPoint, AgnConnection>.ValueCollection Connections
		{
			get
			{
				return _connections.Values;
			}
		}

		public event ClientDataReceiveDelegate ClientDataReceive;

		private Dictionary<IPEndPoint, AgnConnection> _connections;

		public AgnServer( uint protocolId, int port )
			: base( protocolId )
		{
			this.ConnectionTimeout = new TimeSpan( 0, 0, 1 );
			this.Port = port;

			_connections = new Dictionary<IPEndPoint, AgnConnection>();
		}

		public void Start()
		{
			this.Socket.Bind( new IPEndPoint( IPAddress.Any, this.Port ) );
		}

		public override void Update( TimeSpan elapsedTime )
		{
			base.Update( elapsedTime );

			var remotesToRemove = new List<IPEndPoint>();

			var now = DateTime.Now;
			foreach( var connection in this.Connections )
			{
				if( now - connection.LastReceiveTime > this.ConnectionTimeout )
				{
					remotesToRemove.Add( connection.Remote );
				}
			}

			foreach( var remote in remotesToRemove )
			{
				_connections.Remove( remote );
			}
		}

		protected override void ProcessReceivedData( MemoryStream data, IPEndPoint remote )
		{
			AgnConnection connection = null;
			if( _connections.TryGetValue( remote, out connection ) == false )
			{
				connection = new AgnConnection( this, remote, this.ProtocolId );
				connection.DataReceive += this.OnConnectionDataReceived;
				_connections.Add( remote, connection );
			}

			connection.ProcessReceivedData( data, remote );
		}

		private void OnConnectionDataReceived( AgnConnection sender, BinaryReader reader )
		{
			if( this.ClientDataReceive != null )
			{
				this.ClientDataReceive( reader, sender );
			}
		}
	}
}
