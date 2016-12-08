using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
    public class Character
    {
		public Vector2 Position { get; set; }

		public float Speed { get; set; }

		public int Id { get; private set; }

		public bool IsHost { get; private set; }

		public Color Color { get; private set; }

		public Vector2 CurrentDirection { get; set; }

		public int CurrentInputId { get; set; }

		private Texture2D _texture;

		public Character( int id, bool isHost, Vector2 initialPosition, Color color )
		{
			this.Id = id;
			this.IsHost = isHost;
			this.Position = initialPosition;
			this.Color = color;
			this.Speed = 500.0f;
			this.CurrentInputId = 0;
		}

		public void Load( ContentManager content )
		{
			_texture = content.Load<Texture2D>( "Heal" );
		}

		public void Simulate( float elapsedTime )
		{
			this.Position += this.CurrentDirection * this.Speed * elapsedTime;
		}

		public void UpdateInput()
		{
			if( this.IsHost == false )
			{
				return;
			}

			var dir = Vector2.Zero;
			var keyboardState = Keyboard.GetState();
			if( keyboardState.IsKeyDown( Keys.W ) )
			{
				dir -= Vector2.UnitY;
			}
			if( keyboardState.IsKeyDown( Keys.S ) )
			{
				dir += Vector2.UnitY;
			}
			if( keyboardState.IsKeyDown( Keys.A ) )
			{
				dir -= Vector2.UnitX;
			}
			if( keyboardState.IsKeyDown( Keys.D ) )
			{
				dir += Vector2.UnitX;
			}

			if( dir == Vector2.Zero )
			{
				this.CurrentDirection = Vector2.Zero;
			}
			else
			{
				this.CurrentDirection = Vector2.Normalize( dir );
			}
		}

		public void Draw( SpriteBatch spriteBatch )
		{
			spriteBatch.Draw( _texture, this.Position, this.Color );
		}
    }
}
