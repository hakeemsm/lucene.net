/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Blockterms;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Sharpen;

namespace Lucene.Net.Codecs.Blockterms
{
	/// <summary>
	/// Selects index terms according to provided pluggable
	/// <see cref="IndexTermSelector">IndexTermSelector</see>
	/// , and stores them in a prefix trie that's
	/// loaded entirely in RAM stored as an FST.  This terms
	/// index only supports unsigned byte term sort order
	/// (unicode codepoint order when the bytes are UTF8).
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class VariableGapTermsIndexWriter : TermsIndexWriterBase
	{
		protected internal IndexOutput @out;

		/// <summary>Extension of terms index file</summary>
		internal static readonly string TERMS_INDEX_EXTENSION = "tiv";

		internal static readonly string CODEC_NAME = "VARIABLE_GAP_TERMS_INDEX";

		internal const int VERSION_START = 0;

		internal const int VERSION_APPEND_ONLY = 1;

		internal const int VERSION_CHECKSUM = 2;

		internal const int VERSION_CURRENT = VERSION_CHECKSUM;

		private readonly IList<VariableGapTermsIndexWriter.FSTFieldWriter> fields = new AList
			<VariableGapTermsIndexWriter.FSTFieldWriter>();

		private readonly FieldInfos fieldInfos;

		private readonly VariableGapTermsIndexWriter.IndexTermSelector policy;

		/// <summary>Hook for selecting which terms should be placed in the terms index.</summary>
		/// <remarks>
		/// Hook for selecting which terms should be placed in the terms index.
		/// <p>
		/// <see cref="NewField(Lucene.Net.Index.FieldInfo)">NewField(Lucene.Net.Index.FieldInfo)
		/// 	</see>
		/// is called at the start of each new field, and
		/// <see cref="IsIndexTerm(Lucene.Net.Util.BytesRef, Lucene.Net.Codecs.TermStats)
		/// 	">IsIndexTerm(Lucene.Net.Util.BytesRef, Lucene.Net.Codecs.TermStats)
		/// 	</see>
		/// for each term in that field.
		/// </remarks>
		/// <lucene.experimental></lucene.experimental>
		public abstract class IndexTermSelector
		{
			// unread
			/// <summary>
			/// Called sequentially on every term being written,
			/// returning true if this term should be indexed
			/// </summary>
			public abstract bool IsIndexTerm(BytesRef term, TermStats stats);

			/// <summary>Called when a new field is started.</summary>
			/// <remarks>Called when a new field is started.</remarks>
			public abstract void NewField(FieldInfo fieldInfo);
		}

		/// <summary>
		/// Same policy as
		/// <see cref="FixedGapTermsIndexWriter">FixedGapTermsIndexWriter</see>
		/// 
		/// </summary>
		public sealed class EveryNTermSelector : VariableGapTermsIndexWriter.IndexTermSelector
		{
			private int count;

			private readonly int interval;

			public EveryNTermSelector(int interval)
			{
				this.interval = interval;
				// First term is first indexed term:
				count = interval;
			}

			public override bool IsIndexTerm(BytesRef term, TermStats stats)
			{
				if (count >= interval)
				{
					count = 1;
					return true;
				}
				else
				{
					count++;
					return false;
				}
			}

			public override void NewField(FieldInfo fieldInfo)
			{
				count = interval;
			}
		}

		/// <summary>
		/// Sets an index term when docFreq &gt;= docFreqThresh, or
		/// every interval terms.
		/// </summary>
		/// <remarks>
		/// Sets an index term when docFreq &gt;= docFreqThresh, or
		/// every interval terms.  This should reduce seek time
		/// to high docFreq terms.
		/// </remarks>
		public sealed class EveryNOrDocFreqTermSelector : VariableGapTermsIndexWriter.IndexTermSelector
		{
			private int count;

			private readonly int docFreqThresh;

			private readonly int interval;

			public EveryNOrDocFreqTermSelector(int docFreqThresh, int interval)
			{
				this.interval = interval;
				this.docFreqThresh = docFreqThresh;
				// First term is first indexed term:
				count = interval;
			}

			public override bool IsIndexTerm(BytesRef term, TermStats stats)
			{
				if (stats.docFreq >= docFreqThresh || count >= interval)
				{
					count = 1;
					return true;
				}
				else
				{
					count++;
					return false;
				}
			}

			public override void NewField(FieldInfo fieldInfo)
			{
				count = interval;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public VariableGapTermsIndexWriter(SegmentWriteState state, VariableGapTermsIndexWriter.IndexTermSelector
			 policy)
		{
			// TODO: it'd be nice to let the FST builder prune based
			// on term count of each node (the prune1/prune2 that it
			// accepts), and build the index based on that.  This
			// should result in a more compact terms index, more like
			// a prefix trie than the other selectors, because it
			// only stores enough leading bytes to get down to N
			// terms that may complete that prefix.  It becomes
			// "deeper" when terms are dense, and "shallow" when they
			// are less dense.
			//
			// However, it's not easy to make that work this this
			// API, because that pruning doesn't immediately know on
			// seeing each term whether that term will be a seek point
			// or not.  It requires some non-causality in the API, ie
			// only on seeing some number of future terms will the
			// builder decide which past terms are seek points.
			// Somehow the API'd need to be able to return a "I don't
			// know" value, eg like a Future, which only later on is
			// flipped (frozen) to true or false.
			//
			// We could solve this with a 2-pass approach, where the
			// first pass would build an FSA (no outputs) solely to
			// determine which prefixes are the 'leaves' in the
			// pruning. The 2nd pass would then look at this prefix
			// trie to mark the seek points and build the FST mapping
			// to the true output.
			//
			// But, one downside to this approach is that it'd result
			// in uneven index term selection.  EG with prune1=10, the
			// resulting index terms could be as frequent as every 10
			// terms or as rare as every <maxArcCount> * 10 (eg 2560),
			// in the extremes.
			string indexFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
				.segmentSuffix, TERMS_INDEX_EXTENSION);
			@out = state.directory.CreateOutput(indexFileName, state.context);
			bool success = false;
			try
			{
				fieldInfos = state.fieldInfos;
				this.policy = policy;
				WriteHeader(@out);
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

		/// <exception cref="System.IO.IOException"></exception>
		public override TermsIndexWriterBase.FieldWriter AddField(FieldInfo field, long termsFilePointer
			)
		{
			////System.out.println("VGW: field=" + field.name);
			policy.NewField(field);
			VariableGapTermsIndexWriter.FSTFieldWriter writer = new VariableGapTermsIndexWriter.FSTFieldWriter
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

		private class FSTFieldWriter : TermsIndexWriterBase.FieldWriter
		{
			private readonly Builder<long> fstBuilder;

			private readonly PositiveIntOutputs fstOutputs;

			private readonly long startTermsFilePointer;

			internal readonly FieldInfo fieldInfo;

			internal FST<long> fst;

			internal readonly long indexStart;

			private readonly BytesRef lastTerm = new BytesRef();

			private bool first = true;

			/// <exception cref="System.IO.IOException"></exception>
			public FSTFieldWriter(VariableGapTermsIndexWriter _enclosing, FieldInfo fieldInfo
				, long termsFilePointer) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				this.fieldInfo = fieldInfo;
				this.fstOutputs = PositiveIntOutputs.GetSingleton();
				this.fstBuilder = new Builder<long>(FST.INPUT_TYPE.BYTE1, this.fstOutputs);
				this.indexStart = this._enclosing.@out.GetFilePointer();
				////System.out.println("VGW: field=" + fieldInfo.name);
				// Always put empty string in
				this.fstBuilder.Add(new IntsRef(), termsFilePointer);
				this.startTermsFilePointer = termsFilePointer;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool CheckIndexTerm(BytesRef text, TermStats stats)
			{
				//System.out.println("VGW: index term=" + text.utf8ToString());
				// NOTE: we must force the first term per field to be
				// indexed, in case policy doesn't:
				if (this._enclosing.policy.IsIndexTerm(text, stats) || this.first)
				{
					this.first = false;
					//System.out.println("  YES");
					return true;
				}
				else
				{
					this.lastTerm.CopyBytes(text);
					return false;
				}
			}

			private readonly IntsRef scratchIntsRef = new IntsRef();

			/// <exception cref="System.IO.IOException"></exception>
			public override void Add(BytesRef text, TermStats stats, long termsFilePointer)
			{
				if (text.length == 0)
				{
					// We already added empty string in ctor
					//HM:revisit 
					//assert termsFilePointer == startTermsFilePointer;
					return;
				}
				int lengthSave = text.length;
				text.length = this._enclosing.IndexedTermPrefixLength(this.lastTerm, text);
				try
				{
					this.fstBuilder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(text, this.scratchIntsRef
						), termsFilePointer);
				}
				finally
				{
					text.length = lengthSave;
				}
				this.lastTerm.CopyBytes(text);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(long termsFilePointer)
			{
				this.fst = this.fstBuilder.Finish();
				if (this.fst != null)
				{
					this.fst.Save(this._enclosing.@out);
				}
			}

			private readonly VariableGapTermsIndexWriter _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			if (@out != null)
			{
				try
				{
					long dirStart = @out.GetFilePointer();
					int fieldCount = fields.Count;
					int nonNullFieldCount = 0;
					for (int i = 0; i < fieldCount; i++)
					{
						VariableGapTermsIndexWriter.FSTFieldWriter field = fields[i];
						if (field.fst != null)
						{
							nonNullFieldCount++;
						}
					}
					@out.WriteVInt(nonNullFieldCount);
					for (int i_1 = 0; i_1 < fieldCount; i_1++)
					{
						VariableGapTermsIndexWriter.FSTFieldWriter field = fields[i_1];
						if (field.fst != null)
						{
							@out.WriteVInt(field.fieldInfo.number);
							@out.WriteVLong(field.indexStart);
						}
					}
					WriteTrailer(dirStart);
					CodecUtil.WriteFooter(@out);
				}
				finally
				{
					@out.Close();
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
