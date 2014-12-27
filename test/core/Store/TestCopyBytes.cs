/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Store
{
	public class TestCopyBytes : LuceneTestCase
	{
		private byte Value(int idx)
		{
			return unchecked((byte)((idx % 256) * (1 + (idx / 256))));
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestCopyBytes()
		{
			int num = AtLeast(10);
			for (int iter = 0; iter < num; iter++)
			{
				Directory dir = NewDirectory();
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: iter=" + iter + " dir=" + dir);
				}
				// make random file
				IndexOutput @out = dir.CreateOutput("test", NewIOContext(Random()));
				byte[] bytes = new byte[TestUtil.NextInt(Random(), 1, 77777)];
				int size = TestUtil.NextInt(Random(), 1, 1777777);
				int upto = 0;
				int byteUpto = 0;
				while (upto < size)
				{
					bytes[byteUpto++] = Value(upto);
					upto++;
					if (byteUpto == bytes.Length)
					{
						@out.WriteBytes(bytes, 0, bytes.Length);
						byteUpto = 0;
					}
				}
				@out.WriteBytes(bytes, 0, byteUpto);
				AreEqual(size, @out.FilePointer);
				@out.Dispose();
				AreEqual(size, dir.FileLength("test"));
				// copy from test -> test2
				IndexInput @in = dir.OpenInput("test", NewIOContext(Random()));
				@out = dir.CreateOutput("test2", NewIOContext(Random()));
				upto = 0;
				while (upto < size)
				{
					if (Random().NextBoolean())
					{
						@out.WriteByte(@in.ReadByte());
						upto++;
					}
					else
					{
						int chunk = Math.Min(TestUtil.NextInt(Random(), 1, bytes.Length), size - upto);
						@out.CopyBytes(@in, chunk);
						upto += chunk;
					}
				}
				AreEqual(size, upto);
				@out.Dispose();
				@in.Dispose();
				// verify
				IndexInput in2 = dir.OpenInput("test2", NewIOContext(Random()));
				upto = 0;
				while (upto < size)
				{
					if (Random().NextBoolean())
					{
						byte v = in2.ReadByte();
						AreEqual(Value(upto), v);
						upto++;
					}
					else
					{
						int limit = Math.Min(TestUtil.NextInt(Random(), 1, bytes.Length), size - upto);
						in2.ReadBytes(bytes, 0, limit);
						for (int byteIdx = 0; byteIdx < limit; byteIdx++)
						{
							AreEqual(Value(upto), bytes[byteIdx]);
							upto++;
						}
					}
				}
				in2.Dispose();
				dir.DeleteFile("test");
				dir.DeleteFile("test2");
				dir.Dispose();
			}
		}

		// LUCENE-3541
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCopyBytesWithThreads()
		{
			int datalen = TestUtil.NextInt(Random(), 101, 10000);
			byte[] data = new byte[datalen];
			Random().NextBytes(data);
			Directory d = NewDirectory();
			IndexOutput output = d.CreateOutput("data", IOContext.DEFAULT);
			output.WriteBytes(data, 0, datalen);
			output.Dispose();
			IndexInput input = d.OpenInput("data", IOContext.DEFAULT);
			IndexOutput outputHeader = d.CreateOutput("header", IOContext.DEFAULT);
			// copy our 100-byte header
			outputHeader.CopyBytes(input, 100);
			outputHeader.Dispose();
			// now make N copies of the remaining bytes
			TestCopyBytes.CopyThread[] copies = new TestCopyBytes.CopyThread[10];
			for (int i = 0; i < copies.Length; i++)
			{
				copies[i] = new TestCopyBytes.CopyThread(((IndexInput)input.Clone()), d.CreateOutput
					("copy" + i, IOContext.DEFAULT));
			}
			for (int i_1 = 0; i_1 < copies.Length; i_1++)
			{
				copies[i_1].Start();
			}
			for (int i_2 = 0; i_2 < copies.Length; i_2++)
			{
				copies[i_2].Join();
			}
			for (int i_3 = 0; i_3 < copies.Length; i_3++)
			{
				IndexInput copiedData = d.OpenInput("copy" + i_3, IOContext.DEFAULT);
				byte[] dataCopy = new byte[datalen];
				System.Array.Copy(data, 0, dataCopy, 0, 100);
				// copy the header for easy testing
				copiedData.ReadBytes(dataCopy, 100, datalen - 100);
				AssertArrayEquals(data, dataCopy);
				copiedData.Dispose();
			}
			input.Dispose();
			d.Dispose();
		}

		internal class CopyThread : Sharpen.Thread
		{
			internal readonly IndexInput src;

			internal readonly IndexOutput dst;

			internal CopyThread(IndexInput src, IndexOutput dst)
			{
				this.src = src;
				this.dst = dst;
			}

			public override void Run()
			{
				try
				{
					dst.CopyBytes(src, src.Length() - 100);
					dst.Dispose();
				}
				catch (IOException ex)
				{
					throw new RuntimeException(ex);
				}
			}
		}
	}
}
