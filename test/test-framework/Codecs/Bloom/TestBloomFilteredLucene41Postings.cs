using Lucene.Net.Index;
using Lucene.Net.Codecs.Lucene41;

namespace Lucene.Net.Codecs.Bloom.TestFramework
{
	/// <summary>
	/// A class used for testing
	/// <see cref="BloomFilteringPostingsFormat">BloomFilteringPostingsFormat</see>
	/// with a concrete
	/// delegate (Lucene41). Creates a Bloom filter on ALL fields and with tiny
	/// amounts of memory reserved for the filter. DO NOT USE IN A PRODUCTION
	/// APPLICATION This is not a realistic application of Bloom Filters as they
	/// ordinarily are larger and operate on only primary key type fields.
	/// </summary>
	public sealed class TestBloomFilteredLucene41Postings : PostingsFormat
	{
		private BloomFilteringPostingsFormat delegate_;

		internal class LowMemoryBloomFactory : BloomFilterFactory
		{
			// Special class used to avoid OOM exceptions where Junit tests create many
			// fields.
			public override FuzzySet GetSetForField(SegmentWriteState state, FieldInfo info)
			{
				return FuzzySet.CreateSetBasedOnMaxMemory(1024);
			}

			public override bool IsSaturated(FuzzySet bloomFilter, FieldInfo fieldInfo)
			{
				// For test purposes always maintain the BloomFilter - even past the point
				// of usefulness when all bits are set
				return false;
			}
		}

		public TestBloomFilteredLucene41Postings() : base("TestBloomFilteredLucene41Postings"
			)
		{
			delegate_ = new BloomFilteringPostingsFormat(new Lucene41PostingsFormat(), new TestBloomFilteredLucene41Postings.LowMemoryBloomFactory
				());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
		{
			return delegate_.FieldsConsumer(state);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldsProducer FieldsProducer(SegmentReadState state)
		{
			return delegate_.FieldsProducer(state);
		}
	}
}
