/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestFilteredSearch : LuceneTestCase
	{
		private static readonly string FIELD = "category";

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFilteredSearch()
		{
			bool enforceSingleSegment = true;
			Directory directory = NewDirectory();
			int[] filterBits = new int[] { 1, 36 };
			TestFilteredSearch.SimpleDocIdSetFilter filter = new TestFilteredSearch.SimpleDocIdSetFilter
				(filterBits);
			IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			SearchFiltered(writer, directory, filter, enforceSingleSegment);
			// run the test on more than one segment
			enforceSingleSegment = false;
			writer.Dispose();
			writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMaxBufferedDocs
				(10)).SetMergePolicy(NewLogMergePolicy()));
			// we index 60 docs - this will create 6 segments
			SearchFiltered(writer, directory, filter, enforceSingleSegment);
			writer.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void SearchFiltered(IndexWriter writer, Directory directory, Filter
			 filter, bool fullMerge)
		{
			for (int i = 0; i < 60; i++)
			{
				//Simple docs
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField(FIELD, i.ToString(), Field.Store.YES));
				writer.AddDocument(doc);
			}
			if (fullMerge)
			{
				writer.ForceMerge(1);
			}
			writer.Dispose();
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(new TermQuery(new Term(FIELD, "36")), BooleanClause.Occur.SHOULD
				);
			IndexReader reader = DirectoryReader.Open(directory);
			IndexSearcher indexSearcher = NewSearcher(reader);
			ScoreDoc[] hits = indexSearcher.Search(booleanQuery, filter, 1000).ScoreDocs;
			AreEqual("Number of matched documents", 1, hits.Length);
			reader.Dispose();
		}

		public sealed class SimpleDocIdSetFilter : Filter
		{
			private readonly int[] docs;

			public SimpleDocIdSetFilter(int[] docs)
			{
				this.docs = docs;
			}

			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				IsNull("acceptDocs should be null, as we have an index without deletions"
					, acceptDocs);
				FixedBitSet set = new FixedBitSet(((AtomicReader)context.Reader).MaxDoc);
				int docBase = context.docBase;
				int limit = docBase + ((AtomicReader)context.Reader).MaxDoc;
				for (int index = 0; index < docs.Length; index++)
				{
					int docId = docs[index];
					if (docId >= docBase && docId < limit)
					{
						set.Set(docId - docBase);
					}
				}
				return set.Cardinality() == 0 ? null : set;
			}
		}
	}
}
