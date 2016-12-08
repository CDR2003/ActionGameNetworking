using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class UpdateCharacterStatePacket : Packet
	{
		public override Type PacketType
		{
			get
			{
				return Type.UpdateCharacterState;
			}
		}

		public int Id { get; set; }

		public int InputId { get; set; }

		public Vector2 Direction { get; set; }

		public Vector2 Position { get; set; }

		public override void ReadFromStream( BinaryReader reader )
		{
			this.Id = reader.ReadInt32();
			this.InputId = reader.ReadInt32();
			this.Direction = reader.ReadVector2();
			this.Position = reader.ReadVector2();
		}

		public override void WriteToStream( BinaryWriter writer )
		{
			writer.Write( this.Id );
			writer.Write( this.InputId );
			writer.Write( this.Direction );
			writer.Write( this.Position );
		}
	}
}
