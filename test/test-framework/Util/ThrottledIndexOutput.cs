/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.TestFramework.Store;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>Intentionally slow IndexOutput for testing.</summary>
	/// <remarks>Intentionally slow IndexOutput for testing.</remarks>
	public class ThrottledIndexOutput : IndexOutput
	{
		public const int DEFAULT_MIN_WRITTEN_BYTES = 1024;

		private readonly int bytesPerSecond;

		private IndexOutput delegate_;

		private long flushDelayMillis;

		private long closeDelayMillis;

		private long seekDelayMillis;

		private long pendingBytes;

		private long minBytesWritten;

		private long timeElapsed;

		private readonly byte[] bytes = new byte[1];

		public virtual Lucene.Net.TestFramework.Util.ThrottledIndexOutput NewFromDelegate(IndexOutput
			 output)
		{
			return new Lucene.Net.TestFramework.Util.ThrottledIndexOutput(bytesPerSecond, flushDelayMillis
				, closeDelayMillis, seekDelayMillis, minBytesWritten, output);
		}

		public ThrottledIndexOutput(int bytesPerSecond, long delayInMillis, IndexOutput delegate_
			) : this(bytesPerSecond, delayInMillis, delayInMillis, delayInMillis, DEFAULT_MIN_WRITTEN_BYTES
			, delegate_)
		{
		}

		public ThrottledIndexOutput(int bytesPerSecond, long delays, int minBytesWritten, 
			IndexOutput delegate_) : this(bytesPerSecond, delays, delays, delays, minBytesWritten
			, delegate_)
		{
		}

		public static int MBitsToBytes(int mbits)
		{
			return mbits * 125000;
		}

		public ThrottledIndexOutput(int bytesPerSecond, long flushDelayMillis, long closeDelayMillis
			, long seekDelayMillis, long minBytesWritten, IndexOutput delegate_)
		{
			 
			//assert bytesPerSecond > 0;
			this.delegate_ = delegate_;
			this.bytesPerSecond = bytesPerSecond;
			this.flushDelayMillis = flushDelayMillis;
			this.closeDelayMillis = closeDelayMillis;
			this.seekDelayMillis = seekDelayMillis;
			this.minBytesWritten = minBytesWritten;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Flush()
		{
			Sleep(flushDelayMillis);
			delegate_.Flush();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				Sleep(closeDelayMillis + GetDelay(true));
			}
			finally
			{
				delegate_.Close();
			}
		}

		public override long GetFilePointer()
		{
			return delegate_.GetFilePointer();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Seek(long pos)
		{
			Sleep(seekDelayMillis);
			delegate_.Seek(pos);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long Length()
		{
			return delegate_.Length();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void WriteByte(byte b)
		{
			bytes[0] = b;
			WriteBytes(bytes, 0, 1);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void WriteBytes(byte[] b, int offset, int length)
		{
			long before = Runtime.NanoTime();
			// TODO: sometimes, write only half the bytes, then
			// sleep, then 2nd half, then sleep, so we sometimes
			// interrupt having only written not all bytes
			delegate_.WriteBytes(b, offset, length);
			timeElapsed += Runtime.NanoTime() - before;
			pendingBytes += length;
			Sleep(GetDelay(false));
		}

		protected internal virtual long GetDelay(bool closing)
		{
			if (pendingBytes > 0 && (closing || pendingBytes > minBytesWritten))
			{
				long actualBps = (timeElapsed / pendingBytes) * 1000000000l;
				// nano to sec
				if (actualBps > bytesPerSecond)
				{
					long expected = (pendingBytes * 1000l / bytesPerSecond);
					long delay = expected - (timeElapsed / 1000000l);
					pendingBytes = 0;
					timeElapsed = 0;
					return delay;
				}
			}
			return 0;
		}

		private static void Sleep(long ms)
		{
			if (ms <= 0)
			{
				return;
			}
			try
			{
				Sharpen.Thread.Sleep(ms);
			}
			catch (Exception e)
			{
				throw new ThreadInterruptedException(e);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetLength(long length)
		{
			delegate_.SetLength(length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CopyBytes(DataInput input, long numBytes)
		{
			delegate_.CopyBytes(input, numBytes);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long GetChecksum()
		{
			return delegate_.GetChecksum();
		}
	}
}
