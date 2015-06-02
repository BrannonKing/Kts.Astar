using System;
using System.Collections.Generic;

namespace Kts.AStar
{
	/// <summary>
	/// This is a priority queue similar to a MinBinaryHeap. Instead of calling insert, you make a new heap and meld it to your current one. DecreaseKey and DeleteMin are similar.
	/// </summary>
	public sealed class RandomMeldablePriorityTree<T> where T : IComparable<T>
	{
		private static int _childrenCount = 4;

		/// <summary>
		/// Number of children allocated for each node.
		/// </summary>
		public static int ChildrenCount
		{
			get { return _childrenCount; }
			set
			{
				if (value < 2 || value >= 256)
					throw new ArgumentOutOfRangeException("value", value, "The value must be creater than 1 and less than 256.");
				_childrenCount = value;
			}
		}


		private readonly RandomMeldablePriorityTree<T>[] _children;

		public RandomMeldablePriorityTree(T firstElement)
		{
			Element = firstElement;
			_children = new RandomMeldablePriorityTree<T>[ChildrenCount];
		}

		public T Element { get; private set; }

		public RandomMeldablePriorityTree<T> Parent { get; private set; }

		public IReadOnlyList<RandomMeldablePriorityTree<T>> Children { get { return _children; } }

		/// <summary>
		/// Return a new heap containing the additional element.
		/// </summary>
		public static RandomMeldablePriorityTree<T> Meld(RandomMeldablePriorityTree<T> q1, T element)
		{
			return Meld(q1, new RandomMeldablePriorityTree<T>(element));
		}

		private static void BreakConnectionToParent(RandomMeldablePriorityTree<T> node, RandomMeldablePriorityTree<T> replacement = null)
		{
			if (node == null) return;
			if (node.Parent == null) return;
			for (int i = 0; i < node.Parent._children.Length; i++)
			{
				if (node.Parent._children[i] == node)
				{
					node.Parent._children[i] = replacement;
					break;
				}
			}
			node.Parent = null;
		}

		/// <summary>
		/// Merge two heaps into one.
		/// </summary>
		public static RandomMeldablePriorityTree<T> Meld(RandomMeldablePriorityTree<T> q1, RandomMeldablePriorityTree<T> q2)
		{
			return Meld(q1, q2, x => ThreadLocalXorShifter.NextRandom(q1._children.Length));
		}

		/// <summary>
		/// Merge two heaps into one.
		/// </summary>
		public static RandomMeldablePriorityTree<T> Meld(RandomMeldablePriorityTree<T> q1, RandomMeldablePriorityTree<T> q2,
			Func<int, byte> getChildIndex)
		{
			// think this through:
			// we will return either q1 or q2 (and if either is null, return is obvious)
			if (q1 == null)
			{
				BreakConnectionToParent(q2);
				return q2;
			}
			if (q2 == null)
			{
				BreakConnectionToParent(q1);
				return q1;
			}
			if (q1 == q2)
			{
#if DEBUG
				throw new InvalidOperationException("Merging with self was not expected.");
#else
				return q1;
#endif
			}

			// q1 > q2, swap them so that q1 is the smallest
			if (q1.Element.CompareTo(q2.Element) > 0)
			{
				var tmp = q1;
				q1 = q2;
				q2 = tmp;
			}

			var ret = q1;
			BreakConnectionToParent(ret);

			do
			{
				// pick a random child branch
				var childIdx = getChildIndex.Invoke(q1._children.Length);

				// at this point q2 is larger than or equal to q1
				if (q1._children[childIdx] == null)
				{
					q2.Parent = q1;
					q1._children[childIdx] = q2;
					break;
				}

				// if the random child of q1 is less than or equal to q2 make that q1 the new head
				if (q1._children[childIdx].Element.CompareTo(q2.Element) <= 0)
				{
					q1 = q1._children[childIdx];
					continue;
				}

				// our random child is larger than our q2 needing to be merged
				// things just got ugly: do the insert
				// we are going to disconnect the q1Child and replace it with q2
				// we are then going to continue with q2 in place of q1 and the child that needs to be merged as q2
				var tmp = q1._children[childIdx];
				q1._children[childIdx] = q2;
				q2.Parent = q1;
				q1 = q2; // q1 has to be the smaller
				q2 = tmp; // tmp is larger than q2
			} while (true);

			return ret;
		}

		// this is quite a bit slower than the above implementation but much easier to read:
		//public static RandomMeldablePriorityQueue<T> Meld(RandomMeldablePriorityQueue<T> q1, RandomMeldablePriorityQueue<T> q2)
		//{
		//	if (q1 == null) return q2;
		//	if (q2 == null) return q1;
		//	if (q1.Element.CompareTo(q2.Element) > 0)
		//	{
		//		var tmp = q1;
		//		q1 = q2;
		//		q2 = tmp;
		//	}
		//	var childIdx = _rand.Next(q1._children.Length);
		//	q1._children[childIdx] = Meld(q1._children[childIdx], q2);
		//	q1._children[childIdx]._parent = q1;
		//	return q1;
		//}

		/// <summary>
		/// Remove the root of the heap and return the updated heap.
		/// </summary>
		public RandomMeldablePriorityTree<T> DeleteMin()
		{
			var parent = Parent;
			var newRoot = Meld(_children[0], _children[1]);
			for (var i = 2; i < _children.Length; i++)
				newRoot = Meld(newRoot, _children[i]);

			if (parent != null)
			{
				// really, this should never happen in standard A* usage
				BreakConnectionToParent(this, newRoot);
				if (newRoot != null)
				{
					newRoot.Parent = parent;
				}
			}
			return newRoot;
		}

		/// <summary>
		/// Modify a node in the heap that has a new score and return the new heap.
		/// </summary>
		public RandomMeldablePriorityTree<T> DecreaseKey(RandomMeldablePriorityTree<T> elementToBeChanged, T newElement)
		{
			if (elementToBeChanged.Element.CompareTo(newElement) <= 0) return this;

			elementToBeChanged.Element = newElement;
			if (elementToBeChanged.Parent != null)
			{
				BreakConnectionToParent(elementToBeChanged);
				return Meld(this, elementToBeChanged);
			}

			return this; // we must already be the lowest value item
		}
	}
}