/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Sharpen;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>FST-based term dict, using metadata as FST output.</summary>
	/// <remarks>
	/// FST-based term dict, using metadata as FST output.
	/// The FST directly holds the mapping between &lt;term, metadata&gt;.
	/// Term metadata consists of three parts:
	/// 1. term statistics: docFreq, totalTermFreq;
	/// 2. monotonic long[], e.g. the pointer to the postings list for that term;
	/// 3. generic byte[], e.g. other information need by postings reader.
	/// <p>
	/// File:
	/// <ul>
	/// <li><tt>.tst</tt>: <a href="#Termdictionary">Term Dictionary</a></li>
	/// </ul>
	/// <p>
	/// <a name="Termdictionary" id="Termdictionary"></a>
	/// <h3>Term Dictionary</h3>
	/// <p>
	/// The .tst contains a list of FSTs, one for each field.
	/// The FST maps a term to its corresponding statistics (e.g. docfreq)
	/// and metadata (e.g. information for postings list reader like file pointer
	/// to postings list).
	/// </p>
	/// <p>
	/// Typically the metadata is separated into two parts:
	/// <ul>
	/// <li>
	/// Monotonical long array: Some metadata will always be ascending in order
	/// with the corresponding term. This part is used by FST to share outputs between arcs.
	/// </li>
	/// <li>
	/// Generic byte array: Used to store non-monotonic metadata.
	/// </li>
	/// </ul>
	/// </p>
	/// File format:
	/// <ul>
	/// <li>TermsDict(.tst) --&gt; Header, <i>PostingsHeader</i>, FieldSummary, DirOffset</li>
	/// <li>FieldSummary --&gt; NumFields, &lt;FieldNumber, NumTerms, SumTotalTermFreq?,
	/// SumDocFreq, DocCount, LongsSize, TermFST &gt;<sup>NumFields</sup></li>
	/// <li>TermFST --&gt;
	/// <see cref="Lucene.Net.Util.Fst.FST{T}">FST&lt;TermData&gt;</see>
	/// </li>
	/// <li>TermData --&gt; Flag, BytesSize?, LongDelta<sup>LongsSize</sup>?, Byte<sup>BytesSize</sup>?,
	/// &lt; DocFreq[Same?], (TotalTermFreq-DocFreq) &gt; ? </li>
	/// <li>Header --&gt;
	/// <see cref="Lucene.Net.Codecs.CodecUtil.WriteHeader(Lucene.Net.Store.DataOutput, string, int)
	/// 	">CodecHeader</see>
	/// </li>
	/// <li>DirOffset --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteLong(long)">Uint64</see>
	/// </li>
	/// <li>DocFreq, LongsSize, BytesSize, NumFields,
	/// FieldNumber, DocCount --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteVInt(int)">VInt</see>
	/// </li>
	/// <li>TotalTermFreq, NumTerms, SumTotalTermFreq, SumDocFreq, LongDelta --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteVLong(long)">VLong</see>
	/// </li>
	/// </ul>
	/// <p>Notes:</p>
	/// <ul>
	/// <li>
	/// The format of PostingsHeader and generic meta bytes are customized by the specific postings implementation:
	/// they contain arbitrary per-file data (such as parameters or versioning information), and per-term data
	/// (non-monotonic ones like pulsed postings data).
	/// </li>
	/// <li>
	/// The format of TermData is determined by FST, typically monotonic metadata will be dense around shallow arcs,
	/// while in deeper arcs only generic bytes and term statistics exist.
	/// </li>
	/// <li>
	/// The byte Flag is used to indicate which part of metadata exists on current arc. Specially the monotonic part
	/// is omitted when it is an array of 0s.
	/// </li>
	/// <li>
	/// Since LongsSize is per-field fixed, it is only written once in field summary.
	/// </li>
	/// </ul>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class FSTTermsWriter : FieldsConsumer
	{
		internal static readonly string TERMS_EXTENSION = "tmp";

		internal static readonly string TERMS_CODEC_NAME = "FST_TERMS_DICT";

		public const int TERMS_VERSION_START = 0;

		public const int TERMS_VERSION_CHECKSUM = 1;

		public const int TERMS_VERSION_CURRENT = TERMS_VERSION_CHECKSUM;

		internal readonly PostingsWriterBase postingsWriter;

		internal readonly FieldInfos fieldInfos;

		internal IndexOutput @out;

		internal readonly IList<FSTTermsWriter.FieldMetaData> fields = new AList<FSTTermsWriter.FieldMetaData
			>();

		/// <exception cref="System.IO.IOException"></exception>
		public FSTTermsWriter(SegmentWriteState state, PostingsWriterBase postingsWriter)
		{
			string termsFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
				.segmentSuffix, TERMS_EXTENSION);
			this.postingsWriter = postingsWriter;
			this.fieldInfos = state.fieldInfos;
			this.@out = state.directory.CreateOutput(termsFileName, state.context);
			bool success = false;
			try
			{
				WriteHeader(@out);
				this.postingsWriter.Init(@out);
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
			CodecUtil.WriteHeader(@out, TERMS_CODEC_NAME, TERMS_VERSION_CURRENT);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteTrailer(IndexOutput @out, long dirStart)
		{
			@out.WriteLong(dirStart);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override TermsConsumer AddField(FieldInfo field)
		{
			return new FSTTermsWriter.TermsWriter(this, field);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			if (@out != null)
			{
				bool success = false;
				try
				{
					// write field summary
					long dirStart = @out.FilePointer;
					@out.WriteVInt(fields.Count);
					foreach (FSTTermsWriter.FieldMetaData field in fields)
					{
						@out.WriteVInt(field.fieldInfo.number);
						@out.WriteVLong(field.numTerms);
						if (field.fieldInfo.GetIndexOptions() != FieldInfo.IndexOptions.DOCS_ONLY)
						{
							@out.WriteVLong(field.sumTotalTermFreq);
						}
						@out.WriteVLong(field.sumDocFreq);
						@out.WriteVInt(field.docCount);
						@out.WriteVInt(field.longsSize);
						field.dict.Save(@out);
					}
					WriteTrailer(@out, dirStart);
					CodecUtil.WriteFooter(@out);
					success = true;
				}
				finally
				{
					if (success)
					{
						IOUtils.Close(@out, postingsWriter);
					}
					else
					{
						IOUtils.CloseWhileHandlingException(@out, postingsWriter);
					}
					@out = null;
				}
			}
		}

		private class FieldMetaData
		{
			public readonly FieldInfo fieldInfo;

			public readonly long numTerms;

			public readonly long sumTotalTermFreq;

			public readonly long sumDocFreq;

			public readonly int docCount;

			public readonly int longsSize;

			public readonly FST<FSTTermOutputs.TermData> dict;

			public FieldMetaData(FieldInfo fieldInfo, long numTerms, long sumTotalTermFreq, long
				 sumDocFreq, int docCount, int longsSize, FST<FSTTermOutputs.TermData> fst)
			{
				this.fieldInfo = fieldInfo;
				this.numTerms = numTerms;
				this.sumTotalTermFreq = sumTotalTermFreq;
				this.sumDocFreq = sumDocFreq;
				this.docCount = docCount;
				this.longsSize = longsSize;
				this.dict = fst;
			}
		}

		internal sealed class TermsWriter : TermsConsumer
		{
			private readonly Builder<FSTTermOutputs.TermData> builder;

			private readonly FSTTermOutputs outputs;

			private readonly FieldInfo fieldInfo;

			private readonly int longsSize;

			private long numTerms;

			private readonly IntsRef scratchTerm = new IntsRef();

			private readonly RAMOutputStream statsWriter = new RAMOutputStream();

			private readonly RAMOutputStream metaWriter = new RAMOutputStream();

			internal TermsWriter(FSTTermsWriter _enclosing, FieldInfo fieldInfo)
			{
				this._enclosing = _enclosing;
				this.numTerms = 0;
				this.fieldInfo = fieldInfo;
				this.longsSize = this._enclosing.postingsWriter.SetField(fieldInfo);
				this.outputs = new FSTTermOutputs(fieldInfo, this.longsSize);
				this.builder = new Builder<FSTTermOutputs.TermData>(FST.INPUT_TYPE.BYTE1, this.outputs
					);
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override PostingsConsumer StartTerm(BytesRef text)
			{
				this._enclosing.postingsWriter.StartTerm();
				return this._enclosing.postingsWriter;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm(BytesRef text, TermStats stats)
			{
				// write term meta data into fst
				BlockTermState state = this._enclosing.postingsWriter.NewTermState();
				FSTTermOutputs.TermData meta = new FSTTermOutputs.TermData();
				meta.longs = new long[this.longsSize];
				meta.bytes = null;
				meta.docFreq = state.docFreq = stats.docFreq;
				meta.totalTermFreq = state.totalTermFreq = stats.totalTermFreq;
				this._enclosing.postingsWriter.FinishTerm(state);
				this._enclosing.postingsWriter.EncodeTerm(meta.longs, this.metaWriter, this.fieldInfo
					, state, true);
				int bytesSize = (int)this.metaWriter.FilePointer;
				if (bytesSize > 0)
				{
					meta.bytes = new byte[bytesSize];
					this.metaWriter.WriteTo(meta.bytes, 0);
					this.metaWriter.Reset();
				}
				this.builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(text, this.scratchTerm
					), meta);
				this.numTerms++;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
			{
				// save FST dict
				if (this.numTerms > 0)
				{
					FST<FSTTermOutputs.TermData> fst = this.builder.Finish();
					this._enclosing.fields.AddItem(new FSTTermsWriter.FieldMetaData(this.fieldInfo, this
						.numTerms, sumTotalTermFreq, sumDocFreq, docCount, this.longsSize, fst));
				}
			}

			private readonly FSTTermsWriter _enclosing;
		}
	}
}
