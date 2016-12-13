using ActionGameNetworking;
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
			CS_Login,
			CS_CommitCharacterInput,
			CS_AttackCharacter,

			SC_CreateCharacter,
			SC_DestroyCharacter,
			SC_UpdateCharacterState,
			SC_Shoot,
			SC_Hurt,
		}

		public abstract Type PacketType { get; }

		public static TPacket Receive<TPacket>( BinaryReader reader ) where TPacket : Packet, new()
		{
			var packet = new TPacket();
			packet.ReadFromStream( reader );
			return packet;
		}

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

		public void Broadcast( AgnServer server, AgnConnection except )
		{
			var data = this.CreatePacketData();
			server.Broadcast( data.GetBuffer(), (int)data.Length, except );
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
