/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestElevationComparator : LuceneTestCase
	{
		private readonly IDictionary<BytesRef, int> priority = new Dictionary<BytesRef, int
			>();

		//@Test
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSorting()
		{
			Directory directory = NewDirectory();
			IndexWriter writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy
				(NewLogMergePolicy(1000)).SetSimilarity(new DefaultSimilarity()));
			writer.AddDocument(Adoc(new string[] { "id", "a", "title", "ipod", "str_s", "a" }
				));
			writer.AddDocument(Adoc(new string[] { "id", "b", "title", "ipod ipod", "str_s", 
				"b" }));
			writer.AddDocument(Adoc(new string[] { "id", "c", "title", "ipod ipod ipod", "str_s"
				, "c" }));
			writer.AddDocument(Adoc(new string[] { "id", "x", "title", "boosted", "str_s", "x"
				 }));
			writer.AddDocument(Adoc(new string[] { "id", "y", "title", "boosted boosted", "str_s"
				, "y" }));
			writer.AddDocument(Adoc(new string[] { "id", "z", "title", "boosted boosted boosted"
				, "str_s", "z" }));
			IndexReader r = DirectoryReader.Open(writer, true);
			writer.Close();
			IndexSearcher searcher = NewSearcher(r);
			searcher.SetSimilarity(new DefaultSimilarity());
			RunTest(searcher, true);
			RunTest(searcher, false);
			r.Close();
			directory.Close();
		}

		/// <exception cref="System.Exception"></exception>
		private void RunTest(IndexSearcher searcher, bool reversed)
		{
			BooleanQuery newq = new BooleanQuery(false);
			TermQuery query = new TermQuery(new Term("title", "ipod"));
			newq.Add(query, BooleanClause.Occur.SHOULD);
			newq.Add(GetElevatedQuery(new string[] { "id", "a", "id", "x" }), BooleanClause.Occur
				.SHOULD);
			Sort sort = new Sort(new SortField("id", new ElevationComparatorSource(priority), 
				false), new SortField(null, SortField.Type.SCORE, reversed));
			TopDocsCollector<FieldValueHitQueue.Entry> topCollector = TopFieldCollector.Create
				(sort, 50, false, true, true, true);
			searcher.Search(newq, null, topCollector);
			TopDocs topDocs = topCollector.TopDocs(0, 10);
			int nDocsReturned = topDocs.scoreDocs.Length;
			AreEqual(4, nDocsReturned);
			// 0 & 3 were elevated
			AreEqual(0, topDocs.scoreDocs[0].doc);
			AreEqual(3, topDocs.scoreDocs[1].doc);
			if (reversed)
			{
				AreEqual(2, topDocs.scoreDocs[2].doc);
				AreEqual(1, topDocs.scoreDocs[3].doc);
			}
			else
			{
				AreEqual(1, topDocs.scoreDocs[2].doc);
				AreEqual(2, topDocs.scoreDocs[3].doc);
			}
		}

		private Query GetElevatedQuery(string[] vals)
		{
			BooleanQuery q = new BooleanQuery(false);
			q.SetBoost(0);
			int max = (vals.Length / 2) + 5;
			for (int i = 0; i < vals.Length - 1; i += 2)
			{
				q.Add(new TermQuery(new Term(vals[i], vals[i + 1])), BooleanClause.Occur.SHOULD);
				priority.Put(new BytesRef(vals[i + 1]), Sharpen.Extensions.ValueOf(max--));
			}
			// System.out.println(" pri doc=" + vals[i+1] + " pri=" + (1+max));
			return q;
		}

		private Lucene.Net.Documents.Document Adoc(string[] vals)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			for (int i = 0; i < vals.Length - 2; i += 2)
			{
				doc.Add(NewTextField(vals[i], vals[i + 1], Field.Store.YES));
			}
			return doc;
		}
	}

	internal class ElevationComparatorSource : FieldComparatorSource
	{
		private readonly IDictionary<BytesRef, int> priority;

		public ElevationComparatorSource(IDictionary<BytesRef, int> boosts)
		{
			this.priority = boosts;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldComparator<object> NewComparator(string fieldname, int numHits
			, int sortPos, bool reversed)
		{
			return new _FieldComparator_143(this, numHits, fieldname);
		}

		private sealed class _FieldComparator_143 : FieldComparator<int>
		{
			public _FieldComparator_143(ElevationComparatorSource _enclosing, int numHits, string
				 fieldname)
			{
				this._enclosing = _enclosing;
				this.numHits = numHits;
				this.fieldname = fieldname;
				this.values = new int[numHits];
				this.tempBR = new BytesRef();
			}

			internal SortedDocValues idIndex;

			private readonly int[] values;

			private readonly BytesRef tempBR;

			internal int bottomVal;

			public override int Compare(int slot1, int slot2)
			{
				return this.values[slot2] - this.values[slot1];
			}

			// values will be small enough that there is no overflow concern
			public override void SetBottom(int slot)
			{
				this.bottomVal = this.values[slot];
			}

			public override void SetTopValue(int value)
			{
				throw new NotSupportedException();
			}

			private int DocVal(int doc)
			{
				int ord = this.idIndex.GetOrd(doc);
				if (ord == -1)
				{
					return 0;
				}
				else
				{
					this.idIndex.LookupOrd(ord, this.tempBR);
					int prio = this._enclosing.priority.Get(this.tempBR);
					return prio == null ? 0 : prio;
				}
			}

			public override int CompareBottom(int doc)
			{
				return this.DocVal(doc) - this.bottomVal;
			}

			public override void Copy(int slot, int doc)
			{
				this.values[slot] = this.DocVal(doc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override FieldComparator<int> SetNextReader(AtomicReaderContext context)
			{
				this.idIndex = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader()), 
					fieldname);
				return this;
			}

			public override int Value(int slot)
			{
				return Sharpen.Extensions.ValueOf(this.values[slot]);
			}

			public override int CompareTop(int doc)
			{
				throw new NotSupportedException();
			}

			private readonly ElevationComparatorSource _enclosing;

			private readonly int numHits;

			private readonly string fieldname;
		}
	}
}
