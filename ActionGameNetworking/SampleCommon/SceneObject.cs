using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public abstract class SceneObject
	{
		public Vector2 Position { get; set; }

		public SceneObject()
		{
			SceneManager.Instance.AddObject( this );
		}

		public static void Destroy( SceneObject obj )
		{
			SceneManager.Instance.RemoveObject( obj );
		}

		public abstract void Update( GameTime gameTime );

		public abstract void Draw( SpriteBatch spriteBatch );
	}
}
