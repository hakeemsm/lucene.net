using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// Simple test that adds numeric terms, where each term has the
	/// totalTermFreq of its integer value, and checks that the totalTermFreq is correct.
	/// </summary>
	/// <remarks>
	/// Simple test that adds numeric terms, where each term has the
	/// totalTermFreq of its integer value, and checks that the totalTermFreq is correct.
	/// </remarks>
	[TestFixture]
    public class TestBagOfPositions : LuceneTestCase
	{
		// TODO: somehow factor this with BagOfPostings? its almost the same
		// at night this makes like 200k/300k docs and will make Direct's heart beat!
		// Lucene3x doesnt have totalTermFreq, so the test isn't interesting there.
		[Test]
		public virtual void TestBagPositions()
		{
			IList<string> postingsList = new List<string>();
			int numTerms = AtLeast(300);
			int maxTermsPerDoc = Random().NextInt(10, 20);
			bool isSimpleText = "SimpleText".Equals(TestUtil.GetPostingsFormat("field"));
			IndexWriterConfig iwc = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
			if ((isSimpleText || iwc.MergePolicy is MockRandomMergePolicy) && (TEST_NIGHTLY
				 || RANDOM_MULTIPLIER > 1))
			{
				// Otherwise test can take way too long (> 2 hours)
				numTerms /= 2;
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("maxTermsPerDoc=" + maxTermsPerDoc);
				System.Console.Out.WriteLine("numTerms=" + numTerms);
			}
			for (int i = 0; i < numTerms; i++)
			{
				string term = i.ToString();
				for (int j = 0; j < i; j++)
				{
					postingsList.Add(term);
				}
			}
			postingsList.Shuffle(Random());
			ConcurrentLinkedQueue<string> postings = new ConcurrentLinkedQueue<string>(postingsList);
			Directory dir = NewFSDirectory(CreateTempDir("bagofpositions"));
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			int threadCount = TestUtil.NextInt(Random(), 1, 5);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("config: " + iw.w.Config);
				System.Console.Out.WriteLine("threadCount=" + threadCount);
			}
			Field prototype = NewTextField("field", string.Empty, Field.Store.NO);
			FieldType fieldType = new FieldType((FieldType) prototype.FieldTypeValue);
			if (Random().NextBoolean())
			{
				fieldType.OmitsNorms = (true);
			}
			int options = Random().Next(3);
			if (options == 0)
			{
				fieldType.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS);
				// we dont actually need positions
				fieldType.StoreTermVectors = true;
			}
			else
			{
				// but enforce term vectors when we do this so we check SOMETHING
				if (options == 1 && !doesntSupportOffsets.Contains(TestUtil.GetPostingsFormat("field"
					)))
				{
					fieldType.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
						);
				}
			}
			// else just positions
			Thread[] threads = new Thread[threadCount];
			CountdownEvent startingGun = new CountdownEvent(1);
			for (int threadID = 0; threadID < threadCount; threadID++)
			{
				Random threadRandom = new Random(Random().NextInt());
				var document = new Lucene.Net.Documents.Document();
				Field field = new Field("field", string.Empty, fieldType);
				document.Add(field);
				threads[threadID] = new Thread(() =>
				{
				   
					startingGun.Wait();
					while (postings.Any())
					{
						StringBuilder text = new StringBuilder();
						int numTerms2 = threadRandom.Next(maxTermsPerDoc);
						for (int i = 0; i < numTerms2; i++)
						{
							string token = postings.Poll();
							if (token == null)
							{
								break;
							}
							text.Append(' ');
							text.Append(token);
						}
						field.StringValue = text.ToString();
						iw.AddDocument(document);
					}
				
				});
				threads[threadID].Start();
			}
			startingGun.Signal();
			foreach (Thread t in threads)
			{
				t.Join();
			}
			iw.ForceMerge(1);
			DirectoryReader ir = iw.Reader;
			AreEqual(1, ir.Leaves.Count);
			AtomicReader air = ((AtomicReader)ir.Leaves[0].Reader);
			Terms terms = air.Terms("field");
			// numTerms-1 because there cannot be a term 0 with 0 postings:
			AreEqual(numTerms - 1, terms.Size);
			TermsEnum termsEnum = terms.Iterator(null);
			BytesRef term_1;
			while ((term_1 = termsEnum.Next()) != null)
			{
				int value = System.Convert.ToInt32(term_1.Utf8ToString());
				AreEqual(value, termsEnum.TotalTermFreq);
			}
			// don't really need to check more than this, as CheckIndex
			// will verify that totalTermFreq == total number of positions seen
			// from a docsAndPositionsEnum.
			ir.Dispose();
			iw.Close();
			dir.Dispose();
		}
	}
}
