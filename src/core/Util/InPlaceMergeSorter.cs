namespace Lucene.Net.Util
{
	/// <summary>
	/// <see cref="Sorter">Sorter</see>
	/// implementation based on the merge-sort algorithm that merges
	/// in place (no extra memory will be allocated). Small arrays are sorted with
	/// insertion sort.
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public abstract class InPlaceMergeSorter : Sorter
	{
		/// <summary>
		/// Create a new
		/// <see cref="InPlaceMergeSorter">InPlaceMergeSorter</see>
		/// 
		/// </summary>
		public InPlaceMergeSorter()
		{
		}

		public sealed override void Sort(int from, int to)
		{
			CheckRange(from, to);
			MergeSort(from, to);
		}

		internal virtual void MergeSort(int from, int to)
		{
			if (to - from < THRESHOLD)
			{
				InsertionSort(from, to);
			}
			else
			{
				int mid = (int)(((uint)(from + to)) >> 1);
				MergeSort(from, mid);
				MergeSort(mid, to);
				MergeInPlace(from, mid, to);
			}
		}
	}
}
