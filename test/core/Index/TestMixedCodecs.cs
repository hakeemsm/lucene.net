/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestMixedCodecs : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			int NUM_DOCS = AtLeast(1000);
			Directory dir = NewDirectory();
			RandomIndexWriter w = null;
			int docsLeftInThisSegment = 0;
			int docUpto = 0;
			while (docUpto < NUM_DOCS)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: " + docUpto + " of " + NUM_DOCS);
				}
				if (docsLeftInThisSegment == 0)
				{
					IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
						(Random()));
					if (Random().NextBoolean())
					{
						// Make sure we aggressively mix in SimpleText
						// since it has different impls for all codec
						// formats...
						iwc.SetCodec(Codec.ForName("SimpleText"));
					}
					if (w != null)
					{
						w.Dispose();
					}
					w = new RandomIndexWriter(Random(), dir, iwc);
					docsLeftInThisSegment = TestUtil.NextInt(Random(), 10, 100);
				}
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("id", docUpto.ToString(), Field.Store.YES));
				w.AddDocument(doc);
				docUpto++;
				docsLeftInThisSegment--;
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: now delete...");
			}
			// Random delete half the docs:
			ICollection<int> deleted = new HashSet<int>();
			while (deleted.Count < NUM_DOCS / 2)
			{
				int toDelete = Random().Next(NUM_DOCS);
				if (!deleted.Contains(toDelete))
				{
					deleted.AddItem(toDelete);
					w.DeleteDocuments(new Term("id", toDelete.ToString()));
					if (Random().Next(17) == 6)
					{
						IndexReader r = w.GetReader();
						AreEqual(NUM_DOCS - deleted.Count, r.NumDocs);
						r.Dispose();
					}
				}
			}
			w.Dispose();
			dir.Dispose();
		}
	}
}
