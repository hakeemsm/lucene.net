using Lucene.Net.Store;

namespace Lucene.Net.Util
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Store.DataOutput">Lucene.Net.Store.DataOutput
	/// 	</see>
	/// that can be used to build a byte[].
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public sealed class GrowableByteArrayDataOutput : DataOutput
	{
		/// <summary>The bytes</summary>
		public byte[] bytes;

		/// <summary>The length</summary>
		public int length;

		/// <summary>
		/// Create a
		/// <see cref="GrowableByteArrayDataOutput">GrowableByteArrayDataOutput</see>
		/// with the given initial capacity.
		/// </summary>
		public GrowableByteArrayDataOutput(int cp)
		{
			this.bytes = new byte[ArrayUtil.Oversize(cp, 1)];
			this.length = 0;
		}

		public override void WriteByte(byte b)
		{
			if (length >= bytes.Length)
			{
				bytes = ArrayUtil.Grow(bytes);
			}
			bytes[length++] = b;
		}

		public override void WriteBytes(byte[] b, int off, int len)
		{
			int newLength = length + len;
			bytes = ArrayUtil.Grow(bytes, newLength);
			System.Array.Copy(b, off, bytes, length, len);
			length = newLength;
		}
	}
}
