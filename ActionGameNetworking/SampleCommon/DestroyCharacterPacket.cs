using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class DestroyCharacterPacket : Packet
	{
		public override Type PacketType
		{
			get
			{
				return Type.DestroyCharacter;
			}
		}

		public int Id { get; set; }

		public override void ReadFromStream( BinaryReader reader )
		{
			this.Id = reader.ReadInt32();
		}

		public override void WriteToStream( BinaryWriter writer )
		{
			writer.Write( this.Id );
		}
	}
}
