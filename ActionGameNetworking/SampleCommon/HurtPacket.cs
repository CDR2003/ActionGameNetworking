using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class HurtPacket : Packet
	{
		public override Type PacketType
		{
			get
			{
				return Type.SC_Hurt;
			}
		}

		public int VictimId { get; set; }

		public override void ReadFromStream( BinaryReader reader )
		{
			this.VictimId = reader.ReadInt32();
		}

		public override void WriteToStream( BinaryWriter writer )
		{
			writer.Write( this.VictimId );
		}
	}
}
