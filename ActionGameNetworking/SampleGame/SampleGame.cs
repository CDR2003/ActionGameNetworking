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

		private ButtonState _previousMouseState = ButtonState.Released;

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
				case Packet.Type.SC_Shoot:
					this.ProcessShoot( reader );
					break;
				case Packet.Type.SC_Hurt:
					this.ProcessHurt( reader );
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

				this.UpdateShooting();

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

		private void UpdateShooting()
		{
			var buttonState = Mouse.GetState().LeftButton;
			if( buttonState == _previousMouseState || _previousMouseState == ButtonState.Pressed )
			{
				_previousMouseState = buttonState;
				return;
			}

			_previousMouseState = buttonState;

			if( _hostCharacter == null )
			{
				return;
			}

			if( this.IsActive == false )
			{
				return;
			}

			var mousePosition = Mouse.GetState().Position.ToVector2();
			var direction = Vector2.Normalize( mousePosition - _hostCharacter.Position );
			var line = new BulletLine( direction );
			line.Position = _hostCharacter.Position;

			var victim = line.Shoot( _hostCharacter );
			if( victim != null )
			{
				victim.Hurt();
			}

			var packet = new AttackCharacterPacket();
			packet.Direction = direction;
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

		private void ProcessShoot( BinaryReader reader )
		{
			var packet = Packet.Receive<ShootPacket>( reader );

			var bulletLine = new BulletLine( packet.BulletDirection );
			bulletLine.Position = packet.BulletOrigin;
		}

		private void ProcessHurt( BinaryReader reader )
		{
			var packet = Packet.Receive<HurtPacket>( reader );
			
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
	}
}
