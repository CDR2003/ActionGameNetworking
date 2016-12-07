using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		public delegate void DataReceiveDelegate( BinaryReader reader, IPEndPoint remote );

		private const float RttSmooth = 0.1f;

		private const float DropRateSmooth = 0.1f;

		private static TimeSpan MaxRtt = new TimeSpan( 0, 0, 5 );

		public float LatencySimulation { get; set; }

		public float DropRateSimulation { get; set; }

		public float CurrentRtt { get; private set; }

		public float CurrentDropRate { get; private set; }

		public event DataReceiveDelegate DataReceive;

		#region DEBUG ONLY

		public uint CurrentSequence
		{
			get
			{
				return _currentSequence;
			}
		}

		public uint CurrentAck
		{
			get
			{
				return _currentAck;
			}
		}

		#endregion

		protected Socket Socket { get; set; }

		private byte[] _receiveBuffer;

		private List<AgnLaggedBuffer> _laggedBuffers;

		private Random _lossRateRandom;

		private uint _currentSequence;

		private uint _protocolId;

		private uint _currentAck;

		private uint _currentAckBitfield;

		private List<AgnPacketSendInfo> _sendInfos;

		public AgnNode( uint protocolId, int receiveBufferLength = 0x10000 )
		{
			this.Socket = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
			this.Socket.Blocking = false;

			this.LatencySimulation = 0.0f;
			this.DropRateSimulation = 0.0f;
			this.CurrentRtt = 0.0f;

			_receiveBuffer = new byte[receiveBufferLength];
			_laggedBuffers = new List<AgnLaggedBuffer>();
			_lossRateRandom = new Random();
			_currentSequence = 0;
			_protocolId = protocolId;
			_currentAck = 0;
			_currentAckBitfield = 0;
			_sendInfos = new List<AgnPacketSendInfo>();
		}

		public void Close()
		{
			this.Dispose();
		}

		public void SendTo( byte[] buffer, IPEndPoint remote )
		{
			this.SendTo( buffer, 0, buffer.Length, remote );
		}

		public void SendTo( byte[] buffer, int size, IPEndPoint remote )
		{
			this.SendTo( buffer, 0, size, remote );
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
			var remote = new IPEndPoint( IPAddress.Parse( hostname ), port );
			this.SendTo( buffer, offset, size, remote );
		}

		public void SendTo( byte[] buffer, int offset, int size, IPEndPoint remote )
		{
			if( this.LatencySimulation == 0.0f )
			{
				this.DoSendTo( buffer, offset, size, remote, DateTime.Now );
			}
			else
			{
				_laggedBuffers.Add( new AgnLaggedBuffer( DateTime.Now, buffer, offset, size, remote ) );
			}
		}

		public virtual void Update( TimeSpan elapsedTime )
		{
			this.ReceiveData();
			this.SendLaggedBuffers();
		}

		public void Dispose()
		{
			this.Socket.Close();
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

		private void ProcessReceivedData( MemoryStream data, IPEndPoint remote )
		{
			var reader = new BinaryReader( data );
			while( data.Position < data.Length )
			{
				var header = new AgnPacketHeader();
				try
				{
					header.Read( reader );
				}
				catch( Exception )
				{
					return;
				}

				if( header.ProtocolId != _protocolId )
				{
					return;
				}

				if( header.Sequence > _currentAck )
				{
					var offset = header.Sequence - _currentAck;
					if( offset > sizeof( uint ) * 8 )
					{
						_currentAckBitfield = 0;
					}
					else
					{
						_currentAckBitfield = _currentAckBitfield << (byte)offset;
						_currentAckBitfield |= 1;
					}
					_currentAck = header.Sequence;
				}
				else if( header.Sequence > _currentAck - sizeof( uint ) * 8 )
				{
					var offset = header.Sequence - _currentAck;
					_currentAckBitfield &= (uint)( 1 << (byte)offset );
				}

				this.UpdateRtt( header.Ack );
				this.UpdateDropRate( header.Ack, header.AckBitfield );

				if( this.DataReceive != null )
				{
					this.DataReceive( reader, remote );
				}
			}
		}

		private void UpdateRtt( uint sequence )
		{
			var info = _sendInfos.Find( s => s.Sequence == sequence );

			if( info == null || info.Acked )
			{
				return;
			}

			var rttSpan = DateTime.Now - info.Time;
			var rtt = (float)rttSpan.TotalSeconds;
			this.CurrentRtt += ( rtt - this.CurrentRtt ) * 0.1f;
		}

		private void UpdateDropRate( uint ack, uint ackBitfield )
		{
			var zeroes = ackBitfield.GetNumberOfZeroes();
			if( ack < sizeof( uint ) * 8 )
			{
				zeroes -= sizeof( uint ) * 8 - (int)ack;
			}

			var dropRate = (float)zeroes / ( sizeof( uint ) * 8 );
			this.CurrentDropRate += ( dropRate - this.CurrentDropRate ) * DropRateSmooth;
		}

		private void DoSendTo( byte[] buffer, int offset, int size, IPEndPoint remote, DateTime time )
		{
			var header = this.GenerateHeader();

			if( this.DropRateSimulation != 0.0f )
			{
				var rand = (float)_lossRateRandom.NextDouble();
				if( rand < this.DropRateSimulation )
				{
					return;
				}
			}

			var packet = new MemoryStream();
			var writer = new BinaryWriter( packet );

			header.Write( writer );
			writer.Write( buffer, offset, size );

			this.Socket.SendTo( packet.GetBuffer(), (int)packet.Length, SocketFlags.None, remote );

			_sendInfos.Add( new AgnPacketSendInfo( _currentSequence, time ) );

			var now = DateTime.Now;
			_sendInfos.RemoveAll( s => now - s.Time > MaxRtt );
		}

		private void DoSendTo( AgnLaggedBuffer laggedBuffer )
		{
			this.DoSendTo( laggedBuffer.Buffer, laggedBuffer.Offset, laggedBuffer.Size, laggedBuffer.Remote, laggedBuffer.SendTime );
		}

		private AgnPacketHeader GenerateHeader()
		{
			_currentSequence++;

			var header = new AgnPacketHeader();
			header.ProtocolId = _protocolId;
			header.Sequence = _currentSequence;
			header.Ack = _currentAck;
			header.AckBitfield = _currentAckBitfield;
			return header;
		}
	}
}
