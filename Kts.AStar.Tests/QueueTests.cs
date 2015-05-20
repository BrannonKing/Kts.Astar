using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Kts.AStar.Tests
{
	public class QueueTests
	{
		[Fact]
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

			Assert.True(randoms.SequenceEqual(sorted));
		}

		[Fact]
		public void DecreaseSome()
		{
			var rand = new Random(42);
			const int count = 100000;
			var randoms = new List<int>(count);
			for (int i = 0; i < count; i++)
				randoms.Add(rand.Next());

			RandomMeldablePriorityQueue<int> heap = null;
			var nodes = new List<RandomMeldablePriorityQueue<int>>(count);
			for (int i = 0; i < count; i++)
			{
				var node = new RandomMeldablePriorityQueue<int>(randoms[i]);
				nodes.Add(node);
				heap = RandomMeldablePriorityQueue<int>.Meld(heap, node);
			}
			for(int i = 0; i < 200; i++)
			{
				var spot = rand.Next(count);
				var node = nodes[spot];
				heap = heap.DecreaseKey(node, node.Element - 10);
				randoms[spot] -= 10;
			}

			var sorted = new List<int>(count);
			while (heap != null)
			{
				sorted.Add(heap.Element);
				heap = heap.DeleteMin();
			}

			randoms.Sort();

			Assert.True(randoms.SequenceEqual(sorted));
		}
	}
}
