using System;
using System.Collections.Generic;

namespace Kts.AStar
{
	public abstract class SearchNodeBase<T> : IComparable<SearchNodeBase<T>>
	{
		protected SearchNodeBase(T position, SearchNodeBase<T> parent, double scoreFromParent)
		{
			Position = position;
			Parent = parent;
			G = scoreFromParent;
			if (parent != null)
				G += parent.G;
		}

		public T Position { get; private set; }

		public SearchNodeBase<T> Parent { get; private set; }

		/// <summary>
		/// The exact score/distance up to this node.
		/// </summary>
		public double G { get; private set; }

		/// <summary>
		/// This is the best guess at the score/distance from this node to the end not to exceed the actual value.
		/// </summary>
		public abstract double H { get; }

		/// <summary>
		/// G + H
		/// </summary>
		public double F { get { return G + H; } }

		public override int GetHashCode()
		{
			return Position.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as SearchNodeBase<T>;
			if (other == null) return false;
			var comparer = EqualityComparer<T>.Default;
			return comparer.Equals(Position, other.Position);
		}

		public override string ToString()
		{
			return string.Format("P: {0}, F: {1}, G: {2}, H: {3}", Position, F, G, H);
		}

		public int CompareTo(SearchNodeBase<T> other)
		{
			var ret = F.CompareTo(other.F);
			if (ret != 0)
				return ret;

			// wikipedia says that you should return itens in LIFO fashion for improved A* performance.
			// we could increment some node creation seed, but after some thought, 
			// I think falling back to an inverse G comparision should be almost equivalent; favor the larger G
			return other.G.CompareTo(G);
		}
	}
}
