using Lucene.Net.Support;

namespace Lucene.Net.Util
{
	/// <summary>
	/// <see cref="Sorter">Sorter</see>
	/// implementation based on a variant of the quicksort algorithm
	/// called <a href="http://en.wikipedia.org/wiki/Introsort">introsort</a>: when
	/// the recursion level exceeds the log of the length of the array to sort, it
	/// falls back to heapsort. This prevents quicksort from running into its
	/// worst-case quadratic runtime. Small arrays are sorted with
	/// insertion sort.
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public abstract class IntroSorter : Sorter
	{
		internal static int CeilLog2(int n)
		{
			return (8*sizeof(int)) - (n-1).NumberOfLeadingZeros();
		}

	    public sealed override void Sort(int from, int to)
		{
			CheckRange(from, to);
			Quicksort(from, to, CeilLog2(to - from));
		}

		internal virtual void Quicksort(int from, int to, int maxDepth)
		{
			if (to - from < THRESHOLD)
			{
				InsertionSort(from, to);
				return;
			}
		    if (--maxDepth < 0)
		    {
		        HeapSort(@from, to);
		        return;
		    }
		    int mid = (int)(((uint)(from + to)) >> 1);
			if (Compare(from, mid) > 0)
			{
				Swap(from, mid);
			}
			if (Compare(mid, to - 1) > 0)
			{
				Swap(mid, to - 1);
				if (Compare(from, mid) > 0)
				{
					Swap(from, mid);
				}
			}
			int left = from + 1;
			int right = to - 2;
			SetPivot(mid);
			for (; ; )
			{
				while (ComparePivot(right) < 0)
				{
					--right;
				}
				while (left < right && ComparePivot(left) >= 0)
				{
					++left;
				}
				if (left < right)
				{
					Swap(left, right);
					--right;
				}
				else
				{
					break;
				}
			}
			Quicksort(from, left + 1, maxDepth);
			Quicksort(left + 1, to, maxDepth);
		}

		/// <summary>
		/// Save the value at slot <code>i</code> so that it can later be used as a
		/// pivot, see
		/// <see cref="ComparePivot(int)">ComparePivot(int)</see>
		/// .
		/// </summary>
		protected internal abstract void SetPivot(int i);

		/// <summary>
		/// Compare the pivot with the slot at <code>j</code>, similarly to
		/// <see cref="Sorter.Compare(int, int)">compare(i, j)</see>
		/// .
		/// </summary>
		protected internal abstract int ComparePivot(int j);
	}
}
