using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class ShootImmediateBulletPacket : Packet
	{
		public override Type PacketType
		{
			get
			{
				return Type.CS_ShootImmediateBullet;
			}
		}

		public Vector2 Direction { get; set; }

		public override void ReadFromStream( BinaryReader reader )
		{
			this.Direction = reader.ReadVector2();
		}

		public override void WriteToStream( BinaryWriter writer )
		{
			writer.Write( this.Direction );
		}
	}
}
