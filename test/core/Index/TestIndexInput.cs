using System;
using System.IO;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
    public class TestIndexInput : LuceneTestCase
	{
		internal static readonly byte[] READ_TEST_BYTES =
		{ 
            0x80,  0x01,
            0xFF,  0x7F,
            0x80,  0x80, 0x01,
            0x81,  0x80, 0x01,
            0xFF,  0xFF,  0xFF,  0xFF,  0x07,
            0xFF,  0xFF,  0xFF,  0xFF,  0x0F,
            0xFF,  0xFF,  0xFF,  0xFF,  0x07,
            0xFF,  0xFF,  0xFF,  0xFF,  0xFF,  0xFF,  0xFF,  0xFF,  0x7F,
            0x06, (byte)'L', (byte)'u', (byte)'c', (byte)'e', (byte)'n', (byte)'e',

    // 2-byte UTF-8 (U+00BF "INVERTED QUESTION MARK") 
            0x02,  0xC2,  0xBF,
            0x0A, (byte)'L', (byte)'u',  0xC2,  0xBF, 
                  (byte)'c', (byte)'e',  0xC2,  0xBF, 
                  (byte)'n', (byte)'e',

    // 3-byte UTF-8 (U+2620 "SKULL AND CROSSBONES") 
            0x03,  0xE2,  0x98,  0xA0,
            0x0C, (byte)'L', (byte)'u',  0xE2,  0x98,  0xA0,
                  (byte)'c', (byte)'e',  0xE2,  0x98,  0xA0,
                  (byte)'n', (byte)'e',

    // surrogate pairs
    // (U+1D11E "MUSICAL SYMBOL G CLEF")
    // (U+1D160 "MUSICAL SYMBOL EIGHTH NOTE")
            0x04,  0xF0,  0x9D,  0x84,  0x9E,
            0x08,  0xF0,  0x9D,  0x84,  0x9E, 
                   0xF0,  0x9D,  0x85,  0xA0, 
            0x0E, (byte)'L', (byte)'u',
                   0xF0,  0x9D,  0x84,  0x9E,
                  (byte)'c', (byte)'e', 
                   0xF0,  0x9D,  0x85,  0xA0, 
                  (byte)'n', (byte)'e',  

    // null bytes
            0x01, 0x00,
            0x08, (byte)'L', (byte)'u', 0x00, (byte)'c', (byte)'e', 0x00, (byte)'n', (byte)'e',
    
    // tests for Exceptions on invalid values
             0xFF,  0xFF,  0xFF,  0xFF,  0x17,
             0x01, // guard value
             0xFF,  0xFF,  0xFF,  0xFF,  0xFF,  0xFF,  0xFF,  0xFF,  0xFF,
             0x01, // guard value
        };

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
		[SetUp]
		public void Setup()
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
					l1 = LONGS[i] = random.NextLong(0, int.MaxValue) << 32;
				}
				else
				{
					l1 = LONGS[i] = random.NextLong(0, long.MaxValue);
				}
				bdo.WriteVLong(l1);
				bdo.WriteLong(l1);
			}
		}

		[TearDown]
		public void TearDown()
		{
			INTS = null;
			LONGS = null;
			RANDOM_TEST_BYTES = null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckReads(DataInput @is, Type expectedEx)
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
				Fail("Should throw " + expectedEx);
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
		[Test]
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
		[Test]
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

		[Test]
		public virtual void TestByteArrayDataInput()
		{
			ByteArrayDataInput @is = new ByteArrayDataInput(READ_TEST_BYTES);
			CheckReads(@is, typeof(SystemException));
			@is = new ByteArrayDataInput(RANDOM_TEST_BYTES);
			CheckRandomReads(@is);
		}
	}
}
