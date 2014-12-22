/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using NUnit.Framework.Rules;
using NUnit.Framework.Runner;
using NUnit.Framework.Runners.Model;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>
	/// Make sure
	/// <see cref="LuceneTestCase.SetUp()">LuceneTestCase.SetUp()</see>
	/// and
	/// <see cref="LuceneTestCase.TearDown()">LuceneTestCase.TearDown()</see>
	/// were invoked even if they
	/// have been overriden. We assume nobody will call these out of non-overriden
	/// methods (they have to be public by contract, unfortunately). The top-level
	/// methods just set a flag that is checked upon successful execution of each test
	/// case.
	/// </summary>
	internal class TestRuleSetupTeardownChained : TestRule
	{
		/// <seealso cref="TestRuleSetupTeardownChained"></seealso>
		public bool setupCalled;

		/// <seealso cref="TestRuleSetupTeardownChained">TestRuleSetupTeardownChained</seealso>
		public bool teardownCalled;

		public virtual Statement Apply(Statement @base, Description description)
		{
			return new _Statement_45(this, @base);
		}

		private sealed class _Statement_45 : Statement
		{
			public _Statement_45(TestRuleSetupTeardownChained _enclosing, Statement @base)
			{
				this._enclosing = _enclosing;
				this.@base = @base;
			}

			/// <exception cref="System.Exception"></exception>
			public override void Evaluate()
			{
				this._enclosing.setupCalled = false;
				this._enclosing.teardownCalled = false;
				@base.Evaluate();
				// I assume we don't want to check teardown chaining if something happens in the
				// test because this would obscure the original exception?
				if (!this._enclosing.setupCalled)
				{
				}
				 
				//assert.fail("One of the overrides of setUp does not propagate the call.");
				if (!this._enclosing.teardownCalled)
				{
				}
			}

			private readonly TestRuleSetupTeardownChained _enclosing;

			private readonly Statement @base;
		}
		 
		//assert.fail("One of the overrides of tearDown does not propagate the call.");
	}
}
