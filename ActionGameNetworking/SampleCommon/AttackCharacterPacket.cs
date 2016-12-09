using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class AttackCharacterPacket : Packet
	{
		public override Type PacketType
		{
			get
			{
				return Type.CS_AttackCharacter;
			}
		}

		public override void ReadFromStream( BinaryReader reader )
		{
		}

		public override void WriteToStream( BinaryWriter writer )
		{
		}
	}
}
