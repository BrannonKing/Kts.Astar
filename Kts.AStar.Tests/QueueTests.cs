using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace Kts.AStar.Tests
{
	public class QueueTests
	{
		[Fact]
		public void SortsAMillion()
		{
			var rand = new Random(42);
			const int count = 1000000;
			var randoms = new List<int>(count);
			for(int i = 0; i < count; i++)
				randoms.Add(rand.Next());

			RandomMeldablePriorityTree<int> heap = null;
			for (int i = 0; i < count; i++)
				heap = RandomMeldablePriorityTree<int>.Meld(heap, randoms[i]);

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
		public void SomeCanBeDecreased()
		{
			var rand = new Random(42);
			const int count = 100000;
			var randoms = new List<int>(count);
			for (int i = 0; i < count; i++)
				randoms.Add(rand.Next());

			RandomMeldablePriorityTree<int> heap = null;
			var nodes = new List<RandomMeldablePriorityTree<int>>(count);
			for (int i = 0; i < count; i++)
			{
				var node = new RandomMeldablePriorityTree<int>(randoms[i]);
				nodes.Add(node);
				heap = RandomMeldablePriorityTree<int>.Meld(heap, node);
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

		sealed class SimpleInt : IComparable<SimpleInt>
		{
			private static int CreationSeed;
			public readonly int Value;
			public readonly int Created;
			public SimpleInt(int value)
			{
				Value = value;
				Created = Interlocked.Increment(ref CreationSeed);
			}

			public override int GetHashCode()
			{
				return Value.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				return Value == ((SimpleInt)obj).Value;
			}

			public int CompareTo(SimpleInt other)
			{
				var ret = Value.CompareTo(other.Value);
				if (ret != 0)
					return ret;
				return other.Created.CompareTo(Created);
			}
		}

		[Fact]
		public void WorksAsLifoForEqualValues()
		{
			// according to Wikipedia, it is faster to use a queue that works as a LIFO for equal values
			// we enforce that requirement here

			var items = new List<SimpleInt>(20);
			for (int i = 0; i < 10; i++)
				items.Add(new SimpleInt(i));
			for (int i = 0; i < 10; i++)
				items.Add(new SimpleInt(i));

			RandomMeldablePriorityTree<SimpleInt> heap = null;
			for (int i = 0; i < items.Count; i++)
				heap = RandomMeldablePriorityTree<SimpleInt>.Meld(heap, items[i]);

			var sorted = new List<SimpleInt>(20);
			while (heap != null)
			{
				sorted.Add(heap.Element);
				heap = heap.DeleteMin();
			}

			for (int i = 0; i < 10; i++)
			{
				Assert.True(ReferenceEquals(items[i + 10], sorted[i * 2]));
				Assert.True(ReferenceEquals(items[i], sorted[i * 2 + 1]));
			}
		}
	}
}
