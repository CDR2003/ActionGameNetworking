using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class CreateImmediateBulletPacket : Packet
	{
		public override Type PacketType
		{
			get
			{
				return Type.SC_CreateImmediateBullet;
			}
		}

		public Vector2 BulletOrigin { get; set; }

		public Vector2 BulletDirection { get; set; }

		public override void ReadFromStream( BinaryReader reader )
		{
			this.BulletOrigin = reader.ReadVector2();
			this.BulletDirection = reader.ReadVector2();
		}

		public override void WriteToStream( BinaryWriter writer )
		{
			writer.Write( this.BulletOrigin );
			writer.Write( this.BulletDirection );
		}
	}
}
