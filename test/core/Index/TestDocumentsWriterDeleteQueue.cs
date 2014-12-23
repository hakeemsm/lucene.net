/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>
	/// Unit test for
	/// <see cref="DocumentsWriterDeleteQueue">DocumentsWriterDeleteQueue</see>
	/// </summary>
	public class TestDocumentsWriterDeleteQueue : LuceneTestCase
	{
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
				uniqueValues.AddItem(term[0]);
				queue.AddDelete(term);
				if (Random().Next(20) == 0 || j == ids.Length - 1)
				{
					queue.UpdateSlice(slice1);
					NUnit.Framework.Assert.IsTrue(slice1.IsTailItem(term));
					slice1.Apply(bd1, j);
					AssertAllBetween(last1, j, bd1, ids);
					last1 = j + 1;
				}
				if (Random().Next(10) == 5 || j == ids.Length - 1)
				{
					queue.UpdateSlice(slice2);
					NUnit.Framework.Assert.IsTrue(slice2.IsTailItem(term));
					slice2.Apply(bd2, j);
					AssertAllBetween(last2, j, bd2, ids);
					last2 = j + 1;
				}
				NUnit.Framework.Assert.AreEqual(j + 1, queue.NumGlobalTermDeletes());
			}
			NUnit.Framework.Assert.AreEqual(uniqueValues, bd1.terms.Keys);
			NUnit.Framework.Assert.AreEqual(uniqueValues, bd2.terms.Keys);
			HashSet<Term> frozenSet = new HashSet<Term>();
			foreach (Term t in queue.FreezeGlobalBuffer(null).TermsIterable())
			{
				BytesRef bytesRef = new BytesRef();
				bytesRef.CopyBytes(t.bytes);
				frozenSet.AddItem(new Term(t.field, bytesRef));
			}
			NUnit.Framework.Assert.AreEqual(uniqueValues, frozenSet);
			NUnit.Framework.Assert.AreEqual("num deletes must be 0 after freeze", 0, queue.NumGlobalTermDeletes
				());
		}

		private void AssertAllBetween(int start, int end, BufferedUpdates deletes, int[] 
			ids)
		{
			for (int i = start; i <= end; i++)
			{
				NUnit.Framework.Assert.AreEqual(Sharpen.Extensions.ValueOf(end), deletes.terms.Get
					(new Term("id", ids[i].ToString())));
			}
		}

		public virtual void TestClear()
		{
			DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
			NUnit.Framework.Assert.IsFalse(queue.AnyChanges());
			queue.Clear();
			NUnit.Framework.Assert.IsFalse(queue.AnyChanges());
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
				NUnit.Framework.Assert.IsTrue(queue.AnyChanges());
				if (Random().Next(10) == 0)
				{
					queue.Clear();
					queue.TryApplyGlobalSlice();
					NUnit.Framework.Assert.IsFalse(queue.AnyChanges());
				}
			}
		}

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
				NUnit.Framework.Assert.IsTrue(queue.AnyChanges());
				if (Random().Next(5) == 0)
				{
					FrozenBufferedUpdates freezeGlobalBuffer = queue.FreezeGlobalBuffer(null);
					NUnit.Framework.Assert.AreEqual(termsSinceFreeze, freezeGlobalBuffer.termCount);
					NUnit.Framework.Assert.AreEqual(queriesSinceFreeze, freezeGlobalBuffer.queries.Length
						);
					queriesSinceFreeze = 0;
					termsSinceFreeze = 0;
					NUnit.Framework.Assert.IsFalse(queue.AnyChanges());
				}
			}
		}

		/// <exception cref="System.Security.SecurityException"></exception>
		/// <exception cref="Sharpen.NoSuchFieldException"></exception>
		/// <exception cref="System.ArgumentException"></exception>
		/// <exception cref="System.MemberAccessException"></exception>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPartiallyAppliedGlobalSlice()
		{
			DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
			FieldInfo field = Sharpen.Runtime.GetDeclaredField(typeof(DocumentsWriterDeleteQueue
				), "globalBufferLock");
			ReentrantLock Lock = (ReentrantLock)field.GetValue(queue);
			Lock.Lock();
			Sharpen.Thread t = new _Thread_156(queue);
			t.Start();
			t.Join();
			Lock.Unlock();
			NUnit.Framework.Assert.IsTrue("changes in del queue but not in slice yet", queue.
				AnyChanges());
			queue.TryApplyGlobalSlice();
			NUnit.Framework.Assert.IsTrue("changes in global buffer", queue.AnyChanges());
			FrozenBufferedUpdates freezeGlobalBuffer = queue.FreezeGlobalBuffer(null);
			NUnit.Framework.Assert.IsTrue(freezeGlobalBuffer.Any());
			NUnit.Framework.Assert.AreEqual(1, freezeGlobalBuffer.termCount);
			NUnit.Framework.Assert.IsFalse("all changes applied", queue.AnyChanges());
		}

		private sealed class _Thread_156 : Sharpen.Thread
		{
			public _Thread_156(DocumentsWriterDeleteQueue queue)
			{
				this.queue = queue;
			}

			public override void Run()
			{
				queue.AddDelete(new Term("foo", "bar"));
			}

			private readonly DocumentsWriterDeleteQueue queue;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestStressDeleteQueue()
		{
			DocumentsWriterDeleteQueue queue = new DocumentsWriterDeleteQueue();
			ICollection<Term> uniqueValues = new HashSet<Term>();
			int size = 10000 + Random().Next(500) * RANDOM_MULTIPLIER;
			int[] ids = new int[size];
			for (int i = 0; i < ids.Length; i++)
			{
				ids[i] = Random().Next();
				uniqueValues.AddItem(new Term("id", ids[i].ToString()));
			}
			CountDownLatch latch = new CountDownLatch(1);
			AtomicInteger index = new AtomicInteger(0);
			int numThreads = 2 + Random().Next(5);
			TestDocumentsWriterDeleteQueue.UpdateThread[] threads = new TestDocumentsWriterDeleteQueue.UpdateThread
				[numThreads];
			for (int i_1 = 0; i_1 < threads.Length; i_1++)
			{
				threads[i_1] = new TestDocumentsWriterDeleteQueue.UpdateThread(queue, index, ids, 
					latch);
				threads[i_1].Start();
			}
			latch.CountDown();
			for (int i_2 = 0; i_2 < threads.Length; i_2++)
			{
				threads[i_2].Join();
			}
			foreach (TestDocumentsWriterDeleteQueue.UpdateThread updateThread in threads)
			{
				DocumentsWriterDeleteQueue.DeleteSlice slice = updateThread.slice;
				queue.UpdateSlice(slice);
				BufferedUpdates deletes = updateThread.deletes;
				slice.Apply(deletes, BufferedUpdates.MAX_INT);
				NUnit.Framework.Assert.AreEqual(uniqueValues, deletes.terms.Keys);
			}
			queue.TryApplyGlobalSlice();
			ICollection<Term> frozenSet = new HashSet<Term>();
			foreach (Term t in queue.FreezeGlobalBuffer(null).TermsIterable())
			{
				BytesRef bytesRef = new BytesRef();
				bytesRef.CopyBytes(t.bytes);
				frozenSet.AddItem(new Term(t.field, bytesRef));
			}
			NUnit.Framework.Assert.AreEqual("num deletes must be 0 after freeze", 0, queue.NumGlobalTermDeletes
				());
			NUnit.Framework.Assert.AreEqual(uniqueValues.Count, frozenSet.Count);
			NUnit.Framework.Assert.AreEqual(uniqueValues, frozenSet);
		}

		private class UpdateThread : Sharpen.Thread
		{
			internal readonly DocumentsWriterDeleteQueue queue;

			internal readonly AtomicInteger index;

			internal readonly int[] ids;

			internal readonly DocumentsWriterDeleteQueue.DeleteSlice slice;

			internal readonly BufferedUpdates deletes;

			internal readonly CountDownLatch latch;

			protected internal UpdateThread(DocumentsWriterDeleteQueue queue, AtomicInteger index
				, int[] ids, CountDownLatch latch)
			{
				this.queue = queue;
				this.index = index;
				this.ids = ids;
				this.slice = queue.NewSlice();
				deletes = new BufferedUpdates();
				this.latch = latch;
			}

			public override void Run()
			{
				try
				{
					latch.Await();
				}
				catch (Exception e)
				{
					throw new ThreadInterruptedException(e);
				}
				int i = 0;
				while ((i = index.GetAndIncrement()) < ids.Length)
				{
					Term term = new Term("id", ids[i].ToString());
					queue.Add(term, slice);
					NUnit.Framework.Assert.IsTrue(slice.IsTailItem(term));
					slice.Apply(deletes, BufferedUpdates.MAX_INT);
				}
			}
		}
	}
}
