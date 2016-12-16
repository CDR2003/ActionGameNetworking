using ActionGameNetworking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SampleCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace SampleGame
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class SampleGame : Game
	{
		public enum BulletType
		{
			Immediate,
			Fast,
			Slow
		}

		public BulletType CurrentBulletType = BulletType.Fast;

		public const float FrameInterval = 1.0f / 60.0f;

		private GraphicsDeviceManager _graphics;

		private SpriteBatch _spriteBatch;

		private AgnClient _client;

		private TimeSpan _currentTime = TimeSpan.Zero;

		private SpriteFont _font;

		private Dictionary<int, Character> _characters;

		private Character _hostCharacter;

		private int _currentInputId;

		private List<CommitCharacterInputPacket> _previousInputPackets;

		private float _currentShootTime = BulletLine.ShootInterval;

		private int _currentBulletId = 0;

		private Dictionary<int, FastBullet> _bullets = new Dictionary<int, FastBullet>();

		private Dictionary<int, FastBullet> _localBullets = new Dictionary<int, FastBullet>();

		public SampleGame()
		{
			_graphics = new GraphicsDeviceManager( this );
			_characters = new Dictionary<int, Character>();
			_currentInputId = 0;
			_previousInputPackets = new List<CommitCharacterInputPacket>();
			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			this.IsMouseVisible = true;

			_client = new AgnClient( 0x0bad );
			_client.LatencySimulation = 0.2f;
			_client.ServerDataReceive += OnServerDataReceived;

			_client.Connect( "192.168.0.104", 30000 );

			var login = new LoginPacket();
			login.Send( _client.Connection );

			base.Initialize();
		}

		private void OnServerDataReceived( BinaryReader reader )
		{
			var type = (Packet.Type)reader.ReadUInt32();
			switch( type )
			{
				case Packet.Type.SC_CreateCharacter:
					this.ProcessCreateCharacter( reader );
					break;
				case Packet.Type.SC_UpdateCharacterState:
					this.ProcessUpdateCharacterState( reader );
					break;
				case Packet.Type.SC_DestroyCharacter:
					this.ProcessDestroyCharacter( reader );
					break;
				case Packet.Type.SC_CreateImmediateBullet:
					this.ProcessCreateImmediateBullet( reader );
					break;
				case Packet.Type.SC_CreateFastBullet:
					this.ProcessCreateFastBullet( reader );
					break;
				case Packet.Type.SC_DestroyFastBullet:
					this.ProcessDestroyFastBullet( reader );
					break;
				case Packet.Type.SC_HurtByImmediateBullet:
					this.ProcessHurtByImmediateBullet( reader );
					break;
				case Packet.Type.SC_HurtByFastBullet:
					this.ProcessHurtByFastBullet( reader );
					break;
				default:
					throw new Exception();
			}
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch( GraphicsDevice );
			_font = this.Content.Load<SpriteFont>( "Arial" );
		}

		protected override void UnloadContent()
		{
		}

		protected override void Update( GameTime gameTime )
		{
			if( GamePad.GetState( PlayerIndex.One ).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown( Keys.Escape ) )
			{
				this.Exit();
			}

			var keys = Keyboard.GetState().GetPressedKeys();
			if( keys.Length > 0 )
			{
				var key = keys[0];
				if( Keys.D0 <= key && key <= Keys.D9 )
				{
					var number = key - Keys.D0;
					_client.LatencySimulation = number * 0.05f;
				}
			}

			if( Keyboard.GetState().IsKeyDown( Keys.G ) )
			{
				Character.DrawGhosts = !Character.DrawGhosts;
			}

			_currentTime += gameTime.ElapsedGameTime;
			if( _currentTime.TotalSeconds > FrameInterval )
			{
				_currentTime = TimeSpan.Zero;

				_client.Update();

				if( _hostCharacter != null )
				{
					_hostCharacter.UpdateInput();
					_hostCharacter.Simulate( FrameInterval );
				}

				foreach( var character in _characters.Values )
				{
					if( character == _hostCharacter )
					{
						continue;
					}
					this.InterpolateCharacter( character );
				}

				this.UpdateShooting( FrameInterval );
				
				foreach( var bullet in _bullets.Values )
				{
					bullet.Simulate( FrameInterval );
					if( this.CheckBulletHit( bullet ) )
					{
						bullet.Visible = false;
					}
				}

				foreach( var bullet in _localBullets.Values )
				{
					bullet.Simulate( FrameInterval );
					if( this.CheckBulletHit( bullet ) )
					{
						bullet.Visible = false;
					}
				}

				this.CommitHostCharacter();
			}

			SceneManager.Instance.Update( gameTime );

			base.Update( gameTime );
		}

		protected override void Draw( GameTime gameTime )
		{
			GraphicsDevice.Clear( Color.CornflowerBlue );

			var sb = new StringBuilder();
			sb.AppendLine( "Client" );
			sb.AppendFormatLine( "Latency Simulation: {0:0}ms", _client.LatencySimulation * 1000.0f );
			sb.AppendFormatLine( "Drop Rate Simulation: {0:0}%", _client.DropRateSimulation * 100.0f );
			sb.AppendFormatLine( "Sequence: {0}", _client.CurrentSequence );
			sb.AppendFormatLine( "RTT: {0:0}ms", _client.CurrentRtt * 1000.0f );
			sb.AppendFormatLine( "Drop Rate: {0:0}%", _client.CurrentDropRate * 100.0f );

			_spriteBatch.Begin();

			SceneManager.Instance.Draw( _spriteBatch );

			_spriteBatch.DrawString( _font, sb.ToString(), Vector2.Zero, this.IsActive ? Color.White : Color.LightGray );
			_spriteBatch.End();

			base.Draw( gameTime );
		}

		private void UpdateShooting( float elapsedTime )
		{
			_currentShootTime += elapsedTime;

			var buttonState = Mouse.GetState().LeftButton;
			if( buttonState == ButtonState.Released )
			{
				return;
			}

			if( _hostCharacter == null )
			{
				return;
			}

			if( this.IsActive == false )
			{
				return;
			}

			if( _currentShootTime < BulletLine.ShootInterval )
			{
				return;
			}

			_currentShootTime = 0.0f;

			var mousePosition = Mouse.GetState().Position.ToVector2();
			var direction = Vector2.Normalize( mousePosition - _hostCharacter.Position );

			switch( this.CurrentBulletType )
			{
				case BulletType.Immediate:
					this.ShootImmediateBullet( direction );
					break;
				case BulletType.Fast:
					this.ShootFastBullet( direction );
					break;
				case BulletType.Slow:
					break;
				default:
					break;
			}
		}

		private void ShootImmediateBullet( Vector2 direction )
		{
			var line = new BulletLine( direction );
			line.Position = _hostCharacter.Position;

			var victim = line.Shoot( _hostCharacter );
			if( victim != null )
			{
				victim.Hurt();
			}

			var packet = new ShootImmediateBulletPacket();
			packet.Direction = direction;
			packet.Send( _client.Connection );
		}

		private void ShootFastBullet( Vector2 direction )
		{
			_currentBulletId++;

			var bullet = new FastBullet( _currentBulletId, _hostCharacter );
			bullet.Load( this.Content );
			bullet.Position = _hostCharacter.Position;
			bullet.Direction = direction;
			_localBullets.Add( bullet.Id, bullet );

			var packet = new ShootFastBulletPacket();
			packet.LocalId = bullet.Id;
			packet.Direction = bullet.Direction;
			packet.Send( _client.Connection );
		}

		private void CommitHostCharacter()
		{
			if( _hostCharacter == null )
			{
				return;
			}

			_currentInputId++;

			var packet = new CommitCharacterInputPacket();
			packet.InputId = _currentInputId;
			packet.Direction = _hostCharacter.CurrentDirection;
			packet.Send( _client.Connection );

			_previousInputPackets.Add( packet );
		}

		private void ProcessCreateCharacter( BinaryReader reader )
		{
			var packet = Packet.Receive<CreateCharacterPacket>( reader );

			var character = new Character( packet.Id, packet.IsHost, packet.Position, packet.Color );
			character.Load( this.Content );
			_characters.Add( character.Id, character );

			if( character.IsHost )
			{
				_hostCharacter = character;
			}
		}

		private void ProcessDestroyCharacter( BinaryReader reader )
		{
			var packet = Packet.Receive<DestroyCharacterPacket>( reader );

			Character character = null;
			if( _characters.TryGetValue( packet.Id, out character ) == false )
			{
				throw new Exception();
			}

			SceneObject.Destroy( character );
			_characters.Remove( packet.Id );
		}

		private void ProcessCreateImmediateBullet( BinaryReader reader )
		{
			var packet = Packet.Receive<CreateImmediateBulletPacket>( reader );

			var bulletLine = new BulletLine( packet.BulletDirection );
			bulletLine.Position = packet.BulletOrigin;
		}

		private void ProcessCreateFastBullet( BinaryReader reader )
		{
			var packet = Packet.Receive<CreateFastBulletPacket>( reader );

			Character shooter = null;
			if( _characters.TryGetValue( packet.ShooterId, out shooter ) == false )
			{
				throw new Exception();
			}

			if( shooter == _hostCharacter )
			{
				var bullet = _localBullets[packet.LocalId];
				_localBullets.Remove( packet.LocalId );

				bullet.Id = packet.RemoteId;

				_bullets.Add( bullet.Id, bullet );
			}
			else
			{
				var bullet = new FastBullet( packet.RemoteId, shooter );
				bullet.Load( this.Content );
				bullet.Position = packet.Position;
				bullet.Direction = packet.Direction;
				_bullets.Add( bullet.Id, bullet );
			}
		}

		private void ProcessDestroyFastBullet( BinaryReader reader )
		{
			var packet = Packet.Receive<DestroyFastBulletPacket>( reader );

			FastBullet bullet = null;
			if( _bullets.TryGetValue( packet.BulletId, out bullet ) == false )
			{
				throw new Exception();
			}

			SceneObject.Destroy( bullet );
			_bullets.Remove( bullet.Id );
		}

		private void ProcessHurtByImmediateBullet( BinaryReader reader )
		{
			var packet = Packet.Receive<HurtByImmediateBulletPacket>( reader );

			Character character = null;
			if( _characters.TryGetValue( packet.VictimId, out character ) == false )
			{
				throw new Exception();
			}

			if( packet.AttackerId != _hostCharacter.Id )
			{
				character.Hurt();
			}

			character.CurrentHealth = packet.VictimHealth;
		}

		private void ProcessHurtByFastBullet( BinaryReader reader )
		{
			var packet = Packet.Receive<HurtByFastBulletPacket>( reader );

			Character victim = null;
			if( _characters.TryGetValue( packet.VictimId, out victim ) == false )
			{
				throw new Exception();
			}

			FastBullet bullet = null;
			if( _bullets.TryGetValue( packet.BulletId, out bullet ) == false )
			{
				throw new Exception();
			}

			SceneObject.Destroy( bullet );
			_bullets.Remove( bullet.Id );

			victim.CurrentHealth = packet.VictimHealth;
			victim.Hurt();
		}

		private void ProcessUpdateCharacterState( BinaryReader reader )
		{
			var packet = Packet.Receive<UpdateCharacterStatePacket>( reader );

			Character character = null;
			if( _characters.TryGetValue( packet.Id, out character ) == false )
			{
				throw new Exception();
			}

			if( character == _hostCharacter )
			{
				character.CurrentDirection = packet.Direction;
				character.Position = packet.Position;
				this.ResimulateFrom( packet.InputId );
			}
			else
			{
				character.CurrentDirection = packet.Direction;
				character.Position = packet.Position;
				var snapshot = new CharacterSnapshot( packet.Position, packet.Direction );
				character.History.AddSnapshot( snapshot );
			}
		}

		private void InterpolateCharacter( Character character )
		{
			var pastTime = DateTime.Now - Character.InterpolationInterval;

			CharacterSnapshot previousSnapshot = null;
			CharacterSnapshot nextSnapshot = null;
			if( character.History.TryFindSnapshots( pastTime, out previousSnapshot, out nextSnapshot ) == false )
			{
				return;
			}

			var totalSegmentTime = nextSnapshot.Time - previousSnapshot.Time;
			var currentSegmentTime = pastTime - previousSnapshot.Time;
			var amount = currentSegmentTime.TotalMilliseconds / totalSegmentTime.TotalMilliseconds;
			Debug.Assert( 0.0f <= amount && amount <= 1.0f );

			character.Position = Vector2.Lerp( previousSnapshot.Position, nextSnapshot.Position, (float)amount );

			character.History.Update();

			character.InterpolationGhosts.Clear();
			character.History.AddTo( character.InterpolationGhosts );
		}

		private void ResimulateFrom( int inputId )
		{
			var index = _previousInputPackets.FindIndex( c => c.InputId == inputId );
			if( index == -1 )
			{
				return;
			}

			for( int i = index + 1; i < _previousInputPackets.Count; i++ )
			{
				var packet = _previousInputPackets[i];
				_hostCharacter.CurrentDirection = packet.Direction;
				_hostCharacter.Simulate( FrameInterval );
			}

			_previousInputPackets.RemoveRange( 0, index + 1 );
		}

		private bool CheckBulletHit( FastBullet bullet )
		{
			Character target = null;
			foreach( var character in _characters.Values )
			{
				if( character == bullet.Shooter )
				{
					continue;
				}

				if( Vector2.Distance( character.Position, bullet.Position ) < character.Radius )
				{
					target = character;
					break;
				}
			}

			if( target == null )
			{
				return false;
			}

			return true;
		}
	}
}
