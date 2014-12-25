using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Util.Fst;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>FST-based terms dictionary reader.</summary>
	/// <remarks>
	/// FST-based terms dictionary reader.
	/// The FST index maps each term and its ord, and during seek
	/// the ord is used fetch metadata from a single block.
	/// The term dictionary is fully memory resident.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class FSTOrdTermsReader : FieldsProducer
	{
		internal const int INTERVAL = FSTOrdTermsWriter.SKIP_INTERVAL;

		internal readonly SortedDictionary<string, FSTOrdTermsReader.TermsReader> fields = 
			new SortedDictionary<string, FSTOrdTermsReader.TermsReader>();

		internal readonly PostingsReaderBase postingsReader;

		internal int version;

		/// <exception cref="System.IO.IOException"></exception>
		public FSTOrdTermsReader(SegmentReadState state, PostingsReaderBase postingsReader
			)
		{
			//static final boolean TEST = false;
			string termsIndexFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name
				, state.segmentSuffix, FSTOrdTermsWriter.TERMS_INDEX_EXTENSION);
			string termsBlockFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name
				, state.segmentSuffix, FSTOrdTermsWriter.TERMS_BLOCK_EXTENSION);
			this.postingsReader = postingsReader;
			ChecksumIndexInput indexIn = null;
			IndexInput blockIn = null;
			bool success = false;
			try
			{
				indexIn = state.directory.OpenChecksumInput(termsIndexFileName, state.context);
				blockIn = state.directory.OpenInput(termsBlockFileName, state.context);
				version = ReadHeader(indexIn);
				ReadHeader(blockIn);
				if (version >= FSTOrdTermsWriter.TERMS_VERSION_CHECKSUM)
				{
					CodecUtil.ChecksumEntireFile(blockIn);
				}
				this.postingsReader.Init(blockIn);
				SeekDir(blockIn);
				FieldInfos fieldInfos = state.fieldInfos;
				int numFields = blockIn.ReadVInt();
				for (int i = 0; i < numFields; i++)
				{
					FieldInfo fieldInfo = fieldInfos.FieldInfo(blockIn.ReadVInt());
					bool hasFreq = fieldInfo.GetIndexOptions() != FieldInfo.IndexOptions.DOCS_ONLY;
					long numTerms = blockIn.ReadVLong();
					long sumTotalTermFreq = hasFreq ? blockIn.ReadVLong() : -1;
					long sumDocFreq = blockIn.ReadVLong();
					int docCount = blockIn.ReadVInt();
					int longsSize = blockIn.ReadVInt();
					FST<long> index = new FST<long>(indexIn, PositiveIntOutputs.GetSingleton());
					FSTOrdTermsReader.TermsReader current = new FSTOrdTermsReader.TermsReader(this, fieldInfo
						, blockIn, numTerms, sumTotalTermFreq, sumDocFreq, docCount, longsSize, index);
					FSTOrdTermsReader.TermsReader previous = fields.Put(fieldInfo.name, current);
					CheckFieldSummary(state.segmentInfo, indexIn, blockIn, current, previous);
				}
				if (version >= FSTOrdTermsWriter.TERMS_VERSION_CHECKSUM)
				{
					CodecUtil.CheckFooter(indexIn);
				}
				else
				{
					CodecUtil.CheckEOF(indexIn);
				}
				success = true;
			}
			finally
			{
				if (success)
				{
					IOUtils.Close(indexIn, blockIn);
				}
				else
				{
					IOUtils.CloseWhileHandlingException(indexIn, blockIn);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int ReadHeader(IndexInput @in)
		{
			return CodecUtil.CheckHeader(@in, FSTOrdTermsWriter.TERMS_CODEC_NAME, FSTOrdTermsWriter
				.TERMS_VERSION_START, FSTOrdTermsWriter.TERMS_VERSION_CURRENT);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SeekDir(IndexInput @in)
		{
			if (version >= FSTOrdTermsWriter.TERMS_VERSION_CHECKSUM)
			{
				@in.Seek(@in.Length() - CodecUtil.FooterLength() - 8);
			}
			else
			{
				@in.Seek(@in.Length() - 8);
			}
			@in.Seek(@in.ReadLong());
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckFieldSummary(SegmentInfo info, IndexInput indexIn, IndexInput blockIn
			, FSTOrdTermsReader.TermsReader field, FSTOrdTermsReader.TermsReader previous)
		{
			// #docs with field must be <= #docs
			if (field.docCount < 0 || field.docCount > info.GetDocCount())
			{
				throw new CorruptIndexException("invalid docCount: " + field.docCount + " maxDoc: "
					 + info.GetDocCount() + " (resource=" + indexIn + ", " + blockIn + ")");
			}
			// #postings must be >= #docs with field
			if (field.sumDocFreq < field.docCount)
			{
				throw new CorruptIndexException("invalid sumDocFreq: " + field.sumDocFreq + " docCount: "
					 + field.docCount + " (resource=" + indexIn + ", " + blockIn + ")");
			}
			// #positions must be >= #postings
			if (field.sumTotalTermFreq != -1 && field.sumTotalTermFreq < field.sumDocFreq)
			{
				throw new CorruptIndexException("invalid sumTotalTermFreq: " + field.sumTotalTermFreq
					 + " sumDocFreq: " + field.sumDocFreq + " (resource=" + indexIn + ", " + blockIn
					 + ")");
			}
			if (previous != null)
			{
				throw new CorruptIndexException("duplicate fields: " + field.fieldInfo.name + " (resource="
					 + indexIn + ", " + blockIn + ")");
			}
		}

		public override Sharpen.Iterator<string> Iterator()
		{
			return Sharpen.Collections.UnmodifiableSet(fields.Keys).Iterator();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Index.Terms Terms(string field)
		{
			//HM:revisit 
			//assert field != null;
			return fields.Get(field);
		}

		public override int Size()
		{
			return fields.Count;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				IOUtils.Close(postingsReader);
			}
			finally
			{
				fields.Clear();
			}
		}

		internal sealed class TermsReader : Terms
		{
			internal readonly FieldInfo fieldInfo;

			internal readonly long numTerms;

			internal readonly long sumTotalTermFreq;

			internal readonly long sumDocFreq;

			internal readonly int docCount;

			internal readonly int longsSize;

			internal readonly FST<long> index;

			internal readonly int numSkipInfo;

			internal readonly long[] skipInfo;

			internal readonly byte[] statsBlock;

			internal readonly byte[] metaLongsBlock;

			internal readonly byte[] metaBytesBlock;

			/// <exception cref="System.IO.IOException"></exception>
			internal TermsReader(FSTOrdTermsReader _enclosing, FieldInfo fieldInfo, IndexInput
				 blockIn, long numTerms, long sumTotalTermFreq, long sumDocFreq, int docCount, int
				 longsSize, FST<long> index)
			{
				this._enclosing = _enclosing;
				this.fieldInfo = fieldInfo;
				this.numTerms = numTerms;
				this.sumTotalTermFreq = sumTotalTermFreq;
				this.sumDocFreq = sumDocFreq;
				this.docCount = docCount;
				this.longsSize = longsSize;
				this.index = index;
				//HM:revisit 
				//assert (numTerms & (~0xffffffffL)) == 0;
				int numBlocks = (int)(numTerms + FSTOrdTermsReader.INTERVAL - 1) / FSTOrdTermsReader
					.INTERVAL;
				this.numSkipInfo = longsSize + 3;
				this.skipInfo = new long[numBlocks * this.numSkipInfo];
				this.statsBlock = new byte[(int)blockIn.ReadVLong()];
				this.metaLongsBlock = new byte[(int)blockIn.ReadVLong()];
				this.metaBytesBlock = new byte[(int)blockIn.ReadVLong()];
				int last = 0;
				int next = 0;
				for (int i = 1; i < numBlocks; i++)
				{
					next = this.numSkipInfo * i;
					for (int j = 0; j < this.numSkipInfo; j++)
					{
						this.skipInfo[next + j] = this.skipInfo[last + j] + blockIn.ReadVLong();
					}
					last = next;
				}
				blockIn.ReadBytes(this.statsBlock, 0, this.statsBlock.Length);
				blockIn.ReadBytes(this.metaLongsBlock, 0, this.metaLongsBlock.Length);
				blockIn.ReadBytes(this.metaBytesBlock, 0, this.metaBytesBlock.Length);
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}

			public override bool HasFreqs()
			{
				return this.fieldInfo.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS
					) >= 0;
			}

			public override bool HasOffsets()
			{
				return this.fieldInfo.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					) >= 0;
			}

			public override bool HasPositions()
			{
				return this.fieldInfo.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) >= 0;
			}

			public override bool HasPayloads()
			{
				return this.fieldInfo.HasPayloads();
			}

			public override long Size()
			{
				return this.numTerms;
			}

			public override long GetSumTotalTermFreq()
			{
				return this.sumTotalTermFreq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long GetSumDocFreq()
			{
				return this.sumDocFreq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int GetDocCount()
			{
				return this.docCount;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum Iterator(TermsEnum reuse)
			{
				return new FSTOrdTermsReader.TermsReader.SegmentTermsEnum(this);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm
				)
			{
				return new FSTOrdTermsReader.TermsReader.IntersectTermsEnum(this, compiled, startTerm
					);
			}

			internal abstract class BaseTermsEnum : TermsEnum
			{
				internal BytesRef term;

				internal long ord;

				internal readonly BlockTermState state;

				internal readonly ByteArrayDataInput statsReader = new ByteArrayDataInput();

				internal readonly ByteArrayDataInput metaLongsReader = new ByteArrayDataInput();

				internal readonly ByteArrayDataInput metaBytesReader = new ByteArrayDataInput();

				internal int statsBlockOrd;

				internal int metaBlockOrd;

				internal long[][] longs;

				internal int[] bytesStart;

				internal int[] bytesLength;

				internal int[] docFreq;

				internal long[] totalTermFreq;

				/// <exception cref="System.IO.IOException"></exception>
				public BaseTermsEnum(TermsReader _enclosing)
				{
					this._enclosing = _enclosing;
					// Only wraps common operations for PBF interact
					this.state = this._enclosing._enclosing.postingsReader.NewTermState();
					this.term = null;
					this.statsReader.Reset(this._enclosing.statsBlock);
					this.metaLongsReader.Reset(this._enclosing.metaLongsBlock);
					this.metaBytesReader.Reset(this._enclosing.metaBytesBlock);
					this.longs = new long[][] { new long[this._enclosing.longsSize], new long[this._enclosing
						.longsSize], new long[this._enclosing.longsSize], new long[this._enclosing.longsSize
						], new long[this._enclosing.longsSize], new long[this._enclosing.longsSize], new 
						long[this._enclosing.longsSize], new long[this._enclosing.longsSize] };
					this.bytesStart = new int[FSTOrdTermsReader.INTERVAL];
					this.bytesLength = new int[FSTOrdTermsReader.INTERVAL];
					this.docFreq = new int[FSTOrdTermsReader.INTERVAL];
					this.totalTermFreq = new long[FSTOrdTermsReader.INTERVAL];
					this.statsBlockOrd = -1;
					this.metaBlockOrd = -1;
					if (!this._enclosing.HasFreqs())
					{
						Arrays.Fill(this.totalTermFreq, -1);
					}
				}

				public override IComparer<BytesRef> GetComparator()
				{
					return BytesRef.GetUTF8SortedAsUnicodeComparator();
				}

				/// <summary>Decodes stats data into term state</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal virtual void DecodeStats()
				{
					int upto = (int)this.ord % FSTOrdTermsReader.INTERVAL;
					int oldBlockOrd = this.statsBlockOrd;
					this.statsBlockOrd = (int)this.ord / FSTOrdTermsReader.INTERVAL;
					if (oldBlockOrd != this.statsBlockOrd)
					{
						this.RefillStats();
					}
					this.state.docFreq = this.docFreq[upto];
					this.state.totalTermFreq = this.totalTermFreq[upto];
				}

				/// <summary>Let PBF decode metadata</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal virtual void DecodeMetaData()
				{
					int upto = (int)this.ord % FSTOrdTermsReader.INTERVAL;
					int oldBlockOrd = this.metaBlockOrd;
					this.metaBlockOrd = (int)this.ord / FSTOrdTermsReader.INTERVAL;
					if (this.metaBlockOrd != oldBlockOrd)
					{
						this.RefillMetadata();
					}
					this.metaBytesReader.SetPosition(this.bytesStart[upto]);
					this._enclosing._enclosing.postingsReader.DecodeTerm(this.longs[upto], this.metaBytesReader
						, this._enclosing.fieldInfo, this.state, true);
				}

				/// <summary>Load current stats shard</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal void RefillStats()
				{
					int offset = this.statsBlockOrd * this._enclosing.numSkipInfo;
					int statsFP = (int)this._enclosing.skipInfo[offset];
					this.statsReader.SetPosition(statsFP);
					for (int i = 0; i < FSTOrdTermsReader.INTERVAL && !this.statsReader.Eof(); i++)
					{
						int code = this.statsReader.ReadVInt();
						if (this._enclosing.HasFreqs())
						{
							this.docFreq[i] = ((int)(((uint)code) >> 1));
							if ((code & 1) == 1)
							{
								this.totalTermFreq[i] = this.docFreq[i];
							}
							else
							{
								this.totalTermFreq[i] = this.docFreq[i] + this.statsReader.ReadVLong();
							}
						}
						else
						{
							this.docFreq[i] = code;
						}
					}
				}

				/// <summary>Load current metadata shard</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal void RefillMetadata()
				{
					int offset = this.metaBlockOrd * this._enclosing.numSkipInfo;
					int metaLongsFP = (int)this._enclosing.skipInfo[offset + 1];
					int metaBytesFP = (int)this._enclosing.skipInfo[offset + 2];
					this.metaLongsReader.SetPosition(metaLongsFP);
					for (int j = 0; j < this._enclosing.longsSize; j++)
					{
						this.longs[0][j] = this._enclosing.skipInfo[offset + 3 + j] + this.metaLongsReader
							.ReadVLong();
					}
					this.bytesStart[0] = metaBytesFP;
					this.bytesLength[0] = (int)this.metaLongsReader.ReadVLong();
					for (int i = 1; i < FSTOrdTermsReader.INTERVAL && !this.metaLongsReader.Eof(); i++)
					{
						for (int j_1 = 0; j_1 < this._enclosing.longsSize; j_1++)
						{
							this.longs[i][j_1] = this.longs[i - 1][j_1] + this.metaLongsReader.ReadVLong();
						}
						this.bytesStart[i] = this.bytesStart[i - 1] + this.bytesLength[i - 1];
						this.bytesLength[i] = (int)this.metaLongsReader.ReadVLong();
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override Lucene.Net.Index.TermState TermState()
				{
					this.DecodeMetaData();
					return this.state.Clone();
				}

				public override BytesRef Term()
				{
					return this.term;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override int DocFreq()
				{
					return this.state.docFreq;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override long TotalTermFreq()
				{
					return this.state.totalTermFreq;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
				{
					this.DecodeMetaData();
					return this._enclosing._enclosing.postingsReader.Docs(this._enclosing.fieldInfo, 
						this.state, liveDocs, reuse, flags);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
					 reuse, int flags)
				{
					if (!this._enclosing.HasPositions())
					{
						return null;
					}
					this.DecodeMetaData();
					return this._enclosing._enclosing.postingsReader.DocsAndPositions(this._enclosing
						.fieldInfo, this.state, liveDocs, reuse, flags);
				}

				// TODO: this can be achieved by making use of Util.getByOutput()
				//           and should have related tests
				/// <exception cref="System.IO.IOException"></exception>
				public override void SeekExact(long ord)
				{
					throw new NotSupportedException();
				}

				public override long Ord()
				{
					throw new NotSupportedException();
				}

				private readonly TermsReader _enclosing;
			}

			private sealed class SegmentTermsEnum : FSTOrdTermsReader.TermsReader.BaseTermsEnum
			{
				internal readonly BytesRefFSTEnum<long> fstEnum;

				internal bool decoded;

				internal bool seekPending;

				/// <exception cref="System.IO.IOException"></exception>
				public SegmentTermsEnum(TermsReader _enclosing) : base(_enclosing)
				{
					this._enclosing = _enclosing;
					// Iterates through all terms in this field
					this.fstEnum = new BytesRefFSTEnum<long>(this._enclosing.index);
					this.decoded = false;
					this.seekPending = false;
				}

				/// <exception cref="System.IO.IOException"></exception>
				internal override void DecodeMetaData()
				{
					if (!this.decoded && !this.seekPending)
					{
						base.DecodeMetaData();
						this.decoded = true;
					}
				}

				// Update current enum according to FSTEnum
				/// <exception cref="System.IO.IOException"></exception>
				internal void UpdateEnum(BytesRefFSTEnum.InputOutput<long> pair)
				{
					if (pair == null)
					{
						this.term = null;
					}
					else
					{
						this.term = pair.input;
						this.ord = pair.output;
						this.DecodeStats();
					}
					this.decoded = false;
					this.seekPending = false;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override BytesRef Next()
				{
					if (this.seekPending)
					{
						// previously positioned, but termOutputs not fetched
						this.seekPending = false;
						TermsEnum.SeekStatus status = this.SeekCeil(this.term);
					}
					//HM:revisit 
					//assert status == SeekStatus.FOUND;  // must positioned on valid term
					this.UpdateEnum(this.fstEnum.Next());
					return this.term;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override bool SeekExact(BytesRef target)
				{
					this.UpdateEnum(this.fstEnum.SeekExact(target));
					return this.term != null;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override TermsEnum.SeekStatus SeekCeil(BytesRef target)
				{
					this.UpdateEnum(this.fstEnum.SeekCeil(target));
					if (this.term == null)
					{
						return TermsEnum.SeekStatus.END;
					}
					else
					{
						return this.term.Equals(target) ? TermsEnum.SeekStatus.FOUND : TermsEnum.SeekStatus
							.NOT_FOUND;
					}
				}

				public override void SeekExact(BytesRef target, TermState otherState)
				{
					if (!target.Equals(this.term))
					{
						this.state.CopyFrom(otherState);
						this.term = BytesRef.DeepCopyOf(target);
						this.seekPending = true;
					}
				}

				private readonly TermsReader _enclosing;
			}

			private sealed class IntersectTermsEnum : FSTOrdTermsReader.TermsReader.BaseTermsEnum
			{
				internal bool decoded;

				internal bool pending;

				internal FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame[] stack;

				internal int level;

				internal readonly FST<long> fst;

				internal readonly FST.BytesReader fstReader;

				internal readonly Outputs<long> fstOutputs;

				internal readonly ByteRunAutomaton fsa;

				private sealed class Frame
				{
					internal FST.Arc<long> arc;

					internal int state;

					public Frame(IntersectTermsEnum _enclosing)
					{
						this._enclosing = _enclosing;
						// Iterates intersect result with automaton (cannot seek!)
						this.arc = new FST.Arc<long>();
						this.state = -1;
					}

					public override string ToString()
					{
						return "arc=" + this.arc + " state=" + this.state;
					}

					private readonly IntersectTermsEnum _enclosing;
				}

				/// <exception cref="System.IO.IOException"></exception>
				internal IntersectTermsEnum(TermsReader _enclosing, CompiledAutomaton compiled, BytesRef
					 startTerm) : base(_enclosing)
				{
					this._enclosing = _enclosing;
					//if (TEST) System.out.println("Enum init, startTerm=" + startTerm);
					this.fst = this._enclosing.index;
					this.fstReader = this.fst.GetBytesReader();
					this.fstOutputs = this._enclosing.index.outputs;
					this.fsa = compiled.runAutomaton;
					this.level = -1;
					this.stack = new FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame[16];
					for (int i = 0; i < this.stack.Length; i++)
					{
						this.stack[i] = new FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame(this);
					}
					FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame frame;
					frame = this.LoadVirtualFrame(this.NewFrame());
					this.level++;
					frame = this.LoadFirstFrame(this.NewFrame());
					this.PushFrame(frame);
					this.decoded = false;
					this.pending = false;
					if (startTerm == null)
					{
						this.pending = this.IsAccept(this.TopFrame());
					}
					else
					{
						this.DoSeekCeil(startTerm);
						this.pending = !startTerm.Equals(this.term) && this.IsValid(this.TopFrame()) && this
							.IsAccept(this.TopFrame());
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				internal override void DecodeMetaData()
				{
					if (!this.decoded)
					{
						base.DecodeMetaData();
						this.decoded = true;
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				internal override void DecodeStats()
				{
					FST.Arc<long> arc = this.TopFrame().arc;
					//HM:revisit 
					//assert arc.nextFinalOutput == fstOutputs.getNoOutput();
					this.ord = arc.output;
					base.DecodeStats();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override TermsEnum.SeekStatus SeekCeil(BytesRef target)
				{
					throw new NotSupportedException();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override BytesRef Next()
				{
					//if (TEST) System.out.println("Enum next()");
					if (this.pending)
					{
						this.pending = false;
						this.DecodeStats();
						return this.term;
					}
					this.decoded = false;
					while (this.level > 0)
					{
						FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame frame = this.NewFrame();
						if (this.LoadExpandFrame(this.TopFrame(), frame) != null)
						{
							// has valid target
							this.PushFrame(frame);
							if (this.IsAccept(frame))
							{
								// gotcha
								break;
							}
							continue;
						}
						// check next target
						frame = this.PopFrame();
						while (this.level > 0)
						{
							if (this.LoadNextFrame(this.TopFrame(), frame) != null)
							{
								// has valid sibling 
								this.PushFrame(frame);
								if (this.IsAccept(frame))
								{
									// gotcha
									goto DFS_break;
								}
								goto DFS_continue;
							}
							// check next target 
							frame = this.PopFrame();
						}
						return null;
DFS_continue: ;
					}
DFS_break: ;
					this.DecodeStats();
					return this.term;
				}

				/// <exception cref="System.IO.IOException"></exception>
				internal BytesRef DoSeekCeil(BytesRef target)
				{
					//if (TEST) System.out.println("Enum doSeekCeil()");
					FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame frame = null;
					int label;
					int upto = 0;
					int limit = target.length;
					while (upto < limit)
					{
						// to target prefix, or ceil label (rewind prefix)
						frame = this.NewFrame();
						label = target.bytes[upto] & unchecked((int)(0xff));
						frame = this.LoadCeilFrame(label, this.TopFrame(), frame);
						if (frame == null || frame.arc.label != label)
						{
							break;
						}
						//HM:revisit 
						//assert isValid(frame);  // target must be fetched from automaton
						this.PushFrame(frame);
						upto++;
					}
					if (upto == limit)
					{
						// got target
						return this.term;
					}
					if (frame != null)
					{
						// got larger term('s prefix)
						this.PushFrame(frame);
						return this.IsAccept(frame) ? this.term : this.Next();
					}
					while (this.level > 0)
					{
						// got target's prefix, advance to larger term
						frame = this.PopFrame();
						while (this.level > 0 && !this.CanRewind(frame))
						{
							frame = this.PopFrame();
						}
						if (this.LoadNextFrame(this.TopFrame(), frame) != null)
						{
							this.PushFrame(frame);
							return this.IsAccept(frame) ? this.term : this.Next();
						}
					}
					return null;
				}

				/// <summary>Virtual frame, never pop</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame LoadVirtualFrame(
					FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame frame)
				{
					frame.arc.output = this.fstOutputs.GetNoOutput();
					frame.arc.nextFinalOutput = this.fstOutputs.GetNoOutput();
					frame.state = -1;
					return frame;
				}

				/// <summary>Load frame for start arc(node) on fst</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame LoadFirstFrame(FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame
					 frame)
				{
					frame.arc = this.fst.GetFirstArc(frame.arc);
					frame.state = this.fsa.GetInitialState();
					return frame;
				}

				/// <summary>Load frame for target arc(node) on fst</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame LoadExpandFrame(FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame
					 top, FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame frame)
				{
					if (!this.CanGrow(top))
					{
						return null;
					}
					frame.arc = this.fst.ReadFirstRealTargetArc(top.arc.target, frame.arc, this.fstReader
						);
					frame.state = this.fsa.Step(top.state, frame.arc.label);
					//if (TEST) System.out.println(" loadExpand frame="+frame);
					if (frame.state == -1)
					{
						return this.LoadNextFrame(top, frame);
					}
					return frame;
				}

				/// <summary>Load frame for sibling arc(node) on fst</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame LoadNextFrame(FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame
					 top, FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame frame)
				{
					if (!this.CanRewind(frame))
					{
						return null;
					}
					while (!frame.arc.IsLast())
					{
						frame.arc = this.fst.ReadNextRealArc(frame.arc, this.fstReader);
						frame.state = this.fsa.Step(top.state, frame.arc.label);
						if (frame.state != -1)
						{
							break;
						}
					}
					//if (TEST) System.out.println(" loadNext frame="+frame);
					if (frame.state == -1)
					{
						return null;
					}
					return frame;
				}

				/// <summary>
				/// Load frame for target arc(node) on fst, so that
				/// arc.label &gt;= label and !fsa.reject(arc.label)
				/// </summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame LoadCeilFrame(int
					 label, FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame top, FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame
					 frame)
				{
					FST.Arc<long> arc = frame.arc;
					arc = Lucene.Net.Util.Fst.Util.ReadCeilArc(label, this.fst, top.arc, arc, 
						this.fstReader);
					if (arc == null)
					{
						return null;
					}
					frame.state = this.fsa.Step(top.state, arc.label);
					//if (TEST) System.out.println(" loadCeil frame="+frame);
					if (frame.state == -1)
					{
						return this.LoadNextFrame(top, frame);
					}
					return frame;
				}

				internal bool IsAccept(FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame frame
					)
				{
					// reach a term both fst&fsa accepts
					return this.fsa.IsAccept(frame.state) && frame.arc.IsFinal();
				}

				internal bool IsValid(FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame frame
					)
				{
					// reach a prefix both fst&fsa won't reject
					return frame.state != -1;
				}

				internal bool CanGrow(FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame frame
					)
				{
					// can walk forward on both fst&fsa
					return frame.state != -1 && FST.TargetHasArcs(frame.arc);
				}

				internal bool CanRewind(FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame frame
					)
				{
					// can jump to sibling
					return !frame.arc.IsLast();
				}

				internal void PushFrame(FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame frame
					)
				{
					FST.Arc<long> arc = frame.arc;
					arc.output = this.fstOutputs.Add(this.TopFrame().arc.output, arc.output);
					this.term = this.Grow(arc.label);
					this.level++;
				}

				//HM:revisit 
				//assert frame == stack[level];
				internal FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame PopFrame()
				{
					this.term = this.Shrink();
					return this.stack[this.level--];
				}

				internal FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame NewFrame()
				{
					if (this.level + 1 == this.stack.Length)
					{
						FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame[] temp = new FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame
							[ArrayUtil.Oversize(this.level + 2, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
						System.Array.Copy(this.stack, 0, temp, 0, this.stack.Length);
						for (int i = this.stack.Length; i < temp.Length; i++)
						{
							temp[i] = new FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame(this);
						}
						this.stack = temp;
					}
					return this.stack[this.level + 1];
				}

				internal FSTOrdTermsReader.TermsReader.IntersectTermsEnum.Frame TopFrame()
				{
					return this.stack[this.level];
				}

				internal BytesRef Grow(int label)
				{
					if (this.term == null)
					{
						this.term = new BytesRef(new byte[16], 0, 0);
					}
					else
					{
						if (this.term.length == this.term.bytes.Length)
						{
							this.term.Grow(this.term.length + 1);
						}
						this.term.bytes[this.term.length++] = unchecked((byte)label);
					}
					return this.term;
				}

				internal BytesRef Shrink()
				{
					if (this.term.length == 0)
					{
						this.term = null;
					}
					else
					{
						this.term.length--;
					}
					return this.term;
				}

				private readonly TermsReader _enclosing;
			}

			private readonly FSTOrdTermsReader _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void Walk<T>(FST<T> fst)
		{
			AList<FST.Arc<T>> queue = new AList<FST.Arc<T>>();
			BitSet seen = new BitSet();
			FST.BytesReader reader = fst.GetBytesReader();
			FST.Arc<T> startArc = fst.GetFirstArc(new FST.Arc<T>());
			queue.AddItem(startArc);
			while (!queue.IsEmpty())
			{
				FST.Arc<T> arc = queue.Remove(0);
				long node = arc.target;
				//System.out.println(arc);
				if (FST.TargetHasArcs(arc) && !seen.Get((int)node))
				{
					seen.Set((int)node);
					fst.ReadFirstRealTargetArc(node, arc, reader);
					while (true)
					{
						queue.AddItem(new FST.Arc<T>().CopyFrom(arc));
						if (arc.IsLast())
						{
							break;
						}
						else
						{
							fst.ReadNextRealArc(arc, reader);
						}
					}
				}
			}
		}

		public override long RamBytesUsed()
		{
			long ramBytesUsed = 0;
			foreach (FSTOrdTermsReader.TermsReader r in fields.Values)
			{
				if (r.index != null)
				{
					ramBytesUsed += r.index.SizeInBytes();
					ramBytesUsed += RamUsageEstimator.SizeOf(r.metaBytesBlock);
					ramBytesUsed += RamUsageEstimator.SizeOf(r.metaLongsBlock);
					ramBytesUsed += RamUsageEstimator.SizeOf(r.skipInfo);
					ramBytesUsed += RamUsageEstimator.SizeOf(r.statsBlock);
				}
			}
			return ramBytesUsed;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CheckIntegrity()
		{
			postingsReader.CheckIntegrity();
		}
	}
}
