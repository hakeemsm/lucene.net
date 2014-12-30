/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>random sorting tests</summary>
	public class TestSortRandom : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandomStringSort()
		{
			Random random = new Random(Random().NextLong());
			int NUM_DOCS = AtLeast(100);
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(random, dir);
			bool allowDups = random.NextBoolean();
			ICollection<string> seen = new HashSet<string>();
			int maxLength = TestUtil.NextInt(random, 5, 100);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS + " maxLength=" + maxLength
					 + " allowDups=" + allowDups);
			}
			int numDocs = 0;
			IList<BytesRef> docValues = new List<BytesRef>();
			// TODO: deletions
			while (numDocs < NUM_DOCS)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				// 10% of the time, the document is missing the value:
				BytesRef br;
				if (Random().Next(10) != 7)
				{
					string s;
					if (random.NextBoolean())
					{
						s = TestUtil.RandomSimpleString(random, maxLength);
					}
					else
					{
						s = TestUtil.RandomUnicodeString(random, maxLength);
					}
					if (!allowDups)
					{
						if (seen.Contains(s))
						{
							continue;
						}
						seen.Add(s);
					}
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  " + numDocs + ": s=" + s);
					}
					br = new BytesRef(s);
					if (DefaultCodecSupportsDocValues())
					{
						doc.Add(new SortedDocValuesField("stringdv", br));
						doc.Add(new NumericDocValuesField("id", numDocs));
					}
					else
					{
						doc.Add(NewStringField("id", Extensions.ToString(numDocs), Field.Store.NO
							));
					}
					doc.Add(NewStringField("string", s, Field.Store.NO));
					docValues.Add(br);
				}
				else
				{
					br = null;
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  " + numDocs + ": <missing>");
					}
					docValues.Add(null);
					if (DefaultCodecSupportsDocValues())
					{
						doc.Add(new NumericDocValuesField("id", numDocs));
					}
					else
					{
						doc.Add(NewStringField("id", Extensions.ToString(numDocs), Field.Store.NO
							));
					}
				}
				doc.Add(new StoredField("id", numDocs));
				writer.AddDocument(doc);
				numDocs++;
				if (random.Next(40) == 17)
				{
					// force flush
					writer.Reader.Dispose();
				}
			}
			IndexReader r = writer.Reader;
			writer.Dispose();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("  reader=" + r);
			}
			IndexSearcher s_1 = NewSearcher(r, false);
			int ITERS = AtLeast(100);
			for (int iter = 0; iter < ITERS; iter++)
			{
				bool reverse = random.NextBoolean();
				TopFieldDocs hits;
				SortField sf;
				bool sortMissingLast;
				bool missingIsNull;
				if (DefaultCodecSupportsDocValues() && random.NextBoolean())
				{
					sf = new SortField("stringdv", SortField.Type.STRING, reverse);
					// Can only use sort missing if the DVFormat
					// supports docsWithField:
					sortMissingLast = DefaultCodecSupportsDocsWithField() && Random().NextBoolean();
					missingIsNull = DefaultCodecSupportsDocsWithField();
				}
				else
				{
					sf = new SortField("string", SortField.Type.STRING, reverse);
					sortMissingLast = Random().NextBoolean();
					missingIsNull = true;
				}
				if (sortMissingLast)
				{
					sf.SetMissingValue(SortField.STRING_LAST);
				}
				Sort sort;
				if (random.NextBoolean())
				{
					sort = new Sort(sf);
				}
				else
				{
					sort = new Sort(sf, SortField.FIELD_DOC);
				}
				int hitCount = TestUtil.NextInt(random, 1, r.MaxDoc + 20);
				TestSortRandom.RandomFilter f = new TestSortRandom.RandomFilter(random, random.NextFloat
					(), docValues);
				int queryType = random.Next(3);
				if (queryType == 0)
				{
					// force out of order
					BooleanQuery bq = new BooleanQuery();
					// Add a Query with SHOULD, since bw.scorer() returns BooleanScorer2
					// which delegates to BS if there are no mandatory clauses.
					bq.Add(new MatchAllDocsQuery(), BooleanClause.Occur.SHOULD);
					// Set minNrShouldMatch to 1 so that BQ will not optimize rewrite to return
					// the clause instead of BQ.
					bq.SetMinimumNumberShouldMatch(1);
					hits = s_1.Search(bq, f, hitCount, sort, random.NextBoolean(), random.NextBoolean
						());
				}
				else
				{
					if (queryType == 1)
					{
						hits = s_1.Search(new ConstantScoreQuery(f), null, hitCount, sort, random.NextBoolean
							(), random.NextBoolean());
					}
					else
					{
						hits = s_1.Search(new MatchAllDocsQuery(), f, hitCount, sort, random.NextBoolean(
							), random.NextBoolean());
					}
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter + " " + hits.TotalHits + " hits; topN="
						 + hitCount + "; reverse=" + reverse + "; sortMissingLast=" + sortMissingLast + 
						" sort=" + sort);
				}
				// Compute expected results:
				f.matchValues.Sort(new _IComparer_184(sortMissingLast));
				if (reverse)
				{
					Collections.Reverse(f.matchValues);
				}
				IList<BytesRef> expected = f.matchValues;
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  expected:");
					for (int idx = 0; idx < expected.Count; idx++)
					{
						BytesRef br = expected[idx];
						if (br == null && missingIsNull == false)
						{
							br = new BytesRef();
						}
						System.Console.Out.WriteLine("    " + idx + ": " + (br == null ? "<missing>" : br
							.Utf8ToString()));
						if (idx == hitCount - 1)
						{
							break;
						}
					}
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  actual:");
					for (int hitIDX = 0; hitIDX < hits.ScoreDocs.Length; hitIDX++)
					{
						FieldDoc fd = (FieldDoc)hits.ScoreDocs[hitIDX];
						BytesRef br = (BytesRef)fd.fields[0];
						System.Console.Out.WriteLine("    " + hitIDX + ": " + (br == null ? "<missing>" : 
							br.Utf8ToString()) + " id=" + s_1.Doc(fd.Doc).Get("id"));
					}
				}
				for (int hitIDX_1 = 0; hitIDX_1 < hits.ScoreDocs.Length; hitIDX_1++)
				{
					FieldDoc fd = (FieldDoc)hits.ScoreDocs[hitIDX_1];
					BytesRef br = expected[hitIDX_1];
					if (br == null && missingIsNull == false)
					{
						br = new BytesRef();
					}
					// Normally, the old codecs (that don't support
					// docsWithField via doc values) will always return
					// an empty BytesRef for the missing case; however,
					// if all docs in a given segment were missing, in
					// that case it will return null!  So we must map
					// null here, too:
					BytesRef br2 = (BytesRef)fd.fields[0];
					if (br2 == null && missingIsNull == false)
					{
						br2 = new BytesRef();
					}
					AreEqual("hit=" + hitIDX_1 + " has wrong sort value", br, 
						br2);
				}
			}
			r.Dispose();
			dir.Dispose();
		}

		private sealed class _IComparer_184 : IComparer<BytesRef>
		{
			public _IComparer_184(bool sortMissingLast)
			{
				this.sortMissingLast = sortMissingLast;
			}

			public int Compare(BytesRef a, BytesRef b)
			{
				if (a == null)
				{
					if (b == null)
					{
						return 0;
					}
					if (sortMissingLast)
					{
						return 1;
					}
					else
					{
						return -1;
					}
				}
				else
				{
					if (b == null)
					{
						if (sortMissingLast)
						{
							return -1;
						}
						else
						{
							return 1;
						}
					}
					else
					{
						return a.CompareTo(b);
					}
				}
			}

			private readonly bool sortMissingLast;
		}

		private class RandomFilter : Filter
		{
			private readonly Random random;

			private float density;

			private readonly IList<BytesRef> docValues;

			public readonly IList<BytesRef> matchValues = Collections.SynchronizedList
				(new List<BytesRef>());

			public RandomFilter(Random random, float density, IList<BytesRef> docValues)
			{
				// density should be 0.0 ... 1.0
				this.random = random;
				this.density = density;
				this.docValues = docValues;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				int maxDoc = ((AtomicReader)context.Reader).MaxDoc;
				FieldCache.Ints idSource = FieldCache.DEFAULT.GetInts(((AtomicReader)context.Reader
					()), "id", false);
				IsNotNull(idSource);
				FixedBitSet bits = new FixedBitSet(maxDoc);
				for (int docID = 0; docID < maxDoc; docID++)
				{
					if (random.NextFloat() <= density && (acceptDocs == null || acceptDocs.Get(docID)
						))
					{
						bits.Set(docID);
						//System.out.println("  acc id=" + idSource.get(docID) + " docID=" + docID + " id=" + idSource.get(docID) + " v=" + docValues.get(idSource.get(docID)).utf8ToString());
						matchValues.Add(docValues[idSource.Get(docID)]);
					}
				}
				return bits;
			}
		}
	}
}
