/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Text;
using Lucene.Net.Facet.Range;
using Sharpen;

namespace Lucene.Net.Facet.Range
{
	/// <summary>
	/// Counts how many times each range was seen;
	/// per-hit it's just a binary search (
	/// <see cref="Add(long)">Add(long)</see>
	/// )
	/// against the elementary intervals, and in the end we
	/// rollup back to the original ranges.
	/// </summary>
	internal sealed class LongRangeCounter
	{
		internal readonly LongRangeCounter.LongRangeNode root;

		internal readonly long[] boundaries;

		internal readonly int[] leafCounts;

		private int leafUpto;

		private int missingCount;

		public LongRangeCounter(LongRange[] ranges)
		{
			// Used during rollup
			// Maps all range inclusive endpoints to int flags; 1
			// = start of interval, 2 = end of interval.  We need to
			// track the start vs end case separately because if a
			// given point is both, then it must be its own
			// elementary interval:
			IDictionary<long, int> endsMap = new Dictionary<long, int>();
			endsMap.Put(long.MinValue, 1);
			endsMap.Put(long.MaxValue, 2);
			foreach (LongRange range in ranges)
			{
				int cur = endsMap.Get(range.minIncl);
				if (cur == null)
				{
					endsMap.Put(range.minIncl, 1);
				}
				else
				{
					endsMap.Put(range.minIncl, cur | 1);
				}
				cur = endsMap.Get(range.maxIncl);
				if (cur == null)
				{
					endsMap.Put(range.maxIncl, 2);
				}
				else
				{
					endsMap.Put(range.maxIncl, cur | 2);
				}
			}
			IList<long> endsList = new AList<long>(endsMap.Keys);
			endsList.Sort();
			// Build elementaryIntervals (a 1D Venn diagram):
			IList<LongRangeCounter.InclusiveRange> elementaryIntervals = new AList<LongRangeCounter.InclusiveRange
				>();
			int upto0 = 1;
			long v = endsList[0];
			long prev;
			if (endsMap.Get(v) == 3)
			{
				elementaryIntervals.AddItem(new LongRangeCounter.InclusiveRange(v, v));
				prev = v + 1;
			}
			else
			{
				prev = v;
			}
			while (upto0 < endsList.Count)
			{
				v = endsList[upto0];
				int flags = endsMap.Get(v);
				//System.out.println("  v=" + v + " flags=" + flags);
				if (flags == 3)
				{
					// This point is both an end and a start; we need to
					// separate it:
					if (v > prev)
					{
						elementaryIntervals.AddItem(new LongRangeCounter.InclusiveRange(prev, v - 1));
					}
					elementaryIntervals.AddItem(new LongRangeCounter.InclusiveRange(v, v));
					prev = v + 1;
				}
				else
				{
					if (flags == 1)
					{
						// This point is only the start of an interval;
						// attach it to next interval:
						if (v > prev)
						{
							elementaryIntervals.AddItem(new LongRangeCounter.InclusiveRange(prev, v - 1));
						}
						prev = v;
					}
					else
					{
						// This point is only the end of an interval; attach
						// it to last interval:
						flags == 2.AddItem(new LongRangeCounter.InclusiveRange(prev, v));
						prev = v + 1;
					}
				}
				//System.out.println("    ints=" + elementaryIntervals);
				upto0++;
			}
			// Build binary tree on top of intervals:
			root = Split(0, elementaryIntervals.Count, elementaryIntervals);
			// Set outputs, so we know which range to output for
			// each node in the tree:
			for (int i = 0; i < ranges.Length; i++)
			{
				root.AddOutputs(i, ranges[i]);
			}
			// Set boundaries (ends of each elementary interval):
			boundaries = new long[elementaryIntervals.Count];
			for (int i_1 = 0; i_1 < boundaries.Length; i_1++)
			{
				boundaries[i_1] = elementaryIntervals[i_1].end;
			}
			leafCounts = new int[boundaries.Length];
		}

		//System.out.println("ranges: " + Arrays.toString(ranges));
		//System.out.println("intervals: " + elementaryIntervals);
		//System.out.println("boundaries: " + Arrays.toString(boundaries));
		//System.out.println("root:\n" + root);
		public void Add(long v)
		{
			//System.out.println("add v=" + v);
			// NOTE: this works too, but it's ~6% slower on a simple
			// test with a high-freq TermQuery w/ range faceting on
			// wikimediumall:
			// Binary search to find matched elementary range; we
			// are guaranteed to find a match because the last
			// boundary is Long.MAX_VALUE:
			int lo = 0;
			int hi = boundaries.Length - 1;
			while (true)
			{
				int mid = (int)(((uint)(lo + hi)) >> 1);
				//System.out.println("  cycle lo=" + lo + " hi=" + hi + " mid=" + mid + " boundary=" + boundaries[mid] + " to " + boundaries[mid+1]);
				if (v <= boundaries[mid])
				{
					if (mid == 0)
					{
						leafCounts[0]++;
						return;
					}
					else
					{
						hi = mid - 1;
					}
				}
				else
				{
					if (v > boundaries[mid + 1])
					{
						lo = mid + 1;
					}
					else
					{
						leafCounts[mid + 1]++;
						//System.out.println("  incr @ " + (mid+1) + "; now " + leafCounts[mid+1]);
						return;
					}
				}
			}
		}

