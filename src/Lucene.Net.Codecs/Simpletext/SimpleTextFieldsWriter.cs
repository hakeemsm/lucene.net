using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Simpletext
{
	internal class SimpleTextFieldsWriter : FieldsConsumer
	{
		private IndexOutput @out;

		private readonly BytesRef scratch = new BytesRef(10);

		internal static readonly BytesRef END = new BytesRef("END");

		internal static readonly BytesRef FIELD = new BytesRef("field ");

		internal static readonly BytesRef TERM = new BytesRef("  term ");

		internal static readonly BytesRef DOC = new BytesRef("    doc ");

		internal static readonly BytesRef FREQ = new BytesRef("      freq ");

		internal static readonly BytesRef POS = new BytesRef("      pos ");

		internal static readonly BytesRef START_OFFSET = new BytesRef("      startOffset "
			);

		internal static readonly BytesRef END_OFFSET = new BytesRef("      endOffset ");

		internal static readonly BytesRef PAYLOAD = new BytesRef("        payload ");

		/// <exception cref="System.IO.IOException"></exception>
		public SimpleTextFieldsWriter(SegmentWriteState state)
		{
			string fileName = SimpleTextPostingsFormat.GetPostingsFileName(state.segmentInfo.
				name, state.segmentSuffix);
			@out = state.directory.CreateOutput(fileName, state.context);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Write(string s)
		{
			SimpleTextUtil.Write(@out, s, scratch);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Write(BytesRef b)
		{
			SimpleTextUtil.Write(@out, b);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Newline()
		{
			SimpleTextUtil.WriteNewline(@out);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override TermsConsumer AddField(FieldInfo field)
		{
			Write(FIELD);
			Write(field.name);
			Newline();
			return new SimpleTextFieldsWriter.SimpleTextTermsWriter(this, field);
		}

		private class SimpleTextTermsWriter : TermsConsumer
		{
			private readonly SimpleTextFieldsWriter.SimpleTextPostingsWriter postingsWriter;

			public SimpleTextTermsWriter(SimpleTextFieldsWriter _enclosing, FieldInfo field)
			{
				this._enclosing = _enclosing;
				this.postingsWriter = new SimpleTextFieldsWriter.SimpleTextPostingsWriter(this, field
					);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override PostingsConsumer StartTerm(BytesRef term)
			{
				return this.postingsWriter.Reset(term);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm(BytesRef term, TermStats stats)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
			{
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}

			private readonly SimpleTextFieldsWriter _enclosing;
		}

		private class SimpleTextPostingsWriter : PostingsConsumer
		{
			private BytesRef term;

			private bool wroteTerm;

			private readonly FieldInfo.IndexOptions indexOptions;

			private readonly bool writePositions;

			private readonly bool writeOffsets;

			private int lastStartOffset = 0;

			public SimpleTextPostingsWriter(SimpleTextFieldsWriter _enclosing, FieldInfo field
				)
			{
				this._enclosing = _enclosing;
				// for 
				//HM:revisit 
				//assert:
				this.indexOptions = field.GetIndexOptions();
				this.writePositions = this.indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) >= 0;
				this.writeOffsets = this.indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					) >= 0;
			}

			//System.out.println("writeOffsets=" + writeOffsets);
			//System.out.println("writePos=" + writePositions);
			/// <exception cref="System.IO.IOException"></exception>
			public override void StartDoc(int docID, int termDocFreq)
			{
				if (!this.wroteTerm)
				{
					// we lazily do this, in case the term had zero docs
					this._enclosing.Write(SimpleTextFieldsWriter.TERM);
					this._enclosing.Write(this.term);
					this._enclosing.Newline();
					this.wroteTerm = true;
				}
				this._enclosing.Write(SimpleTextFieldsWriter.DOC);
				this._enclosing.Write(Sharpen.Extensions.ToString(docID));
				this._enclosing.Newline();
				if (this.indexOptions != FieldInfo.IndexOptions.DOCS_ONLY)
				{
					this._enclosing.Write(SimpleTextFieldsWriter.FREQ);
					this._enclosing.Write(Sharpen.Extensions.ToString(termDocFreq));
					this._enclosing.Newline();
				}
				this.lastStartOffset = 0;
			}

			public virtual PostingsConsumer Reset(BytesRef term)
			{
				this.term = term;
				this.wroteTerm = false;
				return this;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddPosition(int position, BytesRef payload, int startOffset, 
				int endOffset)
			{
				if (this.writePositions)
				{
					this._enclosing.Write(SimpleTextFieldsWriter.POS);
					this._enclosing.Write(Sharpen.Extensions.ToString(position));
					this._enclosing.Newline();
				}
				if (this.writeOffsets)
				{
					//HM:revisit 
					//assert endOffset >= startOffset;
					//HM:revisit 
					//assert startOffset >= lastStartOffset: "startOffset=" + startOffset + " lastStartOffset=" + lastStartOffset;
					this.lastStartOffset = startOffset;
					this._enclosing.Write(SimpleTextFieldsWriter.START_OFFSET);
					this._enclosing.Write(Sharpen.Extensions.ToString(startOffset));
					this._enclosing.Newline();
					this._enclosing.Write(SimpleTextFieldsWriter.END_OFFSET);
					this._enclosing.Write(Sharpen.Extensions.ToString(endOffset));
					this._enclosing.Newline();
				}
				if (payload != null && payload.length > 0)
				{
					//HM:revisit 
					//assert payload.length != 0;
					this._enclosing.Write(SimpleTextFieldsWriter.PAYLOAD);
					this._enclosing.Write(payload);
					this._enclosing.Newline();
				}
			}

			public override void FinishDoc()
			{
			}

			private readonly SimpleTextFieldsWriter _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			if (@out != null)
			{
				try
				{
					Write(END);
					Newline();
					SimpleTextUtil.WriteChecksum(@out, scratch);
				}
				finally
				{
					@out.Close();
					@out = null;
				}
			}
		}
	}
}
