using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.TestFramework;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// tests basic
	/// <see cref="Lucene.Net.Util.IntBlockPool">Lucene.Net.Util.IntBlockPool
	/// 	</see>
	/// functionality
	/// </summary>
	[TestFixture]
    public class TestIntBlockPool : LuceneTestCase
	{
	    [Test]
        public virtual void TestSingleWriterReader()
		{
            Counter bytesUsed = Lucene.Net.Util.Counter.NewCounter();
			IntBlockPool pool = new IntBlockPool(new TestIntBlockPool.ByteTrackingAllocator(bytesUsed
				));
			for (int j = 0; j < 2; j++)
			{
				IntBlockPool.SliceWriter writer = new IntBlockPool.SliceWriter(pool);
				int start = writer.StartNewSlice();
				int num = AtLeast(100);
				for (int i = 0; i < num; i++)
				{
					writer.WriteInt(i);
				}
				int upto = writer.CurrentOffset;
				IntBlockPool.SliceReader reader = new IntBlockPool.SliceReader(pool);
				reader.Reset(start, upto);
				for (int i_1 = 0; i_1 < num; i_1++)
				{
					AreEqual(i_1, reader.ReadInt());
				}
				IsTrue(reader.EndOfSlice());
				if (Random().NextBoolean())
				{
					pool.Reset(true, false);
					AreEqual(0, bytesUsed.Get());
				}
				else
				{
					pool.Reset(true, true);
					AreEqual(IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT
						, bytesUsed.Get());
				}
			}
		}

        [Test]
		public virtual void TestMultipleWriterReader()
		{
            Counter bytesUsed = Lucene.Net.Util.Counter.NewCounter();
			IntBlockPool pool = new IntBlockPool(new TestIntBlockPool.ByteTrackingAllocator(bytesUsed
				));
			for (int j = 0; j < 2; j++)
			{
				IList<TestIntBlockPool.StartEndAndValues> holders = new List<TestIntBlockPool.StartEndAndValues
					>();
				int num = AtLeast(4);
				for (int i = 0; i < num; i++)
				{
					holders.Add(new TestIntBlockPool.StartEndAndValues(Random().Next(1000)));
				}
				IntBlockPool.SliceWriter writer = new IntBlockPool.SliceWriter(pool);
				IntBlockPool.SliceReader reader = new IntBlockPool.SliceReader(pool);
				int numValuesToWrite = AtLeast(10000);
				for (int i_1 = 0; i_1 < numValuesToWrite; i_1++)
				{
					TestIntBlockPool.StartEndAndValues values = holders[Random().Next(holders.Count)];
					if (values.valueCount == 0)
					{
						values.start = writer.StartNewSlice();
					}
					else
					{
						writer.Reset(values.end);
					}
					writer.WriteInt(values.NextValue());
					values.end = writer.CurrentOffset;
					if (Random().Next(5) == 0)
					{
						// pick one and reader the ints
						AssertReader(reader, holders[Random().Next(holders.Count)]);
					}
				}
				while (holders.Any())
				{
                    TestIntBlockPool.StartEndAndValues values = holders[Random().Next(holders.Count)];
                    holders.RemoveAt(Random().Next(holders.Count));
					AssertReader(reader, values);
				}
				if (Random().NextBoolean())
				{
					pool.Reset(true, false);
					AreEqual(0, bytesUsed.Get());
				}
				else
				{
					pool.Reset(true, true);
					AreEqual(IntBlockPool.INT_BLOCK_SIZE * RamUsageEstimator.NUM_BYTES_INT
						, bytesUsed.Get());
				}
			}
		}

		private class ByteTrackingAllocator : IntBlockPool.Allocator
		{
			private readonly Counter bytesUsed;

			public ByteTrackingAllocator(Counter bytesUsed) : this(IntBlockPool.INT_BLOCK_SIZE
				, bytesUsed)
			{
			}

			public ByteTrackingAllocator(int blockSize, Counter bytesUsed) : base(blockSize)
			{
				this.bytesUsed = bytesUsed;
			}

			public override int[] IntBlock
			{
			    get
			    {
			        bytesUsed.AddAndGet(blockSize*RamUsageEstimator.NUM_BYTES_INT);
			        return new int[blockSize];
			    }
			}

			public override void RecycleIntBlocks(int[][] blocks, int start, int end)
			{
				bytesUsed.AddAndGet(-((end - start) * blockSize * RamUsageEstimator.NUM_BYTES_INT
					));
			}
		}

		private void AssertReader(IntBlockPool.SliceReader reader, TestIntBlockPool.StartEndAndValues
			 values)
		{
			reader.Reset(values.start, values.end);
			for (int i = 0; i < values.valueCount; i++)
			{
				AreEqual(values.valueOffset + i, reader.ReadInt());
			}
			IsTrue(reader.EndOfSlice());
		}

		private class StartEndAndValues
		{
			internal int valueOffset;

			internal int valueCount;

			internal int start;

			internal int end;

			public StartEndAndValues(int valueOffset)
			{
				this.valueOffset = valueOffset;
			}

			public virtual int NextValue()
			{
				return valueOffset + valueCount++;
			}
		}
	}
}
