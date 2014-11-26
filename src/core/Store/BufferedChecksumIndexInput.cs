using System;
using Lucene.Net.Support;

namespace Lucene.Net.Store
{
	/// <summary>
	/// Simple implementation of
	/// <see cref="ChecksumIndexInput">ChecksumIndexInput</see>
	/// that wraps
	/// another input and delegates calls.
	/// </summary>
	public class BufferedChecksumIndexInput : ChecksumIndexInput
	{
		internal readonly IndexInput main;

		internal readonly IChecksum digest;

		/// <summary>Creates a new BufferedChecksumIndexInput</summary>
		public BufferedChecksumIndexInput(IndexInput main) : base(main)
		{
			this.main = main;
			this.digest = new BufferedChecksum(new CRC32());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override byte ReadByte()
		{
			byte b = main.ReadByte();
			digest.Update(b);
			return b;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void ReadBytes(byte[] b, int offset, int len)
		{
			main.ReadBytes(b, offset, len);
			digest.Update(b, offset, len);
		}

		public override long GetChecksum()
		{
			return digest.GetValue();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			main.Close();
		}

		public override long GetFilePointer()
		{
			return main.GetFilePointer();
		}

		public override long Length()
		{
			return main.Length();
		}

		public override DataInput Clone()
		{
			throw new NotSupportedException();
		}
	}
}
