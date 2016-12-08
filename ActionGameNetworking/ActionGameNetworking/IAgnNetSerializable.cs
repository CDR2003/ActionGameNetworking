using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionGameNetworking
{
	public interface IAgnNetSerializable
	{
		void WriteToStream( BinaryWriter writer );

		void ReadFromStream( BinaryReader reader );
	}
}
