using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class CharacterSnapshot : Snapshot
	{
		public Vector2 Position { get; set; }

		public Vector2 Direction { get; set; }

		public CharacterSnapshot()
			: this( Vector2.Zero, Vector2.Zero )
		{
		}

		public CharacterSnapshot( Vector2 position, Vector2 direction )
		{
			this.Position = position;
			this.Direction = direction;
		}
	}
}
