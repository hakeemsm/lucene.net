using Lucene.Net.Index;

namespace Lucene.Net.Util
{
	/// <summary>Abstraction over an array of longs.</summary>
	/// <remarks>
	/// Abstraction over an array of longs.
	/// This class extends NumericDocValues so that we don't need to add another
	/// level of abstraction every time we want eg. to use the
	/// <see cref="Lucene.Net.Util.Packed.PackedInts">Lucene.Net.Util.Packed.PackedInts
	/// 	</see>
	/// utility classes to represent a
	/// <see cref="Lucene.Net.Index.NumericDocValues">Lucene.Net.Index.NumericDocValues
	/// 	</see>
	/// instance.
	/// </remarks>
	/// <lucene.internal></lucene.internal>
	public abstract class LongValues : NumericDocValues
	{
		/// <summary>Get value at <code>index</code>.</summary>
		/// <remarks>Get value at <code>index</code>.</remarks>
		public abstract long Get(long index);

		public override long Get(int idx)
		{
			return Get((long)idx);
		}
	}
}
