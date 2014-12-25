/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs.Intblock;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Codecs.Intblock
{
	/// <summary>
	/// Abstract base class that writes fixed-size blocks of ints
	/// to an IndexOutput.
	/// </summary>
	/// <remarks>
	/// Abstract base class that writes fixed-size blocks of ints
	/// to an IndexOutput.  While this is a simple approach, a
	/// more performant approach would directly create an impl
	/// of IntIndexOutput inside Directory.  Wrapping a generic
	/// IndexInput will likely cost performance.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class FixedIntBlockIndexOutput : IntIndexOutput
	{
		protected internal readonly IndexOutput @out;

		private readonly int blockSize;

		protected internal readonly int[] buffer;

		private int upto;

		/// <exception cref="System.IO.IOException"></exception>
		protected internal FixedIntBlockIndexOutput(IndexOutput @out, int fixedBlockSize)
		{
			blockSize = fixedBlockSize;
			this.@out = @out;
			@out.WriteVInt(blockSize);
			buffer = new int[blockSize];
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal abstract void FlushBlock();

		public override IntIndexOutput.Index Index()
		{
			return new FixedIntBlockIndexOutput.Index(this);
		}

		private class Index : IntIndexOutput.Index
		{
			internal long fp;

			internal int upto;

			internal long lastFP;

			internal int lastUpto;

			/// <exception cref="System.IO.IOException"></exception>
			public override void Mark()
			{
				this.fp = this._enclosing.@out.FilePointer;
				this.upto = this._enclosing.upto;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void CopyFrom(IntIndexOutput.Index other, bool copyLast)
			{
				FixedIntBlockIndexOutput.Index idx = (FixedIntBlockIndexOutput.Index)other;
				this.fp = idx.fp;
				this.upto = idx.upto;
				if (copyLast)
				{
					this.lastFP = this.fp;
					this.lastUpto = this.upto;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(DataOutput indexOut, bool absolute)
			{
				if (absolute)
				{
					indexOut.WriteVInt(this.upto);
					indexOut.WriteVLong(this.fp);
				}
				else
				{
					if (this.fp == this.lastFP)
					{
						// same block
						//HM:revisit 
						//assert upto >= lastUpto;
						int uptoDelta = this.upto - this.lastUpto;
						indexOut.WriteVInt(uptoDelta << 1 | 1);
					}
					else
					{
						// new block
						indexOut.WriteVInt(this.upto << 1);
						indexOut.WriteVLong(this.fp - this.lastFP);
					}
				}
				this.lastUpto = this.upto;
				this.lastFP = this.fp;
			}

			public override string ToString()
			{
				return "fp=" + this.fp + " upto=" + this.upto;
			}

			internal Index(FixedIntBlockIndexOutput _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly FixedIntBlockIndexOutput _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(int v)
		{
			buffer[upto++] = v;
			if (upto == blockSize)
			{
				FlushBlock();
				upto = 0;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				if (upto > 0)
				{
					// NOTE: entries in the block after current upto are
					// invalid
					FlushBlock();
				}
			}
			finally
			{
				@out.Close();
			}
		}
	}
}
