namespace Lucene.Net.Index
{
	/// <summary>
	/// Extension of
	/// <see cref="SortedSetDocValues">SortedSetDocValues</see>
	/// that supports random access
	/// to the ordinals of a document.
	/// <p>
	/// Operations via this API are independent of the iterator api (
	/// <see cref="SortedSetDocValues.NextOrd()">SortedSetDocValues.NextOrd()</see>
	/// )
	/// and do not impact its state.
	/// <p>
	/// Codecs can optionally extend this API if they support constant-time access
	/// to ordinals for the document.
	/// </summary>
	public abstract class RandomAccessOrds : SortedSetDocValues
	{
		/// <summary>Sole constructor.</summary>
		/// <remarks>
		/// Sole constructor. (For invocation by subclass
		/// constructors, typically implicit.)
		/// </remarks>
		public RandomAccessOrds()
		{
		}

		/// <summary>
		/// Retrieve the ordinal for the current document (previously
		/// set by
		/// <see cref="SortedSetDocValues.SetDocument(int)">SortedSetDocValues.SetDocument(int)
		/// 	</see>
		/// at the specified index.
		/// <p>
		/// An index ranges from
		/// <code>0</code>
		/// to
		/// <code>cardinality()-1</code>
		/// .
		/// The first ordinal value is at index
		/// <code>0</code>
		/// , the next at index
		/// <code>1</code>
		/// ,
		/// and so on, as for array indexing.
		/// </summary>
		/// <param name="index">index of the ordinal for the document.</param>
		/// <returns>ordinal for the document at the specified index.</returns>
		public abstract long OrdAt(int index);

		/// <summary>
		/// Returns the cardinality for the current document (previously
		/// set by
		/// <see cref="SortedSetDocValues.SetDocument(int)">SortedSetDocValues.SetDocument(int)
		/// 	</see>
		/// .
		/// </summary>
		public abstract int Cardinality();
	}
}
