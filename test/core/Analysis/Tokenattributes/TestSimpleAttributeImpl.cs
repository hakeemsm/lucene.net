/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Test.Analysis.TokenAttributes;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Analysis.TokenAttributes
{
	public class TestSimpleAttributeImpl : LuceneTestCase
	{
		// this checks using reflection API if the defaults are correct
		public virtual void TestAttributes()
		{
			TestUtil.AssertAttributeReflection(new PositionIncrementAttributeImpl(), Collections
				.SingletonMap(typeof(PositionIncrementAttribute).FullName + "#positionIncrement"
				, 1));
			TestUtil.AssertAttributeReflection(new PositionLengthAttributeImpl(), Collections
				.SingletonMap(typeof(PositionLengthAttribute).FullName + "#positionLength", 1));
			TestUtil.AssertAttributeReflection(new FlagsAttributeImpl(), Collections.SingletonMap
				(typeof(FlagsAttribute).FullName + "#flags", 0));
			TestUtil.AssertAttributeReflection(new TypeAttributeImpl(), Collections.SingletonMap
				(typeof(TypeAttribute).FullName + "#type", TypeAttribute.DEFAULT_TYPE));
			TestUtil.AssertAttributeReflection(new PayloadAttributeImpl(), Collections.SingletonMap
				(typeof(PayloadAttribute).FullName + "#payload", null));
			TestUtil.AssertAttributeReflection(new KeywordAttributeImpl(), Collections.SingletonMap
				(typeof(KeywordAttribute).FullName + "#keyword", false));
			TestUtil.AssertAttributeReflection(new OffsetAttributeImpl(), new _Dictionary_42(
				));
		}

		private sealed class _Dictionary_42 : Dictionary<string, object>
		{
			public _Dictionary_42()
			{
				{
					this.Put(typeof(OffsetAttribute).FullName + "#startOffset", 0);
					this.Put(typeof(OffsetAttribute).FullName + "#endOffset", 0);
				}
			}
		}
	}
}
