/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Util.Fst;
using Sharpen;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>FST-based terms dictionary reader.</summary>
	/// <remarks>
	/// FST-based terms dictionary reader.
	/// The FST directly maps each term and its metadata,
	/// it is memory resident.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class FSTTermsReader : FieldsProducer
	{
		internal readonly SortedDictionary<string, FSTTermsReader.TermsReader> fields = new 
			SortedDictionary<string, FSTTermsReader.TermsReader>();

		internal readonly PostingsReaderBase postingsReader;

		internal readonly int version;

		/// <exception cref="System.IO.IOException"></exception>
		public FSTTermsReader(SegmentReadState state, PostingsReaderBase postingsReader)
		{
			//static boolean TEST = false;
			string termsFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state
				.segmentSuffix, FSTTermsWriter.TERMS_EXTENSION);
			this.postingsReader = postingsReader;
			IndexInput @in = state.directory.OpenInput(termsFileName, state.context);
			bool success = false;
			try
			{
				version = ReadHeader(@in);
				if (version >= FSTTermsWriter.TERMS_VERSION_CHECKSUM)
				{
					CodecUtil.ChecksumEntireFile(@in);
				}
				this.postingsReader.Init(@in);
				SeekDir(@in);
				FieldInfos fieldInfos = state.fieldInfos;
				int numFields = @in.ReadVInt();
				for (int i = 0; i < numFields; i++)
				{
					int fieldNumber = @in.ReadVInt();
					FieldInfo fieldInfo = fieldInfos.FieldInfo(fieldNumber);
					long numTerms = @in.ReadVLong();
					long sumTotalTermFreq = fieldInfo.GetIndexOptions() == FieldInfo.IndexOptions.DOCS_ONLY
						 ? -1 : @in.ReadVLong();
					long sumDocFreq = @in.ReadVLong();
					int docCount = @in.ReadVInt();
					int longsSize = @in.ReadVInt();
					FSTTermsReader.TermsReader current = new FSTTermsReader.TermsReader(this, fieldInfo
						, @in, numTerms, sumTotalTermFreq, sumDocFreq, docCount, longsSize);
					FSTTermsReader.TermsReader previous = fields.Put(fieldInfo.name, current);
					CheckFieldSummary(state.segmentInfo, @in, current, previous);
				}
				success = true;
			}
			finally
			{
				if (success)
				{
					IOUtils.Close(@in);
				}
				else
				{
					IOUtils.CloseWhileHandlingException(@in);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int ReadHeader(IndexInput @in)
		{
			return CodecUtil.CheckHeader(@in, FSTTermsWriter.TERMS_CODEC_NAME, FSTTermsWriter
				.TERMS_VERSION_START, FSTTermsWriter.TERMS_VERSION_CURRENT);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SeekDir(IndexInput @in)
		{
			if (version >= FSTTermsWriter.TERMS_VERSION_CHECKSUM)
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
		private void CheckFieldSummary(SegmentInfo info, IndexInput @in, FSTTermsReader.TermsReader
			 field, FSTTermsReader.TermsReader previous)
		{
			// #docs with field must be <= #docs
			if (field.docCount < 0 || field.docCount > info.GetDocCount())
			{
				throw new CorruptIndexException("invalid docCount: " + field.docCount + " maxDoc: "
					 + info.GetDocCount() + " (resource=" + @in + ")");
			}
			// #postings must be >= #docs with field
			if (field.sumDocFreq < field.docCount)
			{
				throw new CorruptIndexException("invalid sumDocFreq: " + field.sumDocFreq + " docCount: "
					 + field.docCount + " (resource=" + @in + ")");
			}
			// #positions must be >= #postings
			if (field.sumTotalTermFreq != -1 && field.sumTotalTermFreq < field.sumDocFreq)
			{
				throw new CorruptIndexException("invalid sumTotalTermFreq: " + field.sumTotalTermFreq
					 + " sumDocFreq: " + field.sumDocFreq + " (resource=" + @in + ")");
			}
			if (previous != null)
			{
				throw new CorruptIndexException("duplicate fields: " + field.fieldInfo.name + " (resource="
					 + @in + ")");
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

			internal readonly FST<FSTTermOutputs.TermData> dict;

			/// <exception cref="System.IO.IOException"></exception>
			internal TermsReader(FSTTermsReader _enclosing, FieldInfo fieldInfo, IndexInput @in
				, long numTerms, long sumTotalTermFreq, long sumDocFreq, int docCount, int longsSize
				)
			{
				this._enclosing = _enclosing;
				this.fieldInfo = fieldInfo;
				this.numTerms = numTerms;
				this.sumTotalTermFreq = sumTotalTermFreq;
				this.sumDocFreq = sumDocFreq;
				this.docCount = docCount;
				this.longsSize = longsSize;
				this.dict = new FST<FSTTermOutputs.TermData>(@in, new FSTTermOutputs(fieldInfo, longsSize
					));
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
				return new FSTTermsReader.TermsReader.SegmentTermsEnum(this);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm
				)
			{
				return new FSTTermsReader.TermsReader.IntersectTermsEnum(this, compiled, startTerm
					);
			}

			internal abstract class BaseTermsEnum : TermsEnum
			{
				internal BytesRef term;

				internal readonly BlockTermState state;

				internal FSTTermOutputs.TermData meta;

				internal ByteArrayDataInput bytesReader;

				// Only wraps common operations for PBF interact
				/// <summary>Decodes metadata into customized term state</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal abstract void DecodeMetaData();

				/// <exception cref="System.IO.IOException"></exception>
				public BaseTermsEnum(TermsReader _enclosing)
				{
					this._enclosing = _enclosing;
					this.state = this._enclosing._enclosing.postingsReader.NewTermState();
					this.bytesReader = new ByteArrayDataInput();
					this.term = null;
				}

				// NOTE: metadata will only be initialized in child class
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

			private sealed class SegmentTermsEnum : FSTTermsReader.TermsReader.BaseTermsEnum
			{
				internal readonly BytesRefFSTEnum<FSTTermOutputs.TermData> fstEnum;

				internal bool decoded;

				internal bool seekPending;

				/// <exception cref="System.IO.IOException"></exception>
				public SegmentTermsEnum(TermsReader _enclosing) : base(_enclosing)
				{
					this._enclosing = _enclosing;
					// Iterates through all terms in this field
					this.fstEnum = new BytesRefFSTEnum<FSTTermOutputs.TermData>(this._enclosing.dict);
					this.decoded = false;
					this.seekPending = false;
					this.meta = null;
				}

				public override IComparer<BytesRef> GetComparator()
				{
					return BytesRef.GetUTF8SortedAsUnicodeComparator();
				}

				// Let PBF decode metadata from long[] and byte[]
				/// <exception cref="System.IO.IOException"></exception>
				internal override void DecodeMetaData()
				{
					if (!this.decoded && !this.seekPending)
					{
						if (this.meta.bytes != null)
						{
							this.bytesReader.Reset(this.meta.bytes, 0, this.meta.bytes.Length);
						}
						this._enclosing._enclosing.postingsReader.DecodeTerm(this.meta.longs, this.bytesReader
							, this._enclosing.fieldInfo, this.state, true);
						this.decoded = true;
					}
				}

				// Update current enum according to FSTEnum
				internal void UpdateEnum(BytesRefFSTEnum.InputOutput<FSTTermOutputs.TermData> pair
					)
				{
					if (pair == null)
					{
						this.term = null;
					}
					else
					{
						this.term = pair.input;
						this.meta = pair.output;
						this.state.docFreq = this.meta.docFreq;
						this.state.totalTermFreq = this.meta.totalTermFreq;
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

			private sealed class IntersectTermsEnum : FSTTermsReader.TermsReader.BaseTermsEnum
			{
				internal bool decoded;

				internal bool pending;

				internal FSTTermsReader.TermsReader.IntersectTermsEnum.Frame[] stack;

				internal int level;

				internal int metaUpto;

				internal readonly FST<FSTTermOutputs.TermData> fst;

				internal readonly FST.BytesReader fstReader;

				internal readonly Outputs<FSTTermOutputs.TermData> fstOutputs;

				internal readonly ByteRunAutomaton fsa;

				private sealed class Frame
				{
					internal FST.Arc<FSTTermOutputs.TermData> fstArc;

					internal int fsaState;

					public Frame(IntersectTermsEnum _enclosing)
					{
						this._enclosing = _enclosing;
						// Iterates intersect result with automaton (cannot seek!)
						this.fstArc = new FST.Arc<FSTTermOutputs.TermData>();
						this.fsaState = -1;
					}

					public override string ToString()
					{
						return "arc=" + this.fstArc + " state=" + this.fsaState;
					}

					private readonly IntersectTermsEnum _enclosing;
				}

				/// <exception cref="System.IO.IOException"></exception>
				internal IntersectTermsEnum(TermsReader _enclosing, CompiledAutomaton compiled, BytesRef
					 startTerm) : base(_enclosing)
				{
					this._enclosing = _enclosing;
					//if (TEST) System.out.println("Enum init, startTerm=" + startTerm);
					this.fst = this._enclosing.dict;
					this.fstReader = this.fst.GetBytesReader();
					this.fstOutputs = this._enclosing.dict.outputs;
					this.fsa = compiled.runAutomaton;
					this.level = -1;
					this.stack = new FSTTermsReader.TermsReader.IntersectTermsEnum.Frame[16];
					for (int i = 0; i < this.stack.Length; i++)
					{
						this.stack[i] = new FSTTermsReader.TermsReader.IntersectTermsEnum.Frame(this);
					}
					FSTTermsReader.TermsReader.IntersectTermsEnum.Frame frame;
					frame = this.LoadVirtualFrame(this.NewFrame());
					this.level++;
					frame = this.LoadFirstFrame(this.NewFrame());
					this.PushFrame(frame);
					this.meta = null;
					this.metaUpto = 1;
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

				public override IComparer<BytesRef> GetComparator()
				{
					return BytesRef.GetUTF8SortedAsUnicodeComparator();
				}

				/// <exception cref="System.IO.IOException"></exception>
				internal override void DecodeMetaData()
				{
					//HM:revisit 
					//assert term != null;
					if (!this.decoded)
					{
						if (this.meta.bytes != null)
						{
							this.bytesReader.Reset(this.meta.bytes, 0, this.meta.bytes.Length);
						}
						this._enclosing._enclosing.postingsReader.DecodeTerm(this.meta.longs, this.bytesReader
							, this._enclosing.fieldInfo, this.state, true);
						this.decoded = true;
					}
				}

				/// <summary>Lazily accumulate meta data, when we got a accepted term</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal void LoadMetaData()
				{
					FST.Arc<FSTTermOutputs.TermData> last;
					FST.Arc<FSTTermOutputs.TermData> next;
					last = this.stack[this.metaUpto].fstArc;
					while (this.metaUpto != this.level)
					{
						this.metaUpto++;
						next = this.stack[this.metaUpto].fstArc;
						next.output = this.fstOutputs.Add(next.output, last.output);
						last = next;
					}
					if (last.IsFinal())
					{
						this.meta = this.fstOutputs.Add(last.output, last.nextFinalOutput);
					}
					else
					{
						this.meta = last.output;
					}
					this.state.docFreq = this.meta.docFreq;
					this.state.totalTermFreq = this.meta.totalTermFreq;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override TermsEnum.SeekStatus SeekCeil(BytesRef target)
				{
					this.decoded = false;
					this.term = this.DoSeekCeil(target);
					this.LoadMetaData();
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

				/// <exception cref="System.IO.IOException"></exception>
				public override BytesRef Next()
				{
					//if (TEST) System.out.println("Enum next()");
					if (this.pending)
					{
						this.pending = false;
						this.LoadMetaData();
						return this.term;
					}
					this.decoded = false;
					while (this.level > 0)
					{
						FSTTermsReader.TermsReader.IntersectTermsEnum.Frame frame = this.NewFrame();
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
					this.LoadMetaData();
					return this.term;
				}

				/// <exception cref="System.IO.IOException"></exception>
				private BytesRef DoSeekCeil(BytesRef target)
				{
					//if (TEST) System.out.println("Enum doSeekCeil()");
					FSTTermsReader.TermsReader.IntersectTermsEnum.Frame frame = null;
					int label;
					int upto = 0;
					int limit = target.length;
					while (upto < limit)
					{
						// to target prefix, or ceil label (rewind prefix)
						frame = this.NewFrame();
						label = target.bytes[upto] & unchecked((int)(0xff));
						frame = this.LoadCeilFrame(label, this.TopFrame(), frame);
						if (frame == null || frame.fstArc.label != label)
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
				internal FSTTermsReader.TermsReader.IntersectTermsEnum.Frame LoadVirtualFrame(FSTTermsReader.TermsReader.IntersectTermsEnum.Frame
					 frame)
				{
					frame.fstArc.output = this.fstOutputs.GetNoOutput();
					frame.fstArc.nextFinalOutput = this.fstOutputs.GetNoOutput();
					frame.fsaState = -1;
					return frame;
				}

				/// <summary>Load frame for start arc(node) on fst</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal FSTTermsReader.TermsReader.IntersectTermsEnum.Frame LoadFirstFrame(FSTTermsReader.TermsReader.IntersectTermsEnum.Frame
					 frame)
				{
					frame.fstArc = this.fst.GetFirstArc(frame.fstArc);
					frame.fsaState = this.fsa.GetInitialState();
					return frame;
				}

				/// <summary>Load frame for target arc(node) on fst</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal FSTTermsReader.TermsReader.IntersectTermsEnum.Frame LoadExpandFrame(FSTTermsReader.TermsReader.IntersectTermsEnum.Frame
					 top, FSTTermsReader.TermsReader.IntersectTermsEnum.Frame frame)
				{
					if (!this.CanGrow(top))
					{
						return null;
					}
					frame.fstArc = this.fst.ReadFirstRealTargetArc(top.fstArc.target, frame.fstArc, this
						.fstReader);
					frame.fsaState = this.fsa.Step(top.fsaState, frame.fstArc.label);
					//if (TEST) System.out.println(" loadExpand frame="+frame);
					if (frame.fsaState == -1)
					{
						return this.LoadNextFrame(top, frame);
					}
					return frame;
				}

				/// <summary>Load frame for sibling arc(node) on fst</summary>
				/// <exception cref="System.IO.IOException"></exception>
				internal FSTTermsReader.TermsReader.IntersectTermsEnum.Frame LoadNextFrame(FSTTermsReader.TermsReader.IntersectTermsEnum.Frame
					 top, FSTTermsReader.TermsReader.IntersectTermsEnum.Frame frame)
				{
					if (!this.CanRewind(frame))
					{
						return null;
					}
					while (!frame.fstArc.IsLast())
					{
						frame.fstArc = this.fst.ReadNextRealArc(frame.fstArc, this.fstReader);
						frame.fsaState = this.fsa.Step(top.fsaState, frame.fstArc.label);
						if (frame.fsaState != -1)
						{
							break;
						}
					}
					//if (TEST) System.out.println(" loadNext frame="+frame);
					if (frame.fsaState == -1)
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
				internal FSTTermsReader.TermsReader.IntersectTermsEnum.Frame LoadCeilFrame(int label
					, FSTTermsReader.TermsReader.IntersectTermsEnum.Frame top, FSTTermsReader.TermsReader.IntersectTermsEnum.Frame
					 frame)
				{
					FST.Arc<FSTTermOutputs.TermData> arc = frame.fstArc;
					arc = Lucene.Net.Util.Fst.Util.ReadCeilArc(label, this.fst, top.fstArc, arc
						, this.fstReader);
					if (arc == null)
					{
						return null;
					}
					frame.fsaState = this.fsa.Step(top.fsaState, arc.label);
					//if (TEST) System.out.println(" loadCeil frame="+frame);
					if (frame.fsaState == -1)
					{
						return this.LoadNextFrame(top, frame);
					}
					return frame;
				}

				internal bool IsAccept(FSTTermsReader.TermsReader.IntersectTermsEnum.Frame frame)
				{
					// reach a term both fst&fsa accepts
					return this.fsa.IsAccept(frame.fsaState) && frame.fstArc.IsFinal();
				}

				internal bool IsValid(FSTTermsReader.TermsReader.IntersectTermsEnum.Frame frame)
				{
					// reach a prefix both fst&fsa won't reject
					return frame.fsaState != -1;
				}

				internal bool CanGrow(FSTTermsReader.TermsReader.IntersectTermsEnum.Frame frame)
				{
					// can walk forward on both fst&fsa
					return frame.fsaState != -1 && FST.TargetHasArcs(frame.fstArc);
				}

				internal bool CanRewind(FSTTermsReader.TermsReader.IntersectTermsEnum.Frame frame
					)
				{
					// can jump to sibling
					return !frame.fstArc.IsLast();
				}

				internal void PushFrame(FSTTermsReader.TermsReader.IntersectTermsEnum.Frame frame
					)
				{
					this.term = this.Grow(frame.fstArc.label);
					this.level++;
				}

				//if (TEST) System.out.println("  term=" + term + " level=" + level);
				internal FSTTermsReader.TermsReader.IntersectTermsEnum.Frame PopFrame()
				{
					this.term = this.Shrink();
					this.level--;
					this.metaUpto = this.metaUpto > this.level ? this.level : this.metaUpto;
					//if (TEST) System.out.println("  term=" + term + " level=" + level);
					return this.stack[this.level + 1];
				}

				internal FSTTermsReader.TermsReader.IntersectTermsEnum.Frame NewFrame()
				{
					if (this.level + 1 == this.stack.Length)
					{
						FSTTermsReader.TermsReader.IntersectTermsEnum.Frame[] temp = new FSTTermsReader.TermsReader.IntersectTermsEnum.Frame
							[ArrayUtil.Oversize(this.level + 2, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
						System.Array.Copy(this.stack, 0, temp, 0, this.stack.Length);
						for (int i = this.stack.Length; i < temp.Length; i++)
						{
							temp[i] = new FSTTermsReader.TermsReader.IntersectTermsEnum.Frame(this);
						}
						this.stack = temp;
					}
					return this.stack[this.level + 1];
				}

				internal FSTTermsReader.TermsReader.IntersectTermsEnum.Frame TopFrame()
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

			private readonly FSTTermsReader _enclosing;
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
			foreach (FSTTermsReader.TermsReader r in fields.Values)
			{
				ramBytesUsed += r.dict == null ? 0 : r.dict.SizeInBytes();
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
