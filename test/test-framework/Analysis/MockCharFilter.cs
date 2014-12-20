using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>
	/// the purpose of this charfilter is to send offsets out of bounds
	/// if the analyzer doesn't use correctOffset or does incorrect offset math.
	/// </summary>
	/// <remarks>
	/// the purpose of this charfilter is to send offsets out of bounds
	/// if the analyzer doesn't use correctOffset or does incorrect offset math.
	/// </remarks>
	public class MockCharFilter : CharFilter
	{
		internal readonly int remainder;

		public MockCharFilter(StreamReader @in, int remainder) : base(@in)
		{
			// for testing only
			// TODO: instead of fixed remainder... maybe a fixed
			// random seed?
			this.remainder = remainder;
			if (remainder < 0 || remainder >= 10)
			{
				throw new ArgumentException("invalid remainder parameter (must be 0..10): " + remainder);
			}
		}

		public MockCharFilter(StreamReader @in) : this(@in, 0)
		{
		}

		internal int currentOffset = -1;

		internal int delta = 0;

		internal int bufferedCh = -1;

		// for testing only, uses a remainder of 0
		/// <exception cref="System.IO.IOException"></exception>
		public override int Read()
		{
			// we have a buffered character, add an offset correction and return it
			if (bufferedCh >= 0)
			{
				int ch = bufferedCh;
				bufferedCh = -1;
				currentOffset++;
				AddOffCorrectMap(currentOffset, delta - 1);
				delta--;
				return ch;
			}
			// otherwise actually read one    
			int ch_1 = input.Read();
			if (ch_1 < 0)
			{
				return ch_1;
			}
			currentOffset++;
			if ((ch_1 % 10) != remainder || char.IsHighSurrogate((char)ch_1) || char.IsLowSurrogate
				((char)ch_1))
			{
				return ch_1;
			}
			// we will double this character, so buffer it.
			bufferedCh = ch_1;
			return ch_1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(char[] cbuf, int off, int len)
		{
			int numRead = 0;
			for (int i = off; i < off + len; i++)
			{
				int c = Read();
				if (c == -1)
				{
					break;
				}
				cbuf[i] = (char)c;
				numRead++;
			}
			return numRead == 0 ? -1 : numRead;
		}

		protected override int Correct(int currentOff)
		{
			KeyValuePair<int, int> lastEntry = corrections.LowerEntry(currentOff + 1);
			int ret = lastEntry == null ? currentOff : currentOff + lastEntry.Value;
			
			//assert ret >= 0 : "currentOff=" + currentOff + ",diff=" + (ret-currentOff);
			return ret;
		}

		protected internal virtual void AddOffCorrectMap(int off, int cumulativeDiff)
		{
			corrections.Put(off, cumulativeDiff);
		}

		internal SortedDictionary<int, int> corrections = new SortedDictionary<int, int>();
	}
}
