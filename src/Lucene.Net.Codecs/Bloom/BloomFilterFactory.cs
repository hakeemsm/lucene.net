/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs.Bloom;
using Lucene.Net.Index;
using Sharpen;

namespace Lucene.Net.Codecs.Bloom
{
	/// <summary>
	/// Class used to create index-time
	/// <see cref="FuzzySet">FuzzySet</see>
	/// appropriately configured for
	/// each field. Also called to right-size bitsets for serialization.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public abstract class BloomFilterFactory
	{
		/// <param name="state">The content to be indexed</param>
		/// <param name="info">the field requiring a BloomFilter</param>
		/// <returns>An appropriately sized set or null if no BloomFiltering required</returns>
		public abstract FuzzySet GetSetForField(SegmentWriteState state, FieldInfo info);

		/// <summary>Called when downsizing bitsets for serialization</summary>
		/// <param name="fieldInfo">The field with sparse set bits</param>
		/// <param name="initialSet">The bits accumulated</param>
		/// <returns>null or a hopefully more densely packed, smaller bitset</returns>
		public virtual FuzzySet Downsize(FieldInfo fieldInfo, FuzzySet initialSet)
		{
			// Aim for a bitset size that would have 10% of bits set (so 90% of searches
			// would fail-fast)
			float targetMaxSaturation = 0.1f;
			return initialSet.Downsize(targetMaxSaturation);
		}

		/// <summary>Used to determine if the given filter has reached saturation and should be retired i.e.
		/// 	</summary>
		/// <remarks>Used to determine if the given filter has reached saturation and should be retired i.e. not saved any more
		/// 	</remarks>
		/// <param name="bloomFilter">The bloomFilter being tested</param>
		/// <param name="fieldInfo">The field with which this filter is associated</param>
		/// <returns>true if the set has reached saturation and should be retired</returns>
		public abstract bool IsSaturated(FuzzySet bloomFilter, FieldInfo fieldInfo);
	}
}
