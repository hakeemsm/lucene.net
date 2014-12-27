/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestDocIdSet : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestFilteredDocIdSet()
		{
			int maxdoc = 10;
			DocIdSet innerSet = new _DocIdSet_39(maxdoc);
			DocIdSet filteredSet = new _FilteredDocIdSet_72(innerSet);
			//validate only even docids
			DocIdSetIterator iter = filteredSet.Iterator();
			AList<int> list = new AList<int>();
			int doc = iter.Advance(3);
			if (doc != DocIdSetIterator.NO_MORE_DOCS)
			{
				list.AddItem(Sharpen.Extensions.ValueOf(doc));
				while ((doc = iter.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
				{
					list.AddItem(Sharpen.Extensions.ValueOf(doc));
				}
			}
			int[] docs = new int[list.Count];
			int c = 0;
			Iterator<int> intIter = list.Iterator();
			while (intIter.HasNext())
			{
				docs[c++] = intIter.Next();
			}
			int[] answer = new int[] { 4, 6, 8 };
			bool same = Arrays.Equals(answer, docs);
			if (!same)
			{
				System.Console.Out.WriteLine("answer: " + Arrays.ToString(answer));
				System.Console.Out.WriteLine("gotten: " + Arrays.ToString(docs));
				Fail();
			}
		}

		private sealed class _DocIdSet_39 : DocIdSet
		{
			public _DocIdSet_39(int maxdoc)
			{
				this.maxdoc = maxdoc;
			}

			public override DocIdSetIterator Iterator()
			{
				return new _DocIdSetIterator_43(maxdoc);
			}

			private sealed class _DocIdSetIterator_43 : DocIdSetIterator
			{
				public _DocIdSetIterator_43(int maxdoc)
				{
					this.maxdoc = maxdoc;
					this.docid = -1;
				}

				internal int docid;

				public override int DocID
				{
					return this.docid;
				}

				public override int NextDoc()
				{
					this.docid++;
					return this.docid < maxdoc ? this.docid : (this.docid = DocIdSetIterator.NO_MORE_DOCS
						);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override int Advance(int target)
				{
					return this.SlowAdvance(target);
				}

				public override long Cost()
				{
					return 1;
				}

				private readonly int maxdoc;
			}

			private readonly int maxdoc;
		}

		private sealed class _FilteredDocIdSet_72 : FilteredDocIdSet
		{
			public _FilteredDocIdSet_72(DocIdSet baseArg1) : base(baseArg1)
			{
			}

			protected override bool Match(int docid)
			{
				return docid % 2 == 0;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNullDocIdSet()
		{
			// Tests that if a Filter produces a null DocIdSet, which is given to
			// IndexSearcher, everything works fine. This came up in LUCENE-1754.
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("c", "val", Field.Store.NO));
			writer.AddDocument(doc);
			IndexReader reader = writer.GetReader();
			writer.Dispose();
			// First verify the document is searchable.
			IndexSearcher searcher = NewSearcher(reader);
			//HM:revisit 
			//assert.assertEquals(1, searcher.search(new MatchAllDocsQuery(), 10).TotalHits);
			// Now search w/ a Filter which returns a null DocIdSet
			Filter f = new _Filter_122();
			//HM:revisit 
			//assert.assertEquals(0, searcher.search(new MatchAllDocsQuery(), f, 10).TotalHits);
			reader.Dispose();
			dir.Dispose();
		}

		private sealed class _Filter_122 : Filter
		{
			public _Filter_122()
			{
			}

			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				return null;
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNullIteratorFilteredDocIdSet()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("c", "val", Field.Store.NO));
			writer.AddDocument(doc);
			IndexReader reader = writer.GetReader();
			writer.Dispose();
			// First verify the document is searchable.
			IndexSearcher searcher = NewSearcher(reader);
			//HM:revisit 
			//assert.assertEquals(1, searcher.search(new MatchAllDocsQuery(), 10).TotalHits);
			// Now search w/ a Filter which returns a null DocIdSet
			Filter f = new _Filter_152();
			//HM:revisit 
			//assert.assertEquals(0, searcher.search(new MatchAllDocsQuery(), f, 10).TotalHits);
			reader.Dispose();
			dir.Dispose();
		}

		private sealed class _Filter_152 : Filter
		{
			public _Filter_152()
			{
			}

			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				DocIdSet innerNullIteratorSet = new _DocIdSet_155();
				return new _FilteredDocIdSet_161(innerNullIteratorSet);
			}

			private sealed class _DocIdSet_155 : DocIdSet
			{
				public _DocIdSet_155()
				{
				}

				public override DocIdSetIterator Iterator()
				{
					return null;
				}
			}

			private sealed class _FilteredDocIdSet_161 : FilteredDocIdSet
			{
				public _FilteredDocIdSet_161(DocIdSet baseArg1) : base(baseArg1)
				{
				}

				protected override bool Match(int docid)
				{
					return true;
				}
			}
		}
	}
}
