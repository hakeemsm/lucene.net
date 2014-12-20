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
	/// This rule will cause the suite to be assumption-ignored if
	/// the test class implements a given marker interface and a special
	/// property is not set.
	/// </summary>
	/// <remarks>
	/// This rule will cause the suite to be assumption-ignored if
	/// the test class implements a given marker interface and a special
	/// property is not set.
	/// <p>This is a workaround for problems with certain JUnit containers (IntelliJ)
	/// which automatically discover test suites and attempt to run nested classes
	/// that we use for testing the test framework itself.
	/// </remarks>
	public sealed class TestRuleIgnoreTestSuites : TestRule
	{
		/// <summary>
		/// Marker interface for nested suites that should be ignored
		/// if executed in stand-alone mode.
		/// </summary>
		/// <remarks>
		/// Marker interface for nested suites that should be ignored
		/// if executed in stand-alone mode.
		/// </remarks>
		public interface NestedTestSuite
		{
		}

		/// <summary>
		/// A boolean system property indicating nested suites should be executed
		/// normally.
		/// </summary>
		/// <remarks>
		/// A boolean system property indicating nested suites should be executed
		/// normally.
		/// </remarks>
		public static readonly string PROPERTY_RUN_NESTED = "tests.runnested";

		public Statement Apply(Statement s, Description d)
		{
			return new _Statement_48(d, s);
		}

		private sealed class _Statement_48 : Statement
		{
			public _Statement_48(Description d, Statement s)
			{
				this.d = d;
				this.s = s;
			}

			/// <exception cref="System.Exception"></exception>
			public override void Evaluate()
			{
				if (typeof(TestRuleIgnoreTestSuites.NestedTestSuite).IsAssignableFrom(d.GetTestClass
					()))
				{
					LuceneTestCase.AssumeTrue("Nested suite class ignored (started as stand-alone).", 
						TestRuleIgnoreTestSuites.IsRunningNested());
				}
				s.Evaluate();
			}

			private readonly Description d;

			private readonly Statement s;
		}

		/// <summary>Check if a suite class is running as a nested test.</summary>
		/// <remarks>Check if a suite class is running as a nested test.</remarks>
		public static bool IsRunningNested()
		{
			return bool.GetBoolean(PROPERTY_RUN_NESTED);
		}
	}
}
