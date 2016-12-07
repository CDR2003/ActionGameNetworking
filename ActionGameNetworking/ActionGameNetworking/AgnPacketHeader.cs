using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionGameNetworking
{
	internal class AgnPacketHeader
	{
		internal uint ProtocolId { get; set; }

		internal uint Sequence { get; set; }

		internal uint Ack { get; set; }

		internal uint AckBitfield { get; set; }

		internal void Read( BinaryReader reader )
		{
			this.ProtocolId = reader.ReadUInt32();
			this.Sequence = reader.ReadUInt32();
			this.Ack = reader.ReadUInt32();
			this.AckBitfield = reader.ReadUInt32();
		}

		internal void Write( BinaryWriter writer )
		{
			writer.Write( this.ProtocolId );
			writer.Write( this.Sequence );
			writer.Write( this.Ack );
			writer.Write( this.AckBitfield );
		}
	}
}
