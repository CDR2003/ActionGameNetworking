using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class SceneManager
	{
		#region Singleton

		public static SceneManager Instance
		{
			get
			{
				if( _instance == null )
				{
					_instance = new SceneManager();
				}
				return _instance;
			}
		}

		private static SceneManager _instance;

		#endregion

		public List<SceneObject> Objects { get; private set; }

		private List<SceneObject> _addList;

		private List<SceneObject> _removeList;

		private SceneManager()
		{
			this.Objects = new List<SceneObject>();
			_addList = new List<SceneObject>();
			_removeList = new List<SceneObject>();
		}

		internal void AddObject( SceneObject obj )
		{
			_addList.Add( obj );
		}

		internal void RemoveObject( SceneObject obj )
		{
			if( this.Objects.Contains( obj ) == false )
			{
				return;
			}

			_removeList.Add( obj );
		}

		public void Update( GameTime gameTime )
		{
			this.Objects.AddRange( _addList );
			_addList.Clear();

			foreach( var obj in this.Objects )
			{
				obj.Update( gameTime );
			}

			foreach( var obj in _removeList )
			{
				this.Objects.Remove( obj );
			}
			_removeList.Clear();
		}

		public void Draw( SpriteBatch spriteBatch )
		{
			foreach( var obj in this.Objects )
			{
				obj.Draw( spriteBatch );
			}
		}
	}
}
