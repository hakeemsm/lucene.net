/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using NUnit.Framework.Rules;
using NUnit.Framework.Runner;
using NUnit.Framework.Runners.Model;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>
	/// A
	/// <see cref="NUnit.Framework.Rules.TestRule">NUnit.Framework.Rules.TestRule</see>
	/// that delegates to another
	/// <see cref="NUnit.Framework.Rules.TestRule">NUnit.Framework.Rules.TestRule</see>
	/// via a delegate
	/// contained in a an
	/// <see cref="Sharpen.AtomicReference{V}">Sharpen.AtomicReference&lt;V&gt;</see>
	/// .
	/// </summary>
	internal sealed class TestRuleDelegate<T> : TestRule where T:TestRule
	{
		private AtomicReference<T> delegate_;

		private TestRuleDelegate(AtomicReference<T> delegate_)
		{
			this.delegate_ = delegate_;
		}

		public Statement Apply(Statement s, Description d)
		{
			return delegate_.Get().Apply(s, d);
		}

		internal static Lucene.Net.TestFramework.Util.TestRuleDelegate<T> Of<T>(AtomicReference<
			T> delegate_) where T:TestRule
		{
			return new Lucene.Net.TestFramework.Util.TestRuleDelegate<T>(delegate_);
		}
	}
}
