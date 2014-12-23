/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestSameScoresWithThreads : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			Directory dir = NewDirectory();
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH
				));
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, analyzer);
			LineFileDocs docs = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
			int charsToIndex = AtLeast(100000);
			int charsIndexed = 0;
			//System.out.println("bytesToIndex=" + charsToIndex);
			while (charsIndexed < charsToIndex)
			{
				Lucene.Net.Document.Document doc = docs.NextDoc();
				charsIndexed += doc.Get("body").Length;
				w.AddDocument(doc);
			}
			//System.out.println("  bytes=" + charsIndexed + " add: " + doc);
			IndexReader r = w.GetReader();
			//System.out.println("numDocs=" + r.numDocs());
			w.Close();
			IndexSearcher s = NewSearcher(r);
			Terms terms = MultiFields.GetFields(r).Terms("body");
			int termCount = 0;
			TermsEnum termsEnum = terms.Iterator(null);
			while (termsEnum.Next() != null)
			{
				termCount++;
			}
			NUnit.Framework.Assert.IsTrue(termCount > 0);
			// Target ~10 terms to search:
			double chance = 10.0 / termCount;
			termsEnum = terms.Iterator(termsEnum);
			IDictionary<BytesRef, TopDocs> answers = new Dictionary<BytesRef, TopDocs>();
			while (termsEnum.Next() != null)
			{
				if (Random().NextDouble() <= chance)
				{
					BytesRef term = BytesRef.DeepCopyOf(termsEnum.Term());
					answers.Put(term, s.Search(new TermQuery(new Term("body", term)), 100));
				}
			}
			if (!answers.IsEmpty())
			{
				CountDownLatch startingGun = new CountDownLatch(1);
				int numThreads = TestUtil.NextInt(Random(), 2, 5);
				Sharpen.Thread[] threads = new Sharpen.Thread[numThreads];
				for (int threadID = 0; threadID < numThreads; threadID++)
				{
					Sharpen.Thread thread = new _Thread_89(startingGun, answers, s);
					// Floats really should be identical:
					threads[threadID] = thread;
					thread.Start();
				}
				startingGun.CountDown();
				foreach (Sharpen.Thread thread_1 in threads)
				{
					thread_1.Join();
				}
			}
			r.Close();
			dir.Close();
		}

		private sealed class _Thread_89 : Sharpen.Thread
		{
			public _Thread_89(CountDownLatch startingGun, IDictionary<BytesRef, TopDocs> answers
				, IndexSearcher s)
			{
				this.startingGun = startingGun;
				this.answers = answers;
				this.s = s;
			}

			public override void Run()
			{
				try
				{
					startingGun.Await();
					for (int i = 0; i < 20; i++)
					{
						IList<KeyValuePair<BytesRef, TopDocs>> shuffled = new AList<KeyValuePair<BytesRef
							, TopDocs>>(answers.EntrySet());
						Sharpen.Collections.Shuffle(shuffled);
						foreach (KeyValuePair<BytesRef, TopDocs> ent in shuffled)
						{
							TopDocs actual = s.Search(new TermQuery(new Term("body", ent.Key)), 100);
							TopDocs expected = ent.Value;
							NUnit.Framework.Assert.AreEqual(expected.totalHits, actual.totalHits);
							NUnit.Framework.Assert.AreEqual("query=" + ent.Key.Utf8ToString(), expected.scoreDocs
								.Length, actual.scoreDocs.Length);
							for (int hit = 0; hit < expected.scoreDocs.Length; hit++)
							{
								NUnit.Framework.Assert.AreEqual(expected.scoreDocs[hit].doc, actual.scoreDocs[hit
									].doc);
								NUnit.Framework.Assert.IsTrue(expected.scoreDocs[hit].score == actual.scoreDocs[hit
									].score);
							}
						}
					}
				}
				catch (Exception e)
				{
					throw new RuntimeException(e);
				}
			}

			private readonly CountDownLatch startingGun;

			private readonly IDictionary<BytesRef, TopDocs> answers;

			private readonly IndexSearcher s;
		}
	}
}