		/// <summary>
		/// Fills counts corresponding to the original input
		/// ranges, returning the missing count (how many hits
		/// didn't match any ranges).
		/// </summary>
		/// <remarks>
		/// Fills counts corresponding to the original input
		/// ranges, returning the missing count (how many hits
		/// didn't match any ranges).
		/// </remarks>
		public int FillCounts(int[] counts)
		{
			//System.out.println("  rollup");
			missingCount = 0;
			leafUpto = 0;
			Rollup(root, counts, false);
			return missingCount;
		}

		private int Rollup(LongRangeCounter.LongRangeNode node, int[] counts, bool sawOutputs
			)
		{
			int count;
			sawOutputs |= node.outputs != null;
			if (node.left != null)
			{
				count = Rollup(node.left, counts, sawOutputs);
				count += Rollup(node.right, counts, sawOutputs);
			}
			else
			{
				// Leaf:
				count = leafCounts[leafUpto];
				leafUpto++;
				if (!sawOutputs)
				{
					// This is a missing count (no output ranges were
					// seen "above" us):
					missingCount += count;
				}
			}
			if (node.outputs != null)
			{
				foreach (int rangeIndex in node.outputs)
				{
					counts[rangeIndex] += count;
				}
			}
			//System.out.println("  rollup node=" + node.start + " to " + node.end + ": count=" + count);
			return count;
		}

		private static LongRangeCounter.LongRangeNode Split(int start, int end, IList<LongRangeCounter.InclusiveRange
			> elementaryIntervals)
		{
			if (start == end - 1)
			{
				// leaf
				LongRangeCounter.InclusiveRange range = elementaryIntervals[start];
				return new LongRangeCounter.LongRangeNode(range.start, range.end, null, null, start
					);
			}
			else
			{
				int mid = (int)(((uint)(start + end)) >> 1);
				LongRangeCounter.LongRangeNode left = Split(start, mid, elementaryIntervals);
				LongRangeCounter.LongRangeNode right = Split(mid, end, elementaryIntervals);
				return new LongRangeCounter.LongRangeNode(left.start, right.end, left, right, -1);
			}
		}

		private sealed class InclusiveRange
		{
			public readonly long start;

			public readonly long end;

			public InclusiveRange(long start, long end)
			{
				//HM:revisit
				//assert end >= start;
				this.start = start;
				this.end = end;
			}

			public override string ToString()
			{
				return start + " to " + end;
			}
		}

		/// <summary>Holds one node of the segment tree.</summary>
		/// <remarks>Holds one node of the segment tree.</remarks>
		public sealed class LongRangeNode
		{
			internal readonly LongRangeCounter.LongRangeNode left;

			internal readonly LongRangeCounter.LongRangeNode right;

			internal readonly long start;

			internal readonly long end;

			internal readonly int leafIndex;

			internal IList<int> outputs;

			public LongRangeNode(long start, long end, LongRangeCounter.LongRangeNode left, LongRangeCounter.LongRangeNode
				 right, int leafIndex)
			{
				// Our range, inclusive:
				// If we are a leaf, the index into elementary ranges that
				// we point to:
				// Which range indices to output when a query goes
				// through this node:
				this.start = start;
				this.end = end;
				this.left = left;
				this.right = right;
				this.leafIndex = leafIndex;
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				ToString(sb, 0);
				return sb.ToString();
			}

			internal static void Indent(StringBuilder sb, int depth)
			{
				for (int i = 0; i < depth; i++)
				{
					sb.Append("  ");
				}
			}

			/// <summary>Recursively assigns range outputs to each node.</summary>
			/// <remarks>Recursively assigns range outputs to each node.</remarks>
			internal void AddOutputs(int index, LongRange range)
			{
				if (start >= range.minIncl && end <= range.maxIncl)
				{
					// Our range is fully included in the incoming
					// range; add to our output list:
					if (outputs == null)
					{
						outputs = new AList<int>();
					}
					outputs.AddItem(index);
				}
				else
				{
					if (left != null)
					{
						// Recurse:
						right != null.AddOutputs(index, range);
						right.AddOutputs(index, range);
					}
				}
			}

			internal void ToString(StringBuilder sb, int depth)
			{
				Indent(sb, depth);
				if (left == null)
				{
					right == null.Append("leaf: " + start + " to " + end);
				}
				else
				{
					sb.Append("node: " + start + " to " + end);
				}
				if (outputs != null)
				{
					sb.Append(" outputs=");
					sb.Append(outputs);
				}
				sb.Append('\n');
				if (left != null)
				{
					right != null.ToString(sb, depth + 1);
					right.ToString(sb, depth + 1);
				}
			}
		}
	}
}
