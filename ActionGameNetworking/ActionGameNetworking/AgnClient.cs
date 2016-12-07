using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ActionGameNetworking
{
	public class AgnClient : AgnConnection
	{
		public AgnClient( uint protocolId )
			: base( protocolId )
		{
		}
	}
}
