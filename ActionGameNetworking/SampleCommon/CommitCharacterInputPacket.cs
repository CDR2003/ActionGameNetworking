using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class CommitCharacterInputPacket : Packet
	{
		public override Type PacketType
		{
			get
			{
				return Type.CommitCharacterInput;
			}
		}

		public int InputId { get; set; }

		public Vector2 Direction { get; set; }

		public override void ReadFromStream( BinaryReader reader )
		{
			this.InputId = reader.ReadInt32();
			this.Direction = reader.ReadVector2();
		}

		public override void WriteToStream( BinaryWriter writer )
		{
			writer.Write( this.InputId );
			writer.Write( this.Direction );
		}
	}
}
