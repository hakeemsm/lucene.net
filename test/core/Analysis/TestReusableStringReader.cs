/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis
{
	public class TestReusableStringReader : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			ReusableStringReader reader = new ReusableStringReader();
			NUnit.Framework.Assert.AreEqual(-1, reader.Read());
			NUnit.Framework.Assert.AreEqual(-1, reader.Read(new char[1]));
			NUnit.Framework.Assert.AreEqual(-1, reader.Read(new char[2], 1, 1));
			NUnit.Framework.Assert.AreEqual(-1, reader.Read(CharBuffer.Wrap(new char[2])));
			reader.SetValue("foobar");
			char[] buf = new char[4];
			NUnit.Framework.Assert.AreEqual(4, reader.Read(buf));
			NUnit.Framework.Assert.AreEqual("foob", new string(buf));
			NUnit.Framework.Assert.AreEqual(2, reader.Read(buf));
			NUnit.Framework.Assert.AreEqual("ar", new string(buf, 0, 2));
			NUnit.Framework.Assert.AreEqual(-1, reader.Read(buf));
			reader.Close();
			reader.SetValue("foobar");
			NUnit.Framework.Assert.AreEqual(0, reader.Read(buf, 1, 0));
			NUnit.Framework.Assert.AreEqual(3, reader.Read(buf, 1, 3));
			NUnit.Framework.Assert.AreEqual("foo", new string(buf, 1, 3));
			NUnit.Framework.Assert.AreEqual(2, reader.Read(CharBuffer.Wrap(buf, 2, 2)));
			NUnit.Framework.Assert.AreEqual("ba", new string(buf, 2, 2));
			NUnit.Framework.Assert.AreEqual('r', (char)reader.Read());
			NUnit.Framework.Assert.AreEqual(-1, reader.Read(buf));
			reader.Close();
			reader.SetValue("foobar");
			StringBuilder sb = new StringBuilder();
			int ch;
			while ((ch = reader.Read()) != -1)
			{
				sb.Append((char)ch);
			}
			reader.Close();
			NUnit.Framework.Assert.AreEqual("foobar", sb.ToString());
		}
	}
}
