/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs.Memory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util.Fst;
using Sharpen;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>
	/// An FST
	/// <see cref="Lucene.Net.Util.Fst.Outputs{T}">Lucene.Net.Util.Fst.Outputs&lt;T&gt;
	/// 	</see>
	/// implementation for
	/// <see cref="FSTTermsWriter">FSTTermsWriter</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	internal class FSTTermOutputs : Outputs<FSTTermOutputs.TermData>
	{
		private static readonly FSTTermOutputs.TermData NO_OUTPUT = new FSTTermOutputs.TermData
			();

		private readonly bool hasPos;

		private readonly int longsSize;

		/// <summary>Represents the metadata for one term.</summary>
		/// <remarks>
		/// Represents the metadata for one term.
		/// On an FST, only long[] part is 'shared' and pushed towards root.
		/// byte[] and term stats will be kept on deeper arcs.
		/// </remarks>
		internal class TermData
		{
			internal long[] longs;

			internal byte[] bytes;

			internal int docFreq;

			internal long totalTermFreq;

			public TermData()
			{
				// NOTE: outputs should be per-field, since
				// longsSize is fixed for each field
				//private static boolean TEST = false;
				this.longs = null;
				this.bytes = null;
				this.docFreq = 0;
				this.totalTermFreq = -1;
			}

			internal TermData(long[] longs, byte[] bytes, int docFreq, long totalTermFreq)
			{
				this.longs = longs;
				this.bytes = bytes;
				this.docFreq = docFreq;
				this.totalTermFreq = totalTermFreq;
			}

			// NOTE: actually, FST nodes are seldom 
			// identical when outputs on their arcs 
			// aren't NO_OUTPUTs.
			public override int GetHashCode()
			{
				int hash = 0;
				if (longs != null)
				{
					int end = longs.Length;
					for (int i = 0; i < end; i++)
					{
						hash -= longs[i];
					}
				}
				if (bytes != null)
				{
					hash = -hash;
					int end = bytes.Length;
					for (int i = 0; i < end; i++)
					{
						hash += bytes[i];
					}
				}
				hash += docFreq + totalTermFreq;
				return hash;
			}

			public override bool Equals(object other_)
			{
				if (other_ == this)
				{
					return true;
				}
				else
				{
					if (!(other_ is FSTTermOutputs.TermData))
					{
						return false;
					}
				}
				FSTTermOutputs.TermData other = (FSTTermOutputs.TermData)other_;
				return StatsEqual(this, other) && LongsEqual(this, other) && BytesEqual(this, other
					);
			}
		}

		protected internal FSTTermOutputs(FieldInfo fieldInfo, int longsSize)
		{
			this.hasPos = (fieldInfo.GetIndexOptions() != FieldInfo.IndexOptions.DOCS_ONLY);
			this.longsSize = longsSize;
		}

		public override FSTTermOutputs.TermData Common(FSTTermOutputs.TermData t1, FSTTermOutputs.TermData
			 t2)
		{
			//
			// The return value will be the smaller one, when these two are 
			// 'comparable', i.e. 
			// 1. every value in t1 is not larger than in t2, or
			// 2. every value in t1 is not smaller than t2.
			//
			//if (TEST) System.out.print("common("+t1+", "+t2+") = ");
			if (t1 == NO_OUTPUT || t2 == NO_OUTPUT)
			{
				//if (TEST) System.out.println("ret:"+NO_OUTPUT);
				return NO_OUTPUT;
			}
			//HM:revisit 
			//assert t1.longs.length == t2.longs.length;
			long[] min = t1.longs;
			long[] max = t2.longs;
			int pos = 0;
			FSTTermOutputs.TermData ret;
			while (pos < longsSize && min[pos] == max[pos])
			{
				pos++;
			}
			if (pos < longsSize)
			{
				// unequal long[]
				if (min[pos] > max[pos])
				{
					min = t2.longs;
					max = t1.longs;
				}
				// check whether strictly smaller
				while (pos < longsSize && min[pos] <= max[pos])
				{
					pos++;
				}
				if (pos < longsSize || AllZero(min))
				{
					// not comparable or all-zero
					ret = NO_OUTPUT;
				}
				else
				{
					ret = new FSTTermOutputs.TermData(min, null, 0, -1);
				}
			}
			else
			{
				// equal long[]
				if (StatsEqual(t1, t2) && BytesEqual(t1, t2))
				{
					ret = t1;
				}
				else
				{
					if (AllZero(min))
					{
						ret = NO_OUTPUT;
					}
					else
					{
						ret = new FSTTermOutputs.TermData(min, null, 0, -1);
					}
				}
			}
			//if (TEST) System.out.println("ret:"+ret);
			return ret;
		}

		public override FSTTermOutputs.TermData Subtract(FSTTermOutputs.TermData t1, FSTTermOutputs.TermData
			 t2)
		{
			//if (TEST) System.out.print("subtract("+t1+", "+t2+") = ");
			if (t2 == NO_OUTPUT)
			{
				//if (TEST) System.out.println("ret:"+t1);
				return t1;
			}
			//HM:revisit 
			//assert t1.longs.length == t2.longs.length;
			int pos = 0;
			long diff = 0;
			long[] share = new long[longsSize];
			while (pos < longsSize)
			{
				share[pos] = t1.longs[pos] - t2.longs[pos];
				diff += share[pos];
				pos++;
			}
			FSTTermOutputs.TermData ret;
			if (diff == 0 && StatsEqual(t1, t2) && BytesEqual(t1, t2))
			{
				ret = NO_OUTPUT;
			}
			else
			{
				ret = new FSTTermOutputs.TermData(share, t1.bytes, t1.docFreq, t1.totalTermFreq);
			}
			//if (TEST) System.out.println("ret:"+ret);
			return ret;
		}

		// TODO: if we refactor a 'addSelf(TermData other)',
		// we can gain about 5~7% for fuzzy queries, however this also 
		// means we are putting too much stress on FST Outputs decoding?
		public override FSTTermOutputs.TermData Add(FSTTermOutputs.TermData t1, FSTTermOutputs.TermData
			 t2)
		{
			//if (TEST) System.out.print("add("+t1+", "+t2+") = ");
			if (t1 == NO_OUTPUT)
			{
				//if (TEST) System.out.println("ret:"+t2);
				return t2;
			}
			else
			{
				if (t2 == NO_OUTPUT)
				{
					//if (TEST) System.out.println("ret:"+t1);
					return t1;
				}
			}
			//HM:revisit 
			//assert t1.longs.length == t2.longs.length;
			int pos = 0;
			long[] accum = new long[longsSize];
			while (pos < longsSize)
			{
				accum[pos] = t1.longs[pos] + t2.longs[pos];
				pos++;
			}
			FSTTermOutputs.TermData ret;
			if (t2.bytes != null || t2.docFreq > 0)
			{
				ret = new FSTTermOutputs.TermData(accum, t2.bytes, t2.docFreq, t2.totalTermFreq);
			}
			else
			{
				ret = new FSTTermOutputs.TermData(accum, t1.bytes, t1.docFreq, t1.totalTermFreq);
			}
			//if (TEST) System.out.println("ret:"+ret);
			return ret;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(FSTTermOutputs.TermData data, DataOutput @out)
		{
			int bit0 = AllZero(data.longs) ? 0 : 1;
			int bit1 = ((data.bytes == null || data.bytes.Length == 0) ? 0 : 1) << 1;
			int bit2 = ((data.docFreq == 0) ? 0 : 1) << 2;
			int bits = bit0 | bit1 | bit2;
			if (bit1 > 0)
			{
				// determine extra length
				if (data.bytes.Length < 32)
				{
					bits |= (data.bytes.Length << 3);
					@out.WriteByte(unchecked((byte)bits));
				}
				else
				{
					@out.WriteByte(unchecked((byte)bits));
					@out.WriteVInt(data.bytes.Length);
				}
			}
			else
			{
				@out.WriteByte(unchecked((byte)bits));
			}
			if (bit0 > 0)
			{
				// not all-zero case
				for (int pos = 0; pos < longsSize; pos++)
				{
					@out.WriteVLong(data.longs[pos]);
				}
			}
			if (bit1 > 0)
			{
				// bytes exists
				@out.WriteBytes(data.bytes, 0, data.bytes.Length);
			}
			if (bit2 > 0)
			{
				// stats exist
				if (hasPos)
				{
					if (data.docFreq == data.totalTermFreq)
					{
						@out.WriteVInt((data.docFreq << 1) | 1);
					}
					else
					{
						@out.WriteVInt((data.docFreq << 1));
						@out.WriteVLong(data.totalTermFreq - data.docFreq);
					}
				}
				else
				{
					@out.WriteVInt(data.docFreq);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FSTTermOutputs.TermData Read(DataInput @in)
		{
			long[] longs = new long[longsSize];
			byte[] bytes = null;
			int docFreq = 0;
			long totalTermFreq = -1;
			int bits = @in.ReadByte() & unchecked((int)(0xff));
			int bit0 = bits & 1;
			int bit1 = bits & 2;
			int bit2 = bits & 4;
			int bytesSize = ((int)(((uint)bits) >> 3));
			if (bit1 > 0 && bytesSize == 0)
			{
				// determine extra length
				bytesSize = @in.ReadVInt();
			}
			if (bit0 > 0)
			{
				// not all-zero case
				for (int pos = 0; pos < longsSize; pos++)
				{
					longs[pos] = @in.ReadVLong();
				}
			}
			if (bit1 > 0)
			{
				// bytes exists
				bytes = new byte[bytesSize];
				@in.ReadBytes(bytes, 0, bytesSize);
			}
			if (bit2 > 0)
			{
				// stats exist
				int code = @in.ReadVInt();
				if (hasPos)
				{
					totalTermFreq = docFreq = (int)(((uint)code) >> 1);
					if ((code & 1) == 0)
					{
						totalTermFreq += @in.ReadVLong();
					}
				}
				else
				{
					docFreq = code;
				}
			}
			return new FSTTermOutputs.TermData(longs, bytes, docFreq, totalTermFreq);
		}

		public override FSTTermOutputs.TermData GetNoOutput()
		{
			return NO_OUTPUT;
		}

		public override string OutputToString(FSTTermOutputs.TermData data)
		{
			return data.ToString();
		}

		internal static bool StatsEqual(FSTTermOutputs.TermData t1, FSTTermOutputs.TermData
			 t2)
		{
			return t1.docFreq == t2.docFreq && t1.totalTermFreq == t2.totalTermFreq;
		}

		internal static bool BytesEqual(FSTTermOutputs.TermData t1, FSTTermOutputs.TermData
			 t2)
		{
			if (t1.bytes == null && t2.bytes == null)
			{
				return true;
			}
			return t1.bytes != null && t2.bytes != null && Arrays.Equals(t1.bytes, t2.bytes);
		}

		internal static bool LongsEqual(FSTTermOutputs.TermData t1, FSTTermOutputs.TermData
			 t2)
		{
			if (t1.longs == null && t2.longs == null)
			{
				return true;
			}
			return t1.longs != null && t2.longs != null && Arrays.Equals(t1.longs, t2.longs);
		}

		internal static bool AllZero(long[] l)
		{
			for (int i = 0; i < l.Length; i++)
			{
				if (l[i] != 0)
				{
					return false;
				}
			}
			return true;
		}
	}
}
