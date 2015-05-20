using System;
using System.Collections.Generic;
using System.Linq;

// to publish: nuget pack Kts.AStar\Kts.AStar.csproj -Prop Configuration=Release

namespace Kts.AStar
{
	public static class AStarUtilities
	{
		// would we allow people to override this class and fill in methods to generate the node data?

		private class EncapsulatedSearchNode<T> : SearchNodeBase<T>
		{
			private readonly double _h;
			public EncapsulatedSearchNode(T position, SearchNodeBase<T> parent, Func<T, T, double> getScoreBetween, Func<T, double> getHeuristicScore)
				: base(position, parent, parent == null ? 0.0 : getScoreBetween.Invoke(parent.Position, position))
			{
				_h = getHeuristicScore.Invoke(position);
			}

			public override double H
			{
				get { return _h; }
			}
		}

		/// <summary>
		/// Returns the best path.
		/// TPosition will be used as a dictionary key.
		/// </summary>
		public static List<TPosition> FindMinimalPath<TPosition>(TPosition startingPosition, TPosition endingPosition, Func<TPosition, IEnumerable<TPosition>> getNeighbors,
			Func<TPosition, TPosition, double> getScoreBetween, Func<TPosition, double> getHeuristicScore, out double distance)
		{
			var list = FindMinimalPath(new EncapsulatedSearchNode<TPosition>(startingPosition, null, (p1, p2) => 0.0, getHeuristicScore), 
				endingPosition, node => getNeighbors.Invoke(node.Position).Select(neighbor => new EncapsulatedSearchNode<TPosition>(neighbor, node, getScoreBetween, getHeuristicScore)));

			distance = list.Last().G;
			var ret = new List<TPosition>(list.Count);
			foreach(var node in list)
				ret.Add(node.Position);
			return ret;
		}

		/// <summary>
		/// Returns the best path.
		/// TPosition will be used as a dictionary key.
		/// </summary>
		public static List<TNode> FindMinimalPath<TNode, TPosition>(TNode startingNode, TPosition endPosition, Func<TNode, IEnumerable<TNode>> getNeighbors)
			where TNode : SearchNodeBase<TPosition>
		{
			TNode best;
			var lookup = new Dictionary<TPosition, RandomMeldablePriorityQueue<TNode>>();
			var opens = new RandomMeldablePriorityQueue<TNode>(startingNode);

			do
			{
				var lowest = opens.Element;

				if (Equals(lowest.Position, endPosition))
				{
					best = lowest;
					break;
				}

				opens = opens.DeleteMin();
				lookup[lowest.Position] = null;

				foreach (var neighbor in getNeighbors.Invoke(lowest))
				{
					RandomMeldablePriorityQueue<TNode> existing;
					if (lookup.TryGetValue(neighbor.Position, out existing))
					{
						if (existing == null) continue;
						opens = opens.DecreaseKey(existing, neighbor);
					}
					else
					{
						existing = new RandomMeldablePriorityQueue<TNode>(neighbor);
						opens = RandomMeldablePriorityQueue<TNode>.Meld(opens, existing);
						lookup[neighbor.Position] = existing;
					}
				}
			} while (true);

			LastExpansionCount = lookup.Count;

			var ret = new List<TNode>();
			while (best != null)
			{
				ret.Add(best);
				best = best.Parent as TNode;
			}
			ret.Reverse();
			return ret;
		}

		public static int LastExpansionCount { get; private set; }
	}
}
