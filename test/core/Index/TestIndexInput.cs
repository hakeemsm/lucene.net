/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestIndexInput : LuceneTestCase
	{
		internal static readonly byte[] READ_TEST_BYTES = new byte[] { unchecked((byte)unchecked(
			(int)(0x80))), unchecked((int)(0x01)), unchecked((byte)unchecked((int)(0xFF))), 
			unchecked((int)(0x7F)), unchecked((byte)unchecked((int)(0x80))), unchecked((byte
			)unchecked((int)(0x80))), unchecked((int)(0x01)), unchecked((byte)unchecked((int
			)(0x81))), unchecked((byte)unchecked((int)(0x80))), unchecked((int)(0x01)), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0x07))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0x0F))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0x07))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0x7F))), unchecked(
			(int)(0x06)), (byte)('L'), (byte)('u'), (byte)('c'), (byte)('e'), (byte)('n'), (
			byte)('e'), unchecked((int)(0x02)), unchecked((byte)unchecked((int)(0xC2))), unchecked(
			(byte)unchecked((int)(0xBF))), unchecked((int)(0x0A)), (byte)('L'), (byte)('u'), 
			unchecked((byte)unchecked((int)(0xC2))), unchecked((byte)unchecked((int)(0xBF)))
			, (byte)('c'), (byte)('e'), unchecked((byte)unchecked((int)(0xC2))), unchecked((
			byte)unchecked((int)(0xBF))), (byte)('n'), (byte)('e'), unchecked((int)(0x03)), 
			unchecked((byte)unchecked((int)(0xE2))), unchecked((byte)unchecked((int)(0x98)))
			, unchecked((byte)unchecked((int)(0xA0))), unchecked((int)(0x0C)), (byte)('L'), 
			(byte)('u'), unchecked((byte)unchecked((int)(0xE2))), unchecked((byte)unchecked(
			(int)(0x98))), unchecked((byte)unchecked((int)(0xA0))), (byte)('c'), (byte)('e')
			, unchecked((byte)unchecked((int)(0xE2))), unchecked((byte)unchecked((int)(0x98)
			)), unchecked((byte)unchecked((int)(0xA0))), (byte)('n'), (byte)('e'), unchecked(
			(int)(0x04)), unchecked((byte)unchecked((int)(0xF0))), unchecked((byte)unchecked(
			(int)(0x9D))), unchecked((byte)unchecked((int)(0x84))), unchecked((byte)unchecked(
			(int)(0x9E))), unchecked((int)(0x08)), unchecked((byte)unchecked((int)(0xF0))), 
			unchecked((byte)unchecked((int)(0x9D))), unchecked((byte)unchecked((int)(0x84)))
			, unchecked((byte)unchecked((int)(0x9E))), unchecked((byte)unchecked((int)(0xF0)
			)), unchecked((byte)unchecked((int)(0x9D))), unchecked((byte)unchecked((int)(0x85
			))), unchecked((byte)unchecked((int)(0xA0))), unchecked((int)(0x0E)), (byte)('L'
			), (byte)('u'), unchecked((byte)unchecked((int)(0xF0))), unchecked((byte)unchecked(
			(int)(0x9D))), unchecked((byte)unchecked((int)(0x84))), unchecked((byte)unchecked(
			(int)(0x9E))), (byte)('c'), (byte)('e'), unchecked((byte)unchecked((int)(0xF0)))
			, unchecked((byte)unchecked((int)(0x9D))), unchecked((byte)unchecked((int)(0x85)
			)), unchecked((byte)unchecked((int)(0xA0))), (byte)('n'), (byte)('e'), unchecked(
			(int)(0x01)), unchecked((int)(0x00)), unchecked((int)(0x08)), (byte)('L'), (byte
			)('u'), unchecked((int)(0x00)), (byte)('c'), (byte)('e'), unchecked((int)(0x00))
			, (byte)('n'), (byte)('e'), unchecked((byte)unchecked((int)(0xFF))), unchecked((
			byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0x17))), unchecked(
			(byte)unchecked((int)(0x01))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0xFF))), unchecked((byte)unchecked((int)(0xFF))), unchecked(
			(byte)unchecked((int)(0x01))) };

		internal static readonly int COUNT = RANDOM_MULTIPLIER * 65536;

		internal static int[] INTS;

		internal static long[] LONGS;

		internal static byte[] RANDOM_TEST_BYTES;

		// 2-byte UTF-8 (U+00BF "INVERTED QUESTION MARK") 
		// 3-byte UTF-8 (U+2620 "SKULL AND CROSSBONES") 
		// surrogate pairs
		// (U+1D11E "MUSICAL SYMBOL G CLEF")
		// (U+1D160 "MUSICAL SYMBOL EIGHTH NOTE")
		// null bytes
		// tests for Exceptions on invalid values
		// guard value
		// guard value
		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			Random random = Random();
			INTS = new int[COUNT];
			LONGS = new long[COUNT];
			RANDOM_TEST_BYTES = new byte[COUNT * (5 + 4 + 9 + 8)];
			ByteArrayDataOutput bdo = new ByteArrayDataOutput(RANDOM_TEST_BYTES);
			for (int i = 0; i < COUNT; i++)
			{
				int i1 = INTS[i] = random.Next();
				bdo.WriteVInt(i1);
				bdo.WriteInt(i1);
				long l1;
				if (Rarely())
				{
					// a long with lots of zeroes at the end
					l1 = LONGS[i] = TestUtil.NextLong(random, 0, int.MaxValue) << 32;
				}
				else
				{
					l1 = LONGS[i] = TestUtil.NextLong(random, 0, long.MaxValue);
				}
				bdo.WriteVLong(l1);
				bdo.WriteLong(l1);
			}
		}

		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			INTS = null;
			LONGS = null;
			RANDOM_TEST_BYTES = null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckReads<_T0>(DataInput @is, Type<_T0> expectedEx) where _T0:Exception
		{
			AreEqual(128, @is.ReadVInt());
			AreEqual(16383, @is.ReadVInt());
			AreEqual(16384, @is.ReadVInt());
			AreEqual(16385, @is.ReadVInt());
			AreEqual(int.MaxValue, @is.ReadVInt());
			AreEqual(-1, @is.ReadVInt());
			AreEqual((long)int.MaxValue, @is.ReadVLong());
			AreEqual(long.MaxValue, @is.ReadVLong());
			AreEqual("Lucene", @is.ReadString());
			AreEqual("\u00BF", @is.ReadString());
			AreEqual("Lu\u00BFce\u00BFne", @is.ReadString());
			AreEqual("\u2620", @is.ReadString());
			AreEqual("Lu\u2620ce\u2620ne", @is.ReadString());
			AreEqual("\uD834\uDD1E", @is.ReadString());
			AreEqual("\uD834\uDD1E\uD834\uDD60", @is.ReadString());
			AreEqual("Lu\uD834\uDD1Ece\uD834\uDD60ne", @is.ReadString(
				));
			AreEqual("\u0000", @is.ReadString());
			AreEqual("Lu\u0000ce\u0000ne", @is.ReadString());
			try
			{
				@is.ReadVInt();
				Fail("Should throw " + expectedEx.FullName);
			}
			catch (Exception e)
			{
				IsTrue(e.Message.StartsWith("Invalid vInt"));
				IsTrue(expectedEx.IsInstanceOfType(e));
			}
			AreEqual(1, @is.ReadVInt());
			// guard value
			try
			{
				@is.ReadVLong();
				Fail("Should throw " + expectedEx.FullName);
			}
			catch (Exception e)
			{
				IsTrue(e.Message.StartsWith("Invalid vLong"));
				IsTrue(expectedEx.IsInstanceOfType(e));
			}
			AreEqual(1L, @is.ReadVLong());
		}

		// guard value
		/// <exception cref="System.IO.IOException"></exception>
		private void CheckRandomReads(DataInput @is)
		{
			for (int i = 0; i < COUNT; i++)
			{
				AreEqual(INTS[i], @is.ReadVInt());
				AreEqual(INTS[i], @is.ReadInt());
				AreEqual(LONGS[i], @is.ReadVLong());
				AreEqual(LONGS[i], @is.ReadLong());
			}
		}

		// this test only checks BufferedIndexInput because MockIndexInput extends BufferedIndexInput
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBufferedIndexInputRead()
		{
			IndexInput @is = new MockIndexInput(READ_TEST_BYTES);
			CheckReads(@is, typeof(IOException));
			@is.Dispose();
			@is = new MockIndexInput(RANDOM_TEST_BYTES);
			CheckRandomReads(@is);
			@is.Dispose();
		}

		// this test checks the raw IndexInput methods as it uses RAMIndexInput which extends IndexInput directly
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRawIndexInputRead()
		{
			Random random = Random();
			RAMDirectory dir = new RAMDirectory();
			IndexOutput os = dir.CreateOutput("foo", NewIOContext(random));
			os.WriteBytes(READ_TEST_BYTES, READ_TEST_BYTES.Length);
			os.Dispose();
			IndexInput @is = dir.OpenInput("foo", NewIOContext(random));
			CheckReads(@is, typeof(IOException));
			@is.Dispose();
			os = dir.CreateOutput("bar", NewIOContext(random));
			os.WriteBytes(RANDOM_TEST_BYTES, RANDOM_TEST_BYTES.Length);
			os.Dispose();
			@is = dir.OpenInput("bar", NewIOContext(random));
			CheckRandomReads(@is);
			@is.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestByteArrayDataInput()
		{
			ByteArrayDataInput @is = new ByteArrayDataInput(READ_TEST_BYTES);
			CheckReads(@is, typeof(RuntimeException));
			@is = new ByteArrayDataInput(RANDOM_TEST_BYTES);
			CheckRandomReads(@is);
		}
	}
}
