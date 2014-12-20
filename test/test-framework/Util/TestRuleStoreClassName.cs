/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using NUnit.Framework.Rules;
using NUnit.Framework.Runner;
using NUnit.Framework.Runners.Model;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>
	/// Stores the suite name so you can retrieve it
	/// from
	/// <see cref="GetTestClass()">GetTestClass()</see>
	/// </summary>
	public class TestRuleStoreClassName : TestRule
	{
		private volatile Description description;

		public virtual Statement Apply(Statement s, Description d)
		{
			if (!d.IsSuite())
			{
				throw new ArgumentException("This is a @ClassRule (applies to suites only).");
			}
			return new _Statement_37(this, d, s);
		}

		private sealed class _Statement_37 : Statement
		{
			public _Statement_37(TestRuleStoreClassName _enclosing, Description d, Statement 
				s)
			{
				this._enclosing = _enclosing;
				this.d = d;
				this.s = s;
			}

			/// <exception cref="System.Exception"></exception>
			public override void Evaluate()
			{
				try
				{
					this._enclosing.description = d;
					s.Evaluate();
				}
				finally
				{
					this._enclosing.description = null;
				}
			}

			private readonly TestRuleStoreClassName _enclosing;

			private readonly Description d;

			private readonly Statement s;
		}

		/// <summary>Returns the test class currently executing in this rule.</summary>
		/// <remarks>Returns the test class currently executing in this rule.</remarks>
		public virtual Type GetTestClass()
		{
			Description localDescription = description;
			if (localDescription == null)
			{
				throw new RuntimeException("The rule is not currently executing.");
			}
			return localDescription.GetTestClass();
		}
	}
}
