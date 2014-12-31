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
	/// <summary>Saves the executing thread and method name of the test case.</summary>
	/// <remarks>Saves the executing thread and method name of the test case.</remarks>
	internal sealed class TestRuleThreadAndTestName : TestRule
	{
		/// <summary>The thread executing the current test case.</summary>
		/// <remarks>The thread executing the current test case.</remarks>
		/// <seealso cref="LuceneTestCase.IsTestThread()">LuceneTestCase.IsTestThread()</seealso>
		public volatile Thread testCaseThread;

		/// <summary>Test method name.</summary>
		/// <remarks>Test method name.</remarks>
		public volatile string testMethodName = "<unknown>";

		public Statement Apply(Statement @base, Description description)
		{
			return new _Statement_41(this, description, @base);
		}

		private sealed class _Statement_41 : Statement
		{
			public _Statement_41(TestRuleThreadAndTestName _enclosing, Description description
				, Statement @base)
			{
				this._enclosing = _enclosing;
				this.description = description;
				this.@base = @base;
			}

			/// <exception cref="System.Exception"></exception>
			public override void Evaluate()
			{
				try
				{
					Thread current = Thread.CurrentThread();
					this._enclosing.testCaseThread = current;
					this._enclosing.testMethodName = description.GetMethodName();
					@base.Evaluate();
				}
				finally
				{
					this._enclosing.testCaseThread = null;
					this._enclosing.testMethodName = null;
				}
			}

			private readonly TestRuleThreadAndTestName _enclosing;

			private readonly Description description;

			private readonly Statement @base;
		}
	}
}
