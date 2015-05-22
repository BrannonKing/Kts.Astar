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
		/// <param name="startingPosition"></param>
		/// <param name="endingPosition"></param>
		/// <param name="getNeighbors">Given a position, return all the neighbors directly accessible from that position.</param>
		/// <param name="getScoreBetween">Given two (adjacent) positions/nodes, return the score/distance between them.</param>
		/// <param name="getHeuristicScore">Given a position/node, return the (admissibly) estimated distance/score for that point to the endingPosition.</param>
		public static List<TPosition> FindMinimalPath<TPosition>(TPosition startingPosition, TPosition endingPosition, Func<TPosition, IEnumerable<TPosition>> getNeighbors,
			Func<TPosition, TPosition, double> getScoreBetween, Func<TPosition, double> getHeuristicScore, out double distance)
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
		public static List<TNode> FindMinimalPath<TNode, TPosition>(TNode startingNode, TPosition endPosition, Func<TNode, IEnumerable<TNode>> getNeighbors)
			where TNode : SearchNodeBase<TPosition>
		{
			return FindMinimalPath<TNode, TPosition>(startingNode, (lookup, best) => Equals(best, endPosition), getNeighbors);
		}

		/// <summary>
		/// Returns the best path.
		/// TPosition will be used as a dictionary key.
		/// </summary>
		/// <param name="getNeighbors">Given a position, return all the neighbors directly accessible from that position.</param>
		public static List<TNode> FindMinimalPath<TNode, TPosition>(TNode startingNode, Func<Dictionary<TPosition, RandomMeldablePriorityQueue<TNode>>, TPosition, bool> isDone, Func<TNode, IEnumerable<TNode>> getNeighbors)
			where TNode : SearchNodeBase<TPosition>
		{
			TNode best;
			var lookup = new Dictionary<TPosition, RandomMeldablePriorityQueue<TNode>>();
			var opens = new RandomMeldablePriorityQueue<TNode>(startingNode);

			do
			{
				var lowest = opens.Element;

				// for dual direction, change this ending situation to be a lambda.
				// spawn two instances of the search method
				// in their lambda to see if they're done see if their position passed
				// exists in the lookup table here (and is open)
				// if that happens they are both done

				if (isDone.Invoke(lookup, lowest.Position))
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

		/// <summary>
		/// Returns the best path. It runs two searches on separate threads. One search starts from each end.
		/// TPosition will be used as a dictionary key.
		/// </summary>
		public static List<TPosition> FindMinimalPathDual<TPosition>(TPosition startingPosition, TPosition endingPosition, Func<TPosition, IEnumerable<TPosition>> getNeighbors,
			Func<TPosition, TPosition, double> getScoreBetween, Func<TPosition, bool, double> getHeuristicScore, out double distance)
			where TPosition : class // needed for thread-safe assignment
		{
			TPosition best1 = null, best2 = null;
			RandomMeldablePriorityQueue<EncapsulatedSearchNode<TPosition>> best1Node = null, best2Node = null;
			bool allDone = false;
			Func<Dictionary<TPosition, RandomMeldablePriorityQueue<EncapsulatedSearchNode<TPosition>>>, TPosition, bool> thread1Done =
				(lookup, best) =>
				{
					if (allDone && best1 != null)
					{
						best1Node = lookup[best1];
						return true;
					}
					best1 = best;
					if (lookup.TryGetValue(best2, out best1Node) && best1Node != null)
					{
						allDone = true;
						return true;
					}
					return false;
				};
			Func<Dictionary<TPosition, RandomMeldablePriorityQueue<EncapsulatedSearchNode<TPosition>>>, TPosition, bool> thread2Done =
				(lookup, best) =>
				{
					if (allDone && best2 != null)
					{
						best2Node = lookup[best2];
						return true;
					}
					best2 = best;
					if (lookup.TryGetValue(best1, out best2Node) && best2Node != null)
					{
						allDone = true;
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

			task1.Wait();
			task2.Wait();

			// trace best1Node to start and best2Node to end, revese the first

			var ret = new List<TPosition>();
			var element = best1Node.Element;
			distance = element.G;
			while (element != null)
			{
				ret.Add(element.Position);
				element = element.Parent as EncapsulatedSearchNode<TPosition>;
			}
			ret.Reverse();

			element = best2Node.Element;
			distance += element.G;
			while (element != null)
			{
				ret.Add(element.Position);
				element = element.Parent as EncapsulatedSearchNode<TPosition>;
			}
			return ret;
		}

	}
}
