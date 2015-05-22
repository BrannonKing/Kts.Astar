using System;

namespace Kts.AStar
{
	public static class RandomMeldablePriorityQueueSettings
	{
		private static int _childrenCount = 4;

		public static void Reseed(int seed)
		{
			_rand = new Random(seed); 
		}
		private static Random _rand = new Random(42);
		public static int NextRandom(int cap)
		{
			// todo: change this to use crypto
			// bring back 2KB of data
			// if cap < 256 just advance one byte
			return _rand.Next(cap);
		}

		/// <summary>
		/// Number of children allocated for each node.
		/// </summary>
		public static int ChildrenCount
		{
			get { return _childrenCount; }
			set
			{
				if (value < 2)
					throw new ArgumentOutOfRangeException("value", value, "The value must be creater than 1.");
				_childrenCount = value;
			}
		}
	}
}