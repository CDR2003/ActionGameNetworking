using ActionGameNetworking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
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

		public SampleServer()
		{
			_graphics = new GraphicsDeviceManager( this );
			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			_server = new AgnServer( 0x0bad, 30000 );
			_server.ClientDataReceive += OnClientDataReceived;

			_server.Start();

			base.Initialize();
		}

		private void OnClientDataReceived( BinaryReader reader, AgnConnection connection )
		{
			reader.ReadDouble();

			var data = new MemoryStream();
			var writer = new BinaryWriter( data );
			writer.Write( DateTime.Now.Second );

			connection.SendTo( data.GetBuffer(), (int)data.Length );
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
			
			_server.Update( gameTime.ElapsedGameTime );

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
			_spriteBatch.DrawString( _font, sb.ToString(), new Vector2( 100.0f, 100.0f ), Color.White );
			_spriteBatch.End();

			base.Draw( gameTime );
		}
	}
}
