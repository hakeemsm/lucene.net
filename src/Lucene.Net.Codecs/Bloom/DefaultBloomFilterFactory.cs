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
	/// <summary>Default policy is to allocate a bitset with 10% saturation given a unique term per document.
	/// 	</summary>
	/// <remarks>
	/// Default policy is to allocate a bitset with 10% saturation given a unique term per document.
	/// Bits are set via MurmurHash2 hashing function.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class DefaultBloomFilterFactory : BloomFilterFactory
	{
		public override FuzzySet GetSetForField(SegmentWriteState state, FieldInfo info)
		{
			//Assume all of the docs have a unique term (e.g. a primary key) and we hope to maintain a set with 10% of bits set
			return FuzzySet.CreateSetBasedOnQuality(state.segmentInfo.GetDocCount(), 0.10f);
		}

		public override bool IsSaturated(FuzzySet bloomFilter, FieldInfo fieldInfo)
		{
			// Don't bother saving bitsets if >90% of bits are set - we don't want to
			// throw any more memory at this problem.
			return bloomFilter.GetSaturation() > 0.9f;
		}
	}
}
