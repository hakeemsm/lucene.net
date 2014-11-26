/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	/// <summary>Writes plain-text term vectors.</summary>
	/// <remarks>
	/// Writes plain-text term vectors.
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextTermVectorsWriter : TermVectorsWriter
	{
		internal static readonly BytesRef END = new BytesRef("END");

		internal static readonly BytesRef DOC = new BytesRef("doc ");

		internal static readonly BytesRef NUMFIELDS = new BytesRef("  numfields ");

		internal static readonly BytesRef FIELD = new BytesRef("  field ");

		internal static readonly BytesRef FIELDNAME = new BytesRef("    name ");

		internal static readonly BytesRef FIELDPOSITIONS = new BytesRef("    positions ");

		internal static readonly BytesRef FIELDOFFSETS = new BytesRef("    offsets   ");

		internal static readonly BytesRef FIELDPAYLOADS = new BytesRef("    payloads  ");

		internal static readonly BytesRef FIELDTERMCOUNT = new BytesRef("    numterms ");

		internal static readonly BytesRef TERMTEXT = new BytesRef("    term ");

		internal static readonly BytesRef TERMFREQ = new BytesRef("      freq ");

		internal static readonly BytesRef POSITION = new BytesRef("      position ");

		internal static readonly BytesRef PAYLOAD = new BytesRef("        payload ");

		internal static readonly BytesRef STARTOFFSET = new BytesRef("        startoffset "
			);

		internal static readonly BytesRef ENDOFFSET = new BytesRef("        endoffset ");

		internal static readonly string VECTORS_EXTENSION = "vec";

		private readonly Directory directory;

		private readonly string segment;

		private IndexOutput @out;

		private int numDocsWritten = 0;

		private readonly BytesRef scratch = new BytesRef();

		private bool offsets;

		private bool positions;

		private bool payloads;

		/// <exception cref="System.IO.IOException"></exception>
		public SimpleTextTermVectorsWriter(Directory directory, string segment, IOContext
			 context)
		{
			this.directory = directory;
			this.segment = segment;
			bool success = false;
			try
			{
				@out = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, string.Empty
					, VECTORS_EXTENSION), context);
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

		/// <exception cref="System.IO.IOException"></exception>
		public override void StartDocument(int numVectorFields)
		{
			Write(DOC);
			Write(Sharpen.Extensions.ToString(numDocsWritten));
			NewLine();
			Write(NUMFIELDS);
			Write(Sharpen.Extensions.ToString(numVectorFields));
			NewLine();
			numDocsWritten++;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void StartField(FieldInfo info, int numTerms, bool positions, bool
			 offsets, bool payloads)
		{
			Write(FIELD);
			Write(Sharpen.Extensions.ToString(info.number));
			NewLine();
			Write(FIELDNAME);
			Write(info.name);
			NewLine();
			Write(FIELDPOSITIONS);
			Write(bool.ToString(positions));
			NewLine();
			Write(FIELDOFFSETS);
			Write(bool.ToString(offsets));
			NewLine();
			Write(FIELDPAYLOADS);
			Write(bool.ToString(payloads));
			NewLine();
			Write(FIELDTERMCOUNT);
			Write(Sharpen.Extensions.ToString(numTerms));
			NewLine();
			this.positions = positions;
			this.offsets = offsets;
			this.payloads = payloads;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void StartTerm(BytesRef term, int freq)
		{
			Write(TERMTEXT);
			Write(term);
			NewLine();
			Write(TERMFREQ);
			Write(Sharpen.Extensions.ToString(freq));
			NewLine();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddPosition(int position, int startOffset, int endOffset, BytesRef
			 payload)
		{
			//HM:revisit 
			//assert positions || offsets;
			if (positions)
			{
				Write(POSITION);
				Write(Sharpen.Extensions.ToString(position));
				NewLine();
				if (payloads)
				{
					Write(PAYLOAD);
					if (payload != null)
					{
						//HM:revisit 
						//assert payload.length > 0;
						Write(payload);
					}
					NewLine();
				}
			}
			if (offsets)
			{
				Write(STARTOFFSET);
				Write(Sharpen.Extensions.ToString(startOffset));
				NewLine();
				Write(ENDOFFSET);
				Write(Sharpen.Extensions.ToString(endOffset));
				NewLine();
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
				, string.Empty, VECTORS_EXTENSION));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Finish(FieldInfos fis, int numDocs)
		{
			if (numDocsWritten != numDocs)
			{
				throw new RuntimeException("mergeVectors produced an invalid result: mergedDocs is "
					 + numDocs + " but vec numDocs is " + numDocsWritten + " file=" + @out.ToString(
					) + "; now aborting this merge to prevent index corruption");
			}
			Write(END);
			NewLine();
			SimpleTextUtil.WriteChecksum(@out, scratch);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				IOUtils.Close(@out);
			}
			finally
			{
				@out = null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IComparer<BytesRef> GetComparator()
		{
			return BytesRef.GetUTF8SortedAsUnicodeComparator();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Write(string s)
		{
			SimpleTextUtil.Write(@out, s, scratch);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Write(BytesRef bytes)
		{
			SimpleTextUtil.Write(@out, bytes);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void NewLine()
		{
			SimpleTextUtil.WriteNewline(@out);
		}
	}
}
