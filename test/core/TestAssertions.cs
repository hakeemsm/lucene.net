/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Util;
using Sharpen;

namespace Org.Apache.Lucene
{
	/// <summary>validate that assertions are enabled during tests</summary>
	public class TestAssertions : LuceneTestCase
	{
		internal class TestTokenStream1 : TokenStream
		{
			public sealed override bool IncrementToken()
			{
				return false;
			}
		}

		internal sealed class TestTokenStream2 : TokenStream
		{
			public override bool IncrementToken()
			{
				return false;
			}
		}

		internal class TestTokenStream3 : TokenStream
		{
			public override bool IncrementToken()
			{
				return false;
			}
		}

		public virtual void TestTokenStreams()
		{
			new TestAssertions.TestTokenStream1();
			new TestAssertions.TestTokenStream2();
			bool doFail = false;
			try
			{
				new TestAssertions.TestTokenStream3();
				doFail = true;
			}
			catch (Exception)
			{
			}
			// expected
			IsFalse("TestTokenStream3 should fail assertion", doFail);
		}
	}
}
