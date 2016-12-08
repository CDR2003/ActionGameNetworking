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
	public abstract class AgnNode : IDisposable
	{
		public float LatencySimulation { get; set; }

		public float DropRateSimulation { get; set; }

		public uint ProtocolId { get; private set; }

		protected Socket Socket { get; set; }

		private byte[] _receiveBuffer;

		private List<AgnLaggedBuffer> _laggedBuffers;

		private Random _dropRateRandom;

		public AgnNode( uint protocolId, int receiveBufferLength = 0x1000 )
		{
			this.LatencySimulation = 0.0f;
			this.DropRateSimulation = 0.0f;
			this.ProtocolId = protocolId;

			this.Socket = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
			this.Socket.Blocking = false;

			_receiveBuffer = new byte[receiveBufferLength];
			_laggedBuffers = new List<AgnLaggedBuffer>();
			_dropRateRandom = new Random();
		}

		public void Dispose()
		{
			this.Socket.Close();
		}

		public void Close()
		{
			this.Dispose();
		}

		public virtual void Update( TimeSpan elapsedTime )
		{
			this.ReceiveData();
			this.SendLaggedBuffers();
		}

		internal void SendTo( byte[] buffer, int offset, int size, IPEndPoint remote, AgnPacketHeader header )
		{
			if( this.LatencySimulation == 0.0f )
			{
				this.DoSendTo( buffer, offset, size, remote, DateTime.Now, header );
			}
			else
			{
				_laggedBuffers.Add( new AgnLaggedBuffer( DateTime.Now, buffer, offset, size, remote, header ) );
			}
		}

		protected abstract void ProcessReceivedData( MemoryStream data, IPEndPoint remote );

		private void DoSendTo( byte[] buffer, int offset, int size, IPEndPoint remote, DateTime time, AgnPacketHeader header )
		{
			if( this.DropRateSimulation != 0.0f )
			{
				var rand = (float)_dropRateRandom.NextDouble();
				if( rand < this.DropRateSimulation )
				{
					return;
				}
			}

			var packet = new MemoryStream();
			var writer = new BinaryWriter( packet );

			header.WriteToStream( writer );
			writer.Write( buffer, offset, size );

			this.Socket.SendTo( packet.GetBuffer(), (int)packet.Length, SocketFlags.None, remote );
		}

		private void DoSendTo( AgnLaggedBuffer laggedBuffer )
		{
			this.DoSendTo( laggedBuffer.Buffer, laggedBuffer.Offset, laggedBuffer.Size, laggedBuffer.Remote, laggedBuffer.SendTime, laggedBuffer.Header );
		}

		private void SendLaggedBuffers()
		{
			if( _laggedBuffers.Count == 0 )
			{
				return;
			}

			var now = DateTime.Now;
			for( int i = 0; i < _laggedBuffers.Count; i++ )
			{
				var laggedBuffer = _laggedBuffers[i];
				if( now - laggedBuffer.SendTime > TimeSpan.FromSeconds( this.LatencySimulation ) )
				{
					this.DoSendTo( laggedBuffer );
					_laggedBuffers.RemoveAt( i );
					i--;
				}
			}
		}

		private void ReceiveData()
		{
			IPEndPoint remote = null;

			for( ;;)
			{
				var data = this.ReceiveFrom( out remote );
				if( data == null )
				{
					break;
				}

				this.ProcessReceivedData( data, remote );
			}
		}

		private MemoryStream ReceiveFrom( out IPEndPoint remote )
		{
			remote = null;

			if( this.Socket.Available == 0 )
			{
				return null;
			}

			EndPoint endPoint = new IPEndPoint( IPAddress.Any, IPEndPoint.MinPort );

			if( this.Socket.Available > _receiveBuffer.Length )
			{
				var length = Math.Max( this.Socket.Available, _receiveBuffer.Length * 2 );
				_receiveBuffer = new byte[length];
			}

			try
			{
				var bytesReceived = this.Socket.ReceiveFrom( _receiveBuffer, SocketFlags.None, ref endPoint );
				remote = endPoint as IPEndPoint;

				var data = new MemoryStream( bytesReceived );
				data.Write( _receiveBuffer, 0, bytesReceived );
				data.Position = 0;
				return data;
			}
			catch( Exception )
			{
				return null;
			}
		}
	}
}
