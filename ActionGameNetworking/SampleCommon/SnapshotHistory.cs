using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
	public class SnapshotHistory<TSnapshot> where TSnapshot : Snapshot
	{
		public TimeSpan Timeout { get; set; }

		public DateTime EarliestTime
		{
			get
			{
				return _snapshots[0].Time;
			}
		}

		public TSnapshot LatestSnapshot
		{
			get
			{
				return _snapshots.Last();
			}
		}

		public int Count
		{
			get
			{
				return _snapshots.Count;
			}
		}

		private List<TSnapshot> _snapshots;

		public SnapshotHistory()
		{
			this.Timeout = new TimeSpan( 0, 0, 1 );
			_snapshots = new List<TSnapshot>();
		}

		public void AddSnapshot( TSnapshot snapshot )
		{
			_snapshots.Add( snapshot );
		}

		public void AddTo( List<TSnapshot> list )
		{
			list.AddRange( _snapshots );
		}

		public bool TryFindSnapshots( DateTime time, out TSnapshot previous, out TSnapshot next )
		{
			previous = null;
			next = null;

			if( _snapshots.Count < 2 )
			{
				return false;
			}

			if( time < _snapshots[0].Time )
			{
				return false;
			}

			if( _snapshots.Last().Time < time )
			{
				return false;
			}

			for( int i = 0; i < _snapshots.Count; i++ )
			{
				var snapshot = _snapshots[i];
				if( time < snapshot.Time )
				{
					previous = _snapshots[i - 1];
					next = _snapshots[i];
					break;
				}
			}

			return true;
		}

		public void Update()
		{
			var expireTime = DateTime.Now - this.Timeout;
			for( int i = 0; i < _snapshots.Count; i++ )
			{
				var snapshot = _snapshots[i];
				if( snapshot.Time > expireTime )
				{
					if( i > 0 )
					{
						_snapshots.RemoveRange( 0, i - 1 );
					}
					break;
				}
			}
		}
	}
}
