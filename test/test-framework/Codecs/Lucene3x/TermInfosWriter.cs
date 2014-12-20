/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Org.Apache.Lucene.Codecs.Lucene3x;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene3x
{
	/// <summary>
	/// This stores a monotonically increasing set of <Term, TermInfo> pairs in a
	/// Directory.
	/// </summary>
	/// <remarks>
	/// This stores a monotonically increasing set of <Term, TermInfo> pairs in a
	/// Directory.  A TermInfos can be written once, in order.
	/// </remarks>
	internal sealed class TermInfosWriter : IDisposable
	{
		/// <summary>The file format version, a negative number.</summary>
		/// <remarks>The file format version, a negative number.</remarks>
		public const int FORMAT = -3;

		public const int FORMAT_VERSION_UTF8_LENGTH_IN_BYTES = -4;

		public const int FORMAT_CURRENT = FORMAT_VERSION_UTF8_LENGTH_IN_BYTES;

		private FieldInfos fieldInfos;

		private IndexOutput output;

		private TermInfo lastTi = new TermInfo();

		private long size;

		/// <summary>
		/// Expert: The fraction of terms in the "dictionary" which should be stored
		/// in RAM.
		/// </summary>
		/// <remarks>
		/// Expert: The fraction of terms in the "dictionary" which should be stored
		/// in RAM.  Smaller values use more memory, but make searching slightly
		/// faster, while larger values use less memory and make searching slightly
		/// slower.  Searching is typically not dominated by dictionary lookup, so
		/// tweaking this is rarely useful.
		/// </remarks>
		internal int indexInterval = 128;

		/// <summary>
		/// Expert: The fraction of term entries stored in skip tables,
		/// used to accelerate skipping.
		/// </summary>
		/// <remarks>
		/// Expert: The fraction of term entries stored in skip tables,
		/// used to accelerate skipping.  Larger values result in
		/// smaller indexes, greater acceleration, but fewer accelerable cases, while
		/// smaller values result in bigger indexes, less acceleration and more
		/// accelerable cases. More detailed experiments would be useful here.
		/// </remarks>
		internal int skipInterval = 16;

		/// <summary>Expert: The maximum number of skip levels.</summary>
		/// <remarks>
		/// Expert: The maximum number of skip levels. Smaller values result in
		/// slightly smaller indexes, but slower skipping in big posting lists.
		/// </remarks>
		internal int maxSkipLevels = 10;

		private long lastIndexPointer;

		private bool isIndex;

		private readonly BytesRef lastTerm = new BytesRef();

		private int lastFieldNumber = -1;

		private Org.Apache.Lucene.Codecs.Lucene3x.TermInfosWriter other;

		/// <exception cref="System.IO.IOException"></exception>
		internal TermInfosWriter(Directory directory, string segment, FieldInfos fis, int
			 interval)
		{
			// Changed strings to true utf8 with length-in-bytes not
			// length-in-chars
			// NOTE: always change this if you switch to a new format!
			// TODO: the default values for these two parameters should be settable from
			// IndexWriter.  However, once that's done, folks will start setting them to
			// ridiculous values and complaining that things don't work well, as with
			// mergeFactor.  So, let's wait until a number of folks find that alternate
			// values work better.  Note that both of these values are stored in the
			// segment, so that it's safe to change these w/o rebuilding all indexes.
			Initialize(directory, segment, fis, interval, false);
			bool success = false;
			try
			{
				other = new Org.Apache.Lucene.Codecs.Lucene3x.TermInfosWriter(directory, segment, 
					fis, interval, true);
				other.other = this;
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(output);
					try
					{
						directory.DeleteFile(IndexFileNames.SegmentFileName(segment, string.Empty, (isIndex
							 ? Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION : Lucene3xPostingsFormat.TERMS_EXTENSION
							)));
					}
					catch (IOException)
					{
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private TermInfosWriter(Directory directory, string segment, FieldInfos fis, int 
			interval, bool isIndex)
		{
			Initialize(directory, segment, fis, interval, isIndex);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Initialize(Directory directory, string segment, FieldInfos fis, int 
			interval, bool isi)
		{
			indexInterval = interval;
			fieldInfos = fis;
			isIndex = isi;
			output = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, string.Empty
				, (isIndex ? Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION : Lucene3xPostingsFormat
				.TERMS_EXTENSION)), IOContext.DEFAULT);
			bool success = false;
			try
			{
				output.WriteInt(FORMAT_CURRENT);
				// write format
				output.WriteLong(0);
				// leave space for size
				output.WriteInt(indexInterval);
				// write indexInterval
				output.WriteInt(skipInterval);
				// write skipInterval
				output.WriteInt(maxSkipLevels);
				// write maxSkipLevels
				//HM:revisit 
				//assert initUTF16Results();
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(output);
					try
					{
						directory.DeleteFile(IndexFileNames.SegmentFileName(segment, string.Empty, (isIndex
							 ? Lucene3xPostingsFormat.TERMS_INDEX_EXTENSION : Lucene3xPostingsFormat.TERMS_EXTENSION
							)));
					}
					catch (IOException)
					{
					}
				}
			}
		}

		internal CharsRef utf16Result1;

		internal CharsRef utf16Result2;

		private readonly BytesRef scratchBytes = new BytesRef();

		// Currently used only by 
		//HM:revisit 
		//assert statements
		// Currently used only by 
		//HM:revisit 
		//assert statements
		private bool InitUTF16Results()
		{
			utf16Result1 = new CharsRef(10);
			utf16Result2 = new CharsRef(10);
			return true;
		}

		/// <summary>note: -1 is the empty field: "" !!!!</summary>
		internal static string FieldName(FieldInfos infos, int fieldNumber)
		{
			if (fieldNumber == -1)
			{
				return string.Empty;
			}
			else
			{
				return infos.FieldInfo(fieldNumber).name;
			}
		}

		// Currently used only by 
		//HM:revisit 
		//assert statement
		private int CompareToLastTerm(int fieldNumber, BytesRef term)
		{
			if (lastFieldNumber != fieldNumber)
			{
				int cmp = Sharpen.Runtime.CompareOrdinal(FieldName(fieldInfos, lastFieldNumber), 
					FieldName(fieldInfos, fieldNumber));
				// If there is a field named "" (empty string) then we
				// will get 0 on this comparison, yet, it's "OK".  But
				// it's not OK if two different field numbers map to
				// the same name.
				if (cmp != 0 || lastFieldNumber != -1)
				{
					return cmp;
				}
			}
			scratchBytes.CopyBytes(term);
			//HM:revisit 
			//assert lastTerm.offset == 0;
			UnicodeUtil.UTF8toUTF16(lastTerm.bytes, 0, lastTerm.length, utf16Result1);
			//HM:revisit 
			//assert scratchBytes.offset == 0;
			UnicodeUtil.UTF8toUTF16(scratchBytes.bytes, 0, scratchBytes.length, utf16Result2);
			int len;
			if (utf16Result1.length < utf16Result2.length)
			{
				len = utf16Result1.length;
			}
			else
			{
				len = utf16Result2.length;
			}
			for (int i = 0; i < len; i++)
			{
				char ch1 = utf16Result1.chars[i];
				char ch2 = utf16Result2.chars[i];
				if (ch1 != ch2)
				{
					return ch1 - ch2;
				}
			}
			if (utf16Result1.length == 0 && lastFieldNumber == -1)
			{
				// If there is a field named "" (empty string) with a term text of "" (empty string) then we
				// will get 0 on this comparison, yet, it's "OK". 
				return -1;
			}
			return utf16Result1.length - utf16Result2.length;
		}

		/// <summary>Adds a new <&lt;fieldNumber, termBytes>, TermInfo&gt; pair to the set.</summary>
		/// <remarks>
		/// Adds a new <&lt;fieldNumber, termBytes>, TermInfo&gt; pair to the set.
		/// Term must be lexicographically greater than all previous Terms added.
		/// TermInfo pointers must be positive and greater than all previous.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public void Add(int fieldNumber, BytesRef term, TermInfo ti)
		{
			//HM:revisit 
			//assert compareToLastTerm(fieldNumber, term) < 0 ||
			//HM:revisit 
			//assert ti.freqPointer >= lastTi.freqPointer: "freqPointer out of order (" + ti.freqPointer + " < " + lastTi.freqPointer + ")";
			//HM:revisit 
			//assert ti.proxPointer >= lastTi.proxPointer: "proxPointer out of order (" + ti.proxPointer + " < " + lastTi.proxPointer + ")";
			if (!isIndex && size % indexInterval == 0)
			{
				other.Add(lastFieldNumber, lastTerm, lastTi);
			}
			// add an index term
			WriteTerm(fieldNumber, term);
			// write term
			output.WriteVInt(ti.docFreq);
			// write doc freq
			output.WriteVLong(ti.freqPointer - lastTi.freqPointer);
			// write pointers
			output.WriteVLong(ti.proxPointer - lastTi.proxPointer);
			if (ti.docFreq >= skipInterval)
			{
				output.WriteVInt(ti.skipOffset);
			}
			if (isIndex)
			{
				output.WriteVLong(other.output.GetFilePointer() - lastIndexPointer);
				lastIndexPointer = other.output.GetFilePointer();
			}
			// write pointer
			lastFieldNumber = fieldNumber;
			lastTi.Set(ti);
			size++;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteTerm(int fieldNumber, BytesRef term)
		{
			//System.out.println("  tiw.write field=" + fieldNumber + " term=" + term.utf8ToString());
			// TODO: UTF16toUTF8 could tell us this prefix
			// Compute prefix in common with last term:
			int start = 0;
			int limit = term.length < lastTerm.length ? term.length : lastTerm.length;
			while (start < limit)
			{
				if (term.bytes[start + term.offset] != lastTerm.bytes[start + lastTerm.offset])
				{
					break;
				}
				start++;
			}
			int length = term.length - start;
			output.WriteVInt(start);
			// write shared prefix length
			output.WriteVInt(length);
			// write delta length
			output.WriteBytes(term.bytes, start + term.offset, length);
			// write delta bytes
			output.WriteVInt(fieldNumber);
			// write field num
			lastTerm.CopyBytes(term);
		}

		/// <summary>Called to complete TermInfos creation.</summary>
		/// <remarks>Called to complete TermInfos creation.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public void Close()
		{
			try
			{
				output.Seek(4);
				// write size after format
				output.WriteLong(size);
			}
			finally
			{
				try
				{
					output.Close();
				}
				finally
				{
					if (!isIndex)
					{
						other.Close();
					}
				}
			}
		}
	}
}
