/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>
	/// Holds all implementations of classes in the o.a.l.search package as a
	/// back-compatibility test.
	/// </summary>
	/// <remarks>
	/// Holds all implementations of classes in the o.a.l.search package as a
	/// back-compatibility test. It does not run any tests per-se, however if
	/// someone adds a method to an interface or abstract method to an abstract
	/// class, one of the implementations here will fail to compile and so we know
	/// back-compat policy was violated.
	/// </remarks>
	internal sealed class JustCompileSearch
	{
		private static readonly string UNSUPPORTED_MSG = "unsupported: used for back-compat testing only !";

		internal sealed class JustCompileCollector : Collector
		{
			public override void Collect(int doc)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override void SetNextReader(AtomicReaderContext context)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override void SetScorer(Scorer scorer)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileDocIdSet : DocIdSet
		{
			public override DocIdSetIterator Iterator()
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileDocIdSetIterator : DocIdSetIterator
		{
			public override int DocID
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override int NextDoc()
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override int Advance(int target)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override long Cost()
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileExtendedFieldCacheLongParser : FieldCache.LongParser
		{
			public long ParseLong(BytesRef @string)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public Lucene.Net.Index.TermsEnum TermsEnum(Terms terms)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileExtendedFieldCacheDoubleParser : FieldCache.DoubleParser
		{
			public double ParseDouble(BytesRef term)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public Lucene.Net.Index.TermsEnum TermsEnum(Terms terms)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileFieldComparator : FieldComparator<object>
		{
			public override int Compare(int slot1, int slot2)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override int CompareBottom(int doc)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override void Copy(int slot, int doc)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override void SetBottom(int slot)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override void SetTopValue(object value)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override FieldComparator<object> SetNextReader(AtomicReaderContext context
				)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override object Value(int slot)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override int CompareTop(int doc)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileFieldComparatorSource : FieldComparatorSource
		{
			public override FieldComparator<object> NewComparator(string fieldname, int numHits
				, int sortPos, bool reversed)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileFilter : Filter
		{
			// Filter is just an abstract class with no abstract methods. However it is
			// still added here in case someone will add abstract methods in the future.
			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				return null;
			}
		}

		internal sealed class JustCompileFilteredDocIdSet : FilteredDocIdSet
		{
			public JustCompileFilteredDocIdSet(DocIdSet innerSet) : base(innerSet)
			{
			}

			protected override bool Match(int docid)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileFilteredDocIdSetIterator : FilteredDocIdSetIterator
		{
			public JustCompileFilteredDocIdSetIterator(DocIdSetIterator innerIter) : base(innerIter
				)
			{
			}

			protected override bool Match(int doc)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override long Cost()
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileQuery : Query
		{
			public override string ToString(string field)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileScorer : Scorer
		{
			protected JustCompileScorer(Weight weight) : base(weight)
			{
			}

			public override float Score()
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override int Freq
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override int DocID
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override int NextDoc()
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override int Advance(int target)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override long Cost()
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileSimilarity : Similarity
		{
			public override Similarity.SimWeight ComputeWeight(float queryBoost, CollectionStatistics
				 collectionStats, params TermStatistics[] termStats)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override Similarity.SimScorer SimScorer(Similarity.SimWeight stats, AtomicReaderContext
				 context)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override long ComputeNorm(FieldInvertState state)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileTopDocsCollector : TopDocsCollector<ScoreDoc>
		{
			protected JustCompileTopDocsCollector(PriorityQueue<ScoreDoc> pq) : base(pq)
			{
			}

			public override void Collect(int doc)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override void SetNextReader(AtomicReaderContext context)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override void SetScorer(Scorer scorer)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override bool AcceptsDocsOutOfOrder()
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override Lucene.Net.Search.TopDocs TopDocs()
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override Lucene.Net.Search.TopDocs TopDocs(int start)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override Lucene.Net.Search.TopDocs TopDocs(int start, int end)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}

		internal sealed class JustCompileWeight : Weight
		{
			public override Explanation Explain(AtomicReaderContext context, int doc)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override Query GetQuery()
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override void Normalize(float norm, float topLevelBoost)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override float GetValueForNormalization()
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}

			public override Lucene.Net.Search.Scorer Scorer(AtomicReaderContext context
				, Bits acceptDocs)
			{
				throw new NotSupportedException(UNSUPPORTED_MSG);
			}
		}
	}
}
