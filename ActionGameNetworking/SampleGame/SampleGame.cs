using ActionGameNetworking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
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
		private GraphicsDeviceManager _graphics;

		private SpriteBatch _spriteBatch;

		private AgnClient _client;

		private TimeSpan _currentTime = TimeSpan.Zero;

		private SpriteFont _font;

		public SampleGame()
		{
			_graphics = new GraphicsDeviceManager( this );
			Content.RootDirectory = "Content";
		}
		
		protected override void Initialize()
		{
			_client = new AgnClient( 0x0bad );
			_client.ServerDataReceive += OnServerDataReceived;
			_client.LatencySimulation = 0.1f;
			_client.DropRateSimulation = 0.1f;

			_client.Connect( "192.168.0.104", 30000 );

			base.Initialize();
		}

		private void OnServerDataReceived( BinaryReader reader )
		{
			reader.ReadInt32();
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

			if( Keyboard.GetState().IsKeyDown( Keys.Enter ) )
			{
				_client.LatencySimulation = 0.0f;
			}

			if( Keyboard.GetState().IsKeyDown( Keys.Space ) )
			{
				_client.DropRateSimulation = 0.0f;
			}

			_currentTime += gameTime.ElapsedGameTime;
			if( _currentTime.TotalSeconds > 0.03 )
			{
				_currentTime = TimeSpan.Zero;

				var data = new MemoryStream();
				var writer = new BinaryWriter( data );
				writer.Write( gameTime.TotalGameTime.TotalSeconds );
				_client.SendTo( data.GetBuffer(), (int)data.Length );
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
			_spriteBatch.DrawString( _font, sb.ToString(), new Vector2( 100.0f, 100.0f ), Color.White );
			_spriteBatch.End();

			base.Draw( gameTime );
		}
	}
}
