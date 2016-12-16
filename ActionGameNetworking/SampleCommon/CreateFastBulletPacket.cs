using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class CreateFastBulletPacket : Packet
	{
		public override Type PacketType
		{
			get
			{
				return Type.SC_CreateFastBullet;
			}
		}

		public int LocalId { get; set; }

		public int RemoteId { get; set; }

		public Vector2 Position { get; set; }

		public Vector2 Direction { get; set; }

		public int ShooterId { get; set; }

		public override void ReadFromStream( BinaryReader reader )
		{
			this.LocalId = reader.ReadInt32();
			this.RemoteId = reader.ReadInt32();
			this.Position = reader.ReadVector2();
			this.Direction = reader.ReadVector2();
			this.ShooterId = reader.ReadInt32();
		}

		public override void WriteToStream( BinaryWriter writer )
		{
			writer.Write( this.LocalId );
			writer.Write( this.RemoteId );
			writer.Write( this.Position );
			writer.Write( this.Direction );
			writer.Write( this.ShooterId );
		}
	}
}
