﻿using ActionGameNetworking;
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
		public const float ClientFrameInterval = 1.0f / 60.0f;

		public const float FrameInterval = 1.0f / 20.0f;

		private GraphicsDeviceManager _graphics;

		private SpriteBatch _spriteBatch;

		private AgnServer _server;

		private TimeSpan _currentTime = TimeSpan.Zero;

		private SpriteFont _font;

		private Random _random;

		private Dictionary<AgnConnection, Character> _clients;

		private Dictionary<int, Character> _characters;

		private Dictionary<int, FastBullet> _bullets = new Dictionary<int, FastBullet>();

		private int _currentCharacterId;

		private int _currentBulletId = 0;

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

			var keys = Keyboard.GetState().GetPressedKeys();
			if( keys.Length > 0 )
			{
				var key = keys[0];
				if( Keys.D0 <= key && key <= Keys.D9 )
				{
					var number = key - Keys.D0;
					_server.LatencySimulation = number * 0.05f;
				}
			}

			_currentTime += gameTime.ElapsedGameTime;
			if( _currentTime.TotalSeconds >= FrameInterval )
			{
				_currentTime = TimeSpan.Zero;

				var bulletsToRemove = new List<int>();
				foreach( var bullet in _bullets.Values )
				{
					var oldPosition = bullet.Position;

					bullet.Simulate( FrameInterval );

					var hit = this.CheckBulletHit( bullet, oldPosition );
					if( hit )
					{
						SceneObject.Destroy( bullet );
						bulletsToRemove.Add( bullet.Id );
					}
					else if( bullet.Position.X < 0.0f || bullet.Position.Y < 0.0f || bullet.Position.X > this.Window.ClientBounds.Width || bullet.Position.Y > this.Window.ClientBounds.Height )
					{
						var packet = new DestroyFastBulletPacket();
						packet.BulletId = bullet.Id;
						packet.Broadcast( _server );

						SceneObject.Destroy( bullet );
						bulletsToRemove.Add( bullet.Id );
					}
				}

				foreach( var bulletId in bulletsToRemove )
				{
					_bullets.Remove( bulletId );
				}

				_server.Update();

				foreach( var character in _characters.Values )
				{
					character.UpdateSnapshotHistory();
					this.BroadcastCharacterState( character );
				}
			}

			SceneManager.Instance.Update( gameTime );
			
			base.Update( gameTime );
		}

		protected override void Draw( GameTime gameTime )
		{
			GraphicsDevice.Clear( Color.CornflowerBlue );

			var sb = new StringBuilder();
			sb.AppendLine( "Server" );
			sb.AppendFormatLine( "    Latency Simulation: {0}ms", _server.LatencySimulation * 1000.0f );
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

			SceneManager.Instance.Draw( _spriteBatch );

			_spriteBatch.DrawString( _font, sb.ToString(), Vector2.Zero, this.IsActive ? Color.White : Color.LightGray );
			_spriteBatch.End();

			base.Draw( gameTime );
		}

		private void OnClientDataReceived( BinaryReader reader, AgnConnection connection )
		{
			var type = (Packet.Type)reader.ReadUInt32();
			switch( type )
			{
				case Packet.Type.CS_Login:
					this.ProcessLogin( reader, connection );
					break;
				case Packet.Type.CS_CommitCharacterInput:
					this.ProcessCommitCharacterInput( reader, connection );
					break;
				case Packet.Type.CS_ShootImmediateBullet:
					this.ProcessShootImmediateBullet( reader, connection );
					break;
				case Packet.Type.CS_ShootFastBullet:
					this.ProcessShootFastBullet( reader, connection );
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
			SceneObject.Destroy( character );

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
			var packet = Packet.Receive<CommitCharacterInputPacket>( reader );

			Character character = null;
			if( _clients.TryGetValue( connection, out character ) == false )
			{
				return;
			}

			character.CurrentInputId = packet.InputId;
			character.CurrentDirection = packet.Direction;

			character.Simulate( ClientFrameInterval );
		}

		private void ProcessShootImmediateBullet( BinaryReader reader, AgnConnection connection )
		{
			var packet = Packet.Receive<ShootImmediateBulletPacket>( reader );

			Character attacker = null;
			if( _clients.TryGetValue( connection, out attacker ) == false )
			{
				return;
			}

			var bulletLine = new BulletLine( packet.Direction );
			bulletLine.Position = attacker.Position;

			var shootPacket = new CreateImmediateBulletPacket();
			shootPacket.BulletOrigin = attacker.Position;
			shootPacket.BulletDirection = packet.Direction;
			shootPacket.Broadcast( _server, connection );

			var rtt = TimeSpan.FromSeconds( connection.CurrentRtt );
			var past = DateTime.Now - Character.InterpolationInterval - rtt;

			Character minCharacter = null;
			var minDistance = float.MaxValue;
			foreach( var character in _characters.Values )
			{
				if( attacker == character )
				{
					continue;
				}

				CharacterSnapshot previousSnapshot = null;
				CharacterSnapshot nextSnapshot = null;
				if( character.History.TryFindSnapshots( past, out previousSnapshot, out nextSnapshot ) == false )
				{
					continue;
				}

				var distance = bulletLine.Hit( previousSnapshot.Position, character.Radius );
				if( distance != null && distance.Value < minDistance )
				{
					minDistance = distance.Value;
					minCharacter = character;
				}
			}

			if( minCharacter != null )
			{
				minCharacter.Hurt();
				minCharacter.TakeDamage( BulletLine.Damage );

				var hurtPacket = new HurtByImmediateBulletPacket();
				hurtPacket.AttackerId = attacker.Id;
				hurtPacket.VictimId = minCharacter.Id;
				hurtPacket.VictimHealth = minCharacter.CurrentHealth;
				hurtPacket.Broadcast( _server );
			}
		}

		private void ProcessShootFastBullet( BinaryReader reader, AgnConnection connection )
		{
			var packet = Packet.Receive<ShootFastBulletPacket>( reader );

			Character shooter = null;
			if( _clients.TryGetValue( connection, out shooter ) == false )
			{
				return;
			}

			_currentBulletId++;

			var bullet = new FastBullet( _currentBulletId, shooter );
			bullet.Load( this.Content );
			bullet.Position = shooter.Position;
			bullet.Direction = packet.Direction;
			_bullets.Add( bullet.Id, bullet );

			var createBulletPacket = new CreateFastBulletPacket();
			createBulletPacket.LocalId = packet.LocalId;
			createBulletPacket.RemoteId = bullet.Id;
			createBulletPacket.Position = bullet.Position;
			createBulletPacket.Direction = bullet.Direction;
			createBulletPacket.ShooterId = shooter.Id;
			createBulletPacket.Broadcast( _server );
		}

		private void BroadcastCharacterState( Character character )
		{
			var packet = new UpdateCharacterStatePacket();
			packet.Id = character.Id;
			packet.InputId = character.CurrentInputId;
			packet.Direction = character.CurrentDirection;
			packet.Position = character.Position;
			packet.Broadcast( _server );
		}

		private bool CheckBulletHit( FastBullet bullet, Vector2 oldPosition )
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

				var t = BulletLine.Intersects( character.Position, character.Radius, oldPosition, bullet.Direction );
				if( t != null && t.Value < FastBullet.Speed * FrameInterval )
				{
					target = character;
					break;
				}
			}

			if( target == null )
			{
				return false;
			}

			target.TakeDamage( FastBullet.Damage );

			var packet = new HurtByFastBulletPacket();
			packet.AttackerId = bullet.Shooter.Id;
			packet.VictimId = target.Id;
			packet.VictimHealth = target.CurrentHealth;
			packet.BulletId = bullet.Id;
			packet.Broadcast( _server );
			return true;
		}
	}
}
