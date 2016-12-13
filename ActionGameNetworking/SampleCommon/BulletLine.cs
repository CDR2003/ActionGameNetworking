using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using C3.XNA;
using System.Diagnostics;

namespace SampleCommon
{
	public class BulletLine : SceneObject
	{
		public const float Length = 1000.0f;

		public const float FadeOutTime = 0.1f;

		public const int Damage = 10;

		private float _currentTime;

		private Vector2 _direction;

		public BulletLine( Vector2 direction )
		{
			_currentTime = 0.0f;
			_direction = direction;
		}

		public Character Shoot( Character shooter )
		{
			Character minCharacter = null;
			var minDistance = float.MaxValue;
			foreach( var obj in SceneManager.Instance.Objects )
			{
				var character = obj as Character;
				if( character == null || character == shooter )
				{
					continue;
				}

				var distance = Intersects( character.Position, character.Radius, this.Position, _direction );
				if( distance != null && distance.Value < minDistance )
				{
					minDistance = distance.Value;
					minCharacter = character;
				}
			}

			return minCharacter;
		}

		public float? Hit( Vector2 characterPosition, float characterRadius )
		{
			return Intersects( characterPosition, characterRadius, this.Position, _direction );
		}

		public override void Draw( SpriteBatch spriteBatch )
		{
			var targetPosition = this.Position + _direction * Length;
			var alpha = 1.0f - _currentTime / FadeOutTime;
			var color = Color.Yellow;
			color.R = (byte)( alpha * 255 );
			color.G = (byte)( alpha * 255 );
			spriteBatch.DrawLine( this.Position, this.Position + _direction * Length, color );
		}

		public override void Update( GameTime gameTime )
		{
			_currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
			if( _currentTime > FadeOutTime )
			{
				SceneObject.Destroy( this );
			}
		}

		private static float? Intersects( Vector2 center, float radius, Vector2 origin, Vector2 direction )
		{
			var a = direction.LengthSquared();
			var b = 2.0f * ( Vector2.Dot( origin, direction ) - Vector2.Dot( direction, center ) );
			var c = origin.LengthSquared() + center.LengthSquared() - 2.0f * Vector2.Dot( origin, center ) - radius * radius;
			var delta = b * b - 4.0f * a * c;

			if( delta < 0.0f )
			{
				return null;
			}

			var t0 = -b - (float)Math.Sqrt( delta ) / 2.0f / a;
			var t1 = -b + (float)Math.Sqrt( delta ) / 2.0f / a;
			if( t1 < 0.0f )
			{
				return null;
			}

			if( t0 < 0.0f )
			{
				return t1;
			}
			else
			{
				return t0;
			}
		}
	}
}
