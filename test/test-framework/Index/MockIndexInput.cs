/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Store;
using Lucene.Net.TestFramework.Store;
using Sharpen;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>IndexInput backed by a byte[] for testing.</summary>
	/// <remarks>IndexInput backed by a byte[] for testing.</remarks>
	public class MockIndexInput : BufferedIndexInput
	{
		private byte[] buffer;

		private int pointer = 0;

		private long length;

		public MockIndexInput(byte[] bytes) : base("MockIndexInput", BufferedIndexInput.BUFFER_SIZE
			)
		{
			// TODO: what is this used for? just testing BufferedIndexInput?
			// if so it should be pkg-private. otherwise its a dup of ByteArrayIndexInput?
			buffer = bytes;
			length = bytes.Length;
		}

		protected override void ReadInternal(byte[] dest, int destOffset, int len)
		{
			int remainder = len;
			int start = pointer;
			while (remainder != 0)
			{
				//          int bufferNumber = start / buffer.length;
				int bufferOffset = start % buffer.Length;
				int bytesInBuffer = buffer.Length - bufferOffset;
				int bytesToCopy = bytesInBuffer >= remainder ? remainder : bytesInBuffer;
				System.Array.Copy(buffer, bufferOffset, dest, destOffset, bytesToCopy);
				destOffset += bytesToCopy;
				start += bytesToCopy;
				remainder -= bytesToCopy;
			}
			pointer += len;
		}

		public override void Close()
		{
		}

		// ignore
		protected override void SeekInternal(long pos)
		{
			pointer = (int)pos;
		}

		public override long Length()
		{
			return length;
		}
	}
}
