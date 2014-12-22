using System;
using Lucene.Net.Util;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>
	/// Attribute factory that implements CharTermAttribute with
	/// <see cref="MockUTF16TermAttributeImpl">MockUTF16TermAttributeImpl</see>
	/// </summary>
	public class MockBytesAttributeFactory : AttributeSource.AttributeFactory
	{
		private readonly AttributeSource.AttributeFactory delegate_ = DEFAULT_ATTRIBUTE_FACTORY;

		public override Attribute CreateAttributeInstance<T>()
		{
			return typeof(T).IsAssignableFrom(typeof(MockUTF16TermAttributeImpl)) ? new MockUTF16TermAttributeImpl
				() : delegate_.CreateAttributeInstance<T>();
		}
	}
}
