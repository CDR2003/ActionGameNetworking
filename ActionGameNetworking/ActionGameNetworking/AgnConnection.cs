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
	public class AgnConnection
	{
		public delegate void DataReceiveDelegate( AgnConnection sender, BinaryReader reader );

		private const float RttSmooth = 0.1f;

		private const float DropRateSmooth = 0.1f;

		private static TimeSpan MaxRtt = new TimeSpan( 0, 0, 5 );

		public float CurrentRtt { get; private set; }

		public float CurrentDropRate { get; private set; }

		public IPEndPoint Remote { get; private set; }

		public DateTime LastReceiveTime { get; private set; }

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

		private AgnNode _node;

		private uint _currentSequence;

		private uint _currentAck;

		private uint _currentAckBitfield;

		private List<AgnPacketSendInfo> _sendInfos;

		private uint _protocolId;

		public AgnConnection( AgnNode node, IPEndPoint remote, uint protocolId, int receiveBufferLength = 0x1000 )
		{
			this.CurrentRtt = 0.0f;
			this.Remote = remote;

			_node = node;
			_currentSequence = 0;
			_currentAck = 0;
			_currentAckBitfield = 0;
			_sendInfos = new List<AgnPacketSendInfo>();
			_protocolId = protocolId;
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
			var now = DateTime.Now;

			var header = this.GenerateHeader();
			_node.SendTo( buffer, offset, size, this.Remote, header );

			_sendInfos.Add( new AgnPacketSendInfo( _currentSequence, now ) );
			_sendInfos.RemoveAll( s => now - s.Time > MaxRtt );
		}

		internal void ProcessReceivedData( MemoryStream data, IPEndPoint remote )
		{
			var reader = new BinaryReader( data );
			while( data.Position < data.Length )
			{
				var header = new AgnPacketHeader();
				try
				{
					header.ReadFromStream( reader );
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
					this.DataReceive( this, reader );
				}
			}
		}

		private void UpdateRtt( uint sequence )
		{
			this.LastReceiveTime = DateTime.Now;

			var info = _sendInfos.Find( s => s.Sequence == sequence );
			if( info == null || info.Acked )
			{
				return;
			}

			var rttSpan = this.LastReceiveTime - info.Time;
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
