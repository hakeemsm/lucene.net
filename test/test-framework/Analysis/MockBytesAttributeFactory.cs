/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>
	/// Attribute factory that implements CharTermAttribute with
	/// <see cref="MockUTF16TermAttributeImpl">MockUTF16TermAttributeImpl</see>
	/// </summary>
	public class MockBytesAttributeFactory : AttributeSource.AttributeFactory
	{
		private readonly AttributeSource.AttributeFactory delegate_ = AttributeSource.AttributeFactory
			.DEFAULT_ATTRIBUTE_FACTORY;

		public override AttributeImpl CreateAttributeInstance<_T0>(Type<_T0> attClass)
		{
			return attClass.IsAssignableFrom(typeof(MockUTF16TermAttributeImpl)) ? new MockUTF16TermAttributeImpl
				() : delegate_.CreateAttributeInstance(attClass);
		}
	}
}
