/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Store
{
	public class TestBufferedChecksum : LuceneTestCase
	{
		public virtual void TestSimple()
		{
			Checksum c = new BufferedChecksum(new CRC32());
			c.Update(1);
			c.Update(2);
			c.Update(3);
			AreEqual(1438416925L, c.GetValue());
		}

		public virtual void TestRandom()
		{
			Checksum c1 = new CRC32();
			Checksum c2 = new BufferedChecksum(new CRC32());
			int iterations = AtLeast(10000);
			for (int i = 0; i < iterations; i++)
			{
				switch (Random().Next(4))
				{
					case 0:
					{
						// update(byte[], int, int)
						int length = Random().Next(1024);
						byte[] bytes = new byte[length];
						Random().NextBytes(bytes);
						c1.Update(bytes, 0, bytes.Length);
						c2.Update(bytes, 0, bytes.Length);
						break;
					}

					case 1:
					{
						// update(int)
						int b = Random().Next(256);
						c1.Update(b);
						c2.Update(b);
						break;
					}

					case 2:
					{
						// reset()
						c1.Reset();
						c2.Reset();
						break;
					}

					case 3:
					{
						// getValue()
						AreEqual(c1.GetValue(), c2.GetValue());
						break;
					}
				}
			}
			AreEqual(c1.GetValue(), c2.GetValue());
		}
	}
}
