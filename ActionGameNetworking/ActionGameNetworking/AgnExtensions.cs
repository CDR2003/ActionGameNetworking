using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionGameNetworking
{
	public static class AgnExtensions
	{
		public static int GetNumberOfOnes( this uint n )
		{
			var count = 0;
			for( byte i = 0; i < sizeof( uint ) * 8; i++ )
			{
				if( ( n & ( 1 << i ) ) > 0 )
				{
					count++;
				}
			}
			return count;
		}

		public static int GetNumberOfZeroes( this uint n )
		{
			return sizeof( uint ) * 8 - n.GetNumberOfOnes();
		}

		public static void AppendFormatLine( this StringBuilder sb, string format, params object[] args )
		{
			sb.AppendFormat( format, args );
			sb.AppendLine();
		}
	}
}
