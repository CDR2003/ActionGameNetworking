﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class LoginPacket : Packet
	{
		public override Type PacketType
		{
			get
			{
				return Type.Login;
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
