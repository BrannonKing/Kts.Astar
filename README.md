Kts.Astar
=========

This library contains an A* search implementation built upon a Random Meldable Priority Queue. Some time ago I was involved in some signal analysis that needed a mechanism for finding the optimal path between beats (high amplitude points). This is a side product of that project.

I had attempted to use [QuickGraph](http://quickgraph.codeplex.com). I like the QuickGraph library. It is a general-purpose library, unlike this one. It's well designed and quite useful. However, it was not fast enough for my needs. Also, it appears to require all graph nodes to be loaded in memory ahead of time. This project keeps explored nodes in memory, but they do not need to be preloaded.

You will notice in the unit tests a failed attempt at getting a true comparison against QuickGraph. It crashes after some time. I'm open to help on finishing this comparison, and I'm very open to contributions in general.

The project supports the portable class library: .NET 4.5, Windows 8, and Windows Phone 8.1.

Instructions
------------

To use the library you will need some object representing a location in your search space. These are sometimes called nodes, sometimes verticies. It will be used as a dictionary key. Therefore, it is typical that this object will override its `GetHashCode()` and `Equals()` methods. This is especially important when using floating-point members. A floating-point vertix might look like this:

```csharp
		sealed class PointDbl
		{
			public PointDbl(double x, double y)
			{
				X = x; Y = y;
			}

			public readonly double X, Y;

			public override int GetHashCode()
			{
				return ((float)X).GetHashCode() ^ ((float)Y).GetHashCode();
			}

			public override bool Equals(object obj)
			{
				var other = obj as PointDbl;
				if (other == null)
					return false;
				// cast to float to allow some tolerance:
				return (float)X == (float)other.X && (float)Y == (float)other.Y;
			}
		}
```

To use the engine you need to specify several things:
 1. A starting point/vertex.
 2. An ending point/vertex.
 3. A method to return the neighbors of a vertex.
 4. A method to return the score/distance between two vertices.
 5. A method to return a projected score between a vertex and the ending vertex.

Example:

```csharp
		sealed class PointInt
		{
			public PointInt(int x, int y)
			{
				X = x; Y = y;
			}

			public readonly int X, Y;

			public override int GetHashCode()
			{
				return X.GetHashCode() ^ Y.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				var other = obj as PointInt;
				if (other == null)
					return false;
				return X == other.X && Y == other.Y;
			}

			public override string ToString()
			{
				return string.Format("X: {0}, Y: {1}", X, Y);
			}
		}

		[Test]
		public void FindShortestPath()
		{
			Func<PointInt, IEnumerable<PointInt>> getNeighbors = p => new[]
			{
				new PointInt(p.X - 1, p.Y + 0), // L
				new PointInt(p.X + 1, p.Y + 0), // R
				new PointInt(p.X + 0, p.Y - 1), // B
				new PointInt(p.X + 0, p.Y + 1), // T
			};

			Func<PointInt, PointInt, double> getScoreBetween = (p1, p2) =>
			{
				// Manhatten Distance
				return Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);
			};

			var rand = new Random(42);

			var start = new PointInt(rand.Next(1000), rand.Next(1000));
			var destination = new PointInt(rand.Next(1000), rand.Next(1000));

			Func<PointInt, double> getHeuristicScore = p =>
			{
				return Math.Abs(p.X - destination.X) + Math.Abs(p.Y - destination.Y);
			};

			Console.WriteLine("Going from {0} to {1}", start, destination);

			var sw = Stopwatch.StartNew();
			double distance;
			var results = AStarUtilities.FindMinimalPath(start, destination, getNeighbors, 
				getScoreBetween, getHeuristicScore, out distance);

			Console.WriteLine("Done in {0}s.", sw.Elapsed.TotalSeconds);
			Console.WriteLine("Expansions: {0}", AStarUtilities.LastExpansionCount);
			Console.WriteLine("Result Count: {0}", results.Count);
			Console.WriteLine("Distance: {0}", distance);

			Assert.AreEqual(start, results.First());
			Assert.AreEqual(destination, results.Last());
		}

```

I've recently added a bidirectional method: `AStarUtilities.BidirectionalFindMinimalPath`. Use it the same as the other overloads. It starts two threads each searching from opposite ends. It runs faster on my not-so-real-world test, but your mileage may vary. It does use a lot of memory. Indeed, this library is fairly aggressive on memory allocation in general. It relies on the fast allocation available in languages like C#.

If you just want a priority queue collection, use the `RandomMeldablePriorityQueue` class. Use its `Enqueue` and `Dequeue` methds to push and pop. It also hast fast arbitrary removal with the `Remove` method, which is different from many implementations.