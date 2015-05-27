using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// to publish: nuget pack Kts.AStar\Kts.AStar.csproj -Prop Configuration=Release

namespace Kts.AStar
{
	public static class AStarUtilities
	{
		// would we allow people to override this class and fill in methods to generate the node data?

		private class EncapsulatedSearchNode<T> : SearchNodeBase<T>
		{
			private readonly double _h;
			public EncapsulatedSearchNode(
				T position, 
				SearchNodeBase<T> parent, 
				Func<T, T, double> getScoreBetween, 
				Func<T, double> getHeuristicScore)
				: base(position, parent, 
					parent == null ? 0.0 : getScoreBetween.Invoke(parent.Position, position))
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
		/// <param name="startingPosition"></param>
		/// <param name="endingPosition"></param>
		/// <param name="getNeighbors">Given a position, return all the neighbors directly accessible from that position.</param>
		/// <param name="getScoreBetween">Given two (adjacent) positions/nodes, return the score/distance between them.</param>
		/// <param name="getHeuristicScore">Given a position/node, return the (admissibly) estimated distance/score for that point to the endingPosition.</param>
		public static List<TPosition> FindMinimalPath<TPosition>(
			TPosition startingPosition, 
			TPosition endingPosition, 
			Func<TPosition, 
			IEnumerable<TPosition>> getNeighbors,
			Func<TPosition, TPosition, double> getScoreBetween, 
			Func<TPosition, double> getHeuristicScore, 
			out double distance)
		{
			var list = FindMinimalPath(new EncapsulatedSearchNode<TPosition>(startingPosition, null, (p1, p2) => 0.0, getHeuristicScore),
				endingPosition, node => getNeighbors.Invoke(node.Position).Select(neighbor => new EncapsulatedSearchNode<TPosition>(neighbor, node, getScoreBetween, getHeuristicScore)));

			distance = list.Last().G;
			var ret = new List<TPosition>(list.Count);
			foreach (var node in list)
				ret.Add(node.Position);
			return ret;
		}

		/// <summary>
		/// Returns the best path.
		/// TPosition will be used as a dictionary key.
		/// </summary>
		public static List<TNode> FindMinimalPath<TNode, TPosition>(
			TNode startingNode, 
			TPosition endPosition, 
			Func<TNode, IEnumerable<TNode>> getNeighbors)
			where TNode : SearchNodeBase<TPosition>
		{
			var comparer = EqualityComparer<TPosition>.Default;
			return FindMinimalPath<TNode, TPosition>(startingNode, (lookup, best) => comparer.Equals(best.Position, endPosition), getNeighbors);
		}

