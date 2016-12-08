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

		public const float MaxLagDistance = 100.0f;

		private GraphicsDeviceManager _graphics;

		private SpriteBatch _spriteBatch;

		private AgnClient _client;

		private TimeSpan _currentTime = TimeSpan.Zero;

		private SpriteFont _font;

		private Dictionary<int, Character> _characters;

		private Character _hostCharacter;

		private int _currentInputId;

		private List<CommitCharacterInputPacket> _previousInputPackets;

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
				case Packet.Type.CreateCharacter:
					this.ProcessCreateCharacter( reader );
					break;
				case Packet.Type.UpdateCharacterState:
					this.ProcessUpdateCharacterState( reader );
					break;
				case Packet.Type.DestroyCharacter:
					this.ProcessDestroyCharacter( reader );
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

			if( _hostCharacter != null )
			{
				_hostCharacter.UpdateInput();
			}

			_currentTime += gameTime.ElapsedGameTime;
			if( _currentTime.TotalSeconds > FrameInterval )
			{
				_currentTime = TimeSpan.Zero;
				this.CommitHostCharacter();
			}

			if( _hostCharacter != null )
			{
				_hostCharacter.Simulate( (float)gameTime.ElapsedGameTime.TotalSeconds );
			}

			_client.Update( gameTime.ElapsedGameTime );

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

			foreach( var character in _characters.Values )
			{
				character.Draw( _spriteBatch );
			}

			_spriteBatch.DrawString( _font, sb.ToString(), Vector2.Zero, this.IsActive ? Color.White : Color.LightGray );
			_spriteBatch.End();

			base.Draw( gameTime );
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
			var packet = new CreateCharacterPacket();
			packet.ReadFromStream( reader );

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
			var packet = new DestroyCharacterPacket();
			packet.ReadFromStream( reader );

			_characters.Remove( packet.Id );
		}

		private void ProcessUpdateCharacterState( BinaryReader reader )
		{
			var packet = new UpdateCharacterStatePacket();
			packet.ReadFromStream( reader );

			Character character = null;
			if( _characters.TryGetValue( packet.Id, out character ) == false )
			{
				throw new Exception();
			}

			var oldPosition = character.Position;

			character.CurrentDirection = packet.Direction;
			character.Position = packet.Position;

			if( character == _hostCharacter )
			{
				this.ResimulateFrom( packet.InputId, oldPosition );
			}
		}

		private void ResimulateFrom( int inputId, Vector2 clientPosition )
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

			if( Vector2.Distance( clientPosition, _hostCharacter.Position ) < MaxLagDistance )
			{
				_hostCharacter.Position = clientPosition;
			}
		}
	}
}
