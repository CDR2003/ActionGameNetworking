using ActionGameNetworking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

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

		public SampleGame()
		{
			_graphics = new GraphicsDeviceManager( this );
			Content.RootDirectory = "Content";
		}
		
		protected override void Initialize()
		{
			_client = new AgnClient();

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
			if( _currentTime.TotalSeconds > 0.016 )
			{
				_currentTime = TimeSpan.Zero;

				var data = new MemoryStream();
				var writer = new BinaryWriter( data );
				writer.Write( gameTime.TotalGameTime.TotalSeconds );
				_client.SendTo( data.GetBuffer(), (int)data.Length, "127.0.0.1", 30000 );
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
