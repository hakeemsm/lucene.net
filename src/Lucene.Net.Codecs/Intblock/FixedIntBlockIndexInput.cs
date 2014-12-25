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
	/// Abstract base class that reads fixed-size blocks of ints
	/// from an IndexInput.
	/// </summary>
	/// <remarks>
	/// Abstract base class that reads fixed-size blocks of ints
	/// from an IndexInput.  While this is a simple approach, a
	/// more performant approach would directly create an impl
	/// of IntIndexInput inside Directory.  Wrapping a generic
	/// IndexInput will likely cost performance.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class FixedIntBlockIndexInput : IntIndexInput
	{
		private readonly IndexInput @in;

		protected internal readonly int blockSize;

		/// <exception cref="System.IO.IOException"></exception>
		public FixedIntBlockIndexInput(IndexInput @in)
		{
			this.@in = @in;
			blockSize = @in.ReadVInt();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IntIndexInput.Reader Reader()
		{
			int[] buffer = new int[blockSize];
			IndexInput clone = ((IndexInput)@in.Clone());
			// TODO: can this be simplified?
			return new FixedIntBlockIndexInput.Reader(clone, buffer, this.GetBlockReader(clone
				, buffer));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			@in.Close();
		}

		public override IntIndexInput.Index Index()
		{
			return new FixedIntBlockIndexInput.Index(this);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal abstract FixedIntBlockIndexInput.BlockReader GetBlockReader(IndexInput
			 @in, int[] buffer);

		/// <summary>Interface for fixed-size block decoders.</summary>
		/// <remarks>
		/// Interface for fixed-size block decoders.
		/// <p>
		/// Implementations should decode into the buffer in
		/// <see cref="ReadBlock()">ReadBlock()</see>
		/// .
		/// </remarks>
		public interface BlockReader
		{
			/// <exception cref="System.IO.IOException"></exception>
			void ReadBlock();
		}

		private class Reader : IntIndexInput.Reader
		{
			private readonly IndexInput @in;

			private readonly FixedIntBlockIndexInput.BlockReader blockReader;

			private readonly int blockSize;

			private readonly int[] pending;

			private int upto;

			private bool seekPending;

			private long pendingFP;

			private long lastBlockFP = -1;

			public Reader(IndexInput @in, int[] pending, FixedIntBlockIndexInput.BlockReader 
				blockReader)
			{
				this.@in = @in;
				this.pending = pending;
				this.blockSize = pending.Length;
				this.blockReader = blockReader;
				upto = blockSize;
			}

			internal virtual void Seek(long fp, int upto)
			{
				//HM:revisit 
				//assert upto < blockSize;
				if (seekPending || fp != lastBlockFP)
				{
					pendingFP = fp;
					seekPending = true;
				}
				this.upto = upto;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Next()
			{
				if (seekPending)
				{
					// Seek & load new block
					@in.Seek(pendingFP);
					lastBlockFP = pendingFP;
					blockReader.ReadBlock();
					seekPending = false;
				}
				else
				{
					if (upto == blockSize)
					{
						// Load new block
						lastBlockFP = @in.FilePointer;
						blockReader.ReadBlock();
						upto = 0;
					}
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

			//HM:revisit 
			//assert upto < blockSize;
			/// <exception cref="System.IO.IOException"></exception>
			public override void Seek(IntIndexInput.Reader other)
			{
				((FixedIntBlockIndexInput.Reader)other).Seek(this.fp, this.upto);
			}

			public override void CopyFrom(IntIndexInput.Index other)
			{
				FixedIntBlockIndexInput.Index idx = (FixedIntBlockIndexInput.Index)other;
				this.fp = idx.fp;
				this.upto = idx.upto;
			}

			public override IntIndexInput.Index Clone()
			{
				FixedIntBlockIndexInput.Index other = new FixedIntBlockIndexInput.Index(this);
				other.fp = this.fp;
				other.upto = this.upto;
				return other;
			}

			public override string ToString()
			{
				return "fp=" + this.fp + " upto=" + this.upto;
			}

			internal Index(FixedIntBlockIndexInput _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly FixedIntBlockIndexInput _enclosing;
		}
	}
}
