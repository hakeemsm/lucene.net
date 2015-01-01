using Lucene.Net.Index;
using Lucene.Net.TestFramework;
using NUnit.Framework;


namespace Lucene.Net.Test.Index
{
	public class TestTerm : LuceneTestCase
	{
        [Test]
		public virtual void TestEquals()
		{
			Term @base = new Term("same", "same");
			Term same = new Term("same", "same");
			Term differentField = new Term("different", "same");
			Term differentText = new Term("same", "different");
			string differentType = "AString";
			AreEqual(@base, @base);
			AreEqual(@base, same);
			IsFalse(@base.Equals(differentField));
			IsFalse(@base.Equals(differentText));
			IsFalse(@base.Equals(differentType));
		}
	}
}
