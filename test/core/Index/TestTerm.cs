/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestTerm : LuceneTestCase
	{
		public virtual void TestEquals()
		{
			Term @base = new Term("same", "same");
			Term same = new Term("same", "same");
			Term differentField = new Term("different", "same");
			Term differentText = new Term("same", "different");
			string differentType = "AString";
			NUnit.Framework.Assert.AreEqual(@base, @base);
			NUnit.Framework.Assert.AreEqual(@base, same);
			NUnit.Framework.Assert.IsFalse(@base.Equals(differentField));
			NUnit.Framework.Assert.IsFalse(@base.Equals(differentText));
			NUnit.Framework.Assert.IsFalse(@base.Equals(differentType));
		}
	}
}
