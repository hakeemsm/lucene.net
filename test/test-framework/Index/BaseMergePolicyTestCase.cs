using System;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Util;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>
	/// Base test case for
	/// <see cref="MergePolicy">MergePolicy</see>
	/// .
	/// </summary>
	public abstract class BaseMergePolicyTestCase : LuceneTestCase
	{
		/// <summary>
		/// Create a new
		/// <see cref="MergePolicy">MergePolicy</see>
		/// instance.
		/// </summary>
		protected internal abstract Lucene.Net.Index.MergePolicy MergePolicy();

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestForceMergeNotNeeded()
		{
			Directory dir = NewDirectory();
			AtomicBoolean mayMerge = new AtomicBoolean(true);
			MergeScheduler mergeScheduler = new AnonymousSerialMergeScheduler(mayMerge);
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergeScheduler(mergeScheduler).SetMergePolicy(MergePolicy
				()));
			writer.Config.MergePolicy.SetNoCFSRatio(Random().NextBoolean() ? 0 : 1);
			int numSegments = Random().NextInt(2, 20);
			for (int i = 0; i < numSegments; ++i)
			{
				int numDocs = Random().NextInt(1, 5);
				for (int j = 0; j < numDocs; ++j)
				{
					writer.AddDocument(new Lucene.Net.Documents.Document());
				}
				writer.Reader.Dispose();
			}
			for (int i_1 = 5; i_1 >= 0; --i_1)
			{
				int segmentCount = writer.SegmentCount;
				int maxNumSegments = i_1 == 0 ? 1 : Random().NextInt(1, 10);
				mayMerge.Set(segmentCount > maxNumSegments);
				writer.ForceMerge(maxNumSegments);
			}
			writer.Dispose();
			dir.Dispose();
		}

		private sealed class AnonymousSerialMergeScheduler : SerialMergeScheduler
		{
			public AnonymousSerialMergeScheduler(AtomicBoolean mayMerge)
			{
				this.mayMerge = mayMerge;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Merge(IndexWriter writer, MergePolicy.MergeTrigger trigger, bool newMergesFound
				)
			{
				lock (this)
				{
					if (!mayMerge.Get() && writer.NextMerge != null)
					{
						throw new Exception();
					}
					base.Merge(writer, trigger, newMergesFound);
				}
			}

			private readonly AtomicBoolean mayMerge;
		}
	}
}
