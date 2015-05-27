using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kts.AStar
{
	internal class RandomMeldablePriorityQueue<T>: ICollection<T>
		where T: IComparable<T>
	{
		private RandomMeldablePriorityTree<T> _tree;
		private int _count;
		public void Add(T item)
		{
			_tree = RandomMeldablePriorityTree<T>.Meld(_tree, item);
			_count++;
		}

		public void Clear()
		{
			_tree = null;
			_count = 0;
		}

		public bool Contains(T item)
		{
			return Contains(_tree, item);
		}

		private static bool Contains(RandomMeldablePriorityTree<T> tree, T item){
			if (tree == null) return false;
			var comparer = EqualityComparer<T>.Default;

			if (comparer.Equals(tree.Element, item))
				return true;

			foreach (var child in tree.Children)
			{
				if (Contains(child, item))
					return true;
			}
			return false;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public int Count
		{
			get { return _count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			throw new NotImplementedException();
		}

		public IEnumerator<T> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
