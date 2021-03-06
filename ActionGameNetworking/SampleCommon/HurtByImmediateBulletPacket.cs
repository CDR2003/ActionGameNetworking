﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class HurtByImmediateBulletPacket : Packet
	{
		public override Type PacketType
		{
			get
			{
				return Type.SC_HurtByImmediateBullet;
			}
		}

		public int AttackerId { get; set; }

		public int VictimId { get; set; }

		public int VictimHealth { get; set; }

		public override void ReadFromStream( BinaryReader reader )
		{
			this.AttackerId = reader.ReadInt32();
			this.VictimId = reader.ReadInt32();
			this.VictimHealth = reader.ReadInt32();
		}

		public override void WriteToStream( BinaryWriter writer )
		{
			writer.Write( this.AttackerId );
			writer.Write( this.VictimId );
			writer.Write( this.VictimHealth );
		}
	}
}
