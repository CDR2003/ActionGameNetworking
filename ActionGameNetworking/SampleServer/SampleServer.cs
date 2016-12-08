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

namespace SampleServer
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class SampleServer : Game
	{
		private GraphicsDeviceManager _graphics;

		private SpriteBatch _spriteBatch;

		private AgnServer _server;

		private TimeSpan _currentTime = TimeSpan.Zero;

		private SpriteFont _font;

		private Random _random;

		private Dictionary<AgnConnection, Character> _clients;

		private Dictionary<int, Character> _characters;

		private int _currentCharacterId;

		public SampleServer()
		{
			_graphics = new GraphicsDeviceManager( this );
			_random = new Random();
			_clients = new Dictionary<AgnConnection, Character>();
			_characters = new Dictionary<int, Character>();
			_currentCharacterId = 0;
			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			this.IsMouseVisible = true;

			_server = new AgnServer( 0x0bad, 30000 );
			_server.ClientDataReceive += OnClientDataReceived;
			_server.ClientDisconnect += OnClientDisconnected;
			_server.LatencySimulation = 0.2f;

			_server.Start();

			base.Initialize();
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
				Exit();

			_currentTime += gameTime.ElapsedGameTime;
			if( _currentTime.TotalSeconds >= 1.0 / 20.0 )
			{
				_currentTime = TimeSpan.Zero;

				foreach( var character in _characters.Values )
				{
					this.BroadcastCharacterState( character );
				}
			}

			//_server.LatencySimulation = _random.NextFloat( 0.05f, 0.2f );
			_server.Update( gameTime.ElapsedGameTime );

			foreach( var character in _characters.Values )
			{
				character.Simulate( gameTime );
			}

			base.Update( gameTime );
		}

		protected override void Draw( GameTime gameTime )
		{
			GraphicsDevice.Clear( Color.CornflowerBlue );

			var sb = new StringBuilder();
			sb.AppendLine( "Server" );
			sb.AppendLine( "Clients:" );

			var index = 0;
			foreach( var connection in _server.Connections )
			{
				sb.AppendFormatLine( "    #{0} @ {1}", index, connection.Remote.ToString() );
				sb.AppendFormatLine( "        Sequence: {0}", connection.CurrentSequence );
				sb.AppendFormatLine( "        RTT: {0:0}ms", connection.CurrentRtt * 1000.0f );
				sb.AppendFormatLine( "        Drop Rate: {0:0}%", connection.CurrentDropRate * 100.0f );
				index++;
			}

			_spriteBatch.Begin();

			foreach( var character in _characters.Values )
			{
				character.Draw( _spriteBatch );
			}

			_spriteBatch.DrawString( _font, sb.ToString(), Vector2.Zero, this.IsActive ? Color.White : Color.LightGray );
			_spriteBatch.End();

			base.Draw( gameTime );
		}

		private void OnClientDataReceived( BinaryReader reader, AgnConnection connection )
		{
			var type = (Packet.Type)reader.ReadUInt32();
			switch( type )
			{
				case Packet.Type.Login:
					this.ProcessLogin( reader, connection );
					break;
				case Packet.Type.CommitCharacterInput:
					this.ProcessCommitCharacterInput( reader, connection );
					break;
				default:
					throw new Exception();
			}
		}

		private void OnClientDisconnected( AgnConnection connection )
		{
			Character character = null;
			if( _clients.TryGetValue( connection, out character ) == false )
			{
				return;
			}

			_clients.Remove( connection );
			_characters.Remove( character.Id );

			var packet = new DestroyCharacterPacket();
			packet.Id = character.Id;
			packet.Broadcast( _server );
		}

		private void ProcessLogin( BinaryReader reader, AgnConnection connection )
		{
			foreach( var existingCharacter in _characters.Values )
			{
				var createExistingCharacter = new CreateCharacterPacket();
				createExistingCharacter.Id = existingCharacter.Id;
				createExistingCharacter.IsHost = false;
				createExistingCharacter.Position = existingCharacter.Position;
				createExistingCharacter.Color = existingCharacter.Color;
				createExistingCharacter.Send( connection );

				var updateExistingCharacter = new UpdateCharacterStatePacket();
				updateExistingCharacter.Id = existingCharacter.Id;
				updateExistingCharacter.Direction = existingCharacter.CurrentDirection;
				updateExistingCharacter.Position = existingCharacter.Position;
				updateExistingCharacter.Send( connection );
			}

			_currentCharacterId++;

			var color = _random.NextColor();
			var initialPosition = _random.NextVector2();
			initialPosition.X *= this.Window.ClientBounds.Width;
			initialPosition.Y *= this.Window.ClientBounds.Height;

			var character = new Character( _currentCharacterId, false, initialPosition, color );
			character.Load( this.Content );
			_clients.Add( connection, character );
			_characters.Add( character.Id, character );

			foreach( var client in _server.Connections )
			{
				var packet = new CreateCharacterPacket();
				packet.Id = character.Id;
				packet.IsHost = connection == client;
				packet.Position = character.Position;
				packet.Color = character.Color;
				packet.Send( client );
			}
		}

		private void ProcessCommitCharacterInput( BinaryReader reader, AgnConnection connection )
		{
			var packet = new CommitCharacterInputPacket();
			packet.ReadFromStream( reader );

			Character character = null;
			if( _clients.TryGetValue( connection, out character ) == false )
			{
				return;
			}

			character.CurrentDirection = packet.Direction;
		}

		private void BroadcastCharacterState( Character character )
		{
			var packet = new UpdateCharacterStatePacket();
			packet.Id = character.Id;
			packet.Direction = character.CurrentDirection;
			packet.Position = character.Position;
			packet.Broadcast( _server );
		}
	}
}
