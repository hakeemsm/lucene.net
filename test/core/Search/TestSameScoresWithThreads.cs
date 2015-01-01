/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;


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
				Lucene.Net.Documents.Document doc = docs.NextDoc();
				charsIndexed += doc.Get("body").Length;
				w.AddDocument(doc);
			}
			//System.out.println("  bytes=" + charsIndexed + " add: " + doc);
			IndexReader r = w.Reader;
			//System.out.println("numDocs=" + r.numDocs());
			w.Dispose();
			IndexSearcher s = NewSearcher(r);
			Terms terms = MultiFields.GetFields(r).Terms("body");
			int termCount = 0;
			TermsEnum termsEnum = terms.Iterator(null);
			while (termsEnum.Next() != null)
			{
				termCount++;
			}
			IsTrue(termCount > 0);
			// Target ~10 terms to search:
			double chance = 10.0 / termCount;
			termsEnum = terms.IEnumerator(termsEnum);
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
				CountdownEvent startingGun = new CountdownEvent(1);
				int numThreads = TestUtil.NextInt(Random(), 2, 5);
				Thread[] threads = new Thread[numThreads];
				for (int threadID = 0; threadID < numThreads; threadID++)
				{
					Thread thread = new _Thread_89(startingGun, answers, s);
					// Floats really should be identical:
					threads[threadID] = thread;
					thread.Start();
				}
				startingGun.CountDown();
				foreach (Thread thread_1 in threads)
				{
					thread_1.Join();
				}
			}
			r.Dispose();
			dir.Dispose();
		}

		private sealed class _Thread_89 : Thread
		{
			public _Thread_89(CountdownEvent startingGun, IDictionary<BytesRef, TopDocs> answers
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
						IList<KeyValuePair<BytesRef, TopDocs>> shuffled = new List<KeyValuePair<BytesRef
							, TopDocs>>(answers.EntrySet());
						Collections.Shuffle(shuffled);
						foreach (KeyValuePair<BytesRef, TopDocs> ent in shuffled)
						{
							TopDocs actual = s.Search(new TermQuery(new Term("body", ent.Key)), 100);
							TopDocs expected = ent.Value;
							AreEqual(expected.TotalHits, actual.TotalHits);
							AreEqual("query=" + ent.Key.Utf8ToString(), expected.ScoreDocs
								.Length, actual.ScoreDocs.Length);
							for (int hit = 0; hit < expected.ScoreDocs.Length; hit++)
							{
								AreEqual(expected.ScoreDocs[hit].Doc, actual.ScoreDocs[hit
									].Doc);
								IsTrue(expected.ScoreDocs[hit].score == actual.ScoreDocs[hit
									].score);
							}
						}
					}
				}
				catch (Exception e)
				{
					throw new SystemException(e);
				}
			}

			private readonly CountdownEvent startingGun;

			private readonly IDictionary<BytesRef, TopDocs> answers;

			private readonly IndexSearcher s;
		}
	}
}
