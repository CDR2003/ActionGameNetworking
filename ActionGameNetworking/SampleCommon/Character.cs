using C3.XNA;
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
    public class Character : SceneObject
    {
		public static readonly TimeSpan InterpolationInterval = new TimeSpan( 0, 0, 0, 0, 100 );

		public const float HurtFadeOutTime = 1.0f;

		public const float HealthBarHeight = 10.0f;

		public const float HealthBarOffset = 10.0f;

		public static bool DrawGhosts { get; set; }

		public float Speed { get; set; }

		public int CurrentHealth { get; set; }

		public int MaxHealth { get; private set; }

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

		public float Radius
		{
			get
			{
				return this.Size.X / 2.0f;
			}
		}

		private Texture2D _texture;

		private SpriteFont _font;

		private float _currentTime;

		private bool _hurting;

		public Character( int id, bool isHost, Vector2 initialPosition, Color color )
		{
			this.Id = id;
			this.IsHost = isHost;
			this.Position = initialPosition;
			this.Color = color;
			this.Speed = 200.0f;
			this.MaxHealth = 100;
			this.CurrentHealth = this.MaxHealth;
			this.CurrentInputId = 0;
			this.History = new SnapshotHistory<CharacterSnapshot>();
			this.InterpolationGhosts = new List<CharacterSnapshot>();

			_currentTime = 0.0f;
			_hurting = false;

			DrawGhosts = false;
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

		public override void Update( GameTime gameTime )
		{
			if( _hurting == false )
			{
				return;
			}

			_currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
			if( _currentTime > HurtFadeOutTime )
			{
				_currentTime = 0.0f;
				_hurting = false;

			}
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

		public override void Draw( SpriteBatch spriteBatch )
		{
			if( DrawGhosts )
			{
				foreach( var snapshot in this.InterpolationGhosts )
				{
					var location = snapshot.Position - this.Size / 2.0f;
					spriteBatch.Draw( _texture, location, new Color( 105, 105, 105, 128 ) );
				}
			}

			var color = Color.DimGray;
			if( _hurting )
			{
				color = new Color( Vector3.Lerp( Color.Red.ToVector3(), Color.DimGray.ToVector3(), _currentTime / HurtFadeOutTime ) );
			}

			var outerLocation = this.Position - this.Size / 2.0f;
			spriteBatch.Draw( _texture, outerLocation, this.IsHost ? Color.White : color );

			if( this.IsHost )
			{
				var innerSize = this.Size - new Vector2( 6.0f, 6.0f );
				var innerLocation = this.Position - innerSize / 2.0f;
				var innerRect = new Rectangle( innerLocation.ToPoint(), innerSize.ToPoint() );
				spriteBatch.Draw( _texture, innerRect, color );
			}

			var text = string.Format( "{0:0}\n{1:0}", this.Position.X, this.Position.Y );
			var textSize = _font.MeasureString( text );
			var textLocation = this.Position - textSize / 2.0f;
			spriteBatch.DrawString( _font, text, textLocation, Color.White );

			var healthBarPosition = this.Position - this.Size / 2.0f - new Vector2( 0.0f, HealthBarOffset + HealthBarHeight / 2.0f );
			var healthBarWidth = this.Size.X;
			spriteBatch.DrawLine( healthBarPosition, healthBarPosition + new Vector2( healthBarWidth, 0.0f ), Color.Black, HealthBarHeight );

			var currentHealthWidth = healthBarWidth * this.CurrentHealth / this.MaxHealth;
			spriteBatch.DrawLine( healthBarPosition, healthBarPosition + new Vector2( currentHealthWidth, 0.0f ), Color.SpringGreen, HealthBarHeight );
		}

		public void UpdateSnapshotHistory()
		{
			var snapshot = new CharacterSnapshot( this.Position, this.CurrentDirection );
			this.History.AddSnapshot( snapshot );
			this.History.Update();
		}

		public void Hurt()
		{
			_currentTime = 0.0f;
			_hurting = true;
		}
    }
}
