/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;
using Lucene.Net.TestFramework.Analysis;
using Lucene.NetDocument;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Search;
using Lucene.Net.TestFramework.Store;
using Lucene.Net.TestFramework.Util;
using Sharpen;
using Directory = System.IO.Directory;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>
	/// Utility class that spawns multiple indexing and
	/// searching threads.
	/// </summary>
	/// <remarks>
	/// Utility class that spawns multiple indexing and
	/// searching threads.
	/// </remarks>
	public abstract class ThreadedIndexingAndSearchingTestCase : LuceneTestCase
	{
		protected internal readonly AtomicBoolean failed = new AtomicBoolean();

		protected internal readonly AtomicInteger addCount = new AtomicInteger();

		protected internal readonly AtomicInteger delCount = new AtomicInteger();

		protected internal readonly AtomicInteger packCount = new AtomicInteger();

		protected internal Directory dir;

		protected internal IndexWriter writer;

		private class SubDocs
		{
			public readonly string packID;

			public readonly IList<string> subIDs;

			public bool deleted;

			public SubDocs(string packID, IList<string> subIDs)
			{
				// TODO
				//   - mix in forceMerge, addIndexes
				//   - randomly mix in non-congruent docs
				this.packID = packID;
				this.subIDs = subIDs;
			}
		}

		// Called per-search
		/// <exception cref="System.Exception"></exception>
		protected internal abstract IndexSearcher GetCurrentSearcher();

		/// <exception cref="System.Exception"></exception>
		protected internal abstract IndexSearcher GetFinalSearcher();

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void ReleaseSearcher(IndexSearcher s)
		{
		}

		// Called once to run searching
		/// <exception cref="System.Exception"></exception>
		protected internal abstract void DoSearching(ExecutorService es, long stopTime);

		protected internal virtual Directory GetDirectory(Directory @in)
		{
			return @in;
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void UpdateDocuments<_T0>(Term id, IList<_T0> docs) where 
			_T0:Iterable<IndexableField>
		{
			writer.UpdateDocuments(id, docs.AsIterable());
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void AddDocuments<_T0>(Term id, IList<_T0> docs) where 
			_T0:Iterable<IndexableField>
		{
			writer.AddDocuments(docs.AsIterable());
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void AddDocument<_T0>(Term id, Iterable<_T0> doc) where 
			_T0:IndexableField
		{
			writer.AddDocument(doc);
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void UpdateDocument<_T0>(Term term, Iterable<_T0> doc)
			 where _T0:IndexableField
		{
			writer.UpdateDocument(term, doc);
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void DeleteDocuments(Term term)
		{
			writer.DeleteDocuments(term);
		}

		protected internal virtual void DoAfterIndexingThreadDone()
		{
		}

		private Sharpen.Thread[] LaunchIndexingThreads(LineFileDocs docs, int numThreads, 
			long stopTime, ICollection<string> delIDs, ICollection<string> delPackIDs, IList
			<ThreadedIndexingAndSearchingTestCase.SubDocs> allSubDocs)
		{
			Sharpen.Thread[] threads = new Sharpen.Thread[numThreads];
			for (int thread = 0; thread < numThreads; thread++)
			{
				threads[thread] = new _Thread_124(this, stopTime, docs, allSubDocs, delIDs, delPackIDs
					);
				// TODO: would be better if this were cross thread, so that we make sure one thread deleting anothers added docs works:
				// Occasional longish pause if running
				// nightly
				// Rate limit ingest rate:
				// Maybe add randomly named field
				// Add/update doc block:
				//HM:revisit 
				//assert !delSubDocs.deleted;
				// Update doc block, replacing prior packID
				// Add doc block, using new packID
				// Add single doc
				// Update single doc, but we never re-use
				// and ID so the delete will never
				// actually happen:
				//HM:revisit 
				//assert !subDocs.deleted;
				threads[thread].SetDaemon(true);
				threads[thread].Start();
			}
			return threads;
		}

		private sealed class _Thread_124 : Sharpen.Thread
		{
			public _Thread_124(ThreadedIndexingAndSearchingTestCase _enclosing, long stopTime
				, LineFileDocs docs, IList<ThreadedIndexingAndSearchingTestCase.SubDocs> allSubDocs
				, ICollection<string> delIDs, ICollection<string> delPackIDs)
			{
				this._enclosing = _enclosing;
				this.stopTime = stopTime;
				this.docs = docs;
				this.allSubDocs = allSubDocs;
				this.delIDs = delIDs;
				this.delPackIDs = delPackIDs;
			}

			public override void Run()
			{
				IList<string> toDeleteIDs = new AList<string>();
				IList<ThreadedIndexingAndSearchingTestCase.SubDocs> toDeleteSubDocs = new AList<ThreadedIndexingAndSearchingTestCase.SubDocs
					>();
				while (DateTime.Now.CurrentTimeMillis() < stopTime && !this._enclosing.failed.Get())
				{
					try
					{
						if (LuceneTestCase.TEST_NIGHTLY && LuceneTestCase.Random().Next(6) == 3)
						{
							if (LuceneTestCase.VERBOSE)
							{
								System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": now long sleep"
									);
							}
							Sharpen.Thread.Sleep(TestUtil.NextInt(LuceneTestCase.Random(), 50, 500));
						}
						if (LuceneTestCase.Random().Next(7) == 5)
						{
							Sharpen.Thread.Sleep(TestUtil.NextInt(LuceneTestCase.Random(), 1, 10));
							if (LuceneTestCase.VERBOSE)
							{
								System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": done sleep"
									);
							}
						}
						Lucene.NetDocument.Document doc = docs.NextDoc();
						if (doc == null)
						{
							break;
						}
						string addedField;
						if (LuceneTestCase.Random().NextBoolean())
						{
							addedField = "extra" + LuceneTestCase.Random().Next(40);
							doc.Add(LuceneTestCase.NewTextField(addedField, "a random field", Field.Store.YES
								));
						}
						else
						{
							addedField = null;
						}
						if (LuceneTestCase.Random().NextBoolean())
						{
							if (LuceneTestCase.Random().NextBoolean())
							{
								string packID;
								ThreadedIndexingAndSearchingTestCase.SubDocs delSubDocs;
								if (toDeleteSubDocs.Count > 0 && LuceneTestCase.Random().NextBoolean())
								{
									delSubDocs = toDeleteSubDocs[LuceneTestCase.Random().Next(toDeleteSubDocs.Count)];
									toDeleteSubDocs.Remove(delSubDocs);
									packID = delSubDocs.packID;
								}
								else
								{
									delSubDocs = null;
									packID = this._enclosing.packCount.GetAndIncrement() + string.Empty;
								}
								Field packIDField = LuceneTestCase.NewStringField("packID", packID, Field.Store.YES
									);
								IList<string> docIDs = new AList<string>();
								ThreadedIndexingAndSearchingTestCase.SubDocs subDocs = new ThreadedIndexingAndSearchingTestCase.SubDocs
									(packID, docIDs);
								IList<Lucene.NetDocument.Document> docsList = new AList<Lucene.NetDocument.Document
									>();
								allSubDocs.AddItem(subDocs);
								doc.Add(packIDField);
								docsList.AddItem(TestUtil.CloneDocument(doc));
								docIDs.AddItem(doc.Get("docid"));
								int maxDocCount = TestUtil.NextInt(LuceneTestCase.Random(), 1, 10);
								while (docsList.Count < maxDocCount)
								{
									doc = docs.NextDoc();
									if (doc == null)
									{
										break;
									}
									docsList.AddItem(TestUtil.CloneDocument(doc));
									docIDs.AddItem(doc.Get("docid"));
								}
								this._enclosing.addCount.AddAndGet(docsList.Count);
								Term packIDTerm = new Term("packID", packID);
								if (delSubDocs != null)
								{
									delSubDocs.deleted = true;
									Sharpen.Collections.AddAll(delIDs, delSubDocs.subIDs);
									this._enclosing.delCount.AddAndGet(delSubDocs.subIDs.Count);
									if (LuceneTestCase.VERBOSE)
									{
										System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": update pack packID="
											 + delSubDocs.packID + " count=" + docsList.Count + " docs=" + docIDs);
									}
									this._enclosing.UpdateDocuments(packIDTerm, docsList);
								}
								else
								{
									if (LuceneTestCase.VERBOSE)
									{
										System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": add pack packID="
											 + packID + " count=" + docsList.Count + " docs=" + docIDs);
									}
									this._enclosing.AddDocuments(packIDTerm, docsList);
								}
								doc.RemoveField("packID");
								if (LuceneTestCase.Random().Next(5) == 2)
								{
									if (LuceneTestCase.VERBOSE)
									{
										System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": buffer del id:"
											 + packID);
									}
									toDeleteSubDocs.AddItem(subDocs);
								}
							}
							else
							{
								string docid = doc.Get("docid");
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": add doc docid:"
										 + docid);
								}
								this._enclosing.AddDocument(new Term("docid", docid), doc);
								this._enclosing.addCount.GetAndIncrement();
								if (LuceneTestCase.Random().Next(5) == 3)
								{
									if (LuceneTestCase.VERBOSE)
									{
										System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": buffer del id:"
											 + doc.Get("docid"));
									}
									toDeleteIDs.AddItem(docid);
								}
							}
						}
						else
						{
							if (LuceneTestCase.VERBOSE)
							{
								System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": update doc id:"
									 + doc.Get("docid"));
							}
							string docid = doc.Get("docid");
							this._enclosing.UpdateDocument(new Term("docid", docid), doc);
							this._enclosing.addCount.GetAndIncrement();
							if (LuceneTestCase.Random().Next(5) == 3)
							{
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": buffer del id:"
										 + doc.Get("docid"));
								}
								toDeleteIDs.AddItem(docid);
							}
						}
						if (LuceneTestCase.Random().Next(30) == 17)
						{
							if (LuceneTestCase.VERBOSE)
							{
								System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": apply "
									 + toDeleteIDs.Count + " deletes");
							}
							foreach (string id in toDeleteIDs)
							{
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": del term=id:"
										 + id);
								}
								this._enclosing.DeleteDocuments(new Term("docid", id));
							}
							int count = this._enclosing.delCount.AddAndGet(toDeleteIDs.Count);
							if (LuceneTestCase.VERBOSE)
							{
								System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": tot " 
									+ count + " deletes");
							}
							Sharpen.Collections.AddAll(delIDs, toDeleteIDs);
							toDeleteIDs.Clear();
							foreach (ThreadedIndexingAndSearchingTestCase.SubDocs subDocs in toDeleteSubDocs)
							{
								delPackIDs.AddItem(subDocs.packID);
								this._enclosing.DeleteDocuments(new Term("packID", subDocs.packID));
								subDocs.deleted = true;
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": del subs: "
										 + subDocs.subIDs + " packID=" + subDocs.packID);
								}
								Sharpen.Collections.AddAll(delIDs, subDocs.subIDs);
								this._enclosing.delCount.AddAndGet(subDocs.subIDs.Count);
							}
							toDeleteSubDocs.Clear();
						}
						if (addedField != null)
						{
							doc.RemoveField(addedField);
						}
					}
					catch (Exception t)
					{
						System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": hit exc"
							);
						Sharpen.Runtime.PrintStackTrace(t);
						this._enclosing.failed.Set(true);
						throw new RuntimeException(t);
					}
				}
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": indexing done"
						);
				}
				this._enclosing.DoAfterIndexingThreadDone();
			}

			private readonly ThreadedIndexingAndSearchingTestCase _enclosing;

			private readonly long stopTime;

			private readonly LineFileDocs docs;

			private readonly IList<ThreadedIndexingAndSearchingTestCase.SubDocs> allSubDocs;

			private readonly ICollection<string> delIDs;

			private readonly ICollection<string> delPackIDs;
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void RunSearchThreads(long stopTimeMS)
		{
			int numThreads = TestUtil.NextInt(Random(), 1, 5);
			Sharpen.Thread[] searchThreads = new Sharpen.Thread[numThreads];
			AtomicInteger totHits = new AtomicInteger();
			// silly starting guess:
			AtomicInteger totTermCount = new AtomicInteger(100);
			// TODO: we should enrich this to do more interesting searches
			for (int thread = 0; thread < searchThreads.Length; thread++)
			{
				searchThreads[thread] = new _Thread_332(this, stopTimeMS, totTermCount, totHits);
				// Verify 1) IW is correctly setting
				// diagnostics, and 2) segment warming for
				// merged segments is actually happening:
				// search 30 terms
				//if (VERBOSE) {
				//System.out.println(Thread.currentThread().getName() + " now search body:" + term.utf8ToString());
				//}
				//if (VERBOSE) {
				//System.out.println(Thread.currentThread().getName() + ": search done");
				//}
				searchThreads[thread].SetDaemon(true);
				searchThreads[thread].Start();
			}
			for (int thread_1 = 0; thread_1 < searchThreads.Length; thread_1++)
			{
				searchThreads[thread_1].Join();
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: DONE search: totHits=" + totHits);
			}
		}

		private sealed class _Thread_332 : Sharpen.Thread
		{
			public _Thread_332(ThreadedIndexingAndSearchingTestCase _enclosing, long stopTimeMS
				, AtomicInteger totTermCount, AtomicInteger totHits)
			{
				this._enclosing = _enclosing;
				this.stopTimeMS = stopTimeMS;
				this.totTermCount = totTermCount;
				this.totHits = totHits;
			}

			public override void Run()
			{
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": launch search thread"
						);
				}
				while (DateTime.Now.CurrentTimeMillis() < stopTimeMS)
				{
					try
					{
						IndexSearcher s = this._enclosing.GetCurrentSearcher();
						try
						{
							foreach (AtomicReaderContext sub in s.GetIndexReader().Leaves())
							{
								SegmentReader segReader = (SegmentReader)((AtomicReader)sub.Reader());
								IDictionary<string, string> diagnostics = segReader.GetSegmentInfo().info.GetDiagnostics
									();
								NUnit.Framework.Assert.IsNotNull(diagnostics);
								string source = diagnostics.Get("source");
								NUnit.Framework.Assert.IsNotNull(source);
								if (source.Equals("merge"))
								{
									NUnit.Framework.Assert.IsTrue("sub reader " + sub + " wasn't warmed: warmed=" + this
										._enclosing.warmed + " diagnostics=" + diagnostics + " si=" + segReader.GetSegmentInfo
										(), !this._enclosing.assertMergedSegmentsWarmed || this._enclosing.warmed.ContainsKey
										(segReader.core));
								}
							}
							if (s.GetIndexReader().NumDocs() > 0)
							{
								this._enclosing.SmokeTestSearcher(s);
								Fields fields = MultiFields.GetFields(s.GetIndexReader());
								if (fields == null)
								{
									continue;
								}
								Terms terms = fields.Terms("body");
								if (terms == null)
								{
									continue;
								}
								TermsEnum termsEnum = terms.Iterator(null);
								int seenTermCount = 0;
								int shift;
								int trigger;
								if (totTermCount.Get() < 30)
								{
									shift = 0;
									trigger = 1;
								}
								else
								{
									trigger = totTermCount.Get() / 30;
									shift = LuceneTestCase.Random().Next(trigger);
								}
								while (DateTime.Now.CurrentTimeMillis() < stopTimeMS)
								{
									BytesRef term = termsEnum.Next();
									if (term == null)
									{
										totTermCount.Set(seenTermCount);
										break;
									}
									seenTermCount++;
									if ((seenTermCount + shift) % trigger == 0)
									{
										totHits.AddAndGet(this._enclosing.RunQuery(s, new TermQuery(new Term("body", term
											))));
									}
								}
							}
						}
						finally
						{
							this._enclosing.ReleaseSearcher(s);
						}
					}
					catch (Exception t)
					{
						System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": hit exc"
							);
						this._enclosing.failed.Set(true);
						Sharpen.Runtime.PrintStackTrace(t, System.Console.Out);
						throw new RuntimeException(t);
					}
				}
			}

			private readonly ThreadedIndexingAndSearchingTestCase _enclosing;

			private readonly long stopTimeMS;

			private readonly AtomicInteger totTermCount;

			private readonly AtomicInteger totHits;
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void DoAfterWriter(ExecutorService es)
		{
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void DoClose()
		{
		}

		protected internal bool assertMergedSegmentsWarmed = true;

		private readonly IDictionary<SegmentCoreReaders, bool> warmed = Sharpen.Collections
			.SynchronizedMap(new WeakHashMap<SegmentCoreReaders, bool>());

		/// <exception cref="System.Exception"></exception>
		public virtual void RunTest(string testName)
		{
			failed.Set(false);
			addCount.Set(0);
			delCount.Set(0);
			packCount.Set(0);
			long t0 = DateTime.Now.CurrentTimeMillis();
			Random random = new Random(Random().NextLong());
			LineFileDocs docs = new LineFileDocs(random, DefaultCodecSupportsDocValues());
			DirectoryInfo tempDir = CreateTempDir(testName);
			dir = GetDirectory(NewMockFSDirectory(tempDir));
			// some subclasses rely on this being MDW
			if (dir is BaseDirectoryWrapper)
			{
				((BaseDirectoryWrapper)dir).SetCheckIndexOnClose(false);
			}
			// don't double-checkIndex, we do it ourselves.
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH
				));
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetInfoStream(new FailOnNonBulkMergesInfoStream());
			if (conf.GetMergePolicy() is MockRandomMergePolicy)
			{
				((MockRandomMergePolicy)conf.GetMergePolicy()).SetDoNonBulkMerges(false);
			}
			if (LuceneTestCase.TEST_NIGHTLY)
			{
				// newIWConfig makes smallish max seg size, which
				// results in tons and tons of segments for this test
				// when run nightly:
				MergePolicy mp = conf.GetMergePolicy();
				if (mp is TieredMergePolicy)
				{
					((TieredMergePolicy)mp).SetMaxMergedSegmentMB(5000.);
				}
				else
				{
					if (mp is LogByteSizeMergePolicy)
					{
						((LogByteSizeMergePolicy)mp).SetMaxMergeMB(1000.);
					}
					else
					{
						if (mp is LogMergePolicy)
						{
							((LogMergePolicy)mp).SetMaxMergeDocs(100000);
						}
					}
				}
			}
			conf.SetMergedSegmentWarmer(new _IndexReaderWarmer_469(this));
			if (VERBOSE)
			{
				conf.SetInfoStream(new _PrintStreamInfoStream_497(System.Console.Out));
			}
			// ignore test points!
			writer = new IndexWriter(dir, conf);
			TestUtil.ReduceOpenFiles(writer);
			ExecutorService es = Random().NextBoolean() ? null : Executors.NewCachedThreadPool
				(new NamedThreadFactory(testName));
			DoAfterWriter(es);
			int NUM_INDEX_THREADS = TestUtil.NextInt(Random(), 2, 4);
			int RUN_TIME_SEC = LuceneTestCase.TEST_NIGHTLY ? 300 : RANDOM_MULTIPLIER;
			ICollection<string> delIDs = Sharpen.Collections.SynchronizedSet(new HashSet<string
				>());
			ICollection<string> delPackIDs = Sharpen.Collections.SynchronizedSet(new HashSet<
				string>());
			IList<ThreadedIndexingAndSearchingTestCase.SubDocs> allSubDocs = Sharpen.Collections
				.SynchronizedList(new AList<ThreadedIndexingAndSearchingTestCase.SubDocs>());
			long stopTime = DateTime.Now.CurrentTimeMillis() + RUN_TIME_SEC * 1000;
			Sharpen.Thread[] indexThreads = LaunchIndexingThreads(docs, NUM_INDEX_THREADS, stopTime
				, delIDs, delPackIDs, allSubDocs);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: DONE start " + NUM_INDEX_THREADS + " indexing threads ["
					 + (DateTime.Now.CurrentTimeMillis() - t0) + " ms]");
			}
			// Let index build up a bit
			Sharpen.Thread.Sleep(100);
			DoSearching(es, stopTime);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: all searching done [" + (Runtime.CurrentTimeMillis
					() - t0) + " ms]");
			}
			for (int thread = 0; thread < indexThreads.Length; thread++)
			{
				indexThreads[thread].Join();
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: done join indexing threads [" + (Runtime.CurrentTimeMillis
					() - t0) + " ms]; addCount=" + addCount + " delCount=" + delCount);
			}
			IndexSearcher s = GetFinalSearcher();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: finalSearcher=" + s);
			}
			NUnit.Framework.Assert.IsFalse(failed.Get());
			bool doFail = false;
			// Verify: make sure delIDs are in fact deleted:
			foreach (string id in delIDs)
			{
				TopDocs hits = s.Search(new TermQuery(new Term("docid", id)), 1);
				if (hits.totalHits != 0)
				{
					System.Console.Out.WriteLine("doc id=" + id + " is supposed to be deleted, but got "
						 + hits.totalHits + " hits; first docID=" + hits.scoreDocs[0].doc);
					doFail = true;
				}
			}
			// Verify: make sure delPackIDs are in fact deleted:
			foreach (string id_1 in delPackIDs)
			{
				TopDocs hits = s.Search(new TermQuery(new Term("packID", id_1)), 1);
				if (hits.totalHits != 0)
				{
					System.Console.Out.WriteLine("packID=" + id_1 + " is supposed to be deleted, but got "
						 + hits.totalHits + " matches");
					doFail = true;
				}
			}
			// Verify: make sure each group of sub-docs are still in docID order:
			foreach (SubDocs subDocs in allSubDocs)
			{
				TopDocs hits = s.Search(new TermQuery(new Term("packID", subDocs.packID)), 20);
				if (!subDocs.deleted)
				{
					// We sort by relevance but the scores should be identical so sort falls back to by docID:
					if (hits.totalHits != subDocs.subIDs.Count)
					{
						System.Console.Out.WriteLine("packID=" + subDocs.packID + ": expected " + subDocs
							.subIDs.Count + " hits but got " + hits.totalHits);
						doFail = true;
					}
					else
					{
						int lastDocID = -1;
						int startDocID = -1;
						foreach (ScoreDoc scoreDoc in hits.scoreDocs)
						{
							int docID = scoreDoc.doc;
							if (lastDocID != -1)
							{
								AreEqual(1 + lastDocID, docID);
							}
							else
							{
								startDocID = docID;
							}
							lastDocID = docID;
							Lucene.NetDocument.Document doc = s.Doc(docID);
							NUnit.Framework.Assert.AreEqual(subDocs.packID, doc.Get("packID"));
						}
						lastDocID = startDocID - 1;
						foreach (string subID in subDocs.subIDs)
						{
							hits = s.Search(new TermQuery(new Term("docid", subID)), 1);
							NUnit.Framework.Assert.AreEqual(1, hits.totalHits);
							int docID = hits.scoreDocs[0].doc;
							if (lastDocID != -1)
							{
								NUnit.Framework.Assert.AreEqual(1 + lastDocID, docID);
							}
							lastDocID = docID;
						}
					}
				}
				else
				{
					// Pack was deleted -- make sure its docs are
					// deleted.  We can't verify packID is deleted
					// because we can re-use packID for update:
					foreach (string subID in subDocs.subIDs)
					{
						NUnit.Framework.Assert.AreEqual(0, s.Search(new TermQuery(new Term("docid", subID
							)), 1).totalHits);
					}
				}
			}
			// Verify: make sure all not-deleted docs are in fact
			// not deleted:
			int endID = System.Convert.ToInt32(docs.NextDoc().Get("docid"));
			docs.Close();
			for (int id_2 = 0; id_2 < endID; id_2++)
			{
				string stringID = string.Empty + id_2;
				if (!delIDs.Contains(stringID))
				{
					TopDocs hits = s.Search(new TermQuery(new Term("docid", stringID)), 1);
					if (hits.totalHits != 1)
					{
						System.Console.Out.WriteLine("doc id=" + stringID + " is not supposed to be deleted, but got hitCount="
							 + hits.totalHits + "; delIDs=" + delIDs);
						doFail = true;
					}
				}
			}
			IsFalse(doFail);
			AreEqual("index=" + writer.SegString() + " addCount=" + addCount
				 + " delCount=" + delCount, addCount.Get() - delCount.Get(), s.GetIndexReader().
				NumDocs());
			ReleaseSearcher(s);
			writer.Commit();
			AreEqual("index=" + writer.SegString() + " addCount=" + addCount
				 + " delCount=" + delCount, addCount.Get() - delCount.Get(), writer.NumDocs());
			DoClose();
			writer.Close(false);
			// Cannot shutdown until after writer is closed because
			// writer has merged segment warmer that uses IS to run
			// searches, and that IS may be using this es!
			if (es != null)
			{
				es.Shutdown();
				es.AwaitTermination(1, TimeUnit.SECONDS);
			}
			TestUtil.CheckIndex(dir);
			dir.Close();
			TestUtil.Rm(tempDir);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: done [" + (DateTime.Now.CurrentTimeMillis() - t0) 
					+ " ms]");
			}
		}

		private sealed class _IndexReaderWarmer_469 : IndexWriter.IndexReaderWarmer
		{
			public _IndexReaderWarmer_469(ThreadedIndexingAndSearchingTestCase _enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Warm(AtomicReader reader)
			{
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: now warm merged reader=" + reader);
				}
				this._enclosing.warmed.Put(((SegmentReader)reader).core, true);
				int maxDoc = reader.MaxDoc();
				Bits liveDocs = reader.GetLiveDocs();
				int sum = 0;
				int inc = Math.Max(1, maxDoc / 50);
				for (int docID = 0; docID < maxDoc; docID += inc)
				{
					if (liveDocs == null || liveDocs.Get(docID))
					{
						Lucene.NetDocument.Document doc = reader.Document(docID);
						sum += doc.GetFields().Count;
					}
				}
				IndexSearcher searcher = LuceneTestCase.NewSearcher(reader);
				sum += searcher.Search(new TermQuery(new Term("body", "united")), 10).totalHits;
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: warm visited " + sum + " fields");
				}
			}

			private readonly ThreadedIndexingAndSearchingTestCase _enclosing;
		}

		private sealed class _PrintStreamInfoStream_497 : PrintStreamInfoStream
		{
			public _PrintStreamInfoStream_497(TextWriter baseArg1) : base(baseArg1)
			{
			}

			public override void Message(string component, string message)
			{
				if ("TP".Equals(component))
				{
					return;
				}
				base.Message(component, message);
			}
		}

		/// <exception cref="System.Exception"></exception>
		private int RunQuery(IndexSearcher s, Query q)
		{
			s.Search(q, 10);
			int hitCount = s.Search(q, null, 10, new Sort(new SortField("title", SortField.Type
				.STRING))).totalHits;
			if (DefaultCodecSupportsDocValues())
			{
				Sort dvSort = new Sort(new SortField("title", SortField.Type.STRING));
				int hitCount2 = s.Search(q, null, 10, dvSort).totalHits;
				NUnit.Framework.Assert.AreEqual(hitCount, hitCount2);
			}
			return hitCount;
		}

		/// <exception cref="System.Exception"></exception>
		protected internal virtual void SmokeTestSearcher(IndexSearcher s)
		{
			RunQuery(s, new TermQuery(new Term("body", "united")));
			RunQuery(s, new TermQuery(new Term("titleTokenized", "states")));
			PhraseQuery pq = new PhraseQuery();
			pq.Add(new Term("body", "united"));
			pq.Add(new Term("body", "states"));
			RunQuery(s, pq);
		}
	}
}
