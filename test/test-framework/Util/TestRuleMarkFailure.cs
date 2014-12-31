/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using NUnit.Framework.Rules;
using NUnit.Framework.Runner;
using NUnit.Framework.Runners.Model;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>A rule for marking failed tests and suites.</summary>
	/// <remarks>A rule for marking failed tests and suites.</remarks>
	public sealed class TestRuleMarkFailure : TestRule
	{
		private readonly Lucene.Net.TestFramework.Util.TestRuleMarkFailure[] chained;

		private volatile bool failures;

		public TestRuleMarkFailure(params Lucene.Net.TestFramework.Util.TestRuleMarkFailure[] chained
			)
		{
			this.chained = chained;
		}

		public Statement Apply(Statement s, Description d)
		{
			return new _Statement_41(this, s);
		}

		private sealed class _Statement_41 : Statement
		{
			public _Statement_41(TestRuleMarkFailure _enclosing, Statement s)
			{
				this._enclosing = _enclosing;
				this.s = s;
			}

			/// <exception cref="System.Exception"></exception>
			public override void Evaluate()
			{
				// Clear status at start.
				this._enclosing.failures = false;
				try
				{
					s.Evaluate();
				}
				catch (Exception t)
				{
					if (!Lucene.Net.TestFramework.Util.TestRuleMarkFailure.IsAssumption(t))
					{
						this._enclosing.MarkFailed();
					}
					throw;
				}
			}

			private readonly TestRuleMarkFailure _enclosing;

			private readonly Statement s;
		}

		/// <summary>
		/// Is a given exception (or a MultipleFailureException) an
		/// <see cref="NUnit.Framework.Internal.AssumptionViolatedException">NUnit.Framework.Internal.AssumptionViolatedException
		/// 	</see>
		/// ?
		/// </summary>
		public static bool IsAssumption(Exception t)
		{
			foreach (Exception t2 in ExpandFromMultiple(t))
			{
				if (!(t2 is AssumptionViolatedException))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Expand from multi-exception wrappers.</summary>
		/// <remarks>Expand from multi-exception wrappers.</remarks>
		private static IList<Exception> ExpandFromMultiple(Exception t)
		{
			return ExpandFromMultiple(t, new List<Exception>());
		}

		/// <summary>Internal recursive routine.</summary>
		/// <remarks>Internal recursive routine.</remarks>
		private static IList<Exception> ExpandFromMultiple(Exception t, IList<Exception> 
			list)
		{
			if (t is MultipleFailureException)
			{
				foreach (Exception sub in ((MultipleFailureException)t).GetFailures())
				{
					ExpandFromMultiple(sub, list);
				}
			}
			else
			{
				list.Add(t);
			}
			return list;
		}

		/// <summary>Taints this object and any chained as having failures.</summary>
		/// <remarks>Taints this object and any chained as having failures.</remarks>
		public void MarkFailed()
		{
			failures = true;
			foreach (Lucene.Net.TestFramework.Util.TestRuleMarkFailure next in chained)
			{
				next.MarkFailed();
			}
		}

		/// <summary>Check if this object had any marked failures.</summary>
		/// <remarks>Check if this object had any marked failures.</remarks>
		public bool HadFailures()
		{
			return failures;
		}

		/// <summary>
		/// Check if this object was successful (the opposite of
		/// <see cref="HadFailures()">HadFailures()</see>
		/// ).
		/// </summary>
		public bool WasSuccessful()
		{
			return !HadFailures();
		}
	}
}
