/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Replicator
{
	/// <summary>
	/// An
	/// <see cref="System.IO.InputStream">System.IO.InputStream</see>
	/// which wraps an
	/// <see cref="Lucene.Net.Store.IndexInput">Lucene.Net.Store.IndexInput
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public sealed class IndexInputInputStream : InputStream
	{
		private readonly IndexInput @in;

		private long remaining;

		public IndexInputInputStream(IndexInput @in)
		{
			this.@in = @in;
			remaining = @in.Length();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read()
		{
			if (remaining == 0)
			{
				return -1;
			}
			else
			{
				--remaining;
				return @in.ReadByte();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Available()
		{
			return (int)@in.Length();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			@in.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(byte[] b)
		{
			return Read(b, 0, b.Length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(byte[] b, int off, int len)
		{
			if (remaining == 0)
			{
				return -1;
			}
			if (remaining < len)
			{
				len = (int)remaining;
			}
			@in.ReadBytes(b, off, len);
			remaining -= len;
			return len;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long Skip(long n)
		{
			if (remaining == 0)
			{
				return -1;
			}
			if (remaining < n)
			{
				n = remaining;
			}
			@in.Seek(@in.GetFilePointer() + n);
			remaining -= n;
			return n;
		}
	}
}
