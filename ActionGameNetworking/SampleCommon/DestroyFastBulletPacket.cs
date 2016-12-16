using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class DestroyFastBulletPacket : Packet
	{
		public override Type PacketType
		{
			get
			{
				return Type.SC_DestroyFastBullet;
			}
		}

		public int BulletId { get; set; }

		public override void ReadFromStream( BinaryReader reader )
		{
			this.BulletId = reader.ReadInt32();
		}

		public override void WriteToStream( BinaryWriter writer )
		{
			writer.Write( this.BulletId );
		}
	}
}
