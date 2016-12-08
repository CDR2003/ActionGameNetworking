using ActionGameNetworking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Xna.Framework;

namespace SampleCommon
{
	public class CreateCharacterPacket : Packet
	{
		public override Type PacketType { get { return Type.CreateCharacter; } }

		public int Id { get; set; }

		public bool IsHost { get; set; }

		public Vector2 Position { get; set; }

		public Color Color { get; set; }

		public override void ReadFromStream( BinaryReader reader )
		{
			this.Id = reader.ReadInt32();
			this.IsHost = reader.ReadBoolean();
			this.Position = reader.ReadVector2();
			this.Color = reader.ReadColor();
		}

		public override void WriteToStream( BinaryWriter writer )
		{
			writer.Write( this.Id );
			writer.Write( this.IsHost );
			writer.Write( this.Position );
			writer.Write( this.Color );
		}
	}
}
