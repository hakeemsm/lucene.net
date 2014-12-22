using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Util;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>MergePolicy that makes random decisions for testing.</summary>
	/// <remarks>MergePolicy that makes random decisions for testing.</remarks>
	public class MockRandomMergePolicy : MergePolicy
	{
		private readonly Random random;

		internal bool doNonBulkMerges = true;

		public MockRandomMergePolicy(Random random)
		{
			// fork a private random, since we are called
			// unpredictably from threads:
			this.random = new Random(random.NextLong());
		}

		/// <summary>
		/// Set to true if sometimes readers to be merged should be wrapped in a FilterReader
		/// to mixup bulk merging.
		/// </summary>
		/// <remarks>
		/// Set to true if sometimes readers to be merged should be wrapped in a FilterReader
		/// to mixup bulk merging.
		/// </remarks>
		public virtual void SetDoNonBulkMerges(bool v)
		{
			doNonBulkMerges = v;
		}

		public override MergeSpecification FindMerges(MergeTrigger? mergeTrigger, SegmentInfos segmentInfos)
		{
			MergePolicy.MergeSpecification mergeSpec = null;
			//System.out.println("MRMP: findMerges sis=" + segmentInfos);
			int numSegments = segmentInfos.Count;
		    ICollection<SegmentCommitInfo> merging = writer.Get().MergingSegments;
		    IList<SegmentCommitInfo> segments = segmentInfos.Where(sipc => !merging.Contains(sipc)).ToList();
		    numSegments = segments.Count;
			if (numSegments > 1 && (numSegments > 30 || random.Next(5) == 3))
			{
                
				Sharpen.Collections.Shuffle(segments, random);
				// TODO: sometimes make more than 1 merge?
				mergeSpec = new MergePolicy.MergeSpecification();
				int segsToMerge = TestUtil.NextInt(random, 1, numSegments);
				if (doNonBulkMerges)
				{
					mergeSpec.Add(new MockRandomMergePolicy.MockRandomOneMerge(segments.SubList(0, segsToMerge
						), random.NextLong()));
				}
				else
				{
					mergeSpec.Add(new MergePolicy.OneMerge(segments.SubList(0, segsToMerge)));
				}
			}
			return mergeSpec;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override MergePolicy.MergeSpecification FindForcedMerges(SegmentInfos segmentInfos
			, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool> segmentsToMerge)
		{
			IList<SegmentCommitInfo> eligibleSegments = new AList<SegmentCommitInfo>();
			foreach (SegmentCommitInfo info in segmentInfos)
			{
				if (segmentsToMerge.ContainsKey(info))
				{
					eligibleSegments.AddItem(info);
				}
			}
			//System.out.println("MRMP: findMerges sis=" + segmentInfos + " eligible=" + eligibleSegments);
			MergePolicy.MergeSpecification mergeSpec = null;
			if (eligibleSegments.Count > 1 || (eligibleSegments.Count == 1 && eligibleSegments
				[0].HasDeletions()))
			{
				mergeSpec = new MergePolicy.MergeSpecification();
				// Already shuffled having come out of a set but
				// shuffle again for good measure:
				Sharpen.Collections.Shuffle(eligibleSegments, random);
				int upto = 0;
				while (upto < eligibleSegments.Count)
				{
					int max = Math.Min(10, eligibleSegments.Count - upto);
					int inc = max <= 2 ? max : TestUtil.NextInt(random, 2, max);
					if (doNonBulkMerges)
					{
						mergeSpec.Add(new MockRandomMergePolicy.MockRandomOneMerge(eligibleSegments.SubList
							(upto, upto + inc), random.NextLong()));
					}
					else
					{
						mergeSpec.Add(new MergePolicy.OneMerge(eligibleSegments.SubList(upto, upto + inc)
							));
					}
					upto += inc;
				}
			}
			if (mergeSpec != null)
			{
				foreach (MergePolicy.OneMerge merge in mergeSpec.merges)
				{
					foreach (SegmentCommitInfo info_1 in merge.segments)
					{
					}
				}
			}
			 
			//assert segmentsToMerge.containsKey(info);
			return mergeSpec;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override MergePolicy.MergeSpecification FindForcedDeletesMerges(SegmentInfos
			 segmentInfos)
		{
			return FindMerges(null, segmentInfos);
		}

		public override void Close()
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool UseCompoundFile(SegmentInfos infos, SegmentCommitInfo mergedInfo
			)
		{
			// 80% of the time we create CFS:
			return random.Next(5) != 1;
		}

		internal class MockRandomOneMerge : MergePolicy.OneMerge
		{
			internal readonly Random r;

			internal AList<AtomicReader> readers;

			internal MockRandomOneMerge(IList<SegmentCommitInfo> segments, long seed) : base(
				segments)
			{
				r = new Random(seed);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IList<AtomicReader> GetMergeReaders()
			{
				if (readers == null)
				{
					readers = new AList<AtomicReader>(base.GetMergeReaders());
					for (int i = 0; i < readers.Count; i++)
					{
						// wrap it (e.g. prevent bulk merge etc)
						if (r.Next(4) == 0)
						{
							readers.Set(i, new FilterAtomicReader(readers[i]));
						}
					}
				}
				return readers;
			}
		}
	}
}
