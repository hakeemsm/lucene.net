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
	/// <summary>FST-based term dict, using ord as FST output.</summary>
	/// <remarks>
	/// FST-based term dict, using ord as FST output.
	/// The FST holds the mapping between &lt;term, ord&gt;, and
	/// term's metadata is delta encoded into a single byte block.
	/// Typically the byte block consists of four parts:
	/// 1. term statistics: docFreq, totalTermFreq;
	/// 2. monotonic long[], e.g. the pointer to the postings list for that term;
	/// 3. generic byte[], e.g. other information customized by postings base.
	/// 4. single-level skip list to speed up metadata decoding by ord.
	/// <p>
	/// Files:
	/// <ul>
	/// <li><tt>.tix</tt>: <a href="#Termindex">Term Index</a></li>
	/// <li><tt>.tbk</tt>: <a href="#Termblock">Term Block</a></li>
	/// </ul>
	/// </p>
	/// <a name="Termindex" id="Termindex"></a>
	/// <h3>Term Index</h3>
	/// <p>
	/// The .tix contains a list of FSTs, one for each field.
	/// The FST maps a term to its corresponding order in current field.
	/// </p>
	/// <ul>
	/// <li>TermIndex(.tix) --&gt; Header, TermFST<sup>NumFields</sup>, Footer</li>
	/// <li>TermFST --&gt;
	/// <see cref="Lucene.Net.Util.Fst.FST{T}">FST&lt;long&gt;</see>
	/// </li>
	/// <li>Header --&gt;
	/// <see cref="Lucene.Net.Codecs.CodecUtil.WriteHeader(Lucene.Net.Store.DataOutput, string, int)
	/// 	">CodecHeader</see>
	/// </li>
	/// <li>Footer --&gt;
	/// <see cref="Lucene.Net.Codecs.CodecUtil.WriteFooter(Lucene.Net.Store.IndexOutput)
	/// 	">CodecFooter</see>
	/// </li>
	/// </ul>
	/// <p>Notes:</p>
	/// <ul>
	/// <li>
	/// Since terms are already sorted before writing to <a href="#Termblock">Term Block</a>,
	/// their ords can directly used to seek term metadata from term block.
	/// </li>
	/// </ul>
	/// <a name="Termblock" id="Termblock"></a>
	/// <h3>Term Block</h3>
	/// <p>
	/// The .tbk contains all the statistics and metadata for terms, along with field summary (e.g.
	/// per-field data like number of documents in current field). For each field, there are four blocks:
	/// <ul>
	/// <li>statistics bytes block: contains term statistics; </li>
	/// <li>metadata longs block: delta-encodes monotonic part of metadata; </li>
	/// <li>metadata bytes block: encodes other parts of metadata; </li>
	/// <li>skip block: contains skip data, to speed up metadata seeking and decoding</li>
	/// </ul>
	/// </p>
	/// <p>File Format:</p>
	/// <ul>
	/// <li>TermBlock(.tbk) --&gt; Header, <i>PostingsHeader</i>, FieldSummary, DirOffset</li>
	/// <li>FieldSummary --&gt; NumFields, &lt;FieldNumber, NumTerms, SumTotalTermFreq?, SumDocFreq,
	/// DocCount, LongsSize, DataBlock &gt; <sup>NumFields</sup>, Footer</li>
	/// <li>DataBlock --&gt; StatsBlockLength, MetaLongsBlockLength, MetaBytesBlockLength,
	/// SkipBlock, StatsBlock, MetaLongsBlock, MetaBytesBlock </li>
	/// <li>SkipBlock --&gt; &lt; StatsFPDelta, MetaLongsSkipFPDelta, MetaBytesSkipFPDelta,
	/// MetaLongsSkipDelta<sup>LongsSize</sup> &gt;<sup>NumTerms</sup>
	/// <li>StatsBlock --&gt; &lt; DocFreq[Same?], (TotalTermFreq-DocFreq) ? &gt; <sup>NumTerms</sup>
	/// <li>MetaLongsBlock --&gt; &lt; LongDelta<sup>LongsSize</sup>, BytesSize &gt; <sup>NumTerms</sup>
	/// <li>MetaBytesBlock --&gt; Byte <sup>MetaBytesBlockLength</sup>
	/// <li>Header --&gt;
	/// <see cref="Lucene.Net.Codecs.CodecUtil.WriteHeader(Lucene.Net.Store.DataOutput, string, int)
	/// 	">CodecHeader</see>
	/// </li>
	/// <li>DirOffset --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteLong(long)">Uint64</see>
	/// </li>
	/// <li>NumFields, FieldNumber, DocCount, DocFreq, LongsSize,
	/// FieldNumber, DocCount --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteVInt(int)">VInt</see>
	/// </li>
	/// <li>NumTerms, SumTotalTermFreq, SumDocFreq, StatsBlockLength, MetaLongsBlockLength, MetaBytesBlockLength,
	/// StatsFPDelta, MetaLongsSkipFPDelta, MetaBytesSkipFPDelta, MetaLongsSkipStart, TotalTermFreq,
	/// LongDelta,--&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteVLong(long)">VLong</see>
	/// </li>
	/// <li>Footer --&gt;
	/// <see cref="Lucene.Net.Codecs.CodecUtil.WriteFooter(Lucene.Net.Store.IndexOutput)
	/// 	">CodecFooter</see>
	/// </li>
	/// </ul>
	/// <p>Notes: </p>
	/// <ul>
	/// <li>
	/// The format of PostingsHeader and MetaBytes are customized by the specific postings implementation:
	/// they contain arbitrary per-file data (such as parameters or versioning information), and per-term data
	/// (non-monotonic ones like pulsed postings data).
	/// </li>
	/// <li>
	/// During initialization the reader will load all the blocks into memory. SkipBlock will be decoded, so that during seek
	/// term dict can lookup file pointers directly. StatsFPDelta, MetaLongsSkipFPDelta, etc. are file offset
	/// for every SkipInterval's term. MetaLongsSkipDelta is the difference from previous one, which indicates
	/// the value of preceding metadata longs for every SkipInterval's term.
	/// </li>
	/// <li>
	/// DocFreq is the count of documents which contain the term. TotalTermFreq is the total number of occurrences of the term.
	/// Usually these two values are the same for long tail terms, therefore one bit is stole from DocFreq to check this case,
	/// so that encoding of TotalTermFreq may be omitted.
	/// </li>
	/// </ul>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class FSTOrdTermsWriter : FieldsConsumer
	{
		internal static readonly string TERMS_INDEX_EXTENSION = "tix";

		internal static readonly string TERMS_BLOCK_EXTENSION = "tbk";

		internal static readonly string TERMS_CODEC_NAME = "FST_ORD_TERMS_DICT";

		public const int TERMS_VERSION_START = 0;

		public const int TERMS_VERSION_CHECKSUM = 1;

		public const int TERMS_VERSION_CURRENT = TERMS_VERSION_CHECKSUM;

		public const int SKIP_INTERVAL = 8;

		internal readonly PostingsWriterBase postingsWriter;

		internal readonly FieldInfos fieldInfos;

		internal readonly IList<FSTOrdTermsWriter.FieldMetaData> fields = new AList<FSTOrdTermsWriter.FieldMetaData
			>();

		internal IndexOutput blockOut = null;

		internal IndexOutput indexOut = null;

		/// <exception cref="System.IO.IOException"></exception>
		public FSTOrdTermsWriter(SegmentWriteState state, PostingsWriterBase postingsWriter
			)
		{
			string termsIndexFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name
				, state.segmentSuffix, TERMS_INDEX_EXTENSION);
			string termsBlockFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name
				, state.segmentSuffix, TERMS_BLOCK_EXTENSION);
			this.postingsWriter = postingsWriter;
			this.fieldInfos = state.fieldInfos;
			bool success = false;
			try
			{
				this.indexOut = state.directory.CreateOutput(termsIndexFileName, state.context);
				this.blockOut = state.directory.CreateOutput(termsBlockFileName, state.context);
				WriteHeader(indexOut);
				WriteHeader(blockOut);
				this.postingsWriter.Init(blockOut);
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(indexOut, blockOut);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override TermsConsumer AddField(FieldInfo field)
		{
			return new FSTOrdTermsWriter.TermsWriter(this, field);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			if (blockOut != null)
			{
				bool success = false;
				try
				{
					long blockDirStart = blockOut.GetFilePointer();
					// write field summary
					blockOut.WriteVInt(fields.Count);
					foreach (FSTOrdTermsWriter.FieldMetaData field in fields)
					{
						blockOut.WriteVInt(field.fieldInfo.number);
						blockOut.WriteVLong(field.numTerms);
						if (field.fieldInfo.GetIndexOptions() != FieldInfo.IndexOptions.DOCS_ONLY)
						{
							blockOut.WriteVLong(field.sumTotalTermFreq);
						}
						blockOut.WriteVLong(field.sumDocFreq);
						blockOut.WriteVInt(field.docCount);
						blockOut.WriteVInt(field.longsSize);
						blockOut.WriteVLong(field.statsOut.GetFilePointer());
						blockOut.WriteVLong(field.metaLongsOut.GetFilePointer());
						blockOut.WriteVLong(field.metaBytesOut.GetFilePointer());
						field.skipOut.WriteTo(blockOut);
						field.statsOut.WriteTo(blockOut);
						field.metaLongsOut.WriteTo(blockOut);
						field.metaBytesOut.WriteTo(blockOut);
						field.dict.Save(indexOut);
					}
					WriteTrailer(blockOut, blockDirStart);
					CodecUtil.WriteFooter(indexOut);
					CodecUtil.WriteFooter(blockOut);
					success = true;
				}
				finally
				{
					if (success)
					{
						IOUtils.Close(blockOut, indexOut, postingsWriter);
					}
					else
					{
						IOUtils.CloseWhileHandlingException(blockOut, indexOut, postingsWriter);
					}
					blockOut = null;
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

		private class FieldMetaData
		{
			public FieldInfo fieldInfo;

			public long numTerms;

			public long sumTotalTermFreq;

			public long sumDocFreq;

			public int docCount;

			public int longsSize;

			public FST<long> dict;

			public RAMOutputStream skipOut;

			public RAMOutputStream statsOut;

			public RAMOutputStream metaLongsOut;

			public RAMOutputStream metaBytesOut;
			// TODO: block encode each part 
			// vint encode next skip point (fully decoded when reading)
			// vint encode df, (ttf-df)
			// vint encode monotonic long[] and length for corresponding byte[]
			// generic byte[]
		}

		internal sealed class TermsWriter : TermsConsumer
		{
			private readonly Builder<long> builder;

			private readonly PositiveIntOutputs outputs;

			private readonly FieldInfo fieldInfo;

			private readonly int longsSize;

			private long numTerms;

			private readonly IntsRef scratchTerm = new IntsRef();

			private readonly RAMOutputStream statsOut = new RAMOutputStream();

			private readonly RAMOutputStream metaLongsOut = new RAMOutputStream();

			private readonly RAMOutputStream metaBytesOut = new RAMOutputStream();

			private readonly RAMOutputStream skipOut = new RAMOutputStream();

			private long lastBlockStatsFP;

			private long lastBlockMetaLongsFP;

			private long lastBlockMetaBytesFP;

			private long[] lastBlockLongs;

			private long[] lastLongs;

			private long lastMetaBytesFP;

			internal TermsWriter(FSTOrdTermsWriter _enclosing, FieldInfo fieldInfo)
			{
				this._enclosing = _enclosing;
				this.numTerms = 0;
				this.fieldInfo = fieldInfo;
				this.longsSize = this._enclosing.postingsWriter.SetField(fieldInfo);
				this.outputs = PositiveIntOutputs.GetSingleton();
				this.builder = new Builder<long>(FST.INPUT_TYPE.BYTE1, this.outputs);
				this.lastBlockStatsFP = 0;
				this.lastBlockMetaLongsFP = 0;
				this.lastBlockMetaBytesFP = 0;
				this.lastBlockLongs = new long[this.longsSize];
				this.lastLongs = new long[this.longsSize];
				this.lastMetaBytesFP = 0;
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
				if (this.numTerms > 0 && this.numTerms % FSTOrdTermsWriter.SKIP_INTERVAL == 0)
				{
					this.BufferSkip();
				}
				// write term meta data into fst
				long[] longs = new long[this.longsSize];
				long delta = stats.totalTermFreq - stats.docFreq;
				if (stats.totalTermFreq > 0)
				{
					if (delta == 0)
					{
						this.statsOut.WriteVInt(stats.docFreq << 1 | 1);
					}
					else
					{
						this.statsOut.WriteVInt(stats.docFreq << 1 | 0);
						this.statsOut.WriteVLong(stats.totalTermFreq - stats.docFreq);
					}
				}
				else
				{
					this.statsOut.WriteVInt(stats.docFreq);
				}
				BlockTermState state = this._enclosing.postingsWriter.NewTermState();
				state.docFreq = stats.docFreq;
				state.totalTermFreq = stats.totalTermFreq;
				this._enclosing.postingsWriter.FinishTerm(state);
				this._enclosing.postingsWriter.EncodeTerm(longs, this.metaBytesOut, this.fieldInfo
					, state, true);
				for (int i = 0; i < this.longsSize; i++)
				{
					this.metaLongsOut.WriteVLong(longs[i] - this.lastLongs[i]);
					this.lastLongs[i] = longs[i];
				}
				this.metaLongsOut.WriteVLong(this.metaBytesOut.GetFilePointer() - this.lastMetaBytesFP
					);
				this.builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(text, this.scratchTerm
					), this.numTerms);
				this.numTerms++;
				this.lastMetaBytesFP = this.metaBytesOut.GetFilePointer();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
			{
				if (this.numTerms > 0)
				{
					FSTOrdTermsWriter.FieldMetaData metadata = new FSTOrdTermsWriter.FieldMetaData();
					metadata.fieldInfo = this.fieldInfo;
					metadata.numTerms = this.numTerms;
					metadata.sumTotalTermFreq = sumTotalTermFreq;
					metadata.sumDocFreq = sumDocFreq;
					metadata.docCount = docCount;
					metadata.longsSize = this.longsSize;
					metadata.skipOut = this.skipOut;
					metadata.statsOut = this.statsOut;
					metadata.metaLongsOut = this.metaLongsOut;
					metadata.metaBytesOut = this.metaBytesOut;
					metadata.dict = this.builder.Finish();
					this._enclosing.fields.AddItem(metadata);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void BufferSkip()
			{
				this.skipOut.WriteVLong(this.statsOut.GetFilePointer() - this.lastBlockStatsFP);
				this.skipOut.WriteVLong(this.metaLongsOut.GetFilePointer() - this.lastBlockMetaLongsFP
					);
				this.skipOut.WriteVLong(this.metaBytesOut.GetFilePointer() - this.lastBlockMetaBytesFP
					);
				for (int i = 0; i < this.longsSize; i++)
				{
					this.skipOut.WriteVLong(this.lastLongs[i] - this.lastBlockLongs[i]);
				}
				this.lastBlockStatsFP = this.statsOut.GetFilePointer();
				this.lastBlockMetaLongsFP = this.metaLongsOut.GetFilePointer();
				this.lastBlockMetaBytesFP = this.metaBytesOut.GetFilePointer();
				System.Array.Copy(this.lastLongs, 0, this.lastBlockLongs, 0, this.longsSize);
			}

			private readonly FSTOrdTermsWriter _enclosing;
		}
	}
}
