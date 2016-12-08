using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public static class Extensions
	{
		public static float NextFloat( this Random random )
		{
			return (float)random.NextDouble();
		}

		public static float NextFloat( this Random random, float min, float max )
		{
			return min + random.NextFloat() * ( max - min );
		}

		public static Vector2 NextVector2( this Random random )
		{
			return new Vector2( random.NextFloat(), random.NextFloat() );
		}

		public static Color NextColor( this Random random, bool randomAlpha = false )
		{
			return new Color( random.NextFloat(), random.NextFloat(), random.NextFloat(), randomAlpha ? random.NextFloat() : 1.0f );
		}

		public static Vector2 ReadVector2( this BinaryReader reader )
		{
			var x = reader.ReadSingle();
			var y = reader.ReadSingle();
			return new Vector2( x, y );
		}

		public static void Write( this BinaryWriter writer, Vector2 value )
		{
			writer.Write( value.X );
			writer.Write( value.Y );
		}

		public static Color ReadColor( this BinaryReader reader )
		{
			var r = reader.ReadByte();
			var g = reader.ReadByte();
			var b = reader.ReadByte();
			var a = reader.ReadByte();
			return new Color( r, g, b, a );
		}

		public static void Write( this BinaryWriter writer, Color value )
		{
			writer.Write( value.R );
			writer.Write( value.G );
			writer.Write( value.B );
			writer.Write( value.A );
		}
	}
}
