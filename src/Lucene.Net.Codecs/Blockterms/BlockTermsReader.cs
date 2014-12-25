using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Blockterms
{
	/// <summary>
	/// Handles a terms dict, but decouples all details of
	/// doc/freqs/positions reading to an instance of
	/// <see cref="Lucene.Net.Codecs.PostingsReaderBase">Lucene.Net.Codecs.PostingsReaderBase
	/// 	</see>
	/// .  This class is reusable for
	/// codecs that use a different format for
	/// docs/freqs/positions (though codecs are also free to
	/// make their own terms dict impl).
	/// <p>This class also interacts with an instance of
	/// <see cref="TermsIndexReaderBase">TermsIndexReaderBase</see>
	/// , to abstract away the specific
	/// implementation of the terms dict index.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class BlockTermsReader : FieldsProducer
	{
		private readonly IndexInput @in;

		private readonly PostingsReaderBase postingsReader;

		private readonly SortedDictionary<string, FieldReader> fields = new SortedDictionary<string, FieldReader>();

		private TermsIndexReaderBase indexReader;

		private long dirOffset;

		private readonly int version;

		private class FieldAndTerm : DoubleBarrelLRUCache.CloneableKey
		{
			internal string field;

			internal BytesRef term;

			public FieldAndTerm()
			{
			}

			public FieldAndTerm(FieldAndTerm other)
			{
				// Open input to the main terms dict file (_X.tis)
				// Reads the terms dict entries, to gather state to
				// produce DocsEnum on demand
				// Reads the terms index
				// keeps the dirStart offset
				// Used as key for the terms cache
				field = other.field;
				term = BytesRef.DeepCopyOf(other.term);
			}

			public override bool Equals(object _other)
			{
				BlockTermsReader.FieldAndTerm other = (FieldAndTerm)_other;
				return other.field.Equals(field) && term.BytesEquals(other.term);
			}

			public override DoubleBarrelLRUCache.CloneableKey Clone()
			{
				return new FieldAndTerm(this);
			}

			public override int GetHashCode()
			{
				return field.GetHashCode() * 31 + term.GetHashCode();
			}
		}

		
		public BlockTermsReader(TermsIndexReaderBase indexReader, Directory dir, FieldInfos
			 fieldInfos, SegmentInfo info, PostingsReaderBase postingsReader, IOContext context
			, string segmentSuffix)
		{
			// private String segment;
			this.postingsReader = postingsReader;
			// this.segment = segment;
			@in = dir.OpenInput(IndexFileNames.SegmentFileName(info.name, segmentSuffix, BlockTermsWriter
				.TERMS_EXTENSION), context);
			bool success = false;
			try
			{
				version = ReadHeader(@in);
				// Have PostingsReader init itself
				postingsReader.Init(@in);
				// Read per-field details
				SeekDir(@in, dirOffset);
				int numFields = @in.ReadVInt();
				if (numFields < 0)
				{
					throw new CorruptIndexException("invalid number of fields: " + numFields + " (resource="
						 + @in + ")");
				}
				for (int i = 0; i < numFields; i++)
				{
					int field = @in.ReadVInt();
					long numTerms = @in.ReadVLong();
					//HM:revisit 
					//assert numTerms >= 0;
					long termsStartPointer = @in.ReadVLong();
					FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
					long sumTotalTermFreq = fieldInfo.IndexOptionsValue.GetValueOrDefault() == FieldInfo.IndexOptions.DOCS_ONLY
						 ? -1 : @in.ReadVLong();
					long sumDocFreq = @in.ReadVLong();
					int docCount = @in.ReadVInt();
					int longsSize = version >= BlockTermsWriter.VERSION_META_ARRAY ? @in.ReadVInt() : 
						0;
					if (docCount < 0 || docCount > info.DocCount)
					{
						// #docs with field must be <= #docs
						throw new CorruptIndexException("invalid docCount: " + docCount + " maxDoc: " + info.DocCount + " (resource=" + @in + ")");
					}
					if (sumDocFreq < docCount)
					{
						// #postings must be >= #docs with field
						throw new CorruptIndexException("invalid sumDocFreq: " + sumDocFreq + " docCount: "
							 + docCount + " (resource=" + @in + ")");
					}
					if (sumTotalTermFreq != -1 && sumTotalTermFreq < sumDocFreq)
					{
						// #positions must be >= #postings
						throw new CorruptIndexException("invalid sumTotalTermFreq: " + sumTotalTermFreq +
							 " sumDocFreq: " + sumDocFreq + " (resource=" + @in + ")");
					}
					FieldReader previous = fields[fieldInfo.name] = new FieldReader
						(this, fieldInfo, numTerms, termsStartPointer, sumTotalTermFreq, sumDocFreq, docCount
						, longsSize);
					if (previous != null)
					{
						throw new CorruptIndexException("duplicate fields: " + fieldInfo.name + " (resource="
							 + @in + ")");
					}
				}
				success = true;
			}
			finally
			{
				if (!success)
				{
					@in.Dispose();
				}
			}
			this.indexReader = indexReader;
		}

		
		private int ReadHeader(IndexInput input)
		{
			int version = CodecUtil.CheckHeader(input, BlockTermsWriter.CODEC_NAME, BlockTermsWriter
				.VERSION_START, BlockTermsWriter.VERSION_CURRENT);
			if (version < BlockTermsWriter.VERSION_APPEND_ONLY)
			{
				dirOffset = input.ReadLong();
			}
			return version;
		}

		
		private void SeekDir(IndexInput input, long dirOffset)
		{
			if (version >= BlockTermsWriter.VERSION_CHECKSUM)
			{
				input.Seek(input.Length - CodecUtil.FooterLength() - 8);
				dirOffset = input.ReadLong();
			}
			else
			{
				if (version >= BlockTermsWriter.VERSION_APPEND_ONLY)
				{
					input.Seek(input.Length - 8);
					dirOffset = input.ReadLong();
				}
			}
			input.Seek(dirOffset);
		}


	    protected override void Dispose(bool disposing)
		{
			try
			{
				try
				{
					if (indexReader != null)
					{
						indexReader.Close();
					}
				}
				finally
				{
					// null so if an app hangs on to us (ie, we are not
					// GCable, despite being closed) we still free most
					// ram
					indexReader = null;
					if (@in != null)
					{
						@in.Dispose();
					}
				}
			}
			finally
			{
				if (postingsReader != null)
				{
					postingsReader.Dispose();
				}
			}
		}

		public override IEnumerator<string> GetEnumerator()
		{
			return fields.Keys.GetEnumerator();
		}

		
		public override Terms Terms(string field)
		{
			Debug.Assert(field != null);
			return fields[field];
		}

		public override int Size
		{
		    get { return fields.Count; }
		}

		private class FieldReader : Terms
		{
			internal readonly long numTerms;

			internal readonly FieldInfo fieldInfo;

			internal readonly long termsStartPointer;

			internal readonly long sumTotalTermFreq;

			internal readonly long sumDocFreq;

			internal readonly int docCount;

			internal readonly int longsSize;

			internal FieldReader(BlockTermsReader _enclosing, FieldInfo fieldInfo, long numTerms
				, long termsStartPointer, long sumTotalTermFreq, long sumDocFreq, int docCount, 
				int longsSize)
			{
				this._enclosing = _enclosing;
				//HM:revisit
				//
				//HM:revisit 
				//assert numTerms > 0;
				this.fieldInfo = fieldInfo;
				this.numTerms = numTerms;
				this.termsStartPointer = termsStartPointer;
				this.sumTotalTermFreq = sumTotalTermFreq;
				this.sumDocFreq = sumDocFreq;
				this.docCount = docCount;
				this.longsSize = longsSize;
			}

			public override IComparer<BytesRef> Comparator
			{
			    get { return BytesRef.UTF8SortedAsUnicodeComparer; }
			}

			
			public override TermsEnum Iterator(TermsEnum reuse)
			{
				return new SegmentTermsEnum(this);
			}

			public override bool HasFreqs
			{
			    get
			    {
			        return this.fieldInfo.IndexOptionsValue.GetValueOrDefault().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS
			            ) >= 0;
			    }
			}

			public override bool HasOffsets
			{
			    get
			    {
			        return this.fieldInfo.IndexOptionsValue.GetValueOrDefault()
			            .CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
			            ) >= 0;
			    }
			}

			public override bool HasPositions
			{
			    get
			    {
			        return this.fieldInfo.IndexOptionsValue.GetValueOrDefault().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
			            ) >= 0;
			    }
			}

			public override bool HasPayloads
			{
			    get { return this.fieldInfo.HasPayloads; }
			}

			public override long Size
			{
			    get { return this.numTerms; }
			}

			public override long SumTotalTermFreq
			{
			    get { return this.sumTotalTermFreq; }
			}

			
			public override long SumDocFreq
			{
			    get { return this.sumDocFreq; }
			}

			
			public override int DocCount
			{
			    get { return this.docCount; }
			}

			private sealed class SegmentTermsEnum : TermsEnum
			{
				private readonly IndexInput @in;

				private readonly BlockTermState state;

				private readonly bool doOrd;

				private readonly FieldAndTerm fieldTerm = new FieldAndTerm();

				private readonly TermsIndexReaderBase.FieldIndexEnum indexEnum;

				private readonly BytesRef term = new BytesRef();

				private bool indexIsCurrent;

				private bool didIndexNext;

				private BytesRef nextIndexTerm;

				private bool seekPending;

				private int blocksSinceSeek;

				private byte[] termSuffixes;

				private ByteArrayDataInput termSuffixesReader = new ByteArrayDataInput();

				private int termBlockPrefix;

				private int blockTermCount;

				private byte[] docFreqBytes;

				private readonly ByteArrayDataInput freqReader = new ByteArrayDataInput();

				private int metaDataUpto;

				private long[] longs;

				private byte[] bytes;

				private ByteArrayDataInput bytesReader;

				/// <exception cref="System.IO.IOException"></exception>
				public SegmentTermsEnum(FieldReader _enclosing)
				{
					this._enclosing = _enclosing;
					// Iterates through terms in this field
					this.@in = ((IndexInput)this._enclosing._enclosing.@in.Clone());
					this.@in.Seek(this._enclosing.termsStartPointer);
					this.indexEnum = this._enclosing._enclosing.indexReader.GetFieldEnum(this._enclosing
						.fieldInfo);
					this.doOrd = this._enclosing._enclosing.indexReader.SupportsOrd();
					this.fieldTerm.field = this._enclosing.fieldInfo.name;
					this.state = this._enclosing._enclosing.postingsReader.NewTermState();
					this.state.totalTermFreq = -1;
					this.state.ord = -1;
					this.termSuffixes = new byte[128];
					this.docFreqBytes = new byte[64];
					//System.out.println("BTR.enum init this=" + this + " postingsReader=" + postingsReader);
					this.longs = new long[this._enclosing.longsSize];
				}

				public override IComparer<BytesRef> Comparator
				{
				    get { return BytesRef.UTF8SortedAsUnicodeComparer; }
				}

				// TODO: we may want an alternate mode here which is
				// "if you are about to return NOT_FOUND I won't use
				// the terms data from that"; eg FuzzyTermsEnum will
				// (usually) just immediately call seek again if we
				// return NOT_FOUND so it's a waste for us to fill in
				// the term that was actually NOT_FOUND
				
				public override SeekStatus SeekCeil(BytesRef target)
				{
					if (this.indexEnum == null)
					{
						throw new InvalidOperationException("terms index was not loaded");
					}
					//System.out.println("BTR.seek seg=" + segment + " target=" + fieldInfo.name + ":" + target.utf8ToString() + " " + target + " current=" + term().utf8ToString() + " " + term() + " indexIsCurrent=" + indexIsCurrent + " didIndexNext=" + didIndexNext + " seekPending=" + seekPending + " divisor=" + indexReader.getDivisor() + " this="  + this);
					if (this.didIndexNext)
					{
						if (this.nextIndexTerm == null)
						{
						}
					}
					//System.out.println("  nextIndexTerm=null");
					//System.out.println("  nextIndexTerm=" + nextIndexTerm.utf8ToString());
					bool doSeek = true;
					// See if we can avoid seeking, because target term
					// is after current term but before next index term:
					if (this.indexIsCurrent)
					{
						int cmp = BytesRef.UTF8SortedAsUnicodeComparer.Compare(this.term, target);
						if (cmp == 0)
						{
							// Already at the requested term
							return TermsEnum.SeekStatus.FOUND;
						}
					    if (cmp < 0)
					    {
					        // Target term is after current term
					        if (!this.didIndexNext)
					        {
					            if (this.indexEnum.Next() == -1)
					            {
					                this.nextIndexTerm = null;
					            }
					            else
					            {
					                this.nextIndexTerm = this.indexEnum.Term();
					            }
					            //System.out.println("  now do index next() nextIndexTerm=" + (nextIndexTerm == null ? "null" : nextIndexTerm.utf8ToString()));
					            this.didIndexNext = true;
					        }
					        if (this.nextIndexTerm == null || BytesRef.UTF8SortedAsUnicodeComparer.Compare
					            (target, this.nextIndexTerm) < 0)
					        {
					            // Optimization: requested term is within the
					            // same term block we are now in; skip seeking
					            // (but do scanning):
					            doSeek = false;
					        }
					    }
					}
					//System.out.println("  skip seek: nextIndexTerm=" + (nextIndexTerm == null ? "null" : nextIndexTerm.utf8ToString()));
					if (doSeek)
					{
						//System.out.println("  seek");
						// Ask terms index to find biggest indexed term (=
						// first term in a block) that's <= our text:
						this.@in.Seek(this.indexEnum.Seek(target));
						bool result = this.NextBlock();
						// Block must exist since, at least, the indexed term
						// is in the block:
						//HM:revisit
						//
						//HM:revisit 
						//assert result;
						this.indexIsCurrent = true;
						this.didIndexNext = false;
						this.blocksSinceSeek = 0;
						if (this.doOrd)
						{
							this.state.ord = this.indexEnum.Ord() - 1;
						}
						this.term.CopyBytes(this.indexEnum.Term());
					}
					else
					{
						//System.out.println("  seek: term=" + term.utf8ToString());
						//System.out.println("  skip seek");
						if (this.state.termBlockOrd == this.blockTermCount && !this.NextBlock())
						{
							this.indexIsCurrent = false;
							return TermsEnum.SeekStatus.END;
						}
					}
					this.seekPending = false;
					int common = 0;
					// Scan within block.  We could do this by calling
					// _next() and testing the resulting term, but this
					// is wasteful.  Instead, we first confirm the
					// target matches the common prefix of this block,
					// and then we scan the term bytes directly from the
					// termSuffixesreader's byte[], saving a copy into
					// the BytesRef term per term.  Only when we return
					// do we then copy the bytes into the term.
					while (true)
					{
						// First, see if target term matches common prefix
						// in this block:
						if (common < this.termBlockPrefix)
						{
							int cmp = (this.term.bytes[common] & unchecked((int)(0xFF))) - (target.bytes[target
								.offset + common] & unchecked((int)(0xFF)));
							if (cmp < 0)
							{
								// TODO: maybe we should store common prefix
								// in block header?  (instead of relying on
								// last term of previous block)
								// Target's prefix is after the common block
								// prefix, so term cannot be in this block
								// but it could be in next block.  We
								// must scan to end-of-block to set common
								// prefix for next block:
								if (this.state.termBlockOrd < this.blockTermCount)
								{
									while (this.state.termBlockOrd < this.blockTermCount - 1)
									{
										this.state.termBlockOrd++;
										this.state.ord++;
										this.termSuffixesReader.SkipBytes(this.termSuffixesReader.ReadVInt());
									}
									int suffix = this.termSuffixesReader.ReadVInt();
									this.term.length = this.termBlockPrefix + suffix;
									if (this.term.bytes.Length < this.term.length)
									{
										this.term.Grow(this.term.length);
									}
									this.termSuffixesReader.ReadBytes(this.term.bytes, this.termBlockPrefix, suffix);
								}
								this.state.ord++;
								if (!this.NextBlock())
								{
									this.indexIsCurrent = false;
									return TermsEnum.SeekStatus.END;
								}
								common = 0;
							}
							else
							{
								if (cmp > 0)
								{
									// Target's prefix is before the common prefix
									// of this block, so we position to start of
									// block and return NOT_FOUND:
									//HM:revisit
									//
									//HM:revisit 
									//assert state.termBlockOrd == 0;
									int suffix = this.termSuffixesReader.ReadVInt();
									this.term.length = this.termBlockPrefix + suffix;
									if (this.term.bytes.Length < this.term.length)
									{
										this.term.Grow(this.term.length);
									}
									this.termSuffixesReader.ReadBytes(this.term.bytes, this.termBlockPrefix, suffix);
									return TermsEnum.SeekStatus.NOT_FOUND;
								}
								else
								{
									common++;
								}
							}
							continue;
						}
						// Test every term in this block
						while (true)
						{
							this.state.termBlockOrd++;
							this.state.ord++;
							int suffix = this.termSuffixesReader.ReadVInt();
							// We know the prefix matches, so just compare the new suffix:
							int termLen = this.termBlockPrefix + suffix;
							int bytePos = this.termSuffixesReader.Position;
							bool next = false;
							int limit = target.offset + (termLen < target.length ? termLen : target.length);
							int targetPos = target.offset + this.termBlockPrefix;
							while (targetPos < limit)
							{
								int cmp = (this.termSuffixes[bytePos++] & unchecked((int)(0xFF))) - (target.bytes
									[targetPos++] & unchecked((int)(0xFF)));
								if (cmp < 0)
								{
									// Current term is still before the target;
									// keep scanning
									next = true;
									break;
								}
								else
								{
									if (cmp > 0)
									{
										// Done!  Current term is after target. Stop
										// here, fill in real term, return NOT_FOUND.
										this.term.length = this.termBlockPrefix + suffix;
										if (this.term.bytes.Length < this.term.length)
										{
											this.term.Grow(this.term.length);
										}
										this.termSuffixesReader.ReadBytes(this.term.bytes, this.termBlockPrefix, suffix);
										//System.out.println("  NOT_FOUND");
										return TermsEnum.SeekStatus.NOT_FOUND;
									}
								}
							}
							if (!next && target.length <= termLen)
							{
								this.term.length = this.termBlockPrefix + suffix;
								if (this.term.bytes.Length < this.term.length)
								{
									this.term.Grow(this.term.length);
								}
								this.termSuffixesReader.ReadBytes(this.term.bytes, this.termBlockPrefix, suffix);
								if (target.length == termLen)
								{
									// Done!  Exact match.  Stop here, fill in
									// real term, return FOUND.
									//System.out.println("  FOUND");
									return TermsEnum.SeekStatus.FOUND;
								}
								else
								{
									//System.out.println("  NOT_FOUND");
									return TermsEnum.SeekStatus.NOT_FOUND;
								}
							}
							if (this.state.termBlockOrd == this.blockTermCount)
							{
								// Must pre-fill term for next block's common prefix
								this.term.length = this.termBlockPrefix + suffix;
								if (this.term.bytes.Length < this.term.length)
								{
									this.term.Grow(this.term.length);
								}
								this.termSuffixesReader.ReadBytes(this.term.bytes, this.termBlockPrefix, suffix);
								break;
							}
							else
							{
								this.termSuffixesReader.SkipBytes(suffix);
							}
						}
						// The purpose of the terms dict index is to seek
						// the enum to the closest index term before the
						// term we are looking for.  So, we should never
						// cross another index term (besides the first
						// one) while we are scanning:
						//HM:revisit
						//
						//HM:revisit 
						//assert indexIsCurrent;
						if (!this.NextBlock())
						{
							//System.out.println("  END");
							this.indexIsCurrent = false;
							return TermsEnum.SeekStatus.END;
						}
						common = 0;
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override BytesRef Next()
				{
					//System.out.println("BTR.next() seekPending=" + seekPending + " pendingSeekCount=" + state.termBlockOrd);
					// If seek was previously called and the term was cached,
					// usually caller is just going to pull a D/&PEnum or get
					// docFreq, etc.  But, if they then call next(),
					// this method catches up all internal state so next()
					// works properly:
					if (this.seekPending)
					{
						//HM:revisit
						//
						//HM:revisit 
						//assert !indexIsCurrent;
						this.@in.Seek(this.state.blockFilePointer);
						int pendingSeekCount = this.state.termBlockOrd;
						bool result = this.NextBlock();
						long savOrd = this.state.ord;
						// Block must exist since seek(TermState) was called w/ a
						// TermState previously returned by this enum when positioned
						// on a real term:
						//HM:revisit
						//
						//HM:revisit 
						//assert result;
						while (this.state.termBlockOrd < pendingSeekCount)
						{
							BytesRef nextResult = this._next();
						}
						//HM:revisit
						//
						//HM:revisit 
						//assert nextResult != null;
						this.seekPending = false;
						this.state.ord = savOrd;
					}
					return this._next();
				}

				/// <exception cref="System.IO.IOException"></exception>
				private BytesRef _next()
				{
					//System.out.println("BTR._next seg=" + segment + " this=" + this + " termCount=" + state.termBlockOrd + " (vs " + blockTermCount + ")");
					if (this.state.termBlockOrd == this.blockTermCount && !this.NextBlock())
					{
						//System.out.println("  eof");
						this.indexIsCurrent = false;
						return null;
					}
					// TODO: cutover to something better for these ints!  simple64?
					int suffix = this.termSuffixesReader.ReadVInt();
					//System.out.println("  suffix=" + suffix);
					this.term.length = this.termBlockPrefix + suffix;
					if (this.term.bytes.Length < this.term.length)
					{
						this.term.Grow(this.term.length);
					}
					this.termSuffixesReader.ReadBytes(this.term.bytes, this.termBlockPrefix, suffix);
					this.state.termBlockOrd++;
					// NOTE: meaningless in the non-ord case
					this.state.ord++;
					//System.out.println("  return term=" + fieldInfo.name + ":" + term.utf8ToString() + " " + term + " tbOrd=" + state.termBlockOrd);
					return this.term;
				}

				public override BytesRef Term
				{
				    get { return this.term; }
				}

				
				public override int DocFreq
				{
				    get
				    {
				        //System.out.println("BTR.docFreq");
				        this.DecodeMetaData();
				        //System.out.println("  return " + state.docFreq);
				        return this.state.docFreq;
				    }
				}

				
				public override long TotalTermFreq
				{
				    get
				    {
				        this.DecodeMetaData();
				        return this.state.totalTermFreq;
				    }
				}

				
				public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
				{
					//System.out.println("BTR.docs this=" + this);
					this.DecodeMetaData();
					//System.out.println("BTR.docs:  state.docFreq=" + state.docFreq);
					return this._enclosing._enclosing.postingsReader.Docs(this._enclosing.fieldInfo, 
						this.state, liveDocs, reuse, flags);
				}

				
				public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum
					 reuse, int flags)
				{
					if (this._enclosing.fieldInfo.IndexOptionsValue.GetValueOrDefault().CompareTo(FieldInfo.IndexOptions.
						DOCS_AND_FREQS_AND_POSITIONS) < 0)
					{
						// Positions were not indexed:
						return null;
					}
					this.DecodeMetaData();
					return this._enclosing._enclosing.postingsReader.DocsAndPositions(this._enclosing
						.fieldInfo, this.state, liveDocs, reuse, flags);
				}

				public override void SeekExact(BytesRef target, Lucene.Net.Index.TermState
					 otherState)
				{
					//System.out.println("BTR.seekExact termState target=" + target.utf8ToString() + " " + target + " this=" + this);
					
					
					//assert otherState != null && otherState instanceof BlockTermState;
					
					//assert !doOrd || ((BlockTermState) otherState).ord < numTerms;
					this.state.CopyFrom(otherState);
					this.seekPending = true;
					this.indexIsCurrent = false;
					this.term.CopyBytes(target);
				}

				
				public override TermState TermState
				{
				    get
				    {
				        //System.out.println("BTR.termState this=" + this);
				        this.DecodeMetaData();
				        Lucene.Net.Index.TermState ts = (TermState) this.state.Clone();
				        //System.out.println("  return ts=" + ts);
				        return ts;
				    }
				}

				
				public override void SeekExact(long ord)
				{
					//System.out.println("BTR.seek by ord ord=" + ord);
					if (this.indexEnum == null)
					{
						throw new InvalidOperationException("terms index was not loaded");
					}
					//HM:revisit
					//
					//HM:revisit 
					//assert ord < numTerms;
					// TODO: if ord is in same terms block and
					// after current ord, we should avoid this seek just
					// like we do in the seek(BytesRef) case
					this.@in.Seek(this.indexEnum.Seek(ord));
					bool result = this.NextBlock();
					// Block must exist since ord < numTerms:
					//HM:revisit
					//
					//HM:revisit 
					//assert result;
					this.indexIsCurrent = true;
					this.didIndexNext = false;
					this.blocksSinceSeek = 0;
					this.seekPending = false;
					this.state.ord = this.indexEnum.Ord() - 1;
					//HM:revisit
					//
					//HM:revisit 
					//assert state.ord >= -1: "ord=" + state.ord;
					this.term.CopyBytes(this.indexEnum.Term());
					// Now, scan:
					int left = (int)(ord - this.state.ord);
					while (left > 0)
					{
						BytesRef term = this._next();
						//HM:revisit
						//
						//HM:revisit 
						//assert term != null;
						left--;
					}
				}

				//HM:revisit
				//
				//HM:revisit 
				//assert indexIsCurrent;
				public override long Ord
				{
				    get
				    {
				        if (!this.doOrd)
				        {
				            throw new NotSupportedException();
				        }
				        return this.state.ord;
				    }
				}

				/// <exception cref="System.IO.IOException"></exception>
				private bool NextBlock()
				{
					// TODO: we still lazy-decode the byte[] for each
					// term (the suffix), but, if we decoded
					// all N terms up front then seeking could do a fast
					// bsearch w/in the block...
					//System.out.println("BTR.nextBlock() fp=" + in.getFilePointer() + " this=" + this);
					this.state.blockFilePointer = this.@in.FilePointer;
					this.blockTermCount = this.@in.ReadVInt();
					//System.out.println("  blockTermCount=" + blockTermCount);
					if (this.blockTermCount == 0)
					{
						return false;
					}
					this.termBlockPrefix = this.@in.ReadVInt();
					// term suffixes:
					int len = this.@in.ReadVInt();
					if (this.termSuffixes.Length < len)
					{
						this.termSuffixes = new byte[ArrayUtil.Oversize(len, 1)];
					}
					//System.out.println("  termSuffixes len=" + len);
					this.@in.ReadBytes(this.termSuffixes, 0, len);
					this.termSuffixesReader.Reset(this.termSuffixes, 0, len);
					// docFreq, totalTermFreq
					len = this.@in.ReadVInt();
					if (this.docFreqBytes.Length < len)
					{
						this.docFreqBytes = new byte[ArrayUtil.Oversize(len, 1)];
					}
					//System.out.println("  freq bytes len=" + len);
					this.@in.ReadBytes(this.docFreqBytes, 0, len);
					this.freqReader.Reset(this.docFreqBytes, 0, len);
					// metadata
					len = this.@in.ReadVInt();
					if (this.bytes == null)
					{
						this.bytes = new byte[ArrayUtil.Oversize(len, 1)];
						this.bytesReader = new ByteArrayDataInput();
					}
					else
					{
						if (this.bytes.Length < len)
						{
							this.bytes = new byte[ArrayUtil.Oversize(len, 1)];
						}
					}
					this.@in.ReadBytes(this.bytes, 0, len);
					this.bytesReader.Reset(this.bytes, 0, len);
					this.metaDataUpto = 0;
					this.state.termBlockOrd = 0;
					this.blocksSinceSeek++;
					this.indexIsCurrent = this.indexIsCurrent && (this.blocksSinceSeek < this._enclosing
						._enclosing.indexReader.GetDivisor());
					//System.out.println("  indexIsCurrent=" + indexIsCurrent);
					return true;
				}

				/// <exception cref="System.IO.IOException"></exception>
				private void DecodeMetaData()
				{
					//System.out.println("BTR.decodeMetadata mdUpto=" + metaDataUpto + " vs termCount=" + state.termBlockOrd + " state=" + state);
					if (!this.seekPending)
					{
						// TODO: cutover to random-access API
						// here.... really stupid that we have to decode N
						// wasted term metadata just to get to the N+1th
						// that we really need...
						// lazily catch up on metadata decode:
						int limit = this.state.termBlockOrd;
						bool absolute = this.metaDataUpto == 0;
						// TODO: better API would be "jump straight to term=N"???
						while (this.metaDataUpto < limit)
						{
							//System.out.println("  decode mdUpto=" + metaDataUpto);
							// TODO: we could make "tiers" of metadata, ie,
							// decode docFreq/totalTF but don't decode postings
							// metadata; this way caller could get
							// docFreq/totalTF w/o paying decode cost for
							// postings
							// TODO: if docFreq were bulk decoded we could
							// just skipN here:
							// docFreq, totalTermFreq
							this.state.docFreq = this.freqReader.ReadVInt();
							//System.out.println("    dF=" + state.docFreq);
							if (this._enclosing.fieldInfo.IndexOptionsValue != FieldInfo.IndexOptions.DOCS_ONLY)
							{
								this.state.totalTermFreq = this.state.docFreq + this.freqReader.ReadVLong();
							}
							//System.out.println("    totTF=" + state.totalTermFreq);
							// metadata
							for (int i = 0; i < this.longs.Length; i++)
							{
								this.longs[i] = this.bytesReader.ReadVLong();
							}
							this._enclosing._enclosing.postingsReader.DecodeTerm(this.longs, this.bytesReader
								, this._enclosing.fieldInfo, this.state, absolute);
							this.metaDataUpto++;
							absolute = false;
						}
					}
				}

				private readonly FieldReader _enclosing;
				//System.out.println("  skip! seekPending");
			}

			private readonly BlockTermsReader _enclosing;
		}

		public override long RamBytesUsed
		{
		    get
		    {
		        long sizeInBytes = (postingsReader != null) ? postingsReader.RamBytesUsed() : 0;
		        sizeInBytes += (indexReader != null) ? indexReader.RamBytesUsed() : 0;
		        return sizeInBytes;
		    }
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CheckIntegrity()
		{
			// verify terms
			if (version >= BlockTermsWriter.VERSION_CHECKSUM)
			{
				CodecUtil.ChecksumEntireFile(@in);
			}
			// verify postings
			postingsReader.CheckIntegrity();
		}
	}
}
