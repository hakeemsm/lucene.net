using System;
using System.IO;
using Lucene.Net.Store;

namespace Lucene.Net.TestFramework.Store
{
	/// <summary>
	/// Used by MockRAMDirectory to create an output stream that
	/// will throw an IOException on fake disk full, track max
	/// disk space actually used, and maybe throw random
	/// IOExceptions.
	/// </summary>
	/// <remarks>
	/// Used by MockRAMDirectory to create an output stream that
	/// will throw an IOException on fake disk full, track max
	/// disk space actually used, and maybe throw random
	/// IOExceptions.
	/// </remarks>
	public class MockIndexOutputWrapper : IndexOutput
	{
		private MockDirectoryWrapper dir;

		private readonly IndexOutput delegate_;

		private bool first = true;

		internal readonly string name;

		internal byte[] singleByte = new byte[1];

		/// <summary>Construct an empty output buffer.</summary>
		/// <remarks>Construct an empty output buffer.</remarks>
		public MockIndexOutputWrapper(MockDirectoryWrapper dir, IndexOutput delegate_, string
			 name)
		{
			this.dir = dir;
			this.name = name;
			this.delegate_ = delegate_;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckCrashed()
		{
			// If MockRAMDir crashed since we were opened, then don't write anything
			if (dir.crashed)
			{
				throw new IOException("MockRAMDirectory was crashed; cannot write to " + name);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckDiskFull(byte[] b, int offset, DataInput @in, long len)
		{
			long freeSpace = dir.maxSize == 0 ? 0 : dir.maxSize - dir.SizeInBytes();
			long realUsage = 0;
			// Enforce disk full:
			if (dir.maxSize != 0 && freeSpace <= len)
			{
				// Compute the real disk free.  This will greatly slow
				// down our test but makes it more accurate:
				realUsage = dir.GetRecomputedActualSizeInBytes();
				freeSpace = dir.maxSize - realUsage;
			}
			if (dir.maxSize != 0 && freeSpace <= len)
			{
				if (freeSpace > 0)
				{
					realUsage += freeSpace;
					if (b != null)
					{
						delegate_.WriteBytes(b, offset, (int)freeSpace);
					}
					else
					{
						delegate_.CopyBytes(@in, len);
					}
				}
				if (realUsage > dir.maxUsedSize)
				{
					dir.maxUsedSize = realUsage;
				}
				string message = "fake disk full at " + dir.GetRecomputedActualSizeInBytes() + " bytes when writing "
					 + name + " (file length=" + delegate_.Length();
				if (freeSpace > 0)
				{
					message += "; wrote " + freeSpace + " of " + len + " bytes";
				}
				message += ")";
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine(Thread.CurrentThread.Name + ": MDW: now throw fake disk full"
						);
					Sharpen.Runtime.PrintStackTrace(new Exception(), System.Console.Out);
				}
				throw new IOException(message);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				dir.MaybeThrowDeterministicException();
			}
			finally
			{
				delegate_.Close();
				if (dir.trackDiskUsage)
				{
					// Now compute actual disk usage & track the maxUsedSize
					// in the MockDirectoryWrapper:
					long size = dir.GetRecomputedActualSizeInBytes();
					if (size > dir.maxUsedSize)
					{
						dir.maxUsedSize = size;
					}
				}
				dir.RemoveIndexOutput(this, name);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Flush()
		{
			dir.MaybeThrowDeterministicException();
			delegate_.Flush();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void WriteByte(byte b)
		{
			singleByte[0] = b;
			WriteBytes(singleByte, 0, 1);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void WriteBytes(byte[] b, int offset, int len)
		{
			CheckCrashed();
			CheckDiskFull(b, offset, null, len);
			if (dir.randomState.Next(200) == 0)
			{
				int half = len / 2;
				delegate_.WriteBytes(b, offset, half);
				Thread.Yield();
				delegate_.WriteBytes(b, offset + half, len - half);
			}
			else
			{
				delegate_.WriteBytes(b, offset, len);
			}
			dir.MaybeThrowDeterministicException();
			if (first)
			{
				// Maybe throw random exception; only do this on first
				// write to a new file:
				first = false;
				dir.MaybeThrowIOException(name);
			}
		}

		public override long GetFilePointer()
		{
			return delegate_.GetFilePointer();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Seek(long pos)
		{
			delegate_.Seek(pos);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long Length()
		{
			return delegate_.Length();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetLength(long length)
		{
			delegate_.SetLength(length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CopyBytes(DataInput input, long numBytes)
		{
			CheckCrashed();
			CheckDiskFull(null, 0, input, numBytes);
			delegate_.CopyBytes(input, numBytes);
			dir.MaybeThrowDeterministicException();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long GetChecksum()
		{
			return delegate_.GetChecksum();
		}

		public override string ToString()
		{
			return "MockIndexOutputWrapper(" + delegate_ + ")";
		}
	}
}
