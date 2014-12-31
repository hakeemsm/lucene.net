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


namespace Lucene.Net.Store
{
	public class TestMockDirectoryWrapper : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFailIfIndexWriterNotClosed()
		{
			MockDirectoryWrapper dir = NewMockDirectory();
			IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				null));
			try
			{
				dir.Dispose();
				Fail();
			}
			catch (Exception expected)
			{
				IsTrue(expected.Message.Contains("there are still open locks"
					));
			}
			iw.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFailIfIndexWriterNotClosedChangeLockFactory()
		{
			MockDirectoryWrapper dir = NewMockDirectory();
			dir.SetLockFactory(new SingleInstanceLockFactory());
			IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				null));
			try
			{
				dir.Dispose();
				Fail();
			}
			catch (Exception expected)
			{
				IsTrue(expected.Message.Contains("there are still open locks"
					));
			}
			iw.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDiskFull()
		{
			// test writeBytes
			MockDirectoryWrapper dir = NewMockDirectory();
			dir.SetMaxSizeInBytes(3);
			byte[] bytes = new byte[] { 1, 2 };
			IndexOutput @out = dir.CreateOutput("foo", IOContext.DEFAULT);
			@out.WriteBytes(bytes, bytes.Length);
			// first write should succeed
			// flush() to ensure the written bytes are not buffered and counted
			// against the directory size
			@out.Flush();
			try
			{
				@out.WriteBytes(bytes, bytes.Length);
				Fail("should have failed on disk full");
			}
			catch (IOException)
			{
			}
			// expected
			@out.Dispose();
			dir.Dispose();
			// test copyBytes
			dir = NewMockDirectory();
			dir.SetMaxSizeInBytes(3);
			@out = dir.CreateOutput("foo", IOContext.DEFAULT);
			@out.CopyBytes(new ByteArrayDataInput(bytes), bytes.Length);
			// first copy should succeed
			// flush() to ensure the written bytes are not buffered and counted
			// against the directory size
			@out.Flush();
			try
			{
				@out.CopyBytes(new ByteArrayDataInput(bytes), bytes.Length);
				Fail("should have failed on disk full");
			}
			catch (IOException)
			{
			}
			// expected
			@out.Dispose();
			dir.Dispose();
		}
	}
}
