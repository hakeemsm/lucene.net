using Lucene.Net.Codecs;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Lucene3x
{
	/// <summary>Test that the SPI magic is returning "PreFlexRWCodec" for Lucene3x</summary>
	/// <lucene.experimental></lucene.experimental>
	[TestFixture]
    public class TestImpersonation : LuceneTestCase
	{
		[Test]
		public virtual void TestImpersonate()
		{
			Codec codec = Codec.ForName("Lucene3x");
			IsTrue(codec is PreFlexRWCodec);
		}
	}
}
