using System.Collections.Generic;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Analysis.TokenAttributes
{
    [TestFixture]
	public class TestSimpleAttributeImpl : LuceneTestCase
	{
		// this checks using reflection API if the defaults are correct
        [Test]
		public virtual void TestAttributes()
		{
			TestUtil.AssertAttributeReflection(new PositionIncrementAttribute(), Collections
				.UnmodifiableMap(new Dictionary<string,int>{{typeof(PositionIncrementAttribute).FullName + "#positionIncrement", 1}}));
			TestUtil.AssertAttributeReflection(new PositionLengthAttribute(), Collections
                .UnmodifiableMap(new Dictionary<string, int> { { typeof(PositionIncrementAttribute).FullName + "#positionLength", 1 } }));
			TestUtil.AssertAttributeReflection(new FlagsAttribute(), Collections.UnmodifiableMap(new Dictionary<string,int>{{typeof(PositionIncrementAttribute).FullName + "#flags", 0}}));
			TestUtil.AssertAttributeReflection(new TypeAttribute(), Collections.UnmodifiableMap(new Dictionary<string,string>{{typeof(PositionIncrementAttribute).FullName + "#type", TypeAttribute.DEFAULT_TYPE}}));
            TestUtil.AssertAttributeReflection(new PayloadAttribute(), Collections.UnmodifiableMap(new Dictionary<string, int?> { { typeof(PositionIncrementAttribute).FullName + "#payload", null } }));
			TestUtil.AssertAttributeReflection(new KeywordAttribute(), Collections.UnmodifiableMap(new Dictionary<string,bool>{{typeof(PositionIncrementAttribute).FullName + "#keyword", false}}));
			TestUtil.AssertAttributeReflection(new OffsetAttribute(), new _Dictionary_42(
				));
		}

		private sealed class _Dictionary_42 : Dictionary<string, object>
		{
			public _Dictionary_42()
			{
				{
					this[typeof(OffsetAttribute).FullName + "#startOffset"] = 0;
					this[typeof(OffsetAttribute).FullName + "#endOffset"] = 0;
				}
			}
		}
	}
}
