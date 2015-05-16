using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Kts.AStar.Tests
{
	[TestFixture]
	class QueueTests
	{
		[Test]
		public void HandlesAMillion()
		{
			var rand = new Random(42);
			const int count = 1000000;
			var randoms = new List<int>(count);
			for(int i = 0; i < count; i++)
				randoms.Add(rand.Next());

			RandomMeldablePriorityQueue<int> heap = null;
			for (int i = 0; i < count; i++)
				heap = RandomMeldablePriorityQueue<int>.Meld(heap, randoms[i]);

			var sorted = new List<int>(count);
			while (heap != null)
			{
				sorted.Add(heap.Element);
				heap = heap.DeleteMin();
			}


			randoms.Sort();

			Assert.IsTrue(randoms.SequenceEqual(sorted));
		}
	}
}
