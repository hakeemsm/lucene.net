/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Globalization;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	internal class SimpleTextUtil
	{
		public const byte NEWLINE = 10;

		public const byte ESCAPE = 92;

		internal static readonly BytesRef CHECKSUM = new BytesRef("checksum ");

		/// <exception cref="System.IO.IOException"></exception>
		public static void Write(DataOutput @out, string s, BytesRef scratch)
		{
			UnicodeUtil.UTF16toUTF8(s, 0, s.Length, scratch);
			Write(@out, scratch);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void Write(DataOutput @out, BytesRef b)
		{
			for (int i = 0; i < b.length; i++)
			{
				byte bx = b.bytes[b.offset + i];
				if (bx == NEWLINE || bx == ESCAPE)
				{
					@out.WriteByte(ESCAPE);
				}
				@out.WriteByte(bx);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void WriteNewline(DataOutput @out)
		{
			@out.WriteByte(NEWLINE);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void ReadLine(DataInput @in, BytesRef scratch)
		{
			int upto = 0;
			while (true)
			{
				byte b = @in.ReadByte();
				if (scratch.bytes.Length == upto)
				{
					scratch.Grow(1 + upto);
				}
				if (b == ESCAPE)
				{
					scratch.bytes[upto++] = @in.ReadByte();
				}
				else
				{
					if (b == NEWLINE)
					{
						break;
					}
					else
					{
						scratch.bytes[upto++] = b;
					}
				}
			}
			scratch.offset = 0;
			scratch.length = upto;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void WriteChecksum(IndexOutput @out, BytesRef scratch)
		{
			// Pad with zeros so different checksum values use the
			// same number of bytes
			// (BaseIndexFileFormatTestCase.testMergeStability cares):
			string checksum = string.Format(CultureInfo.ROOT, "%020d", @out.GetChecksum());
			SimpleTextUtil.Write(@out, CHECKSUM);
			SimpleTextUtil.Write(@out, checksum, scratch);
			SimpleTextUtil.WriteNewline(@out);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void CheckFooter(ChecksumIndexInput input)
		{
			BytesRef scratch = new BytesRef();
			string expectedChecksum = string.Format(CultureInfo.ROOT, "%020d", input.GetChecksum
				());
			SimpleTextUtil.ReadLine(input, scratch);
			if (StringHelper.StartsWith(scratch, CHECKSUM) == false)
			{
				throw new CorruptIndexException("SimpleText failure: expected checksum line but got "
					 + scratch.Utf8ToString() + " (resource=" + input + ")");
			}
			string actualChecksum = new BytesRef(scratch.bytes, CHECKSUM.length, scratch.length
				 - CHECKSUM.length).Utf8ToString();
			if (!expectedChecksum.Equals(actualChecksum))
			{
				throw new CorruptIndexException("SimpleText checksum failure: " + actualChecksum 
					+ " != " + expectedChecksum + " (resource=" + input + ")");
			}
			if (input.Length() != input.FilePointer)
			{
				throw new CorruptIndexException("Unexpected stuff at the end of file, please be careful with your text editor! (resource="
					 + input + ")");
			}
		}
	}
}
