using System;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Mockintblock.TestFramework
{
	/// <summary>
	/// A silly test codec to verify core support for variable
	/// sized int block encoders is working.
	/// </summary>
	/// <remarks>
	/// A silly test codec to verify core support for variable
	/// sized int block encoders is working.  The int encoder
	/// used here writes baseBlockSize ints at once, if the first
	/// int is &lt;= 3, else 2*baseBlockSize.
	/// </remarks>
	public sealed class MockVariableIntBlockPostingsFormat : PostingsFormat
	{
		private readonly int baseBlockSize;

		public MockVariableIntBlockPostingsFormat() : this(1)
		{
		}

		public MockVariableIntBlockPostingsFormat(int baseBlockSize) : base("MockVariableIntBlock"
			)
		{
			this.baseBlockSize = baseBlockSize;
		}

		public override string ToString()
		{
			return Name + "(baseBlockSize=" + baseBlockSize + ")";
		}

		/// <summary>
		/// If the first value is &lt;= 3, writes baseBlockSize vInts at once,
		/// otherwise writes 2*baseBlockSize vInts.
		/// </summary>
		/// <remarks>
		/// If the first value is &lt;= 3, writes baseBlockSize vInts at once,
		/// otherwise writes 2*baseBlockSize vInts.
		/// </remarks>
		public class MockIntFactory : IntStreamFactory
		{
			private readonly int baseBlockSize;

			public MockIntFactory(int baseBlockSize)
			{
				this.baseBlockSize = baseBlockSize;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IntIndexInput OpenInput(Directory dir, string fileName, IOContext
				 context)
			{
				IndexInput @in = dir.OpenInput(fileName, context);
				int baseBlockSize = @in.ReadInt();
				return new AnonVariableIntBlockIndexInput(baseBlockSize, @in);
			}

			private sealed class AnonVariableIntBlockIndexInput : VariableIntBlockIndexInput
			{
				public AnonVariableIntBlockIndexInput(int baseBlockSize, IndexInput baseArg1) : base
					(baseArg1)
				{
					this.baseBlockSize = baseBlockSize;
				}

				protected override VariableIntBlockIndexInput.BlockReader GetBlockReader(IndexInput
					 @in, int[] buffer)
				{
					return new AnonBlockReader(buffer, @in, baseBlockSize);
				}

				private sealed class AnonBlockReader : VariableIntBlockIndexInput.BlockReader
				{
					public AnonBlockReader(int[] buffer, IndexInput @in, int baseBlockSize)
					{
						this.buffer = buffer;
						this.@in = @in;
						this.baseBlockSize = baseBlockSize;
					}

					public void Seek(long pos)
					{
					}

					/// <exception cref="System.IO.IOException"></exception>
					public int ReadBlock()
					{
						buffer[0] = @in.ReadVInt();
						int count = buffer[0] <= 3 ? baseBlockSize - 1 : 2 * baseBlockSize - 1;
						 
						//assert buffer.length >= count: "buffer.length=" + buffer.length + " count=" + count;
						for (int i = 0; i < count; i++)
						{
							buffer[i + 1] = @in.ReadVInt();
						}
						return 1 + count;
					}

					private readonly int[] buffer;

					private readonly IndexInput @in;

					private readonly int baseBlockSize;
				}

				private readonly int baseBlockSize;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IntIndexOutput CreateOutput(Directory dir, string fileName, IOContext
				 context)
			{
				IndexOutput @out = dir.CreateOutput(fileName, context);
				bool success = false;
				try
				{
					@out.WriteInt(baseBlockSize);
					VariableIntBlockIndexOutput ret = new AnonVariableIntBlockIndexOutput(this, @out
						, 2 * baseBlockSize);
					// silly variable block length int encoder: if
					// first value <= 3, we write N vints at once;
					// else, 2*N
					// intentionally be non-causal here:
					success = true;
					return ret;
				}
				finally
				{
					if (!success)
					{
						IOUtils.CloseWhileHandlingException((IDisposable)@out);
					}
				}
			}

			private sealed class AnonVariableIntBlockIndexOutput : VariableIntBlockIndexOutput
			{
				public AnonVariableIntBlockIndexOutput(MockIntFactory _enclosing, IndexOutput baseArg1
					, int baseArg2) : base(baseArg1, baseArg2)
				{
					this._enclosing = _enclosing;
					this.buffer = new int[2 + 2 * this._enclosing.baseBlockSize];
				}

				internal int pendingCount;

				internal readonly int[] buffer;

				/// <exception cref="System.IO.IOException"></exception>
				protected override int Add(int value)
				{
					this.buffer[this.pendingCount++] = value;
					int flushAt = this.buffer[0] <= 3 ? this._enclosing.baseBlockSize : 2 * this._enclosing
						.baseBlockSize;
					if (this.pendingCount == flushAt + 1)
					{
						for (int i = 0; i < flushAt; i++)
						{
							this.@out.WriteVInt(this.buffer[i]);
						}
						this.buffer[0] = this.buffer[flushAt];
						this.pendingCount = 1;
						return flushAt;
					}
					else
					{
						return 0;
					}
				}

				private readonly MockIntFactory _enclosing;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
		{
			PostingsWriterBase postingsWriter = new SepPostingsWriter(state, new MockIntFactory(baseBlockSize));
			bool success = false;
			TermsIndexWriterBase indexWriter;
			try
			{
				indexWriter = new FixedGapTermsIndexWriter(state);
				success = true;
			}
			finally
			{
				if (!success)
				{
					postingsWriter.Close();
				}
			}
			success = false;
			try
			{
				FieldsConsumer ret = new BlockTermsWriter(indexWriter, state, postingsWriter);
				success = true;
				return ret;
			}
			finally
			{
				if (!success)
				{
					try
					{
						postingsWriter.Close();
					}
					finally
					{
						indexWriter.Close();
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldsProducer FieldsProducer(SegmentReadState state)
		{
			PostingsReaderBase postingsReader = new SepPostingsReader(state.directory, state.
				fieldInfos, state.segmentInfo, state.context, new MockVariableIntBlockPostingsFormat.MockIntFactory
				(baseBlockSize), state.segmentSuffix);
			TermsIndexReaderBase indexReader;
			bool success = false;
			try
			{
				indexReader = new FixedGapTermsIndexReader(state.directory, state.fieldInfos, state
					.segmentInfo.name, state.termsIndexDivisor, BytesRef.GetUTF8SortedAsUnicodeComparator
					(), state.segmentSuffix, state.context);
				success = true;
			}
			finally
			{
				if (!success)
				{
					postingsReader.Close();
				}
			}
			success = false;
			try
			{
				FieldsProducer ret = new BlockTermsReader(indexReader, state.directory, state.fieldInfos
					, state.segmentInfo, postingsReader, state.context, state.segmentSuffix);
				success = true;
				return ret;
			}
			finally
			{
				if (!success)
				{
					try
					{
						postingsReader.Close();
					}
					finally
					{
						indexReader.Close();
					}
				}
			}
		}
	}
}
