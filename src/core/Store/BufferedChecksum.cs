using Lucene.Net.Support;

namespace Lucene.Net.Store
{
	/// <summary>
	/// Wraps another
	/// <see cref="IChecksum">Checksum</see>
	/// with an internal buffer
	/// to speed up checksum calculations.
	/// </summary>
	public class BufferedChecksum : IChecksum
	{
		private readonly IChecksum _checkSum;

		private readonly byte[] _buffer;

		private int upto;

		/// <summary>Default buffer size: 256</summary>
		public const int DEFAULT_BUFFERSIZE = 256;

		/// <summary>
		/// Create a new BufferedChecksum with
		/// <see cref="DEFAULT_BUFFERSIZE">DEFAULT_BUFFERSIZE</see>
		/// 
		/// </summary>
		public BufferedChecksum(IChecksum checkSum) : this(checkSum, DEFAULT_BUFFERSIZE)
		{
		}

		/// <summary>Create a new BufferedChecksum with the specified bufferSize</summary>
		public BufferedChecksum(IChecksum checkSum, int bufferSize)
		{
			this._checkSum = checkSum;
			this._buffer = new byte[bufferSize];
		}

		public virtual void Update(int b)
		{
			if (upto == _buffer.Length)
			{
				Flush();
			}
			_buffer[upto++] = unchecked((byte)b);
		}

	    public void Update(byte[] buf)
	    {
            Update(buf, 0, buf.Length);
	    }

	    public virtual void Update(byte[] b, int off, int len)
		{
			if (len >= _buffer.Length)
			{
				Flush();
				_checkSum.Update(b, off, len);
			}
			else
			{
				if (upto + len > _buffer.Length)
				{
					Flush();
				}
				System.Array.Copy(b, off, _buffer, upto, len);
				upto += len;
			}
		}

		public virtual long Value
		{
		    get
		    {
		        Flush();
		        return _checkSum.Value;
		    }
		}

		public virtual void Reset()
		{
			upto = 0;
			_checkSum.Reset();
		}

		private void Flush()
		{
			if (upto > 0)
			{
				_checkSum.Update(_buffer, 0, upto);
			}
			upto = 0;
		}
	}
}
