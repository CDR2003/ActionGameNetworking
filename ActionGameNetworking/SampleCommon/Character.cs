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
		public static readonly TimeSpan InterpolationInterval = new TimeSpan( 0, 0, 0, 0, 100 );

		public static bool DrawGhosts { get; set; }

		public Vector2 Position { get; set; }

		public float Speed { get; set; }

		public int Id { get; private set; }

		public bool IsHost { get; private set; }

		public Color Color { get; private set; }

		public SnapshotHistory<CharacterSnapshot> History { get; private set; }

		public Vector2 CurrentDirection { get; set; }

		public int CurrentInputId { get; set; }

		public List<CharacterSnapshot> InterpolationGhosts { get; private set; }

		public Vector2 Size
		{
			get
			{
				return _texture.Bounds.Size.ToVector2();
			}
		}

		private Texture2D _texture;

		private SpriteFont _font;

		public Character( int id, bool isHost, Vector2 initialPosition, Color color )
		{
			this.Id = id;
			this.IsHost = isHost;
			this.Position = initialPosition;
			this.Color = color;
			this.Speed = 500.0f;
			this.CurrentInputId = 0;
			this.History = new SnapshotHistory<CharacterSnapshot>();
			this.InterpolationGhosts = new List<CharacterSnapshot>();

			DrawGhosts = true;
		}

		public void Load( ContentManager content )
		{
			_texture = content.Load<Texture2D>( "Heal" );
			_font = content.Load<SpriteFont>( "Arial" );
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
			if( DrawGhosts )
			{
				foreach( var snapshot in this.InterpolationGhosts )
				{
					var location = snapshot.Position - this.Size / 2.0f;
					spriteBatch.Draw( _texture, location, new Color( 105, 105, 105, 128 ) );
				}
			}

			var outerLocation = this.Position - this.Size / 2.0f;
			spriteBatch.Draw( _texture, outerLocation, this.IsHost ? Color.White : Color.DimGray );

			if( this.IsHost )
			{
				var innerSize = this.Size - new Vector2( 6.0f, 6.0f );
				var innerLocation = this.Position - innerSize / 2.0f;
				var innerRect = new Rectangle( innerLocation.ToPoint(), innerSize.ToPoint() );
				spriteBatch.Draw( _texture, innerRect, Color.DimGray );
			}

			var text = string.Format( "{0:0}\n{1:0}", this.Position.X, this.Position.Y );
			var textSize = _font.MeasureString( text );
			var textLocation = this.Position - textSize / 2.0f;
			spriteBatch.DrawString( _font, text, textLocation, Color.White );
		}

		public void UpdateSnapshotHistory()
		{
			var snapshot = new CharacterSnapshot( this.Position, this.CurrentDirection );
			this.History.AddSnapshot( snapshot );
			this.History.Update();
		}
    }
}
