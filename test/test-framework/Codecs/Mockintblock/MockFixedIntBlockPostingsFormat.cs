/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Blockterms;
using Org.Apache.Lucene.Codecs.Intblock;
using Org.Apache.Lucene.Codecs.Mockintblock;
using Org.Apache.Lucene.Codecs.Sep;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Mockintblock
{
	/// <summary>
	/// A silly test codec to verify core support for fixed
	/// sized int block encoders is working.
	/// </summary>
	/// <remarks>
	/// A silly test codec to verify core support for fixed
	/// sized int block encoders is working.  The int encoder
	/// used here just writes each block as a series of vInt.
	/// </remarks>
	public sealed class MockFixedIntBlockPostingsFormat : PostingsFormat
	{
		private readonly int blockSize;

		public MockFixedIntBlockPostingsFormat() : this(1)
		{
		}

		public MockFixedIntBlockPostingsFormat(int blockSize) : base("MockFixedIntBlock")
		{
			this.blockSize = blockSize;
		}

		public override string ToString()
		{
			return GetName() + "(blockSize=" + blockSize + ")";
		}

		// only for testing
		public IntStreamFactory GetIntFactory()
		{
			return new MockFixedIntBlockPostingsFormat.MockIntFactory(blockSize);
		}

		/// <summary>Encodes blocks as vInts of a fixed block size.</summary>
		/// <remarks>Encodes blocks as vInts of a fixed block size.</remarks>
		public class MockIntFactory : IntStreamFactory
		{
			private readonly int blockSize;

			public MockIntFactory(int blockSize)
			{
				this.blockSize = blockSize;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IntIndexInput OpenInput(Directory dir, string fileName, IOContext
				 context)
			{
				return new _FixedIntBlockIndexInput_87(dir.OpenInput(fileName, context));
			}

			private sealed class _FixedIntBlockIndexInput_87 : FixedIntBlockIndexInput
			{
				public _FixedIntBlockIndexInput_87(IndexInput baseArg1) : base(baseArg1)
				{
				}

				protected override FixedIntBlockIndexInput.BlockReader GetBlockReader(IndexInput 
					@in, int[] buffer)
				{
					return new _BlockReader_91(buffer, @in);
				}

				private sealed class _BlockReader_91 : FixedIntBlockIndexInput.BlockReader
				{
					public _BlockReader_91(int[] buffer, IndexInput @in)
					{
						this.buffer = buffer;
						this.@in = @in;
					}

					public void Seek(long pos)
					{
					}

					/// <exception cref="System.IO.IOException"></exception>
					public void ReadBlock()
					{
						for (int i = 0; i < buffer.Length; i++)
						{
							buffer[i] = @in.ReadVInt();
						}
					}

					private readonly int[] buffer;

					private readonly IndexInput @in;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IntIndexOutput CreateOutput(Directory dir, string fileName, IOContext
				 context)
			{
				IndexOutput @out = dir.CreateOutput(fileName, context);
				bool success = false;
				try
				{
					FixedIntBlockIndexOutput ret = new _FixedIntBlockIndexOutput_109(@out, blockSize);
					success = true;
					return ret;
				}
				finally
				{
					if (!success)
					{
						IOUtils.CloseWhileHandlingException(@out);
					}
				}
			}

			private sealed class _FixedIntBlockIndexOutput_109 : FixedIntBlockIndexOutput
			{
				public _FixedIntBlockIndexOutput_109(IndexOutput baseArg1, int baseArg2) : base(baseArg1
					, baseArg2)
				{
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected override void FlushBlock()
				{
					for (int i = 0; i < this.buffer.Length; i++)
					{
						this.@out.WriteVInt(this.buffer[i]);
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
		{
			PostingsWriterBase postingsWriter = new SepPostingsWriter(state, new MockFixedIntBlockPostingsFormat.MockIntFactory
				(blockSize));
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
				fieldInfos, state.segmentInfo, state.context, new MockFixedIntBlockPostingsFormat.MockIntFactory
				(blockSize), state.segmentSuffix);
			TermsIndexReaderBase indexReader;
			bool success = false;
			try
			{
				indexReader = new FixedGapTermsIndexReader(state.directory, state.fieldInfos, state
					.segmentInfo.name, state.termsIndexDivisor, BytesRef.GetUTF8SortedAsUnicodeComparator
					(), state.segmentSuffix, IOContext.DEFAULT);
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
