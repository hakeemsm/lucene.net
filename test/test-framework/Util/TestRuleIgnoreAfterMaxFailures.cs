/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Com.Carrotsearch.Randomizedtesting;
using NUnit.Framework.Rules;
using NUnit.Framework.Runner;
using NUnit.Framework.Runners.Model;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>
	/// This rule keeps a count of failed tests (suites) and will result in an
	/// <see cref="NUnit.Framework.Internal.AssumptionViolatedException">NUnit.Framework.Internal.AssumptionViolatedException
	/// 	</see>
	/// after a given number of failures for all
	/// tests following this condition.
	/// <p>
	/// Aborting quickly on failed tests can be useful when used in combination with
	/// test repeats (via the
	/// <see cref="Com.Carrotsearch.Randomizedtesting.Annotations.Repeat">Com.Carrotsearch.Randomizedtesting.Annotations.Repeat
	/// 	</see>
	/// annotation or system property).
	/// </summary>
	public sealed class TestRuleIgnoreAfterMaxFailures : TestRule
	{
		/// <summary>Maximum failures.</summary>
		/// <remarks>Maximum failures. Package scope for tests.</remarks>
		internal int maxFailures;

		/// <param name="maxFailures">
		/// The number of failures after which all tests are ignored. Must be
		/// greater or equal 1.
		/// </param>
		public TestRuleIgnoreAfterMaxFailures(int maxFailures)
		{
			//HM:revisit 
			//assert.assertTrue("maxFailures must be >= 1: " + maxFailures, maxFailures >= 1);
			this.maxFailures = maxFailures;
		}

		public Statement Apply(Statement s, Description d)
		{
			return new _Statement_58(this, s);
		}

		private sealed class _Statement_58 : Statement
		{
			public _Statement_58(TestRuleIgnoreAfterMaxFailures _enclosing, Statement s)
			{
				this._enclosing = _enclosing;
				this.s = s;
			}

			/// <exception cref="System.Exception"></exception>
			public override void Evaluate()
			{
				int failuresSoFar = FailureMarker.GetFailures();
				if (failuresSoFar >= this._enclosing.maxFailures)
				{
					RandomizedTest.AssumeTrue("Ignored, failures limit reached (" + failuresSoFar + " >= "
						 + this._enclosing.maxFailures + ").", false);
				}
				s.Evaluate();
			}

			private readonly TestRuleIgnoreAfterMaxFailures _enclosing;

			private readonly Statement s;
		}
	}
}
