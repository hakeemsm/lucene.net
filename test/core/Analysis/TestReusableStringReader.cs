using System.Text;
using Lucene.Net.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Test.Analysis
{
    [TestFixture]
	public class TestReusableStringReader : LuceneTestCase
	{
		[Test]
		public virtual void TestReusablity()
		{
			ReusableStringReader reader = new ReusableStringReader();
			AreEqual(-1, reader.Read());
			//AreEqual(-1, reader.Read(new char[1]));
			AreEqual(-1, reader.Read(new char[2], 1, 1));
			//AreEqual(-1, reader.Read(CharBuffer.Wrap(new char[2])));
			reader.SetValue("foobar");
			var buf = new char[4];
			AreEqual(4, reader.Read(buf,0,4));
			AreEqual("foob", new string(buf));
			AreEqual(2, reader.Read(buf,0,2));
			AreEqual("ar", new string(buf, 0, 2));
			AreEqual(-1, reader.Read(buf,0,2));
			reader.Close();
			reader.SetValue("foobar");
			AreEqual(0, reader.Read(buf, 1, 0));
			AreEqual(3, reader.Read(buf, 1, 3));
			AreEqual("foo", new string(buf, 1, 3));
			AreEqual(2, reader.Read(buf, 2, 2));
			AreEqual("ba", new string(buf, 2, 2));
			AreEqual('r', (char)reader.Read());
			AreEqual(-1, reader.Read(buf,0,1));
			reader.Close();
			reader.SetValue("foobar");
			StringBuilder sb = new StringBuilder();
			int ch;
			while ((ch = reader.Read()) != -1)
			{
				sb.Append((char)ch);
			}
			reader.Close();
			AreEqual("foobar", sb.ToString());
		}
	}
}
