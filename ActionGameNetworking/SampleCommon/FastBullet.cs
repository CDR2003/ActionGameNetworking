using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace SampleCommon
{
	public class FastBullet : SceneObject
	{
		public const float Speed = 1000.0f;

		public const int Damage = 15;

		public int Id { get; set; }

		public Character Shooter { get; private set; }

		public bool Visible { get; set; }

		public Vector2 Direction { get; set; }

		private Texture2D _texture;

		public FastBullet( int id, Character shooter )
		{
			this.Id = id;
			this.Shooter = shooter;
			this.Visible = true;
		}

		public void Load( ContentManager content )
		{
			_texture = content.Load<Texture2D>( "FastBullet" );
		}

		public void Simulate( float time )
		{
			this.Position += this.Direction * Speed * time;
		}

		public override void Draw( SpriteBatch spriteBatch )
		{
			if( this.Visible == false )
			{
				return;
			}

			var location = this.Position - _texture.Bounds.Size.ToVector2();
			spriteBatch.Draw( _texture, location, Color.White );
		}

		public override void Update( GameTime gameTime )
		{
		}
	}
}
