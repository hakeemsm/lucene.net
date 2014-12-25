using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Blockterms
{
	/// <summary>
	/// Selects every Nth term as and index term, and hold term
	/// bytes (mostly) fully expanded in memory.
	/// </summary>
	/// <remarks>
	/// Selects every Nth term as and index term, and hold term
	/// bytes (mostly) fully expanded in memory.  This terms index
	/// supports seeking by ord.  See
	/// <see cref="VariableGapTermsIndexWriter">VariableGapTermsIndexWriter</see>
	/// for a more memory efficient
	/// terms index that does not support seeking by ord.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class FixedGapTermsIndexWriter : TermsIndexWriterBase
	{
		protected internal IndexOutput @out;

		/// <summary>Extension of terms index file</summary>
		internal static readonly string TERMS_INDEX_EXTENSION = "tii";

		internal static readonly string CODEC_NAME = "SIMPLE_STANDARD_TERMS_INDEX";

		internal const int VERSION_START = 0;

		internal const int VERSION_APPEND_ONLY = 1;

		internal const int VERSION_CHECKSUM = 1000;

		internal const int VERSION_CURRENT = VERSION_CHECKSUM;

		private readonly int termIndexInterval;

		private readonly IList<FixedGapTermsIndexWriter.SimpleFieldWriter> fields = new AList
			<FixedGapTermsIndexWriter.SimpleFieldWriter>();

		private readonly FieldInfos fieldInfos;

		/// <exception cref="System.IO.IOException"></exception>
		public FixedGapTermsIndexWriter(SegmentWriteState state)
		{
			// 4.x "skipped" trunk's monotonic addressing: give any user a nice exception
			// unread
			string indexFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
				.segmentSuffix, TERMS_INDEX_EXTENSION);
			termIndexInterval = state.termIndexInterval;
			@out = state.directory.CreateOutput(indexFileName, state.context);
			bool success = false;
			try
			{
				fieldInfos = state.fieldInfos;
				WriteHeader(@out);
				@out.WriteInt(termIndexInterval);
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(@out);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteHeader(IndexOutput @out)
		{
			CodecUtil.WriteHeader(@out, CODEC_NAME, VERSION_CURRENT);
		}

		public override TermsIndexWriterBase.FieldWriter AddField(FieldInfo field, long termsFilePointer
			)
		{
			//System.out.println("FGW: addFfield=" + field.name);
			FixedGapTermsIndexWriter.SimpleFieldWriter writer = new FixedGapTermsIndexWriter.SimpleFieldWriter
				(this, field, termsFilePointer);
			fields.AddItem(writer);
			return writer;
		}

		/// <summary>
		/// NOTE: if your codec does not sort in unicode code
		/// point order, you must override this method, to simply
		/// return indexedTerm.length.
		/// </summary>
		/// <remarks>
		/// NOTE: if your codec does not sort in unicode code
		/// point order, you must override this method, to simply
		/// return indexedTerm.length.
		/// </remarks>
		protected internal virtual int IndexedTermPrefixLength(BytesRef priorTerm, BytesRef
			 indexedTerm)
		{
			// As long as codec sorts terms in unicode codepoint
			// order, we can safely strip off the non-distinguishing
			// suffix to save RAM in the loaded terms index.
			int idxTermOffset = indexedTerm.offset;
			int priorTermOffset = priorTerm.offset;
			int limit = Math.Min(priorTerm.length, indexedTerm.length);
			for (int byteIdx = 0; byteIdx < limit; byteIdx++)
			{
				if (priorTerm.bytes[priorTermOffset + byteIdx] != indexedTerm.bytes[idxTermOffset
					 + byteIdx])
				{
					return byteIdx + 1;
				}
			}
			return Math.Min(1 + priorTerm.length, indexedTerm.length);
		}

		private class SimpleFieldWriter : TermsIndexWriterBase.FieldWriter
		{
			internal readonly FieldInfo fieldInfo;

			internal int numIndexTerms;

			internal readonly long indexStart;

			internal readonly long termsStart;

			internal long packedIndexStart;

			internal long packedOffsetsStart;

			private long numTerms;

			private short[] termLengths;

			private int[] termsPointerDeltas;

			private long lastTermsPointer;

			private long totTermLength;

			private readonly BytesRef lastTerm = new BytesRef();

			internal SimpleFieldWriter(FixedGapTermsIndexWriter _enclosing, FieldInfo fieldInfo
				, long termsFilePointer) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				// TODO: we could conceivably make a PackedInts wrapper
				// that auto-grows... then we wouldn't force 6 bytes RAM
				// per index term:
				this.fieldInfo = fieldInfo;
				this.indexStart = this._enclosing.@out.FilePointer;
				this.termsStart = this.lastTermsPointer = termsFilePointer;
				this.termLengths = new short[0];
				this.termsPointerDeltas = new int[0];
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool CheckIndexTerm(BytesRef text, TermStats stats)
			{
				// First term is first indexed term:
				//System.out.println("FGW: checkIndexTerm text=" + text.utf8ToString());
				if (0 == (this.numTerms++ % this._enclosing.termIndexInterval))
				{
					return true;
				}
				else
				{
					if (0 == this.numTerms % this._enclosing.termIndexInterval)
					{
						// save last term just before next index term so we
						// can compute wasted suffix
						this.lastTerm.CopyBytes(text);
					}
					return false;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Add(BytesRef text, TermStats stats, long termsFilePointer)
			{
				int indexedTermLength = this._enclosing.IndexedTermPrefixLength(this.lastTerm, text
					);
				//System.out.println("FGW: add text=" + text.utf8ToString() + " " + text + " fp=" + termsFilePointer);
				// write only the min prefix that shows the diff
				// against prior term
				this._enclosing.@out.WriteBytes(text.bytes, text.offset, indexedTermLength);
				if (this.termLengths.Length == this.numIndexTerms)
				{
					this.termLengths = ArrayUtil.Grow(this.termLengths);
				}
				if (this.termsPointerDeltas.Length == this.numIndexTerms)
				{
					this.termsPointerDeltas = ArrayUtil.Grow(this.termsPointerDeltas);
				}
				// save delta terms pointer
				this.termsPointerDeltas[this.numIndexTerms] = (int)(termsFilePointer - this.lastTermsPointer
					);
				this.lastTermsPointer = termsFilePointer;
				// save term length (in bytes)
				//HM:revisit 
				//assert indexedTermLength <= Short.MAX_VALUE;
				this.termLengths[this.numIndexTerms] = (short)indexedTermLength;
				this.totTermLength += indexedTermLength;
				this.lastTerm.CopyBytes(text);
				this.numIndexTerms++;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(long termsFilePointer)
			{
				// write primary terms dict offsets
				this.packedIndexStart = this._enclosing.@out.FilePointer;
				PackedInts.Writer w = PackedInts.GetWriter(this._enclosing.@out, this.numIndexTerms
					, PackedInts.BitsRequired(termsFilePointer), PackedInts.DEFAULT);
				// relative to our indexStart
				long upto = 0;
				for (int i = 0; i < this.numIndexTerms; i++)
				{
					upto += this.termsPointerDeltas[i];
					w.Add(upto);
				}
				w.Finish();
				this.packedOffsetsStart = this._enclosing.@out.FilePointer;
				// write offsets into the byte[] terms
				w = PackedInts.GetWriter(this._enclosing.@out, 1 + this.numIndexTerms, PackedInts
					.BitsRequired(this.totTermLength), PackedInts.DEFAULT);
				upto = 0;
				for (int i_1 = 0; i_1 < this.numIndexTerms; i_1++)
				{
					w.Add(upto);
					upto += this.termLengths[i_1];
				}
				w.Add(upto);
				w.Finish();
				// our referrer holds onto us, while other fields are
				// being written, so don't tie up this RAM:
				this.termLengths = null;
				this.termsPointerDeltas = null;
			}

			private readonly FixedGapTermsIndexWriter _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			if (@out != null)
			{
				bool success = false;
				try
				{
					long dirStart = @out.FilePointer;
					int fieldCount = fields.Count;
					int nonNullFieldCount = 0;
					for (int i = 0; i < fieldCount; i++)
					{
						FixedGapTermsIndexWriter.SimpleFieldWriter field = fields[i];
						if (field.numIndexTerms > 0)
						{
							nonNullFieldCount++;
						}
					}
					@out.WriteVInt(nonNullFieldCount);
					for (int i_1 = 0; i_1 < fieldCount; i_1++)
					{
						FixedGapTermsIndexWriter.SimpleFieldWriter field = fields[i_1];
						if (field.numIndexTerms > 0)
						{
							@out.WriteVInt(field.fieldInfo.number);
							@out.WriteVInt(field.numIndexTerms);
							@out.WriteVLong(field.termsStart);
							@out.WriteVLong(field.indexStart);
							@out.WriteVLong(field.packedIndexStart);
							@out.WriteVLong(field.packedOffsetsStart);
						}
					}
					WriteTrailer(dirStart);
					CodecUtil.WriteFooter(@out);
					success = true;
				}
				finally
				{
					if (success)
					{
						IOUtils.Close(@out);
					}
					else
					{
						IOUtils.CloseWhileHandlingException(@out);
					}
					@out = null;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteTrailer(long dirStart)
		{
			@out.WriteLong(dirStart);
		}
	}
}
