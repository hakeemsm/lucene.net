/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestStressNRT : LuceneTestCase
	{
		internal volatile DirectoryReader reader;

		internal readonly ConcurrentHashMap<int, long> model = new ConcurrentHashMap<int, 
			long>();

		internal IDictionary<int, long> committedModel = new Dictionary<int, long>();

		internal long snapshotCount;

		internal long committedModelClock;

		internal volatile int lastId;

		internal readonly string field = "val_l";

		internal object[] syncArr;

		private void InitModel(int ndocs)
		{
			snapshotCount = 0;
			committedModelClock = 0;
			lastId = 0;
			syncArr = new object[ndocs];
			for (int i = 0; i < ndocs; i++)
			{
				model.Put(i, -1L);
				syncArr[i] = new object();
			}
			committedModel.PutAll(model);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			// update variables
			int commitPercent = Random().Next(20);
			int softCommitPercent = Random().Next(100);
			// what percent of the commits are soft
			int deletePercent = Random().Next(50);
			int deleteByQueryPercent = Random().Next(25);
			int ndocs = AtLeast(50);
			int nWriteThreads = TestUtil.NextInt(Random(), 1, TEST_NIGHTLY ? 10 : 5);
			int maxConcurrentCommits = TestUtil.NextInt(Random(), 1, TEST_NIGHTLY ? 10 : 5);
			// number of committers at a time... needed if we want to avoid commit errors due to exceeding the max
			bool tombstones = Random().NextBoolean();
			// query variables
			AtomicLong operations = new AtomicLong(AtLeast(10000));
			// number of query operations to perform in total
			int nReadThreads = TestUtil.NextInt(Random(), 1, TEST_NIGHTLY ? 10 : 5);
			InitModel(ndocs);
			FieldType storedOnlyType = new FieldType();
			storedOnlyType.Stored = (true);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\n");
				System.Console.Out.WriteLine("TEST: commitPercent=" + commitPercent);
				System.Console.Out.WriteLine("TEST: softCommitPercent=" + softCommitPercent);
				System.Console.Out.WriteLine("TEST: deletePercent=" + deletePercent);
				System.Console.Out.WriteLine("TEST: deleteByQueryPercent=" + deleteByQueryPercent
					);
				System.Console.Out.WriteLine("TEST: ndocs=" + ndocs);
				System.Console.Out.WriteLine("TEST: nWriteThreads=" + nWriteThreads);
				System.Console.Out.WriteLine("TEST: nReadThreads=" + nReadThreads);
				System.Console.Out.WriteLine("TEST: maxConcurrentCommits=" + maxConcurrentCommits
					);
				System.Console.Out.WriteLine("TEST: tombstones=" + tombstones);
				System.Console.Out.WriteLine("TEST: operations=" + operations);
				System.Console.Out.WriteLine("\n");
			}
			AtomicInteger numCommitting = new AtomicInteger();
			IList<Sharpen.Thread> threads = new AList<Sharpen.Thread>();
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
			writer.SetDoRandomForceMergeAssert(false);
			writer.Commit();
			reader = DirectoryReader.Open(dir);
			for (int i = 0; i < nWriteThreads; i++)
			{
				Sharpen.Thread thread = new _Thread_115(this, operations, commitPercent, numCommitting
					, maxConcurrentCommits, softCommitPercent, writer, ndocs, deletePercent, tombstones
					, storedOnlyType, deleteByQueryPercent, "WRITER" + i);
				// take a snapshot
				// increment the reference since we will use this for reopening
				// assertU(h.commit("softCommit","true"));
				// assertU(commit());
				// Code below assumes newReader comes w/
				// extra ref:
				// install the new reader if it's newest (and check the current version since another reader may have already been installed)
				//System.out.println(Thread.currentThread().getName() + ": newVersion=" + newReader.getVersion());
				//HM:revisit 
				//assert newReader.getRefCount() > 0;
				//HM:revisit 
				//assert reader.getRefCount() > 0;
				// Silly: forces fieldInfos to be
				// loaded so we don't hit IOE on later
				// reader.toString
				// install this snapshot only if it's newer than the current one
				// if the same reader, don't decRef.
				// set the lastId before we actually change it sometimes to try and
				// uncover more race conditions between writing and reading
				// We can't concurrently update the same document and retain our invariants of increasing values
				// since we can't guarantee what order the updates will be executed.
				// assertU("<delete><id>" + id + "</id></delete>");
				// add tombstone first
				//assertU("<delete><query>id:" + id + "</query></delete>");
				// add tombstone first
				// assertU(adoc("id",Integer.toString(id), field, Long.toString(nextVal)));
				// remove tombstone after new addition (this should be optional?)
				threads.AddItem(thread);
			}
			for (int i_1 = 0; i_1 < nReadThreads; i_1++)
			{
				Sharpen.Thread thread = new _Thread_299(this, operations, ndocs, tombstones, "READER"
					 + i_1);
				// bias toward a recently changed doc
				// when indexing, we update the index, then the model
				// so when querying, we should first check the model, and then the index
				//  sreq = req("wt","json", "q","id:"+Integer.toString(id), "omitHeader","true");
				// Just re-use lastSearcher, else
				// newSearcher may create too many thread
				// pools (ExecutorService):
				// if we couldn't find the doc, look for its tombstone
				// expected... no doc was added yet
				// nothing to do - we can't tell anything from a deleted doc without tombstones
				// we should have found the document, or its tombstone
				threads.AddItem(thread);
			}
			foreach (Sharpen.Thread thread_1 in threads)
			{
				thread_1.Start();
			}
			foreach (Sharpen.Thread thread_2 in threads)
			{
				thread_2.Join();
			}
			writer.Dispose();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: close reader=" + reader);
			}
			reader.Dispose();
			dir.Dispose();
		}

		private sealed class _Thread_115 : Sharpen.Thread
		{
			public _Thread_115(TestStressNRT _enclosing, AtomicLong operations, int commitPercent
				, AtomicInteger numCommitting, int maxConcurrentCommits, int softCommitPercent, 
				RandomIndexWriter writer, int ndocs, int deletePercent, bool tombstones, FieldType
				 storedOnlyType, int deleteByQueryPercent, string baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.operations = operations;
				this.commitPercent = commitPercent;
				this.numCommitting = numCommitting;
				this.maxConcurrentCommits = maxConcurrentCommits;
				this.softCommitPercent = softCommitPercent;
				this.writer = writer;
				this.ndocs = ndocs;
				this.deletePercent = deletePercent;
				this.tombstones = tombstones;
				this.storedOnlyType = storedOnlyType;
				this.deleteByQueryPercent = deleteByQueryPercent;
				this.rand = new Random(LuceneTestCase.Random().Next());
			}

			internal Random rand;

			public override void Run()
			{
				try
				{
					while (operations.Get() > 0)
					{
						int oper = this.rand.Next(100);
						if (oper < commitPercent)
						{
							if (numCommitting.IncrementAndGet() <= maxConcurrentCommits)
							{
								IDictionary<int, long> newCommittedModel;
								long version;
								DirectoryReader oldReader;
								lock (this._enclosing)
								{
									newCommittedModel = new Dictionary<int, long>(this._enclosing.model);
									version = this._enclosing.snapshotCount++;
									oldReader = this._enclosing.reader;
									oldReader.IncRef();
								}
								DirectoryReader newReader;
								if (this.rand.Next(100) < softCommitPercent)
								{
									if (LuceneTestCase.Random().NextBoolean())
									{
										if (LuceneTestCase.VERBOSE)
										{
											System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
												+ ": call writer.getReader");
										}
										newReader = writer.GetReader(true);
									}
									else
									{
										if (LuceneTestCase.VERBOSE)
										{
											System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
												+ ": reopen reader=" + oldReader + " version=" + version);
										}
										newReader = DirectoryReader.OpenIfChanged(oldReader, writer.w, true);
									}
								}
								else
								{
									if (LuceneTestCase.VERBOSE)
									{
										System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
											+ ": commit+reopen reader=" + oldReader + " version=" + version);
									}
									writer.Commit();
									if (LuceneTestCase.VERBOSE)
									{
										System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
											+ ": now reopen after commit");
									}
									newReader = DirectoryReader.OpenIfChanged(oldReader);
								}
								if (newReader == null)
								{
									oldReader.IncRef();
									newReader = oldReader;
								}
								oldReader.DecRef();
								lock (this._enclosing)
								{
									if (newReader.Version > this._enclosing.reader.Version)
									{
										if (LuceneTestCase.VERBOSE)
										{
											System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
												+ ": install new reader=" + newReader);
										}
										this._enclosing.reader.DecRef();
										this._enclosing.reader = newReader;
										newReader.ToString();
										if (version >= this._enclosing.committedModelClock)
										{
											if (LuceneTestCase.VERBOSE)
											{
												System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
													+ ": install new model version=" + version);
											}
											this._enclosing.committedModel = newCommittedModel;
											this._enclosing.committedModelClock = version;
										}
										else
										{
											if (LuceneTestCase.VERBOSE)
											{
												System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
													+ ": skip install new model version=" + version);
											}
										}
									}
									else
									{
										if (LuceneTestCase.VERBOSE)
										{
											System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
												+ ": skip install new reader=" + newReader);
										}
										newReader.DecRef();
									}
								}
							}
							numCommitting.DecrementAndGet();
						}
						else
						{
							int id = this.rand.Next(ndocs);
							object sync = this._enclosing.syncArr[id];
							bool before = LuceneTestCase.Random().NextBoolean();
							if (before)
							{
								this._enclosing.lastId = id;
							}
							lock (sync)
							{
								long val = this._enclosing.model.Get(id);
								long nextVal = Math.Abs(val) + 1;
								if (oper < commitPercent + deletePercent)
								{
									if (tombstones)
									{
										Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
										d.Add(LuceneTestCase.NewStringField("id", "-" + Sharpen.Extensions.ToString(id), 
											Field.Store.YES));
										d.Add(LuceneTestCase.NewField(this._enclosing.field, System.Convert.ToString(nextVal
											), storedOnlyType));
										writer.UpdateDocument(new Term("id", "-" + Sharpen.Extensions.ToString(id)), d);
									}
									if (LuceneTestCase.VERBOSE)
									{
										System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
											+ ": term delDocs id:" + id + " nextVal=" + nextVal);
									}
									writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(id)));
									this._enclosing.model.Put(id, -nextVal);
								}
								else
								{
									if (oper < commitPercent + deletePercent + deleteByQueryPercent)
									{
										if (tombstones)
										{
											Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
											d.Add(LuceneTestCase.NewStringField("id", "-" + Sharpen.Extensions.ToString(id), 
												Field.Store.YES));
											d.Add(LuceneTestCase.NewField(this._enclosing.field, System.Convert.ToString(nextVal
												), storedOnlyType));
											writer.UpdateDocument(new Term("id", "-" + Sharpen.Extensions.ToString(id)), d);
										}
										if (LuceneTestCase.VERBOSE)
										{
											System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
												+ ": query delDocs id:" + id + " nextVal=" + nextVal);
										}
										writer.DeleteDocuments(new TermQuery(new Term("id", Sharpen.Extensions.ToString(id
											))));
										this._enclosing.model.Put(id, -nextVal);
									}
									else
									{
										Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
										d.Add(LuceneTestCase.NewStringField("id", Sharpen.Extensions.ToString(id), Field.Store
											.YES));
										d.Add(LuceneTestCase.NewField(this._enclosing.field, System.Convert.ToString(nextVal
											), storedOnlyType));
										if (LuceneTestCase.VERBOSE)
										{
											System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
												+ ": u id:" + id + " val=" + nextVal);
										}
										writer.UpdateDocument(new Term("id", Sharpen.Extensions.ToString(id)), d);
										if (tombstones)
										{
											writer.DeleteDocuments(new Term("id", "-" + Sharpen.Extensions.ToString(id)));
										}
										this._enclosing.model.Put(id, nextVal);
									}
								}
							}
							if (!before)
							{
								this._enclosing.lastId = id;
							}
						}
					}
				}
				catch (Exception e)
				{
					System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": FAILED: unexpected exception"
						);
					Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
					throw new RuntimeException(e);
				}
			}

			private readonly TestStressNRT _enclosing;

			private readonly AtomicLong operations;

			private readonly int commitPercent;

			private readonly AtomicInteger numCommitting;

			private readonly int maxConcurrentCommits;

			private readonly int softCommitPercent;

			private readonly RandomIndexWriter writer;

			private readonly int ndocs;

			private readonly int deletePercent;

			private readonly bool tombstones;

			private readonly FieldType storedOnlyType;

			private readonly int deleteByQueryPercent;
		}

		private sealed class _Thread_299 : Sharpen.Thread
		{
			public _Thread_299(TestStressNRT _enclosing, AtomicLong operations, int ndocs, bool
				 tombstones, string baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.operations = operations;
				this.ndocs = ndocs;
				this.tombstones = tombstones;
				this.rand = new Random(LuceneTestCase.Random().Next());
			}

			internal Random rand;

			public override void Run()
			{
				try
				{
					IndexReader lastReader = null;
					IndexSearcher lastSearcher = null;
					while (operations.DecrementAndGet() >= 0)
					{
						int id = this.rand.Next(100) < 25 ? this._enclosing.lastId : this.rand.Next(ndocs
							);
						long val;
						DirectoryReader r;
						lock (this._enclosing)
						{
							val = this._enclosing.committedModel.Get(id);
							r = this._enclosing.reader;
							r.IncRef();
						}
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: " + Sharpen.Thread.CurrentThread().GetName() 
								+ ": s id=" + id + " val=" + val + " r=" + r.Version);
						}
						IndexSearcher searcher;
						if (r == lastReader)
						{
							searcher = lastSearcher;
						}
						else
						{
							searcher = LuceneTestCase.NewSearcher(r);
							lastReader = r;
							lastSearcher = searcher;
						}
						Query q = new TermQuery(new Term("id", Sharpen.Extensions.ToString(id)));
						TopDocs results = searcher.Search(q, 10);
						if (results.TotalHits == 0 && tombstones)
						{
							q = new TermQuery(new Term("id", "-" + Sharpen.Extensions.ToString(id)));
							results = searcher.Search(q, 1);
							if (results.TotalHits == 0)
							{
								if (val == -1L)
								{
									r.DecRef();
									continue;
								}
								Fail("No documents or tombstones found for id " + id + ", expected at least "
									 + val + " reader=" + r);
							}
						}
						if (results.TotalHits == 0 && !tombstones)
						{
						}
						else
						{
							if (results.TotalHits != 1)
							{
								System.Console.Out.WriteLine("FAIL: hits id:" + id + " val=" + val);
								foreach (ScoreDoc sd in results.ScoreDocs)
								{
									Lucene.Net.Documents.Document doc = r.Document(sd.Doc);
									System.Console.Out.WriteLine("  docID=" + sd.Doc + " id:" + doc.Get("id") + " foundVal="
										 + doc.Get(this._enclosing.field));
								}
								Fail("id=" + id + " reader=" + r + " TotalHits=" + results
									.TotalHits);
							}
							Lucene.Net.Documents.Document doc_1 = searcher.Doc(results.ScoreDocs[0].Doc
								);
							long foundVal = long.Parse(doc_1.Get(this._enclosing.field));
							if (foundVal < Math.Abs(val))
							{
								Fail("foundVal=" + foundVal + " val=" + val + " id=" + id 
									+ " reader=" + r);
							}
						}
						r.DecRef();
					}
				}
				catch (Exception e)
				{
					operations.Set(-1L);
					System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": FAILED: unexpected exception"
						);
					Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
					throw new RuntimeException(e);
				}
			}

			private readonly TestStressNRT _enclosing;

			private readonly AtomicLong operations;

			private readonly int ndocs;

			private readonly bool tombstones;
		}
	}
}
