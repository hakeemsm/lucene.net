/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene3x
{
	/// <lucene.experimental></lucene.experimental>
	internal sealed class PreFlexRWStoredFieldsWriter : StoredFieldsWriter
	{
		private readonly Directory directory;

		private readonly string segment;

		private IndexOutput fieldsStream;

		private IndexOutput indexStream;

		/// <exception cref="System.IO.IOException"></exception>
		public PreFlexRWStoredFieldsWriter(Directory directory, string segment, IOContext
			 context)
		{
			 
			//assert directory != null;
			this.directory = directory;
			this.segment = segment;
			bool success = false;
			try
			{
				fieldsStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, string.Empty
					, Lucene3xStoredFieldsReader.FIELDS_EXTENSION), context);
				indexStream = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, string.Empty
					, Lucene3xStoredFieldsReader.FIELDS_INDEX_EXTENSION), context);
				fieldsStream.WriteInt(Lucene3xStoredFieldsReader.FORMAT_CURRENT);
				indexStream.WriteInt(Lucene3xStoredFieldsReader.FORMAT_CURRENT);
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

		// Writes the contents of buffer into the fields stream
		// and adds a new entry for this document into the index
		// stream.  This assumes the buffer was already written
		// in the correct fields format.
		/// <exception cref="System.IO.IOException"></exception>
		public override void StartDocument(int numStoredFields)
		{
			indexStream.WriteLong(fieldsStream.GetFilePointer());
			fieldsStream.WriteVInt(numStoredFields);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				IOUtils.Close(fieldsStream, indexStream);
			}
			finally
			{
				fieldsStream = indexStream = null;
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
				, string.Empty, Lucene3xStoredFieldsReader.FIELDS_EXTENSION), IndexFileNames.SegmentFileName
				(segment, string.Empty, Lucene3xStoredFieldsReader.FIELDS_INDEX_EXTENSION));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void WriteField(FieldInfo info, IndexableField field)
		{
			fieldsStream.WriteVInt(info.number);
			int bits = 0;
			BytesRef bytes;
			string @string;
			// TODO: maybe a field should serialize itself?
			// this way we don't bake into indexer all these
			// specific encodings for different fields?  and apps
			// can customize...
			Number number = field.NumericValue();
			if (number != null)
			{
				if (number is byte || number is short || number is int)
				{
					bits |= Lucene3xStoredFieldsReader.FIELD_IS_NUMERIC_INT;
				}
				else
				{
					if (number is long)
					{
						bits |= Lucene3xStoredFieldsReader.FIELD_IS_NUMERIC_LONG;
					}
					else
					{
						if (number is float)
						{
							bits |= Lucene3xStoredFieldsReader.FIELD_IS_NUMERIC_FLOAT;
						}
						else
						{
							if (number is double)
							{
								bits |= Lucene3xStoredFieldsReader.FIELD_IS_NUMERIC_DOUBLE;
							}
							else
							{
								throw new ArgumentException("cannot store numeric type " + number.GetType());
							}
						}
					}
				}
				@string = null;
				bytes = null;
			}
			else
			{
				bytes = field.BinaryValue();
				if (bytes != null)
				{
					bits |= Lucene3xStoredFieldsReader.FIELD_IS_BINARY;
					@string = null;
				}
				else
				{
					@string = field.StringValue();
					if (@string == null)
					{
						throw new ArgumentException("field " + field.Name() + " is stored but does not have binaryValue, stringValue nor numericValue"
							);
					}
				}
			}
			fieldsStream.WriteByte(unchecked((byte)bits));
			if (bytes != null)
			{
				fieldsStream.WriteVInt(bytes.length);
				fieldsStream.WriteBytes(bytes.bytes, bytes.offset, bytes.length);
			}
			else
			{
				if (@string != null)
				{
					fieldsStream.WriteString(field.StringValue());
				}
				else
				{
					if (number is byte || number is short || number is int)
					{
						fieldsStream.WriteInt(number);
					}
					else
					{
						if (number is long)
						{
							fieldsStream.WriteLong(number);
						}
						else
						{
							if (number is float)
							{
								fieldsStream.WriteInt(Sharpen.Runtime.FloatToIntBits(number));
							}
							else
							{
								if (number is double)
								{
									fieldsStream.WriteLong(double.DoubleToLongBits(number));
								}
							}
						}
					}
				}
			}
		}

		 
		//assert false;
		/// <exception cref="System.IO.IOException"></exception>
		public override void Finish(FieldInfos fis, int numDocs)
		{
			if (4 + ((long)numDocs) * 8 != indexStream.GetFilePointer())
			{
				// This is most likely a bug in Sun JRE 1.6.0_04/_05;
				// we detect that the bug has struck, here, and
				// throw an exception to prevent the corruption from
				// entering the index.  See LUCENE-1282 for
				// details.
				throw new RuntimeException("fdx size mismatch: docCount is " + numDocs + " but fdx file size is "
					 + indexStream.GetFilePointer() + " file=" + indexStream.ToString() + "; now aborting this merge to prevent index corruption"
					);
			}
		}
	}
}
