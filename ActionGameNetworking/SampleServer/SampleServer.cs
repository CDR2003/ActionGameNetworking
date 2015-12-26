using ActionGameNetworking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

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

		public SampleServer()
		{
			_graphics = new GraphicsDeviceManager( this );
			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			_server = new AgnServer( 30000 );

			base.Initialize();
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch( GraphicsDevice );


		}

		protected override void UnloadContent()
		{
		}

		protected override void Update( GameTime gameTime )
		{
			if( GamePad.GetState( PlayerIndex.One ).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown( Keys.Escape ) )
				Exit();

			_currentTime += gameTime.ElapsedGameTime;
			if( _currentTime.TotalSeconds > 0.03 )
			{
				_currentTime = TimeSpan.Zero;
				_server.Update( gameTime.ElapsedGameTime );
			}

			base.Update( gameTime );
		}

		protected override void Draw( GameTime gameTime )
		{
			GraphicsDevice.Clear( Color.CornflowerBlue );



			base.Draw( gameTime );
		}
	}
}
