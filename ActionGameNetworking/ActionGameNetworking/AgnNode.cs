using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ActionGameNetworking
{
	public abstract class AgnNode : IDisposable
	{
		public uint Header { get; set; }

		protected Socket Socket { get; set; }

		private byte[] _receiveBuffer;

		public AgnNode( int receiveBufferLength = 0x10000 )
		{
			this.Socket = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
			this.Socket.Blocking = false;

			_receiveBuffer = new byte[receiveBufferLength];
		}

		public void Close()
		{
			this.Dispose();
		}

		public void SendTo( byte[] buffer, string hostname, int port )
		{
			this.SendTo( buffer, 0, buffer.Length, hostname, port );
		}

		public void SendTo( byte[] buffer, int size, string hostname, int port )
		{
			this.SendTo( buffer, 0, size, hostname, port );
		}

		public void SendTo( byte[] buffer, int offset, int size, string hostname, int port )
		{
			var packet = new MemoryStream( Marshal.SizeOf( typeof( uint ) ) + size );
			var writer = new BinaryWriter( packet );
			writer.Write( this.Header );
			writer.Write( buffer, offset, size );

			this.Socket.SendTo( packet.GetBuffer(), (int)packet.Length, SocketFlags.None, new IPEndPoint( IPAddress.Parse( hostname ), port ) );
		}

		public byte[] ReceiveFrom( ref IPEndPoint remote )
		{
			if( this.Socket.Available == 0 )
			{
				return null;
			}

			EndPoint endPoint = new IPEndPoint( IPAddress.Any, IPEndPoint.MinPort );
			var bytesReceived = this.Socket.ReceiveFrom( _receiveBuffer, SocketFlags.None, ref endPoint );
			remote = endPoint as IPEndPoint;

			var data = new byte[bytesReceived];
			Array.Copy( _receiveBuffer, data, bytesReceived );
			return data;
		}

		public abstract void Update( TimeSpan elapsedTime );

		public void Dispose()
		{
			this.Socket.Close();
		}
	}
}
