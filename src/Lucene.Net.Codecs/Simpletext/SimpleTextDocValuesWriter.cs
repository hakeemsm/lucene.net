using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Simpletext
{
	internal class SimpleTextDocValuesWriter : DocValuesConsumer
	{
		internal static readonly BytesRef END = new BytesRef("END");

		internal static readonly BytesRef FIELD = new BytesRef("field ");

		internal static readonly BytesRef TYPE = new BytesRef("  type ");

		internal static readonly BytesRef MINVALUE = new BytesRef("  minvalue ");

		internal static readonly BytesRef PATTERN = new BytesRef("  pattern ");

		internal static readonly BytesRef LENGTH = new BytesRef("length ");

		internal static readonly BytesRef MAXLENGTH = new BytesRef("  maxlength ");

		internal static readonly BytesRef NUMVALUES = new BytesRef("  numvalues ");

		internal static readonly BytesRef ORDPATTERN = new BytesRef("  ordpattern ");

		internal IndexOutput data;

		internal readonly BytesRef scratch = new BytesRef();

		internal readonly int numDocs;

		private readonly ICollection<string> fieldsSeen = new HashSet<string>();

		/// <exception cref="System.IO.IOException"></exception>
		public SimpleTextDocValuesWriter(SegmentWriteState state, string ext)
		{
			// used for numerics
			// used for bytes
			// used for sorted bytes
			// for asserting
			// System.out.println("WRITE: " + IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, ext) + " " + state.segmentInfo.getDocCount() + " docs");
			data = state.directory.CreateOutput(IndexFileNames.SegmentFileName(state.segmentInfo
				.name, state.segmentSuffix, ext), state.context);
			numDocs = state.segmentInfo.DocCount;
		}

		// for asserting
		private bool FieldSeen(string field)
		{
			//HM:revisit 
			//assert !fieldsSeen.contains(field): "field \"" + field + "\" was added more than once during flush";
			fieldsSeen.AddItem(field);
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddNumericField(FieldInfo field, Iterable<Number> values)
		{
			//HM:revisit 
			//assert fieldSeen(field.name);
			//HM:revisit 
			//assert (field.getDocValuesType() == FieldInfo.DocValuesType.NUMERIC || field.getNormType() == FieldInfo.DocValuesType.NUMERIC);
			WriteFieldEntry(field, FieldInfo.DocValuesType.NUMERIC);
			// first pass to find min/max
			long minValue = long.MaxValue;
			long maxValue = long.MinValue;
			foreach (Number n in values)
			{
				long v = n == null ? 0 : n;
				minValue = Math.Min(minValue, v);
				maxValue = Math.Max(maxValue, v);
			}
			// write our minimum value to the .dat, all entries are deltas from that
			SimpleTextUtil.Write(data, MINVALUE);
			SimpleTextUtil.Write(data, System.Convert.ToString(minValue), scratch);
			SimpleTextUtil.WriteNewline(data);
			// build up our fixed-width "simple text packed ints"
			// format
			BigInteger maxBig = BigInteger.ValueOf(maxValue);
			BigInteger minBig = BigInteger.ValueOf(minValue);
			BigInteger diffBig = maxBig.Subtract(minBig);
			int maxBytesPerValue = diffBig.ToString().Length;
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < maxBytesPerValue; i++)
			{
				sb.Append('0');
			}
			// write our pattern to the .dat
			SimpleTextUtil.Write(data, PATTERN);
			SimpleTextUtil.Write(data, sb.ToString(), scratch);
			SimpleTextUtil.WriteNewline(data);
			string patternString = sb.ToString();
			DecimalFormat encoder = new DecimalFormat(patternString, new DecimalFormatSymbols
				(CultureInfo.ROOT));
			int numDocsWritten = 0;
			// second pass to write the values
			foreach (Number n_1 in values)
			{
				long value = n_1 == null ? 0 : n_1;
				//HM:revisit 
				//assert value >= minValue;
				Number delta = BigInteger.ValueOf(value).Subtract(BigInteger.ValueOf(minValue));
				string s = encoder.Format(delta);
				//HM:revisit 
				//assert s.length() == patternString.length();
				SimpleTextUtil.Write(data, s, scratch);
				SimpleTextUtil.WriteNewline(data);
				if (n_1 == null)
				{
					SimpleTextUtil.Write(data, "F", scratch);
				}
				else
				{
					SimpleTextUtil.Write(data, "T", scratch);
				}
				SimpleTextUtil.WriteNewline(data);
				numDocsWritten++;
			}
		}

		//HM:revisit 
		//assert numDocsWritten <= numDocs;
		//HM:revisit 
		//assert numDocs == numDocsWritten: "numDocs=" + numDocs + " numDocsWritten=" + numDocsWritten;
		/// <exception cref="System.IO.IOException"></exception>
		public override void AddBinaryField(FieldInfo field, Iterable<BytesRef> values)
		{
			//HM:revisit 
			//assert fieldSeen(field.name);
			//HM:revisit 
			//assert field.getDocValuesType() == DocValuesType.BINARY;
			int maxLength = 0;
			foreach (BytesRef value in values)
			{
				int length = value == null ? 0 : value.length;
				maxLength = System.Math.Max(maxLength, length);
			}
			WriteFieldEntry(field, FieldInfo.DocValuesType.BINARY);
			// write maxLength
			SimpleTextUtil.Write(data, MAXLENGTH);
			SimpleTextUtil.Write(data, Sharpen.Extensions.ToString(maxLength), scratch);
			SimpleTextUtil.WriteNewline(data);
			int maxBytesLength = System.Convert.ToString(maxLength).Length;
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < maxBytesLength; i++)
			{
				sb.Append('0');
			}
			// write our pattern for encoding lengths
			SimpleTextUtil.Write(data, PATTERN);
			SimpleTextUtil.Write(data, sb.ToString(), scratch);
			SimpleTextUtil.WriteNewline(data);
			DecimalFormat encoder = new DecimalFormat(sb.ToString(), new DecimalFormatSymbols
				(CultureInfo.ROOT));
			int numDocsWritten = 0;
			foreach (BytesRef value_1 in values)
			{
				// write length
				int length = value_1 == null ? 0 : value_1.length;
				SimpleTextUtil.Write(data, LENGTH);
				SimpleTextUtil.Write(data, encoder.Format(length), scratch);
				SimpleTextUtil.WriteNewline(data);
				// write bytes -- don't use SimpleText.write
				// because it escapes:
				if (value_1 != null)
				{
					data.WriteBytes(value_1.bytes, value_1.offset, value_1.length);
				}
				// pad to fit
				for (int i_1 = length; i_1 < maxLength; i_1++)
				{
					data.WriteByte(unchecked((byte)' '));
				}
				SimpleTextUtil.WriteNewline(data);
				if (value_1 == null)
				{
					SimpleTextUtil.Write(data, "F", scratch);
				}
				else
				{
					SimpleTextUtil.Write(data, "T", scratch);
				}
				SimpleTextUtil.WriteNewline(data);
				numDocsWritten++;
			}
		}

		//HM:revisit 
		//assert numDocs == numDocsWritten;
		/// <exception cref="System.IO.IOException"></exception>
		public override void AddSortedField(FieldInfo field, Iterable<BytesRef> values, Iterable
			<Number> docToOrd)
		{
			//HM:revisit 
			//assert fieldSeen(field.name);
			//HM:revisit 
			//assert field.getDocValuesType() == DocValuesType.SORTED;
			WriteFieldEntry(field, FieldInfo.DocValuesType.SORTED);
			int valueCount = 0;
			int maxLength = -1;
			foreach (BytesRef value in values)
			{
				maxLength = System.Math.Max(maxLength, value.length);
				valueCount++;
			}
			// write numValues
			SimpleTextUtil.Write(data, NUMVALUES);
			SimpleTextUtil.Write(data, Sharpen.Extensions.ToString(valueCount), scratch);
			SimpleTextUtil.WriteNewline(data);
			// write maxLength
			SimpleTextUtil.Write(data, MAXLENGTH);
			SimpleTextUtil.Write(data, Sharpen.Extensions.ToString(maxLength), scratch);
			SimpleTextUtil.WriteNewline(data);
			int maxBytesLength = Sharpen.Extensions.ToString(maxLength).Length;
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < maxBytesLength; i++)
			{
				sb.Append('0');
			}
			// write our pattern for encoding lengths
			SimpleTextUtil.Write(data, PATTERN);
			SimpleTextUtil.Write(data, sb.ToString(), scratch);
			SimpleTextUtil.WriteNewline(data);
			DecimalFormat encoder = new DecimalFormat(sb.ToString(), new DecimalFormatSymbols
				(CultureInfo.ROOT));
			int maxOrdBytes = System.Convert.ToString(valueCount + 1L).Length;
			sb.Length = 0;
			for (int i_1 = 0; i_1 < maxOrdBytes; i_1++)
			{
				sb.Append('0');
			}
			// write our pattern for ords
			SimpleTextUtil.Write(data, ORDPATTERN);
			SimpleTextUtil.Write(data, sb.ToString(), scratch);
			SimpleTextUtil.WriteNewline(data);
			DecimalFormat ordEncoder = new DecimalFormat(sb.ToString(), new DecimalFormatSymbols
				(CultureInfo.ROOT));
			// for asserts:
			int valuesSeen = 0;
			foreach (BytesRef value_1 in values)
			{
				// write length
				SimpleTextUtil.Write(data, LENGTH);
				SimpleTextUtil.Write(data, encoder.Format(value_1.length), scratch);
				SimpleTextUtil.WriteNewline(data);
				// write bytes -- don't use SimpleText.write
				// because it escapes:
				data.WriteBytes(value_1.bytes, value_1.offset, value_1.length);
				// pad to fit
				for (int i_2 = value_1.length; i_2 < maxLength; i_2++)
				{
					data.WriteByte(unchecked((byte)' '));
				}
				SimpleTextUtil.WriteNewline(data);
				valuesSeen++;
			}
			//HM:revisit 
			//assert valuesSeen <= valueCount;
			//HM:revisit 
			//assert valuesSeen == valueCount;
			foreach (Number ord in docToOrd)
			{
				SimpleTextUtil.Write(data, ordEncoder.Format(ord + 1), scratch);
				SimpleTextUtil.WriteNewline(data);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddSortedSetField(FieldInfo field, Iterable<BytesRef> values
			, Iterable<Number> docToOrdCount, Iterable<Number> ords)
		{
			//HM:revisit 
			//assert fieldSeen(field.name);
			//HM:revisit 
			//assert field.getDocValuesType() == DocValuesType.SORTED_SET;
			WriteFieldEntry(field, FieldInfo.DocValuesType.SORTED_SET);
			long valueCount = 0;
			int maxLength = 0;
			foreach (BytesRef value in values)
			{
				maxLength = System.Math.Max(maxLength, value.length);
				valueCount++;
			}
			// write numValues
			SimpleTextUtil.Write(data, NUMVALUES);
			SimpleTextUtil.Write(data, System.Convert.ToString(valueCount), scratch);
			SimpleTextUtil.WriteNewline(data);
			// write maxLength
			SimpleTextUtil.Write(data, MAXLENGTH);
			SimpleTextUtil.Write(data, Sharpen.Extensions.ToString(maxLength), scratch);
			SimpleTextUtil.WriteNewline(data);
			int maxBytesLength = Sharpen.Extensions.ToString(maxLength).Length;
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < maxBytesLength; i++)
			{
				sb.Append('0');
			}
			// write our pattern for encoding lengths
			SimpleTextUtil.Write(data, PATTERN);
			SimpleTextUtil.Write(data, sb.ToString(), scratch);
			SimpleTextUtil.WriteNewline(data);
			DecimalFormat encoder = new DecimalFormat(sb.ToString(), new DecimalFormatSymbols
				(CultureInfo.ROOT));
			// compute ord pattern: this is funny, we encode all values for all docs to find the maximum length
			int maxOrdListLength = 0;
			StringBuilder sb2 = new StringBuilder();
			Iterator<Number> ordStream = ords.Iterator();
			foreach (Number n in docToOrdCount)
			{
				sb2.Length = 0;
				int count = n;
				for (int i_1 = 0; i_1 < count; i_1++)
				{
					long ord = ordStream.Next();
					if (sb2.Length > 0)
					{
						sb2.Append(",");
					}
					sb2.Append(System.Convert.ToString(ord));
				}
				maxOrdListLength = System.Math.Max(maxOrdListLength, sb2.Length);
			}
			sb2.Length = 0;
			for (int i_2 = 0; i_2 < maxOrdListLength; i_2++)
			{
				sb2.Append('X');
			}
			// write our pattern for ord lists
			SimpleTextUtil.Write(data, ORDPATTERN);
			SimpleTextUtil.Write(data, sb2.ToString(), scratch);
			SimpleTextUtil.WriteNewline(data);
			// for asserts:
			long valuesSeen = 0;
			foreach (BytesRef value_1 in values)
			{
				// write length
				SimpleTextUtil.Write(data, LENGTH);
				SimpleTextUtil.Write(data, encoder.Format(value_1.length), scratch);
				SimpleTextUtil.WriteNewline(data);
				// write bytes -- don't use SimpleText.write
				// because it escapes:
				data.WriteBytes(value_1.bytes, value_1.offset, value_1.length);
				// pad to fit
				for (int i_1 = value_1.length; i_1 < maxLength; i_1++)
				{
					data.WriteByte(unchecked((byte)' '));
				}
				SimpleTextUtil.WriteNewline(data);
				valuesSeen++;
			}
			//HM:revisit 
			//assert valuesSeen <= valueCount;
			//HM:revisit 
			//assert valuesSeen == valueCount;
			ordStream = ords.Iterator();
			// write the ords for each doc comma-separated
			foreach (Number n_1 in docToOrdCount)
			{
				sb2.Length = 0;
				int count = n_1;
				for (int i_1 = 0; i_1 < count; i_1++)
				{
					long ord = ordStream.Next();
					if (sb2.Length > 0)
					{
						sb2.Append(",");
					}
					sb2.Append(System.Convert.ToString(ord));
				}
				// now pad to fit: these are numbers so spaces work well. reader calls trim()
				int numPadding = maxOrdListLength - sb2.Length;
				for (int i_3 = 0; i_3 < numPadding; i_3++)
				{
					sb2.Append(' ');
				}
				SimpleTextUtil.Write(data, sb2.ToString(), scratch);
				SimpleTextUtil.WriteNewline(data);
			}
		}

		/// <summary>write the header for this field</summary>
		/// <exception cref="System.IO.IOException"></exception>
		private void WriteFieldEntry(FieldInfo field, FieldInfo.DocValuesType type)
		{
			SimpleTextUtil.Write(data, FIELD);
			SimpleTextUtil.Write(data, field.name, scratch);
			SimpleTextUtil.WriteNewline(data);
			SimpleTextUtil.Write(data, TYPE);
			SimpleTextUtil.Write(data, type.ToString(), scratch);
			SimpleTextUtil.WriteNewline(data);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			if (data != null)
			{
				bool success = false;
				try
				{
					//HM:revisit 
					//assert !fieldsSeen.isEmpty();
					// TODO: sheisty to do this here?
					SimpleTextUtil.Write(data, END);
					SimpleTextUtil.WriteNewline(data);
					SimpleTextUtil.WriteChecksum(data, scratch);
					success = true;
				}
				finally
				{
					if (success)
					{
						IOUtils.Close(data);
					}
					else
					{
						IOUtils.CloseWhileHandlingException(data);
					}
					data = null;
				}
			}
		}
	}
}
