﻿using ActionGameNetworking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SampleCommon
{
	public abstract class Packet : IAgnNetSerializable
	{
		public enum Type : uint
		{
			Login,
			CreateCharacter,
			DestroyCharacter,
			CommitCharacterInput,
			UpdateCharacterState,
		}

		public abstract Type PacketType { get; }

		public void Send( AgnConnection connection )
		{
			var data = this.CreatePacketData();
			connection.SendTo( data.GetBuffer(), (int)data.Length );
		}

		public void Broadcast( AgnServer server )
		{
			var data = this.CreatePacketData();
			server.Broadcast( data.GetBuffer(), (int)data.Length );
		}

		public abstract void ReadFromStream( BinaryReader reader );

		public abstract void WriteToStream( BinaryWriter writer );

		private MemoryStream CreatePacketData()
		{
			var data = new MemoryStream();
			var writer = new BinaryWriter( data );
			writer.Write( (uint)this.PacketType );
			this.WriteToStream( writer );
			return data;
		}
	}
}
