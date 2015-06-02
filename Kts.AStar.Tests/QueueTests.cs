using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Kts.AStar.Tests
{
	public class QueueTests
	{
		private readonly ITestOutputHelper _output;

		public QueueTests(ITestOutputHelper output)
		{
			_output = output;
		}


		[Fact]
		public void SortsAMillion()
		{
			var rand = new Random(42);
			const int count = 1000000;
			var randoms = new HashSet<int>();
			while(randoms.Count < count)
				randoms.Add(rand.Next());

			var queue = new RandomMeldablePriorityQueue<int>();
			foreach(var r in randoms)
				queue.Enqueue(r);

			Assert.Equal(count, queue.Count);

			var copy = new int[count];
			queue.CopyTo(copy, 0);

			Assert.Equal(count, queue.Count);

			var sorted = new List<int>(count);
			for(int i = 0; i < count; i++)
				sorted.Add(queue.Dequeue());

			Assert.Equal(0, queue.Count);

			Assert.True(randoms.OrderBy(x => x).SequenceEqual(sorted));
			Assert.True(copy.SequenceEqual(sorted));
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

		[Fact]
		public void SomeCanBeRemovedFromQueue()
		{
			var rand = new Random(42);
			const int count = 100000;
			var randoms = new List<int>(count);
			while (randoms.Count < count)
				randoms.Add(rand.Next(1000));

			var distinct = randoms.Distinct().ToList();
			var duplicateCount = count - distinct.Count;
			Assert.True(duplicateCount > 0);

			var queue = new RandomMeldablePriorityQueue<int>();
			foreach (var r in randoms)
				queue.Enqueue(r);

			Assert.Equal(distinct.Count, queue.Count);

			const int toBeRemoved = 10;
			for (int i = 0; i < toBeRemoved; i++)
			{
				var item = distinct.Last();
				distinct.RemoveAt(distinct.Count - 1);
				Assert.True(queue.Remove(item));
			}

			var sorted = new List<int>(distinct.Count);
			for (int i = 0; i < distinct.Count; i++)
				sorted.Add(queue.Dequeue());

			Assert.Equal(0, queue.Count);

			Assert.True(distinct.OrderBy(x => x).SequenceEqual(sorted));
		}

		[Fact]
		public void RandomSpeedTest()
		{
			var rand = new Random(42);
			var buffer = new byte[204800];
			var sw = Stopwatch.StartNew();
			for (int i = 0; i < 200; i++)
				rand.NextBytes(buffer);
			_output.WriteLine("Random in {0}ms", sw.Elapsed.TotalMilliseconds);

			sw.Restart();
			for (int i = 0; i < 200; i++)
				ThreadLocalXorShifter.FillBufferWithXorShift(buffer);

			_output.WriteLine("XorShift in {0}ms", sw.Elapsed.TotalMilliseconds);
		}
	}
}
