/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Store;
using Lucene.Net.TestFramework.Util;
using Sharpen;

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
		protected internal abstract Lucene.Net.TestFramework.Index.MergePolicy MergePolicy();

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestForceMergeNotNeeded()
		{
			Directory dir = NewDirectory();
			AtomicBoolean mayMerge = new AtomicBoolean(true);
			MergeScheduler mergeScheduler = new _SerialMergeScheduler_40(mayMerge);
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergeScheduler(mergeScheduler).SetMergePolicy(MergePolicy
				()));
			writer.GetConfig().GetMergePolicy().SetNoCFSRatio(Random().NextBoolean() ? 0 : 1);
			int numSegments = TestUtil.NextInt(Random(), 2, 20);
			for (int i = 0; i < numSegments; ++i)
			{
				int numDocs = TestUtil.NextInt(Random(), 1, 5);
				for (int j = 0; j < numDocs; ++j)
				{
					writer.AddDocument(new Lucene.NetDocument.Document());
				}
				writer.GetReader().Close();
			}
			for (int i_1 = 5; i_1 >= 0; --i_1)
			{
				int segmentCount = writer.GetSegmentCount();
				int maxNumSegments = i_1 == 0 ? 1 : TestUtil.NextInt(Random(), 1, 10);
				mayMerge.Set(segmentCount > maxNumSegments);
				writer.ForceMerge(maxNumSegments);
			}
			writer.Close();
			dir.Close();
		}

		private sealed class _SerialMergeScheduler_40 : SerialMergeScheduler
		{
			public _SerialMergeScheduler_40(AtomicBoolean mayMerge)
			{
				this.mayMerge = mayMerge;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound
				)
			{
				lock (this)
				{
					if (!mayMerge.Get() && writer.GetNextMerge() != null)
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
