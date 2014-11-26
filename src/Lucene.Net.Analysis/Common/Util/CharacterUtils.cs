using System.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Util
{
    public abstract class CharacterUtils
    {
        //private static readonly Java4CharacterUtils JAVA_4 = new Java4CharacterUtils();
        //private static readonly Java5CharacterUtils JAVA_5 = new Java5CharacterUtils();

        // .NET Port: we never changed how we handle strings and chars :-)
        public static readonly DotNetCharacterUtils DOTNET = new DotNetCharacterUtils();

        public static CharacterUtils GetInstance(Lucene.Net.Util.Version? matchVersion)
        {
            //return matchVersion.OnOrAfter(Lucene.Net.Util.Version.LUCENE_31) ? JAVA_5 : JAVA_4;
            return DOTNET;
        }

        public static CharacterUtils GetInstance()
        {
            return DOTNET;
        }

        public abstract int CodePointAt(char[] chars, int offset);

        public abstract int CodePointAt(ICharSequence seq, int offset);

        public abstract int CodePointAt(char[] chars, int offset, int limit);

		/// <summary>Return the number of characters in <code>seq</code>.</summary>
		/// <remarks>Return the number of characters in <code>seq</code>.</remarks>
		public abstract int CodePointCount(ICharSequence seq);
        public static CharacterBuffer NewCharacterBuffer(int bufferSize)
        {
            if (bufferSize < 2)
            {
                throw new ArgumentException("buffersize must be >= 2");
            }
            return new CharacterBuffer(new char[bufferSize], 0, 0);
        }

        public virtual void ToLowerCase(char[] buffer, int offset, int limit)
        {
            //assert buffer.length >= limit;
            //assert offset <=0 && offset <= buffer.length;
            for (int i = offset; i < limit; )
            {
                i += Character.ToChars(Character.ToLowerCase(CodePointAt(buffer, i)), buffer, i);
            }
        }

        public virtual void ToUpperCase(char[] buffer, int offset, int limit)
        {
            //assert buffer.length >= limit;
            //assert offset <=0 && offset <= buffer.length;
            for (int i = offset; i < limit; )
            {
                i += Character.ToChars(Character.ToUpperCase(CodePointAt(buffer, i)), buffer, i);
            }
        }

		/// <summary>Converts a sequence of Java characters to a sequence of unicode code points.
		/// 	</summary>
		/// <remarks>Converts a sequence of Java characters to a sequence of unicode code points.
		/// 	</remarks>
		/// <returns>the number of code points written to the destination buffer</returns>
		public int ToCodePoints(char[] src, int srcOff, int srcLen, int[] dest, int destOff)
		{
			if (srcLen < 0)
			{
				throw new ArgumentException("srcLen must be >= 0");
			}
			int codePointCount = 0;
			for (int i = 0; i < srcLen; )
			{
				int cp = CodePointAt(src, srcOff + i, srcOff + srcLen);
				int charCount = Character.CharCount(cp);
				dest[destOff + codePointCount++] = cp;
				i += charCount;
			}
			return codePointCount;
		}
		/// <summary>Converts a sequence of unicode code points to a sequence of Java characters.
		/// 	</summary>
		/// <remarks>Converts a sequence of unicode code points to a sequence of Java characters.
		/// 	</remarks>
		/// <returns>the number of chars written to the destination buffer</returns>
		public int ToChars(int[] src, int srcOff, int srcLen, char[] dest, int destOff)
		{
			if (srcLen < 0)
			{
				throw new ArgumentException("srcLen must be >= 0");
			}
			int written = 0;
			for (int i = 0; i < srcLen; ++i)
			{
				written += Character.ToChars(src[srcOff + i], dest, destOff + written);
			}
			return written;
		}

        /// <summary>
        /// Fills the
        /// <see cref="CharacterBuffer">CharacterBuffer</see>
        /// with characters read from the given
        /// reader
        /// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
        /// . This method tries to read <code>numChars</code>
        /// characters into the
        /// <see cref="CharacterBuffer">CharacterBuffer</see>
        /// , each call to fill will start
        /// filling the buffer from offset <code>0</code> up to <code>numChars</code>.
        /// In case code points can span across 2 java characters, this method may
        /// only fill <code>numChars - 1</code> characters in order not to split in
        /// the middle of a surrogate pair, even if there are remaining characters in
        /// the
        /// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
        /// .
        /// <p>
        /// Depending on the
        /// <see cref="Lucene..NetUtil.Version">Lucene..NetUtil.Version</see>
        /// passed to
        /// <see cref="GetInstance(Lucene..NetUtil.Version)">GetInstance(Lucene..NetUtil.Version)
        /// 	</see>
        /// this method implements
        /// supplementary character awareness when filling the given buffer. For all
        /// <see cref="Lucene..NetUtil.Version">Lucene..NetUtil.Version</see>
        /// &gt; 3.0
        /// <see cref="Fill(CharacterBuffer, System.IO.StreamReader, int)">Fill(CharacterBuffer, System.IO.StreamReader, int)
        /// 	</see>
        /// guarantees
        /// that the given
        /// <see cref="CharacterBuffer">CharacterBuffer</see>
        /// will never contain a high surrogate
        /// character as the last element in the buffer unless it is the last available
        /// character in the reader. In other words, high and low surrogate pairs will
        /// always be preserved across buffer boarders.
        /// </p>
        /// <p>
        /// A return value of <code>false</code> means that this method call exhausted
        /// the reader, but there may be some bytes which have been read, which can be
        /// verified by checking whether <code>buffer.getLength() &gt; 0</code>.
        /// </p>
        /// </summary>
        /// <param name="buffer">the buffer to fill.</param>
        /// <param name="reader">the reader to read characters from.</param>
        /// <param name="numChars">the number of chars to read</param>
        /// <returns><code>false</code> if and only if reader.read returned -1 while trying to fill the buffer
        /// 	</returns>
        /// <exception cref="System.IO.IOException">
        /// if the reader throws an
        /// <see cref="System.IO.IOException">System.IO.IOException</see>
        /// .
        /// </exception>
        public abstract bool Fill(CharacterBuffer buffer, TextReader reader, int numChars);

		/// <summary>Convenience method which calls <code>fill(buffer, reader, buffer.buffer.length)</code>.
		/// 	</summary>
		/// <remarks>Convenience method which calls <code>fill(buffer, reader, buffer.buffer.length)</code>.
		/// 	</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public bool Fill(CharacterUtils.CharacterBuffer buffer, StreamReader reader)
		{
			return Fill(buffer, reader, buffer.buffer.Length);
		}
		/// <summary>
		/// Return the index within <code>buf[start:start+count]</code> which is by <code>offset</code>
		/// code points from <code>index</code>.
		/// </summary>
		/// <remarks>
		/// Return the index within <code>buf[start:start+count]</code> which is by <code>offset</code>
		/// code points from <code>index</code>.
		/// </remarks>
		public abstract int OffsetByCodePoints(char[] buf, int start, int count, int index
			, int offset);

		/// <exception cref="System.IO.IOException"></exception>
		internal static int ReadFully(TextReader reader, char[] dest, int offset, int len
			)
		{
			int read = 0;
			while (read < len)
			{
				int r = reader.Read(dest, offset + read, len - read);
				if (r == -1)
				{
					break;
				}
				read += r;
			}
			return read;
		}
        // .NET Port: instead of the java-specific types here, we can use .NET's support for UTF-16 strings/chars
        public sealed class DotNetCharacterUtils : CharacterUtils
        {

            public override int CodePointAt(char[] chars, int offset)
            {
                return (int)chars[offset];
            }

            public override int CodePointAt(ICharSequence seq, int offset)
            {
                return (int)seq.CharAt(offset);
            }

            public override int CodePointAt(char[] chars, int offset, int limit)
            {
				if (offset >= limit)
				{
					throw new IndexOutOfRangeException("offset must be less than limit");
				}
                return (int)chars[offset];
            }

            public override bool Fill(CharacterBuffer buffer, TextReader reader, int numChars)
            {
				Debug.Assert(buffer.Buffer.Length>=1);
				
				if (numChars < 1 || numChars > buffer.buffer.Length)
				{
					throw new ArgumentException("numChars must be >= 1 and <= the buffer size");
				}
                buffer.offset = 0;
				int read = ReadFully(reader, buffer.buffer, 0, numChars);
				buffer.length = read;
				buffer.lastTrailingHighSurrogate = (char)0;
				return read == numChars;
        }

			public override int CodePointCount(ICharSequence seq)
			{
				return seq.Length;
			}

			public override int OffsetByCodePoints(char[] buf, int start, int count, int index
				, int offset)
			{
				int result = index + offset;
				if (result < 0 || result > count)
				{
					throw new IndexOutOfRangeException();
				}
				return result;
			}
		}
        public sealed class CharacterBuffer
        {
            internal readonly char[] buffer;
            internal int offset;
            internal int length;
            //// NOTE: not private so outer class can access without
            //// $access methods:
            internal char lastTrailingHighSurrogate;

            internal CharacterBuffer(char[] buffer, int offset, int length)
            {
                this.buffer = buffer;
                this.offset = offset;
                this.length = length;
            }

            public char[] Buffer
            {
                get
                {
                    return buffer;
                }
            }

            public int Offset
            {
                get
                {
                    return offset;
                }
            }

            public int Length
            {
                get
                {
                    return length;
                }
            }

            public void Reset()
            {
                offset = 0;
                length = 0;
                lastTrailingHighSurrogate = '0';
            }
        }
    }
}
