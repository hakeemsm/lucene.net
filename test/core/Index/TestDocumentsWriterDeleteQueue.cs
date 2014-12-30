using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// Unit test for
	/// <see cref="DocumentsWriterDeleteQueue">DocumentsWriterDeleteQueue</see>
	/// </summary>
	[TestFixture]
    public class TestDocumentsWriterDeleteQueue : LuceneTestCase
	{
        [Test]
		public virtual void TestUpdateDelteSlices()
		{
			DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
			int size = 200 + Random().Next(500) * RANDOM_MULTIPLIER;
			int[] ids = new int[size];
			for (int i = 0; i < ids.Length; i++)
			{
				ids[i] = Random().Next();
			}
			DocumentsWriterDeleteQueue.DeleteSlice slice1 = queue.NewSlice();
			DocumentsWriterDeleteQueue.DeleteSlice slice2 = queue.NewSlice();
			BufferedUpdates bd1 = new BufferedUpdates();
			BufferedUpdates bd2 = new BufferedUpdates();
			int last1 = 0;
			int last2 = 0;
			ICollection<Term> uniqueValues = new HashSet<Term>();
			for (int j = 0; j < ids.Length; j++)
			{
				int i_1 = ids[j];
				// create an array here since we compare identity below against tailItem
				Term[] term = new Term[] { new Term("id", i_1.ToString()) };
				uniqueValues.Add(term[0]);
				queue.AddDelete(term);
				if (Random().Next(20) == 0 || j == ids.Length - 1)
				{
					queue.UpdateSlice(slice1);
					IsTrue(slice1.IsTailItem(term));
					slice1.Apply(bd1, j);
					AssertAllBetween(last1, j, bd1, ids);
					last1 = j + 1;
				}
				if (Random().Next(10) == 5 || j == ids.Length - 1)
				{
					queue.UpdateSlice(slice2);
					IsTrue(slice2.IsTailItem(term));
					slice2.Apply(bd2, j);
					AssertAllBetween(last2, j, bd2, ids);
					last2 = j + 1;
				}
				AreEqual(j + 1, queue.NumGlobalTermDeletes);
			}
			AreEqual(uniqueValues, bd1.terms.Keys);
			AreEqual(uniqueValues, bd2.terms.Keys);
			HashSet<Term> frozenSet = new HashSet<Term>();
			foreach (Term t in queue.FreezeGlobalBuffer(null).TermsIterable())
			{
				BytesRef bytesRef = new BytesRef();
				bytesRef.CopyBytes(t.bytes);
				frozenSet.Add(new Term(t.field, bytesRef));
			}
			AreEqual(uniqueValues, frozenSet);
			AssertEquals("num deletes must be 0 after freeze", 0, queue.NumGlobalTermDeletes);
		}

		private void AssertAllBetween(int start, int end, BufferedUpdates deletes, int[] 
			ids)
		{
			for (int i = start; i <= end; i++)
			{
				AreEqual(end, deletes.terms[(new Term("id", ids[i].ToString()))]);
			}
		}

        [Test]
		public virtual void TestClear()
		{
			DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
			IsFalse(queue.AnyChanges);
			queue.Clear();
			IsFalse(queue.AnyChanges);
			int size = 200 + Random().Next(500) * RANDOM_MULTIPLIER;
			int termsSinceFreeze = 0;
			int queriesSinceFreeze = 0;
			for (int i = 0; i < size; i++)
			{
				Term term = new Term("id", string.Empty + i);
				if (Random().Next(10) == 0)
				{
					queue.AddDelete(new TermQuery(term));
					queriesSinceFreeze++;
				}
				else
				{
					queue.AddDelete(term);
					termsSinceFreeze++;
				}
				IsTrue(queue.AnyChanges);
				if (Random().Next(10) == 0)
				{
					queue.Clear();
					queue.TryApplyGlobalSlice();
					IsFalse(queue.AnyChanges);
				}
			}
		}

        [Test]
		public virtual void TestAnyChanges()
		{
			DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
			int size = 200 + Random().Next(500) * RANDOM_MULTIPLIER;
			int termsSinceFreeze = 0;
			int queriesSinceFreeze = 0;
			for (int i = 0; i < size; i++)
			{
				Term term = new Term("id", string.Empty + i);
				if (Random().Next(10) == 0)
				{
					queue.AddDelete(new TermQuery(term));
					queriesSinceFreeze++;
				}
				else
				{
					queue.AddDelete(term);
					termsSinceFreeze++;
				}
				IsTrue(queue.AnyChanges);
				if (Random().Next(5) == 0)
				{
					FrozenBufferedUpdates freezeGlobalBuffer = queue.FreezeGlobalBuffer(null);
					AreEqual(termsSinceFreeze, freezeGlobalBuffer.termCount);
					AreEqual(queriesSinceFreeze, freezeGlobalBuffer.queries.Length
						);
					queriesSinceFreeze = 0;
					termsSinceFreeze = 0;
					IsFalse(queue.AnyChanges);
				}
			}
		}

		/// <exception cref="System.Security.SecurityException"></exception>
		/// <exception cref="NoSuchFieldException"></exception>
		/// <exception cref="System.ArgumentException"></exception>
		/// <exception cref="System.MemberAccessException"></exception>
		[Test]
		public virtual void TestPartiallyAppliedGlobalSlice()
		{
			DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
		    var field = (typeof (DocumentsWriterDeleteQueue).GetField("globalBufferLock", BindingFlags.NonPublic));
			ReentrantLock Lock = (ReentrantLock)field.GetValue(queue);
			Lock.Lock();
            Thread t = new Thread(() => queue.AddDelete(new Term("foo", "bar")));
			t.Start();
			t.Join();
			Lock.Unlock();
			AssertTrue("changes in del queue but not in slice yet", queue.AnyChanges);
			queue.TryApplyGlobalSlice();
			AssertTrue("changes in global buffer", queue.AnyChanges);
			FrozenBufferedUpdates freezeGlobalBuffer = queue.FreezeGlobalBuffer(null);
			IsTrue(freezeGlobalBuffer.Any());
			AreEqual(1, freezeGlobalBuffer.termCount);
			AssertFalse("all changes applied", queue.AnyChanges);
		}

	    [Test]
		public virtual void TestStressDeleteQueue()
		{
			DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
			ICollection<Term> uniqueValues = new HashSet<Term>();
			int size = 10000 + Random().Next(500) * RANDOM_MULTIPLIER;
			int[] ids = new int[size];
			for (int i = 0; i < ids.Length; i++)
			{
				ids[i] = Random().Next();
				uniqueValues.Add(new Term("id", ids[i].ToString()));
			}
			CountdownEvent latch = new CountdownEvent(1);
			AtomicInteger index = new AtomicInteger(0);
			int numThreads = 2 + Random().Next(5);
			var threads = new Thread[numThreads];
	        var slices = new Dictionary<string,DocumentsWriterDeleteQueue.DeleteSlice>();
	        var updates = new Dictionary<string,BufferedUpdates>();
	        for (int i = 0; i < threads.Length; i++)
			{
				
				threads[i] = new Thread(() =>
				{
                    try
                    {
                        latch.Wait();
                    }
                    catch (Exception e)
                    {
                        throw new ThreadInterruptedException();
                    }
                    int x = 0;
				    var slice = queue.NewSlice();
				    var updates2 = new BufferedUpdates();
				    while ((x = index.IncrementAndGet()) < ids.Length)
                    {
                        Term term = new Term("id", ids[x].ToString());
                        queue.Add(term, slice);
                        IsTrue(slice.IsTailItem(term));
                        slice.Apply(updates2, BufferedUpdates.MAX_INT);
                        var threadId = Thread.CurrentThread.ManagedThreadId.ToString();
                        slices.Add(threadId, slice);
                        updates.Add(threadId, updates2);
                    }
				});
				threads[i].Start();
			}
			latch.Signal();
			for (int i_2 = 0; i_2 < threads.Length; i_2++)
			{
				threads[i_2].Join();
			}
            //TODO: this doesnt look clean. revisit
			foreach (var updateThread in threads)
			{
			    DocumentsWriterDeleteQueue.DeleteSlice slice = slices[updateThread.ManagedThreadId.ToString()];
				queue.UpdateSlice(slice);
			    BufferedUpdates deletes = updates[updateThread.ManagedThreadId.ToString()];
				slice.Apply(deletes, BufferedUpdates.MAX_INT);
				AreEqual(uniqueValues, deletes.terms.Keys);
			}
			queue.TryApplyGlobalSlice();
			ICollection<Term> frozenSet = new HashSet<Term>();
			foreach (Term t in queue.FreezeGlobalBuffer(null).TermsIterable())
			{
				BytesRef bytesRef = new BytesRef();
				bytesRef.CopyBytes(t.bytes);
				frozenSet.Add(new Term(t.field, bytesRef));
			}
			AssertEquals("num deletes must be 0 after freeze", 0, queue.NumGlobalTermDeletes);
			AreEqual(uniqueValues.Count, frozenSet.Count);
			AreEqual(uniqueValues, frozenSet);
		}
	}
}
