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
	public class TestByteArrayDataInput : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestBasic()
		{
			byte[] bytes = new byte[] { 1, 65 };
			ByteArrayDataInput @in = new ByteArrayDataInput(bytes);
			NUnit.Framework.Assert.AreEqual("A", @in.ReadString());
			bytes = new byte[] { 1, 1, 65 };
			@in.Reset(bytes, 1, 2);
			NUnit.Framework.Assert.AreEqual("A", @in.ReadString());
		}
	}
}
