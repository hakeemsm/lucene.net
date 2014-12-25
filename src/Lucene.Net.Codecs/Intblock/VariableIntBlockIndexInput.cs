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
	/// Abstract base class that reads variable-size blocks of ints
	/// from an IndexInput.
	/// </summary>
	/// <remarks>
	/// Abstract base class that reads variable-size blocks of ints
	/// from an IndexInput.  While this is a simple approach, a
	/// more performant approach would directly create an impl
	/// of IntIndexInput inside Directory.  Wrapping a generic
	/// IndexInput will likely cost performance.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class VariableIntBlockIndexInput : IntIndexInput
	{
		protected internal readonly IndexInput @in;

		protected internal readonly int maxBlockSize;

		/// <exception cref="System.IO.IOException"></exception>
		protected internal VariableIntBlockIndexInput(IndexInput @in)
		{
			// TODO: much of this can be shared code w/ the fixed case
			this.@in = @in;
			maxBlockSize = @in.ReadInt();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IntIndexInput.Reader Reader()
		{
			int[] buffer = new int[maxBlockSize];
			IndexInput clone = ((IndexInput)@in.Clone());
			// TODO: can this be simplified?
			return new VariableIntBlockIndexInput.Reader(clone, buffer, this.GetBlockReader(clone
				, buffer));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			@in.Close();
		}

		public override IntIndexInput.Index Index()
		{
			return new VariableIntBlockIndexInput.Index(this);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal abstract VariableIntBlockIndexInput.BlockReader GetBlockReader
			(IndexInput @in, int[] buffer);

		/// <summary>Interface for variable-size block decoders.</summary>
		/// <remarks>
		/// Interface for variable-size block decoders.
		/// <p>
		/// Implementations should decode into the buffer in
		/// <see cref="ReadBlock()">ReadBlock()</see>
		/// .
		/// </remarks>
		public interface BlockReader
		{
			/// <exception cref="System.IO.IOException"></exception>
			int ReadBlock();

			/// <exception cref="System.IO.IOException"></exception>
			void Seek(long pos);
		}

		private class Reader : IntIndexInput.Reader
		{
			private readonly IndexInput @in;

			public readonly int[] pending;

			internal int upto;

			private bool seekPending;

			private long pendingFP;

			private int pendingUpto;

			private long lastBlockFP;

			private int blockSize;

			private readonly VariableIntBlockIndexInput.BlockReader blockReader;

			public Reader(IndexInput @in, int[] pending, VariableIntBlockIndexInput.BlockReader
				 blockReader)
			{
				this.@in = @in;
				this.pending = pending;
				this.blockReader = blockReader;
			}

			internal virtual void Seek(long fp, int upto)
			{
				// TODO: should we do this in real-time, not lazy?
				pendingFP = fp;
				pendingUpto = upto;
				//HM:revisit 
				//assert pendingUpto >= 0: "pendingUpto=" + pendingUpto;
				seekPending = true;
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void MaybeSeek()
			{
				if (seekPending)
				{
					if (pendingFP != lastBlockFP)
					{
						// need new block
						@in.Seek(pendingFP);
						blockReader.Seek(pendingFP);
						lastBlockFP = pendingFP;
						blockSize = blockReader.ReadBlock();
					}
					upto = pendingUpto;
					// TODO: if we were more clever when writing the
					// index, such that a seek point wouldn't be written
					// until the int encoder "committed", we could avoid
					// this (likely minor) inefficiency:
					// This is necessary for int encoders that are
					// non-causal, ie must see future int values to
					// encode the current ones.
					while (upto >= blockSize)
					{
						upto -= blockSize;
						lastBlockFP = @in.FilePointer;
						blockSize = blockReader.ReadBlock();
					}
					seekPending = false;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Next()
			{
				this.MaybeSeek();
				if (upto == blockSize)
				{
					lastBlockFP = @in.FilePointer;
					blockSize = blockReader.ReadBlock();
					upto = 0;
				}
				return pending[upto++];
			}
		}

		private class Index : IntIndexInput.Index
		{
			private long fp;

			private int upto;

			/// <exception cref="System.IO.IOException"></exception>
			public override void Read(DataInput indexIn, bool absolute)
			{
				if (absolute)
				{
					this.upto = indexIn.ReadVInt();
					this.fp = indexIn.ReadVLong();
				}
				else
				{
					int uptoDelta = indexIn.ReadVInt();
					if ((uptoDelta & 1) == 1)
					{
						// same block
						this.upto += (int)(((uint)uptoDelta) >> 1);
					}
					else
					{
						// new block
						this.upto = (int)(((uint)uptoDelta) >> 1);
						this.fp += indexIn.ReadVLong();
					}
				}
			}

			// TODO: we can't do this 
			//HM:revisit 
			//assert because non-causal
			// int encoders can have upto over the buffer size
			//
			//HM:revisit 
			//assert upto < maxBlockSize: "upto=" + upto + " max=" + maxBlockSize;
			public override string ToString()
			{
				return "VarIntBlock.Index fp=" + this.fp + " upto=" + this.upto + " maxBlock=" + 
					this._enclosing.maxBlockSize;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Seek(IntIndexInput.Reader other)
			{
				((VariableIntBlockIndexInput.Reader)other).Seek(this.fp, this.upto);
			}

			public override void CopyFrom(IntIndexInput.Index other)
			{
				VariableIntBlockIndexInput.Index idx = (VariableIntBlockIndexInput.Index)other;
				this.fp = idx.fp;
				this.upto = idx.upto;
			}

			public override IntIndexInput.Index Clone()
			{
				VariableIntBlockIndexInput.Index other = new VariableIntBlockIndexInput.Index(this
					);
				other.fp = this.fp;
				other.upto = this.upto;
				return other;
			}

			internal Index(VariableIntBlockIndexInput _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly VariableIntBlockIndexInput _enclosing;
		}
	}
}