		/// <summary>
		/// Returns the best path.
		/// TPosition will be used as a dictionary key.
		/// </summary>
		/// <param name="getNeighbors">Given a position, return all the neighbors directly accessible from that position.</param>
		public static List<TNode> FindMinimalPath<TNode, TPosition>(
			TNode startingNode, 
			Func<Dictionary<TPosition, 
			RandomMeldablePriorityTree<TNode>>, TNode, bool> isDone, 
			Func<TNode, IEnumerable<TNode>> getNeighbors)
			where TNode : SearchNodeBase<TPosition>
		{
			TNode best;
			var lookup = new Dictionary<TPosition, RandomMeldablePriorityTree<TNode>>();
			var opens = new RandomMeldablePriorityTree<TNode>(startingNode);

			do
			{
				var lowest = opens.Element;

				// for dual direction, change this ending situation to be a lambda.
				// spawn two instances of the search method
				// in their lambda to see if they're done see if their position passed
				// exists in the lookup table here (and is open)
				// if that happens they are both done

				if (isDone.Invoke(lookup, lowest))
				{
					best = lowest;
					break;
				}

				opens = opens.DeleteMin();
#if DEBUG
				if (opens != null && opens.Parent != null)
				{
					throw new InvalidOperationException("Expected opens to always point to the root.");
				}
#endif
				lookup[lowest.Position] = null;

				foreach (var neighbor in getNeighbors.Invoke(lowest))
				{
					RandomMeldablePriorityTree<TNode> existing;
					if (lookup.TryGetValue(neighbor.Position, out existing))
					{
						if (existing == null) continue;
						opens = opens.DecreaseKey(existing, neighbor);
					}
					else
					{
						existing = new RandomMeldablePriorityTree<TNode>(neighbor);
						opens = RandomMeldablePriorityTree<TNode>.Meld(opens, existing);
						lookup[neighbor.Position] = existing;
					}
				}

				if (opens == null)
				{
					best = lowest;
					break;
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

		/// <summary>
		/// Returns the best path. It runs two searches on separate threads. One search starts from each end.
		/// TPosition will be used as a dictionary key.
		/// </summary>
		internal static List<TPosition> BidirectionalFindMinimalPath<TPosition>(TPosition startingPosition, TPosition endingPosition, Func<TPosition, IEnumerable<TPosition>> getNeighbors,
			Func<TPosition, TPosition, double> getScoreBetween, Func<TPosition, bool, double> getHeuristicScore, out double distance)
			where TPosition : class // needed for thread-safe assignment
		{
			EncapsulatedSearchNode<TPosition> best1 = null, best2 = null, allDone = null;
			RandomMeldablePriorityTree<EncapsulatedSearchNode<TPosition>> best1Node = null, best2Node = null;
			Func<Dictionary<TPosition, RandomMeldablePriorityTree<EncapsulatedSearchNode<TPosition>>>, EncapsulatedSearchNode<TPosition>, bool> thread1Done =
				(lookup, best) =>
				{
					if (allDone != null)
					{
						best1Node = null;
						return true;
					}
					best1 = best;
					var otherBest = best2;
					if (otherBest != null && lookup.TryGetValue(otherBest.Position, out best1Node) && best1Node != null)
					{
						allDone = otherBest;
						return true;
					}
					return false;
				};
			Func<Dictionary<TPosition, RandomMeldablePriorityTree<EncapsulatedSearchNode<TPosition>>>, EncapsulatedSearchNode<TPosition>, bool> thread2Done =
				(lookup, best) =>
				{
					if (allDone != null)
					{
						best2Node = null;
						return true;
					}
					best2 = best;
					var otherBest = best1;
					if (otherBest != null && lookup.TryGetValue(otherBest.Position, out best2Node) && best2Node != null)
					{
						allDone = otherBest;
						return true;
					}
					return false;
				};

			var startingNode = new EncapsulatedSearchNode<TPosition>(startingPosition, null, (p1, p2) => 0.0, p => getHeuristicScore.Invoke(p, false));
			var endingNode = new EncapsulatedSearchNode<TPosition>(endingPosition, null, (p1, p2) => 0.0, p => getHeuristicScore.Invoke(p, true));
			var task1 = Task.Run(() => FindMinimalPath(startingNode, thread1Done, node =>
				getNeighbors.Invoke(node.Position).Select(neighbor => new EncapsulatedSearchNode<TPosition>(neighbor, node, getScoreBetween, p => getHeuristicScore.Invoke(p, false)))));


			var task2 = Task.Run(() => FindMinimalPath(endingNode, thread2Done, node =>
				getNeighbors.Invoke(node.Position).Select(neighbor => new EncapsulatedSearchNode<TPosition>(neighbor, node, getScoreBetween, p => getHeuristicScore.Invoke(p, true)))));

			var path1 = task1.Result;
			var path2 = task2.Result;

			// the one that finished first has the right path returned
			// add that to the node still set for that path
			// if bestNode1 != null then task 1 finished first

			var ret = new List<TPosition>();

			if (best1Node != null)
			{
				ret.AddRange(path1.Select(p => p.Position));
				var element = allDone;
				distance = element.G + path1.Last().G;
				while (element != null)
				{
					ret.Add(element.Position);
					element = element.Parent as EncapsulatedSearchNode<TPosition>;
				}
				// these should already be in the right order since it was searching backward
			}
			else
			{
				var element = allDone;
				distance = element.G + path2.First().G;
				while (element != null)
				{
					ret.Add(element.Position);
					element = element.Parent as EncapsulatedSearchNode<TPosition>;
				}
				ret.Reverse();
				ret.AddRange(path2.Select(p => p.Position));
			}

			return ret;
		}
	}
}
