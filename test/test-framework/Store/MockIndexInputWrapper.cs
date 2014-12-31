/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.TestFramework.Store;
using Sharpen;

namespace Lucene.Net.TestFramework.Store
{
	/// <summary>
	/// Used by MockDirectoryWrapper to create an input stream that
	/// keeps track of when it's been closed.
	/// </summary>
	/// <remarks>
	/// Used by MockDirectoryWrapper to create an input stream that
	/// keeps track of when it's been closed.
	/// </remarks>
	public class MockIndexInputWrapper : IndexInput
	{
		private MockDirectoryWrapper dir;

		internal readonly string name;

		private IndexInput delegate_;

		private bool isClone;

		private bool closed;

		/// <summary>Construct an empty output buffer.</summary>
		/// <remarks>Construct an empty output buffer.</remarks>
		public MockIndexInputWrapper(MockDirectoryWrapper dir, string name, IndexInput delegate_
			) : base("MockIndexInputWrapper(name=" + name + " delegate=" + delegate_ + ")")
		{
			this.name = name;
			this.dir = dir;
			this.delegate_ = delegate_;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
			}
			finally
			{
				// turn on the following to look for leaks closing inputs,
				// after fixing TestTransactions
				// dir.maybeThrowDeterministicException();
				closed = true;
				delegate_.Close();
				// Pending resolution on LUCENE-686 we may want to
				// remove the conditional check so we also track that
				// all clones get closed:
				if (!isClone)
				{
					dir.RemoveIndexInput(this, name);
				}
			}
		}

		private void EnsureOpen()
		{
			if (closed)
			{
				throw new SystemException("Abusing closed IndexInput!");
			}
		}

		public override DataInput Clone()
		{
			EnsureOpen();
			dir.inputCloneCount.IncrementAndGet();
			IndexInput iiclone = ((IndexInput)delegate_.Clone());
			Lucene.Net.TestFramework.Store.MockIndexInputWrapper clone = new Lucene.Net.TestFramework.Store.MockIndexInputWrapper
				(dir, name, iiclone);
			clone.isClone = true;
			// Pending resolution on LUCENE-686 we may want to
			// uncomment this code so that we also track that all
			// clones get closed:
			return clone;
		}

		public override long GetFilePointer()
		{
			EnsureOpen();
			return delegate_.GetFilePointer();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Seek(long pos)
		{
			EnsureOpen();
			delegate_.Seek(pos);
		}

		public override long Length()
		{
			EnsureOpen();
			return delegate_.Length();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override byte ReadByte()
		{
			EnsureOpen();
			return delegate_.ReadByte();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void ReadBytes(byte[] b, int offset, int len)
		{
			EnsureOpen();
			delegate_.ReadBytes(b, offset, len);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void ReadBytes(byte[] b, int offset, int len, bool useBuffer)
		{
			EnsureOpen();
			delegate_.ReadBytes(b, offset, len, useBuffer);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override short ReadShort()
		{
			EnsureOpen();
			return delegate_.ReadShort();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int ReadInt()
		{
			EnsureOpen();
			return delegate_.ReadInt();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long ReadLong()
		{
			EnsureOpen();
			return delegate_.ReadLong();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override string ReadString()
		{
			EnsureOpen();
			return delegate_.ReadString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IDictionary<string, string> ReadStringStringMap()
		{
			EnsureOpen();
			return delegate_.ReadStringStringMap();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int ReadVInt()
		{
			EnsureOpen();
			return delegate_.ReadVInt();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long ReadVLong()
		{
			EnsureOpen();
			return delegate_.ReadVLong();
		}

		public override string ToString()
		{
			return "MockIndexInputWrapper(" + delegate_ + ")";
		}
	}
}
