/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestForTooMuchCloning : LuceneTestCase
	{
		// Make sure we don't clone IndexInputs too frequently
		// during merging:
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			// NOTE: if we see a fail on this test with "NestedPulsing" its because its 
			// reuse isnt perfect (but reasonable). see TestPulsingReuse.testNestedPulsing 
			// for more details
			MockDirectoryWrapper dir = NewMockDirectory();
			TieredMergePolicy tmp = new TieredMergePolicy();
			tmp.SetMaxMergeAtOnce(2);
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy
				(tmp));
			int numDocs = 20;
			for (int docs = 0; docs < numDocs; docs++)
			{
				StringBuilder sb = new StringBuilder();
				for (int terms = 0; terms < 100; terms++)
				{
					sb.Append(TestUtil.RandomRealisticUnicodeString(Random()));
					sb.Append(' ');
				}
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new TextField("field", sb.ToString(), Field.Store.NO));
				w.AddDocument(doc);
			}
			IndexReader r = w.GetReader();
			w.Close();
			int cloneCount = dir.GetInputCloneCount();
			//System.out.println("merge clone count=" + cloneCount);
			IsTrue("too many calls to IndexInput.clone during merging: "
				 + dir.GetInputCloneCount(), cloneCount < 500);
			IndexSearcher s = NewSearcher(r);
			// MTQ that matches all terms so the AUTO_REWRITE should
			// cutover to filter rewrite and reuse a single DocsEnum
			// across all terms;
			TopDocs hits = s.Search(new TermRangeQuery("field", new BytesRef(), new BytesRef(
				"\uFFFF"), true, true), 10);
			IsTrue(hits.TotalHits > 0);
			int queryCloneCount = dir.GetInputCloneCount() - cloneCount;
			//System.out.println("query clone count=" + queryCloneCount);
			IsTrue("too many calls to IndexInput.clone during TermRangeQuery: "
				 + queryCloneCount, queryCloneCount < 50);
			r.Close();
			dir.Close();
		}
	}
}
