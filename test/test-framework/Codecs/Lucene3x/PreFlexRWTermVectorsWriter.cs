using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Lucene3x.TestFramework
{
	internal sealed class PreFlexRWTermVectorsWriter : TermVectorsWriter
	{
		private readonly Directory directory;

		private readonly string segment;

		private IndexOutput tvx = null;

		private IndexOutput tvd = null;

		private IndexOutput tvf = null;

		/// <exception cref="System.IO.IOException"></exception>
		public PreFlexRWTermVectorsWriter(Directory directory, string segment, IOContext 
			context)
		{
			this.directory = directory;
			this.segment = segment;
			bool success = false;
			try
			{
				// Open files for TermVector storage
				tvx = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, string.Empty
					, Lucene3xTermVectorsReader.VECTORS_INDEX_EXTENSION), context);
				tvx.WriteInt(Lucene3xTermVectorsReader.FORMAT_CURRENT);
				tvd = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, string.Empty
					, Lucene3xTermVectorsReader.VECTORS_DOCUMENTS_EXTENSION), context);
				tvd.WriteInt(Lucene3xTermVectorsReader.FORMAT_CURRENT);
				tvf = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, string.Empty
					, Lucene3xTermVectorsReader.VECTORS_FIELDS_EXTENSION), context);
				tvf.WriteInt(Lucene3xTermVectorsReader.FORMAT_CURRENT);
				success = true;
			}
			finally
			{
				if (!success)
				{
					Abort();
				}
			}
		}

		
		public override void StartDocument(int numVectorFields)
		{
			lastFieldName = null;
			this.numVectorFields = numVectorFields;
			tvx.WriteLong(tvd.FilePointer);
			tvx.WriteLong(tvf.FilePointer);
			tvd.WriteVInt(numVectorFields);
			fieldCount = 0;
			fps = ArrayUtil.Grow(fps, numVectorFields);
		}

		private long[] fps = new long[10];

		private int fieldCount = 0;

		private int numVectorFields = 0;

		private string lastFieldName;

		// pointers to the tvf before writing each field 
		// number of fields we have written so far for this document
		// total number of fields we will write for this document
		/// <exception cref="System.IO.IOException"></exception>
		public override void StartField(FieldInfo info, int numTerms, bool positions, bool
			 offsets, bool payloads)
		{
			 
			//assert lastFieldName == null || info.name.compareTo(lastFieldName) > 0: "fieldName=" + info.name + " lastFieldName=" + lastFieldName;
			lastFieldName = info.name;
			if (payloads)
			{
				throw new NotSupportedException("3.x codec does not support payloads on vectors!"
					);
			}
			this.positions = positions;
			this.offsets = offsets;
			lastTerm.length = 0;
			fps[fieldCount++] = tvf.FilePointer;
			tvd.WriteVInt(info.number);
			tvf.WriteVInt(numTerms);
			byte bits = unchecked((int)(0x0));
			if (positions)
			{
				bits |= Lucene3xTermVectorsReader.STORE_POSITIONS_WITH_TERMVECTOR;
			}
			if (offsets)
			{
				bits |= Lucene3xTermVectorsReader.STORE_OFFSET_WITH_TERMVECTOR;
			}
			tvf.WriteByte(bits);
			 
			//assert fieldCount <= numVectorFields;
			if (fieldCount == numVectorFields)
			{
				// last field of the document
				// this is crazy because the file format is crazy!
				for (int i = 1; i < fieldCount; i++)
				{
					tvd.WriteVLong(fps[i] - fps[i - 1]);
				}
			}
		}

		private readonly BytesRef lastTerm = new BytesRef(10);

		private int[] offsetStartBuffer = new int[10];

		private int[] offsetEndBuffer = new int[10];

		private int offsetIndex = 0;

		private int offsetFreq = 0;

		private bool positions = false;

		private bool offsets = false;

		// NOTE: we override addProx, so we don't need to buffer when indexing.
		// we also don't buffer during bulk merges.
		/// <exception cref="System.IO.IOException"></exception>
		public override void StartTerm(BytesRef term, int freq)
		{
			int prefix = StringHelper.BytesDifference(lastTerm, term);
			int suffix = term.length - prefix;
			tvf.WriteVInt(prefix);
			tvf.WriteVInt(suffix);
			tvf.WriteBytes(term.bytes, term.offset + prefix, suffix);
			tvf.WriteVInt(freq);
			lastTerm.CopyBytes(term);
			lastPosition = lastOffset = 0;
			if (offsets && positions)
			{
				// we might need to buffer if its a non-bulk merge
				offsetStartBuffer = ArrayUtil.Grow(offsetStartBuffer, freq);
				offsetEndBuffer = ArrayUtil.Grow(offsetEndBuffer, freq);
				offsetIndex = 0;
				offsetFreq = freq;
			}
		}

		internal int lastPosition = 0;

		internal int lastOffset = 0;

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddPosition(int position, int startOffset, int endOffset, BytesRef
			 payload)
		{
			 
			//assert payload == null;
			if (positions && offsets)
			{
				// write position delta
				tvf.WriteVInt(position - lastPosition);
				lastPosition = position;
				// buffer offsets
				offsetStartBuffer[offsetIndex] = startOffset;
				offsetEndBuffer[offsetIndex] = endOffset;
				offsetIndex++;
				// dump buffer if we are done
				if (offsetIndex == offsetFreq)
				{
					for (int i = 0; i < offsetIndex; i++)
					{
						tvf.WriteVInt(offsetStartBuffer[i] - lastOffset);
						tvf.WriteVInt(offsetEndBuffer[i] - offsetStartBuffer[i]);
						lastOffset = offsetEndBuffer[i];
					}
				}
			}
			else
			{
				if (positions)
				{
					// write position delta
					tvf.WriteVInt(position - lastPosition);
					lastPosition = position;
				}
				else
				{
					if (offsets)
					{
						// write offset deltas
						tvf.WriteVInt(startOffset - lastOffset);
						tvf.WriteVInt(endOffset - startOffset);
						lastOffset = endOffset;
					}
				}
			}
		}

		public override void Abort()
		{
			try
			{
				Close();
			}
			catch
			{
			}
			IOUtils.DeleteFilesIgnoringExceptions(directory, IndexFileNames.SegmentFileName(segment
				, string.Empty, Lucene3xTermVectorsReader.VECTORS_INDEX_EXTENSION), IndexFileNames
				.SegmentFileName(segment, string.Empty, Lucene3xTermVectorsReader.VECTORS_DOCUMENTS_EXTENSION
				), IndexFileNames.SegmentFileName(segment, string.Empty, Lucene3xTermVectorsReader
				.VECTORS_FIELDS_EXTENSION));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Finish(FieldInfos fis, int numDocs)
		{
			if (4 + ((long)numDocs) * 16 != tvx.FilePointer)
			{
				// This is most likely a bug in Sun JRE 1.6.0_04/_05;
				// we detect that the bug has struck, here, and
				// throw an exception to prevent the corruption from
				// entering the index.  See LUCENE-1282 for
				// details.
				throw new SystemException("tvx size mismatch: mergedDocs is " + numDocs + " but tvx size is "
					 + tvx.FilePointer + " file=" + tvx.ToString() + "; now aborting this merge to prevent index corruption"
					);
			}
		}

		/// <summary>Close all streams.</summary>
		
		protected override void Dispose(bool disposing)
		{
			// make an effort to close all streams we can but remember and re-throw
			// the first exception encountered in this process
			IOUtils.Close(tvx, tvd, tvf);
			tvx = tvd = tvf = null;
		}

		
		public override IComparer<BytesRef> Comparator
		{
		    get { return BytesRef.UTF8SortedAsUnicodeComparer; }
		}
	}
}
