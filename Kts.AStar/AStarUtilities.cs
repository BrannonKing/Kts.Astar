using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
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
			Func<TPosition, IEnumerable<TPosition>> getNeighbors,
			Func<TPosition, TPosition, double> getScoreBetween,
			Func<TPosition, double> getHeuristicScore,
			out double distance,
			out bool success,
			CancellationToken token = new CancellationToken())
		{
			var list = FindMinimalPath(
				new EncapsulatedSearchNode<TPosition>(startingPosition, null, (p1, p2) => 0.0, getHeuristicScore),
				endingPosition,
				node => getNeighbors.Invoke(node.Position).Select(neighbor => new EncapsulatedSearchNode<TPosition>(neighbor, node, getScoreBetween, getHeuristicScore)),
				out success,
				token
				);

			var ret = new List<TPosition>(list.Count);
			if (list.Count <= 0)
			{
				distance = double.PositiveInfinity;
				return ret;
			}

			distance = list.Last().G;
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
			Func<TNode, IEnumerable<TNode>> getNeighbors,
			out bool success,
			CancellationToken token = new CancellationToken())
			where TNode : SearchNodeBase<TPosition>
		{
			var comparer = EqualityComparer<TPosition>.Default;
			Func<Dictionary<TPosition, RandomMeldablePriorityTree<TNode>>, TNode, bool> isDone;
			if (token.CanBeCanceled)
				isDone = (lookup, best) =>
				{
					if (token.IsCancellationRequested)
						return true;
					return comparer.Equals(best.Position, endPosition);
				};
			else
				isDone = (lookup, best) => comparer.Equals(best.Position, endPosition);
			
			var ret = FindMinimalPath(startingNode, isDone, getNeighbors);
			success = ret.Count > 0 && comparer.Equals(ret.Last().Position, endPosition);
			return ret;
		}

		/// <summary>
		/// Returns the best path.
		/// TPosition will be used as a dictionary key.
		/// </summary>
		/// <param name="getNeighbors">Given a position, return all the neighbors directly accessible from that position.</param>
		public static List<TNode> FindMinimalPath<TNode, TPosition>(
			TNode startingNode,
			Func<Dictionary<TPosition, RandomMeldablePriorityTree<TNode>>, TNode, bool> isDone,
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
				lookup[lowest.Position] = null; // keep this below the isDone check

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
		public static List<TPosition> BidirectionalFindMinimalPath<TPosition>(TPosition startingPosition, TPosition endingPosition, Func<TPosition, IEnumerable<TPosition>> getNeighbors,
			Func<TPosition, TPosition, double> getScoreBetween, Func<TPosition, bool, double> getHeuristicScore, out double distance, CancellationToken token = new CancellationToken())
			where TPosition : class // needed for thread-safe assignment
		{
			var dictionary = new ConcurrentDictionary<TPosition, EncapsulatedSearchNode<TPosition>>();
			Dictionary<TPosition, RandomMeldablePriorityTree<EncapsulatedSearchNode<TPosition>>> lookup1 = null, lookup2 = null;
			Func<Dictionary<TPosition, RandomMeldablePriorityTree<EncapsulatedSearchNode<TPosition>>>, EncapsulatedSearchNode<TPosition>, bool> thread1Done =
				(lookup, best) =>
				{
					if (!dictionary.TryAdd(best.Position, best))
					{
						lookup1 = lookup;
						return true;
					}
					return false;
				};
			Func<Dictionary<TPosition, RandomMeldablePriorityTree<EncapsulatedSearchNode<TPosition>>>, EncapsulatedSearchNode<TPosition>, bool> thread2Done =
				(lookup, best) =>
				{
					if (!dictionary.TryAdd(best.Position, best))
					{
						lookup2 = lookup;
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

			LastExpansionCount = lookup1.Count + lookup2.Count;

			var overlap = lookup1.Keys.Intersect(lookup2.Keys);
			var kvps = overlap.ToDictionary(o => o, o => GetG(lookup1, dictionary, o) + GetG(lookup2, dictionary, o));

			var ret = new List<TPosition>();

			var first = kvps.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).FirstOrDefault();
			if (first == null)
			{
				distance = double.PositiveInfinity;
				return ret;
			}

			SearchNodeBase<TPosition> node1 = GetNode(lookup1, dictionary, first);
			SearchNodeBase<TPosition> node2 = GetNode(lookup2, dictionary, first);
			
			distance = node1.G + node2.G;
			while (node1 != null)
			{
				ret.Add(node1.Position);
				node1 = node1.Parent;
			}
			ret.Reverse();
			ret.RemoveAt(ret.Count - 1); // don't double-add the overlap
			while (node2 != null)
			{
				ret.Add(node2.Position);
				node2 = node2.Parent;
			}

			return ret;
		}

		private static double GetG<TPosition>(Dictionary<TPosition, RandomMeldablePriorityTree<EncapsulatedSearchNode<TPosition>>> lookup, ConcurrentDictionary<TPosition, EncapsulatedSearchNode<TPosition>> dictionary, TPosition key)
		{
			RandomMeldablePriorityTree<EncapsulatedSearchNode<TPosition>> tree;
			if (lookup.TryGetValue(key, out tree) && tree != null)
				return tree.Element.G;
			EncapsulatedSearchNode<TPosition> node;
			if (dictionary.TryGetValue(key, out node))
				return node.G;
			return double.PositiveInfinity;
		}

		private static EncapsulatedSearchNode<TPosition> GetNode<TPosition>(Dictionary<TPosition, RandomMeldablePriorityTree<EncapsulatedSearchNode<TPosition>>> lookup, ConcurrentDictionary<TPosition, EncapsulatedSearchNode<TPosition>> dictionary, TPosition key)
		{
			RandomMeldablePriorityTree<EncapsulatedSearchNode<TPosition>> tree;
			if (lookup.TryGetValue(key, out tree) && tree != null)
				return tree.Element;
			return dictionary[key];
		}
	}
}
