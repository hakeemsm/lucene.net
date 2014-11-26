/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	/// <summary>Writes plain-text stored fields.</summary>
	/// <remarks>
	/// Writes plain-text stored fields.
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextStoredFieldsWriter : StoredFieldsWriter
	{
		private int numDocsWritten = 0;

		private readonly Directory directory;

		private readonly string segment;

		private IndexOutput @out;

		internal static readonly string FIELDS_EXTENSION = "fld";

		internal static readonly BytesRef TYPE_STRING = new BytesRef("string");

		internal static readonly BytesRef TYPE_BINARY = new BytesRef("binary");

		internal static readonly BytesRef TYPE_INT = new BytesRef("int");

		internal static readonly BytesRef TYPE_LONG = new BytesRef("long");

		internal static readonly BytesRef TYPE_FLOAT = new BytesRef("float");

		internal static readonly BytesRef TYPE_DOUBLE = new BytesRef("double");

		internal static readonly BytesRef END = new BytesRef("END");

		internal static readonly BytesRef DOC = new BytesRef("doc ");

		internal static readonly BytesRef NUM = new BytesRef("  numfields ");

		internal static readonly BytesRef FIELD = new BytesRef("  field ");

		internal static readonly BytesRef NAME = new BytesRef("    name ");

		internal static readonly BytesRef TYPE = new BytesRef("    type ");

		internal static readonly BytesRef VALUE = new BytesRef("    value ");

		private readonly BytesRef scratch = new BytesRef();

		/// <exception cref="System.IO.IOException"></exception>
		public SimpleTextStoredFieldsWriter(Directory directory, string segment, IOContext
			 context)
		{
			this.directory = directory;
			this.segment = segment;
			bool success = false;
			try
			{
				@out = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, string.Empty
					, FIELDS_EXTENSION), context);
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
		public override void StartDocument(int numStoredFields)
		{
			Write(DOC);
			Write(Sharpen.Extensions.ToString(numDocsWritten));
			NewLine();
			Write(NUM);
			Write(Sharpen.Extensions.ToString(numStoredFields));
			NewLine();
			numDocsWritten++;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void WriteField(FieldInfo info, IndexableField field)
		{
			Write(FIELD);
			Write(Sharpen.Extensions.ToString(info.number));
			NewLine();
			Write(NAME);
			Write(field.Name());
			NewLine();
			Write(TYPE);
			Number n = field.NumericValue();
			if (n != null)
			{
				if (n is byte || n is short || n is int)
				{
					Write(TYPE_INT);
					NewLine();
					Write(VALUE);
					Write(Sharpen.Extensions.ToString(n));
					NewLine();
				}
				else
				{
					if (n is long)
					{
						Write(TYPE_LONG);
						NewLine();
						Write(VALUE);
						Write(System.Convert.ToString(n));
						NewLine();
					}
					else
					{
						if (n is float)
						{
							Write(TYPE_FLOAT);
							NewLine();
							Write(VALUE);
							Write(float.ToString(n));
							NewLine();
						}
						else
						{
							if (n is double)
							{
								Write(TYPE_DOUBLE);
								NewLine();
								Write(VALUE);
								Write(double.ToString(n));
								NewLine();
							}
							else
							{
								throw new ArgumentException("cannot store numeric type " + n.GetType());
							}
						}
					}
				}
			}
			else
			{
				BytesRef bytes = field.BinaryValue();
				if (bytes != null)
				{
					Write(TYPE_BINARY);
					NewLine();
					Write(VALUE);
					Write(bytes);
					NewLine();
				}
				else
				{
					if (field.StringValue() == null)
					{
						throw new ArgumentException("field " + field.Name() + " is stored but does not have binaryValue, stringValue nor numericValue"
							);
					}
					else
					{
						Write(TYPE_STRING);
						NewLine();
						Write(VALUE);
						Write(field.StringValue());
						NewLine();
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
				, string.Empty, FIELDS_EXTENSION));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Finish(FieldInfos fis, int numDocs)
		{
			if (numDocsWritten != numDocs)
			{
				throw new RuntimeException("mergeFields produced an invalid result: docCount is "
					 + numDocs + " but only saw " + numDocsWritten + " file=" + @out.ToString() + "; now aborting this merge to prevent index corruption"
					);
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
