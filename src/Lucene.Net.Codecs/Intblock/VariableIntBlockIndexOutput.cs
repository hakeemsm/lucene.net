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
	/// Abstract base class that writes variable-size blocks of ints
	/// to an IndexOutput.
	/// </summary>
	/// <remarks>
	/// Abstract base class that writes variable-size blocks of ints
	/// to an IndexOutput.  While this is a simple approach, a
	/// more performant approach would directly create an impl
	/// of IntIndexOutput inside Directory.  Wrapping a generic
	/// IndexInput will likely cost performance.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class VariableIntBlockIndexOutput : IntIndexOutput
	{
		protected internal readonly IndexOutput @out;

		private int upto;

		private bool hitExcDuringWrite;

		/// <summary>
		/// NOTE: maxBlockSize must be the maximum block size
		/// plus the max non-causal lookahead of your codec.
		/// </summary>
		/// <remarks>
		/// NOTE: maxBlockSize must be the maximum block size
		/// plus the max non-causal lookahead of your codec.  EG Simple9
		/// requires lookahead=1 because on seeing the Nth value
		/// it knows it must now encode the N-1 values before it.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal VariableIntBlockIndexOutput(IndexOutput @out, int maxBlockSize
			)
		{
			// TODO: much of this can be shared code w/ the fixed case
			// TODO what Var-Var codecs exist in practice... and what are there blocksizes like?
			// if its less than 128 we should set that as max and use byte?
			this.@out = @out;
			@out.WriteInt(maxBlockSize);
		}

		/// <summary>Called one value at a time.</summary>
		/// <remarks>
		/// Called one value at a time.  Return the number of
		/// buffered input values that have been written to out.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal abstract int Add(int value);

		public override IntIndexOutput.Index Index()
		{
			return new VariableIntBlockIndexOutput.Index(this);
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
				this.fp = this._enclosing.@out.GetFilePointer();
				this.upto = this._enclosing.upto;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void CopyFrom(IntIndexOutput.Index other, bool copyLast)
			{
				VariableIntBlockIndexOutput.Index idx = (VariableIntBlockIndexOutput.Index)other;
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
				//HM:revisit 
				//assert upto >= 0;
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

			internal Index(VariableIntBlockIndexOutput _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly VariableIntBlockIndexOutput _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(int v)
		{
			hitExcDuringWrite = true;
			upto -= Add(v) - 1;
			hitExcDuringWrite = false;
		}

		//HM:revisit 
		//assert upto >= 0;
		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				if (!hitExcDuringWrite)
				{
					// stuff 0s in until the "real" data is flushed:
					int stuffed = 0;
					while (upto > stuffed)
					{
						upto -= Add(0) - 1;
						//HM:revisit 
						//assert upto >= 0;
						stuffed += 1;
					}
				}
			}
			finally
			{
				@out.Close();
			}
		}
	}
}
