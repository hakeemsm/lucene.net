/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>
	/// Simple test that adds numeric terms, where each term has the
	/// totalTermFreq of its integer value, and checks that the totalTermFreq is correct.
	/// </summary>
	/// <remarks>
	/// Simple test that adds numeric terms, where each term has the
	/// totalTermFreq of its integer value, and checks that the totalTermFreq is correct.
	/// </remarks>
	public class TestBagOfPositions : LuceneTestCase
	{
		// TODO: somehow factor this with BagOfPostings? its almost the same
		// at night this makes like 200k/300k docs and will make Direct's heart beat!
		// Lucene3x doesnt have totalTermFreq, so the test isn't interesting there.
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			IList<string> postingsList = new AList<string>();
			int numTerms = AtLeast(300);
			int maxTermsPerDoc = TestUtil.NextInt(Random(), 10, 20);
			bool isSimpleText = "SimpleText".Equals(TestUtil.GetPostingsFormat("field"));
			IndexWriterConfig iwc = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random()));
			if ((isSimpleText || iwc.GetMergePolicy() is MockRandomMergePolicy) && (TEST_NIGHTLY
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
				string term = Sharpen.Extensions.ToString(i);
				for (int j = 0; j < i; j++)
				{
					postingsList.AddItem(term);
				}
			}
			Sharpen.Collections.Shuffle(postingsList, Random());
			ConcurrentLinkedQueue<string> postings = new ConcurrentLinkedQueue<string>(postingsList
				);
			Directory dir = NewFSDirectory(CreateTempDir("bagofpositions"));
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			int threadCount = TestUtil.NextInt(Random(), 1, 5);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("config: " + iw.w.GetConfig());
				System.Console.Out.WriteLine("threadCount=" + threadCount);
			}
			Field prototype = NewTextField("field", string.Empty, Field.Store.NO);
			FieldType fieldType = new FieldType(prototype.FieldType());
			if (Random().NextBoolean())
			{
				fieldType.SetOmitNorms(true);
			}
			int options = Random().Next(3);
			if (options == 0)
			{
				fieldType.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS);
				// we dont actually need positions
				fieldType.StoreTermVectors = true;
			}
			else
			{
				// but enforce term vectors when we do this so we check SOMETHING
				if (options == 1 && !doesntSupportOffsets.Contains(TestUtil.GetPostingsFormat("field"
					)))
				{
					fieldType.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
						);
				}
			}
			// else just positions
			Sharpen.Thread[] threads = new Sharpen.Thread[threadCount];
			CountDownLatch startingGun = new CountDownLatch(1);
			for (int threadID = 0; threadID < threadCount; threadID++)
			{
				Random threadRandom = new Random(Random().NextLong());
				Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
					();
				Field field = new Field("field", string.Empty, fieldType);
				document.Add(field);
				threads[threadID] = new _Thread_104(startingGun, postings, threadRandom, maxTermsPerDoc
					, field, iw, document);
				threads[threadID].Start();
			}
			startingGun.CountDown();
			foreach (Sharpen.Thread t in threads)
			{
				t.Join();
			}
			iw.ForceMerge(1);
			DirectoryReader ir = iw.GetReader();
			AreEqual(1, ir.Leaves().Count);
			AtomicReader air = ((AtomicReader)ir.Leaves()[0].Reader());
			Terms terms = air.Terms("field");
			// numTerms-1 because there cannot be a term 0 with 0 postings:
			AreEqual(numTerms - 1, terms.Size());
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
			ir.Close();
			iw.Close();
			dir.Close();
		}

		private sealed class _Thread_104 : Sharpen.Thread
		{
			public _Thread_104(CountDownLatch startingGun, ConcurrentLinkedQueue<string> postings
				, Random threadRandom, int maxTermsPerDoc, Field field, RandomIndexWriter iw, Lucene.Net.Documents.Document
				 document)
			{
				this.startingGun = startingGun;
				this.postings = postings;
				this.threadRandom = threadRandom;
				this.maxTermsPerDoc = maxTermsPerDoc;
				this.field = field;
				this.iw = iw;
				this.document = document;
			}

			public override void Run()
			{
				try
				{
					startingGun.Await();
					while (!postings.IsEmpty())
					{
						StringBuilder text = new StringBuilder();
						int numTerms = threadRandom.Next(maxTermsPerDoc);
						for (int i = 0; i < numTerms; i++)
						{
							string token = postings.Poll();
							if (token == null)
							{
								break;
							}
							text.Append(' ');
							text.Append(token);
						}
						field.StringValue = text.ToString());
						iw.AddDocument(document);
					}
				}
				catch (Exception e)
				{
					throw new RuntimeException(e);
				}
			}

			private readonly CountDownLatch startingGun;

			private readonly ConcurrentLinkedQueue<string> postings;

			private readonly Random threadRandom;

			private readonly int maxTermsPerDoc;

			private readonly Field field;

			private readonly RandomIndexWriter iw;

			private readonly Lucene.Net.Documents.Document document;
		}
	}
}
