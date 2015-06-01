using System;
using System.Collections.Generic;
using System.Linq;

namespace Kts.AStar
{
	/// <summary>
	/// This class is not thread-safe.
	/// </summary>
	public class RandomMeldablePriorityQueue<T>: ICollection<T>
		where T: IComparable<T>
	{
		private RandomMeldablePriorityTree<T> _tree;
		private readonly Dictionary<T, RandomMeldablePriorityTree<T>> _dictionary = new Dictionary<T, RandomMeldablePriorityTree<T>>();
		public void Add(T item)
		{
			Enqueue(item);	
		}

		public void Enqueue(T item)
		{
			RandomMeldablePriorityTree<T> node;
			if (_dictionary.TryGetValue(item, out node))
			{
				_tree = _tree.DecreaseKey(node, item);
			}
			else
			{
				node = new RandomMeldablePriorityTree<T>(item);
				_tree = RandomMeldablePriorityTree<T>.Meld(_tree, node);
			}
			_dictionary[item] = node;
		}

		public T Dequeue()
		{
			if (_tree == null) // return default(T) instead?
				throw new InvalidOperationException("The queue is empty.");

			var element = _tree.Element;
			_tree = _tree.DeleteMin();
			_dictionary.Remove(element);
			return element;
		}

		public void Clear()
		{
			_tree = null;
			_dictionary.Clear();
		}

		public bool Contains(T item)
		{
			return _dictionary.ContainsKey(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (var key in _dictionary.Keys.OrderBy(x => x))
				array[arrayIndex++] = key;
		}

		public int Count
		{
			get { return _dictionary.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			RandomMeldablePriorityTree<T> node;
			if (_dictionary.TryGetValue(item, out node))
			{
				_dictionary.Remove(item);
				node.DeleteMin();
				return true;
			}
			return false;
		}

		/// <summary>
		/// This does not return the items in sorted order (presently).
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			return _dictionary.Keys.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
