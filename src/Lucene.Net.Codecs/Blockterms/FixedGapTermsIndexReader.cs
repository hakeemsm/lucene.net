using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Blockterms
{
	/// <summary>TermsIndexReader for simple every Nth terms indexes.</summary>
	/// <seealso cref="FixedGapTermsIndexWriter">FixedGapTermsIndexWriter</seealso>
	/// <lucene.experimental></lucene.experimental>
	public class FixedGapTermsIndexReader : TermsIndexReaderBase
	{
		private long totalIndexInterval;

		private int indexDivisor;

		private readonly int indexInterval;

		private IndexInput indexInput;

		private volatile bool indexLoaded;

		private readonly IComparer<BytesRef> termComp;

		private const int PAGED_BYTES_BITS = 15;

		private readonly PagedBytes termBytes = new PagedBytes(PAGED_BYTES_BITS);

		private PagedBytes.Reader termBytesReader;

		internal readonly Dictionary<FieldInfo, FieldIndexData> 
			fields = new Dictionary<FieldInfo, FieldIndexData>();

		private long dirOffset;

		private readonly int version;

		/// <exception cref="System.IO.IOException"></exception>
		public FixedGapTermsIndexReader(Directory dir, FieldInfos fieldInfos, string segment
			, int indexDivisor, IComparer<BytesRef> termComp, string segmentSuffix, IOContext
			 context)
		{
			// NOTE: long is overkill here, since this number is 128
			// by default and only indexDivisor * 128 if you change
			// the indexDivisor at search time.  But, we use this in a
			// number of places to multiply out the actual ord, and we
			// will overflow int during those multiplies.  So to avoid
			// having to upgrade each multiple to long in multiple
			// places (error prone), we use long here:
			// Closed if indexLoaded is true:
			// all fields share this single logical byte[]
			// start of the field info data
			this.termComp = termComp;
			//HM:revisit 
			//assert indexDivisor == -1 || indexDivisor > 0;
			indexInput = dir.OpenInput(IndexFileNames.SegmentFileName(segment, segmentSuffix, FixedGapTermsIndexWriter
				.TERMS_INDEX_EXTENSION), context);
			bool success = false;
			try
			{
				version = ReadHeader(indexInput);
				if (version >= FixedGapTermsIndexWriter.VERSION_CHECKSUM)
				{
					CodecUtil.ChecksumEntireFile(indexInput);
				}
				indexInterval = indexInput.ReadInt();
				if (indexInterval < 1)
				{
					throw new CorruptIndexException("invalid indexInterval: " + indexInterval + " (resource="
						 + indexInput + ")");
				}
				this.indexDivisor = indexDivisor;
				if (indexDivisor < 0)
				{
					totalIndexInterval = indexInterval;
				}
				else
				{
					// In case terms index gets loaded, later, on demand
					totalIndexInterval = indexInterval * indexDivisor;
				}
				//HM:revisit 
				//assert totalIndexInterval > 0;
				SeekDir(indexInput, dirOffset);
				// Read directory
				int numFields = indexInput.ReadVInt();
				if (numFields < 0)
				{
					throw new CorruptIndexException("invalid numFields: " + numFields + " (resource="
						 + indexInput + ")");
				}
				//System.out.println("FGR: init seg=" + segment + " div=" + indexDivisor + " nF=" + numFields);
				for (int i = 0; i < numFields; i++)
				{
					int field = indexInput.ReadVInt();
					int numIndexTerms = indexInput.ReadVInt();
					if (numIndexTerms < 0)
					{
						throw new CorruptIndexException("invalid numIndexTerms: " + numIndexTerms + " (resource="
							 + indexInput + ")");
					}
					long termsStart = indexInput.ReadVLong();
					long indexStart = indexInput.ReadVLong();
					long packedIndexStart = indexInput.ReadVLong();
					long packedOffsetsStart = indexInput.ReadVLong();
					if (packedIndexStart < indexStart)
					{
						throw new CorruptIndexException("invalid packedIndexStart: " + packedIndexStart +
							 " indexStart: " + indexStart + "numIndexTerms: " + numIndexTerms + " (resource="
							 + indexInput + ")");
					}
					FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
					FieldIndexData previous = fields[fieldInfo] = new FieldIndexData
						(this, fieldInfo, numIndexTerms, indexStart, termsStart, packedIndexStart, packedOffsetsStart
						);
					if (previous != null)
					{
						throw new CorruptIndexException("duplicate field: " + fieldInfo.name + " (resource="+ indexInput + ")");
					}
				}
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException((IDisposable)indexInput);
				}
				if (indexDivisor > 0)
				{
					indexInput.Dispose();
					indexInput = null;
					if (success)
					{
						indexLoaded = true;
					}
					termBytesReader = termBytes.Freeze(true);
				}
			}
		}

		public override int GetDivisor()
		{
			return indexDivisor;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int ReadHeader(IndexInput input)
		{
			int version = CodecUtil.CheckHeader(input, FixedGapTermsIndexWriter.CODEC_NAME, FixedGapTermsIndexWriter
				.VERSION_START, FixedGapTermsIndexWriter.VERSION_CURRENT);
			if (version < FixedGapTermsIndexWriter.VERSION_APPEND_ONLY)
			{
				dirOffset = input.ReadLong();
			}
			return version;
		}

		private class IndexEnum : FieldIndexEnum
		{
			private readonly FieldIndexData.CoreFieldIndex fieldIndex;

			private readonly BytesRef term = new BytesRef();

			private long ord;

			public IndexEnum(FixedGapTermsIndexReader enclosing, FieldIndexData.CoreFieldIndex
				 fieldIndex)
			{
				this.enclosingInstance = enclosing;
				this.fieldIndex = fieldIndex;
			}

			public override BytesRef Term()
			{
				return this.term;
			}

			public override long Seek(BytesRef target)
			{
				int lo = 0;
				// binary search
				int hi = this.fieldIndex.numIndexTerms - 1;
				//HM:revisit 
				//assert totalIndexInterval > 0 : "totalIndexInterval=" + totalIndexInterval;
				while (hi >= lo)
				{
					int mid = (int)(((uint)(lo + hi)) >> 1);
					long offset = this.fieldIndex.termOffsets.Get(mid);
					int length = (int)(this.fieldIndex.termOffsets.Get(1 + mid) - offset);
					this.enclosingInstance.termBytesReader.FillSlice(this.term, this.fieldIndex.termBytesStart
						 + offset, length);
					int delta = this.enclosingInstance.termComp.Compare(target, this.term);
					if (delta < 0)
					{
						hi = mid - 1;
					}
					else
					{
						if (delta > 0)
						{
							lo = mid + 1;
						}
						else
						{
							//HM:revisit 
							//assert mid >= 0;
							this.ord = mid * this.enclosingInstance.totalIndexInterval;
							return this.fieldIndex.termsStart + this.fieldIndex.termsDictOffsets.Get(mid);
						}
					}
				}
				if (hi < 0)
				{
					//HM:revisit 
					//assert hi == -1;
					hi = 0;
				}
				long offset_1 = this.fieldIndex.termOffsets.Get(hi);
				int length_1 = (int)(this.fieldIndex.termOffsets.Get(1 + hi) - offset_1);
				this.enclosingInstance.termBytesReader.FillSlice(this.term, this.fieldIndex.termBytesStart
					 + offset_1, length_1);
				this.ord = hi * this.enclosingInstance.totalIndexInterval;
				return this.fieldIndex.termsStart + this.fieldIndex.termsDictOffsets.Get(hi);
			}

			public override long Next()
			{
				int idx = 1 + (int)(this.ord / this.enclosingInstance.totalIndexInterval);
				if (idx >= this.fieldIndex.numIndexTerms)
				{
					return -1;
				}
				this.ord += this.enclosingInstance.totalIndexInterval;
				long offset = this.fieldIndex.termOffsets.Get(idx);
				int length = (int)(this.fieldIndex.termOffsets.Get(1 + idx) - offset);
				this.enclosingInstance.termBytesReader.FillSlice(this.term, this.fieldIndex.termBytesStart
					 + offset, length);
				return this.fieldIndex.termsStart + this.fieldIndex.termsDictOffsets.Get(idx);
			}

			public override long Ord()
			{
				return this.ord;
			}

			public override long Seek(long ord)
			{
				int idx = (int)(ord / this.enclosingInstance.totalIndexInterval);
				// caller must ensure ord is in bounds
				//HM:revisit 
				//assert idx < fieldIndex.numIndexTerms;
				long offset = this.fieldIndex.termOffsets.Get(idx);
				int length = (int)(this.fieldIndex.termOffsets.Get(1 + idx) - offset);
				this.enclosingInstance.termBytesReader.FillSlice(this.term, this.fieldIndex.termBytesStart
					 + offset, length);
				this.ord = idx * this.enclosingInstance.totalIndexInterval;
				return this.fieldIndex.termsStart + this.fieldIndex.termsDictOffsets.Get(idx);
			}

			private readonly FixedGapTermsIndexReader enclosingInstance;
		}

		public override bool SupportsOrd()
		{
			return true;
		}

	    internal sealed class FieldIndexData
		{
			internal volatile FixedGapTermsIndexReader.FieldIndexData.CoreFieldIndex coreIndex;

			private readonly long indexStart;

			private readonly long termsStart;

			private readonly long packedIndexStart;

			private readonly long packedOffsetsStart;

			private readonly int numIndexTerms;

			/// <exception cref="System.IO.IOException"></exception>
			public FieldIndexData(FixedGapTermsIndexReader _enclosing, FieldInfo fieldInfo, int
				 numIndexTerms, long indexStart, long termsStart, long packedIndexStart, long packedOffsetsStart
				)
			{
				this._enclosing = _enclosing;
				this.termsStart = termsStart;
				this.indexStart = indexStart;
				this.packedIndexStart = packedIndexStart;
				this.packedOffsetsStart = packedOffsetsStart;
				this.numIndexTerms = numIndexTerms;
				if (this._enclosing.indexDivisor > 0)
				{
					this.LoadTermsIndex();
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void LoadTermsIndex()
			{
				if (this.coreIndex == null)
				{
					this.coreIndex = new FixedGapTermsIndexReader.FieldIndexData.CoreFieldIndex(this, 
						this.indexStart, this.termsStart, this.packedIndexStart, this.packedOffsetsStart
						, this.numIndexTerms);
				}
			}

	        internal sealed class CoreFieldIndex
			{
				internal readonly long termBytesStart;

				internal readonly PackedInts.IReader termOffsets;

				internal readonly PackedInts.IReader termsDictOffsets;

				internal readonly int numIndexTerms;

				internal readonly long termsStart;

				/// <exception cref="System.IO.IOException"></exception>
				public CoreFieldIndex(FieldIndexData _enclosing, long indexStart, long termsStart
					, long packedIndexStart, long packedOffsetsStart, int numIndexTerms)
				{
					this._enclosing = _enclosing;
					// where this field's terms begin in the packed byte[]
					// data
					// offset into index termBytes
					// index pointers into main terms dict
					this.termsStart = termsStart;
					this.termBytesStart = this._enclosing._enclosing.termBytes.Pointer;
					IndexInput clone = ((IndexInput)this._enclosing._enclosing.indexInput.Clone());
					clone.Seek(indexStart);
					// -1 is passed to mean "don't load term index", but
					// if we are then later loaded it's overwritten with
					// a real value
					//HM:revisit 
					//assert indexDivisor > 0;
					this.numIndexTerms = 1 + (numIndexTerms - 1) / this._enclosing._enclosing.indexDivisor;
					//HM:revisit 
					//assert this.numIndexTerms  > 0: "numIndexTerms=" + numIndexTerms + " indexDivisor=" + indexDivisor;
					if (this._enclosing._enclosing.indexDivisor == 1)
					{
						// Default (load all index terms) is fast -- slurp in the images from disk:
						try
						{
							long numTermBytes = packedIndexStart - indexStart;
							this._enclosing._enclosing.termBytes.Copy(clone, numTermBytes);
							// records offsets into main terms dict file
							this.termsDictOffsets = PackedInts.GetReader(clone);
							//HM:revisit 
							//assert termsDictOffsets.size() == numIndexTerms;
							// records offsets into byte[] term data
							this.termOffsets = PackedInts.GetReader(clone);
						}
						finally
						{
							//HM:revisit 
							//assert termOffsets.size() == 1+numIndexTerms;
							clone.Dispose();
						}
					}
					else
					{
						// Get packed iterators
						IndexInput clone1 = ((IndexInput)this._enclosing._enclosing.indexInput.Clone());
						IndexInput clone2 = ((IndexInput)this._enclosing._enclosing.indexInput.Clone());
						try
						{
							// Subsample the index terms
							clone1.Seek(packedIndexStart);
							PackedInts.IReaderIterator termsDictOffsetsIter = PackedInts.GetReaderIterator(clone1
								, PackedInts.DEFAULT_BUFFER_SIZE);
							clone2.Seek(packedOffsetsStart);
							PackedInts.IReaderIterator termOffsetsIter = PackedInts.GetReaderIterator(clone2, 
								PackedInts.DEFAULT_BUFFER_SIZE);
							// TODO: often we can get by w/ fewer bits per
							// value, below.. .but this'd be more complex:
							// we'd have to try @ fewer bits and then grow
							// if we overflowed it.
							PackedInts.IMutable termsDictOffsetsM = PackedInts.GetMutable(this.numIndexTerms, 
								termsDictOffsetsIter.GetBitsPerValue(), PackedInts.DEFAULT);
							PackedInts.IMutable termOffsetsM = PackedInts.GetMutable(this.numIndexTerms + 1, termOffsetsIter
								.GetBitsPerValue(), PackedInts.DEFAULT);
							this.termsDictOffsets = termsDictOffsetsM;
							this.termOffsets = termOffsetsM;
							int upto = 0;
							long termOffsetUpto = 0;
							while (upto < this.numIndexTerms)
							{
								// main file offset copies straight over
								termsDictOffsetsM.Set(upto, termsDictOffsetsIter.Next());
								termOffsetsM.Set(upto, termOffsetUpto);
								long termOffset = termOffsetsIter.Next();
								long nextTermOffset = termOffsetsIter.Next();
								int numTermBytes = (int)(nextTermOffset - termOffset);
								clone.Seek(indexStart + termOffset);
								//HM:revisit 
								//assert indexStart + termOffset < clone.length() : "indexStart=" + indexStart + " termOffset=" + termOffset + " len=" + clone.length();
								//HM:revisit 
								//assert indexStart + termOffset + numTermBytes < clone.length();
								this._enclosing._enclosing.termBytes.Copy(clone, numTermBytes);
								termOffsetUpto += numTermBytes;
								upto++;
								if (upto == this.numIndexTerms)
								{
									break;
								}
								// skip terms:
								termsDictOffsetsIter.Next();
								for (int i = 0; i < this._enclosing._enclosing.indexDivisor - 2; i++)
								{
									termOffsetsIter.Next();
									termsDictOffsetsIter.Next();
								}
							}
							termOffsetsM.Set(upto, termOffsetUpto);
						}
						finally
						{
							clone1.Dispose();
							clone2.Dispose();
							clone.Dispose();
						}
					}
				}

				/// <summary>Returns approximate RAM bytes Used</summary>
				public long RamBytesUsed()
				{
					return ((this.termOffsets != null) ? this.termOffsets.RamBytesUsed() : 0) + ((this
						.termsDictOffsets != null) ? this.termsDictOffsets.RamBytesUsed() : 0);
				}

				private readonly FieldIndexData _enclosing;
			}

			private readonly FixedGapTermsIndexReader _enclosing;
		}

		public override FieldIndexEnum GetFieldEnum(FieldInfo fieldInfo)
		{
			var fieldData = fields[fieldInfo];
			if (fieldData.coreIndex == null)
			{
				return null;
			}
		    return new IndexEnum(this, fieldData.coreIndex);
		}

		
		public override void Close()
		{
			if (indexInput != null && !indexLoaded)
			{
				indexInput.Dispose();
			}
		}

		
		private void SeekDir(IndexInput input, long dirOffset)
		{
			if (version >= FixedGapTermsIndexWriter.VERSION_CHECKSUM)
			{
				input.Seek(input.Length - CodecUtil.FooterLength() - 8);
				dirOffset = input.ReadLong();
			}
			else
			{
				if (version >= FixedGapTermsIndexWriter.VERSION_APPEND_ONLY)
				{
					input.Seek(input.Length - 8);
					dirOffset = input.ReadLong();
				}
			}
			input.Seek(dirOffset);
		}

		public override long RamBytesUsed()
		{
			long sizeInBytes = ((termBytes != null) ? termBytes.RamBytesUsed() : 0) + ((termBytesReader
				 != null) ? termBytesReader.RamBytesUsed() : 0);
			foreach (FixedGapTermsIndexReader.FieldIndexData entry in fields.Values)
			{
				sizeInBytes += entry.coreIndex.RamBytesUsed();
			}
			return sizeInBytes;
		}
	}
}
