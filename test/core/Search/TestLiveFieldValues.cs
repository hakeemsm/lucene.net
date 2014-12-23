/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestLiveFieldValues : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			Directory dir = NewFSDirectory(CreateTempDir("livefieldupdates"));
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter w = new IndexWriter(dir, iwc);
			SearcherManager mgr = new SearcherManager(w, true, new _SearcherFactory_51());
			int missing = -1;
			LiveFieldValues<IndexSearcher, int> rt = new _LiveFieldValues_60(mgr, missing);
			int numThreads = TestUtil.NextInt(Random(), 2, 5);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine(numThreads + " threads");
			}
			CountDownLatch startingGun = new CountDownLatch(1);
			IList<Sharpen.Thread> threads = new AList<Sharpen.Thread>();
			int iters = AtLeast(1000);
			int idCount = TestUtil.NextInt(Random(), 100, 10000);
			double reopenChance = Random().NextDouble() * 0.01;
			double deleteChance = Random().NextDouble() * 0.25;
			double addChance = Random().NextDouble() * 0.5;
			for (int t = 0; t < numThreads; t++)
			{
				int threadID = t;
				Random threadRandom = new Random(Random().NextLong());
				Sharpen.Thread thread = new _Thread_93(startingGun, iters, threadRandom, addChance
					, threadID, idCount, w, rt, deleteChance, missing, reopenChance, mgr);
				// Add/update a document
				// Threads must not update the same id at the
				// same time:
				//System.out.println("refresh @ " + rt.size());
				threads.AddItem(thread);
				thread.Start();
			}
			startingGun.CountDown();
			foreach (Sharpen.Thread thread_1 in threads)
			{
				thread_1.Join();
			}
			mgr.MaybeRefresh();
			NUnit.Framework.Assert.AreEqual(0, rt.Size());
			rt.Close();
			mgr.Close();
			w.Close();
			dir.Close();
		}

		private sealed class _SearcherFactory_51 : SearcherFactory
		{
			public _SearcherFactory_51()
			{
			}

			public override IndexSearcher NewSearcher(IndexReader r)
			{
				return new IndexSearcher(r);
			}
		}

		private sealed class _LiveFieldValues_60 : LiveFieldValues<IndexSearcher, int>
		{
			public _LiveFieldValues_60(ReferenceManager<IndexSearcher> baseArg1, int baseArg2
				) : base(baseArg1, baseArg2)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override int LookupFromSearcher(IndexSearcher s, string id)
			{
				TermQuery tq = new TermQuery(new Term("id", id));
				TopDocs hits = s.Search(tq, 1);
				NUnit.Framework.Assert.IsTrue(hits.totalHits <= 1);
				if (hits.totalHits == 0)
				{
					return null;
				}
				else
				{
					Lucene.Net.Document.Document doc = s.Doc(hits.scoreDocs[0].doc);
					return (int)doc.GetField("field").NumericValue();
				}
			}
		}

		private sealed class _Thread_93 : Sharpen.Thread
		{
			public _Thread_93(CountDownLatch startingGun, int iters, Random threadRandom, double
				 addChance, int threadID, int idCount, IndexWriter w, LiveFieldValues<IndexSearcher
				, int> rt, double deleteChance, int missing, double reopenChance, SearcherManager
				 mgr)
			{
				this.startingGun = startingGun;
				this.iters = iters;
				this.threadRandom = threadRandom;
				this.addChance = addChance;
				this.threadID = threadID;
				this.idCount = idCount;
				this.w = w;
				this.rt = rt;
				this.deleteChance = deleteChance;
				this.missing = missing;
				this.reopenChance = reopenChance;
				this.mgr = mgr;
			}

			public override void Run()
			{
				try
				{
					IDictionary<string, int> values = new Dictionary<string, int>();
					IList<string> allIDs = Sharpen.Collections.SynchronizedList(new AList<string>());
					startingGun.Await();
					for (int iter = 0; iter < iters; iter++)
					{
						Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
							();
						if (threadRandom.NextDouble() <= addChance)
						{
							string id = string.Format(CultureInfo.ROOT, "%d_%04x", threadID, threadRandom.Next
								(idCount));
							int field = threadRandom.Next(int.MaxValue);
							doc.Add(new StringField("id", id, Field.Store.YES));
							doc.Add(new IntField("field", field, Field.Store.YES));
							w.UpdateDocument(new Term("id", id), doc);
							rt.Add(id, field);
							if (values.Put(id, field) == null)
							{
								allIDs.AddItem(id);
							}
						}
						if (allIDs.Count > 0 && threadRandom.NextDouble() <= deleteChance)
						{
							string randomID = allIDs[threadRandom.Next(allIDs.Count)];
							w.DeleteDocuments(new Term("id", randomID));
							rt.Delete(randomID);
							values.Put(randomID, missing);
						}
						if (threadRandom.NextDouble() <= reopenChance || rt.Size() > 10000)
						{
							mgr.MaybeRefresh();
							if (LuceneTestCase.VERBOSE)
							{
								IndexSearcher s = mgr.Acquire();
								try
								{
									System.Console.Out.WriteLine("TEST: reopen " + s);
								}
								finally
								{
									mgr.Release(s);
								}
								System.Console.Out.WriteLine("TEST: " + values.Count + " values");
							}
						}
						if (threadRandom.Next(10) == 7)
						{
							NUnit.Framework.Assert.AreEqual(null, rt.Get("foo"));
						}
						if (allIDs.Count > 0)
						{
							string randomID = allIDs[threadRandom.Next(allIDs.Count)];
							int expected = values.Get(randomID);
							if (expected == missing)
							{
								expected = null;
							}
							NUnit.Framework.Assert.AreEqual("id=" + randomID, expected, rt.Get(randomID));
						}
					}
				}
				catch (Exception t)
				{
					throw new RuntimeException(t);
				}
			}

			private readonly CountDownLatch startingGun;

			private readonly int iters;

			private readonly Random threadRandom;

			private readonly double addChance;

			private readonly int threadID;

			private readonly int idCount;

			private readonly IndexWriter w;

			private readonly LiveFieldValues<IndexSearcher, int> rt;

			private readonly double deleteChance;

			private readonly int missing;

			private readonly double reopenChance;

			private readonly SearcherManager mgr;
		}
	}
}
