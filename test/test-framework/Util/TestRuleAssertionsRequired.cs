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
	/// <summary>Require assertions for Lucene/Solr packages.</summary>
	/// <remarks>Require assertions for Lucene/Solr packages.</remarks>
	public class TestRuleAssertionsRequired : TestRule
	{
		public virtual Statement Apply(Statement @base, Description description)
		{
			return new _Statement_30(description, @base);
		}

		private sealed class _Statement_30 : Statement
		{
			public _Statement_30(Description description, Statement @base)
			{
				this.description = description;
				this.@base = @base;
			}

			/// <exception cref="System.Exception"></exception>
			public override void Evaluate()
			{
				try
				{
					//HM:revisit 
					//assert false;
					string msg = "Test class requires enabled assertions, enable globally (-ea)" + " or for Solr/Lucene subpackages only: "
						 + description.GetClassName();
					System.Console.Error.WriteLine(msg);
					throw new Exception(msg);
				}
				catch (Exception)
				{
				}
				// Ok, enabled.
				@base.Evaluate();
			}

			private readonly Description description;

			private readonly Statement @base;
		}
	}
}
