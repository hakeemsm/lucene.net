/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using NUnit.Framework.Rules;
using NUnit.Framework.Runner;
using NUnit.Framework.Runners.Model;
using Lucene.Net.TestFramework.Search;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>This rule will fail the test if it has insane field caches.</summary>
	/// <remarks>
	/// This rule will fail the test if it has insane field caches.
	/// <p>
	/// calling assertSaneFieldCaches here isn't as useful as having test
	/// classes call it directly from the scope where the index readers
	/// are used, because they could be gc'ed just before this tearDown
	/// method is called.
	/// <p>
	/// But it's better then nothing.
	/// <p>
	/// If you are testing functionality that you know for a fact
	/// "violates" FieldCache sanity, then you should either explicitly
	/// call purgeFieldCache at the end of your test method, or refactor
	/// your Test class so that the inconsistent FieldCache usages are
	/// isolated in distinct test methods
	/// </remarks>
	/// <seealso cref="FieldCacheSanityChecker">FieldCacheSanityChecker</seealso>
	public class TestRuleFieldCacheSanity : TestRule
	{
		// javadocs
		public virtual Statement Apply(Statement s, Description d)
		{
			return new _Statement_48(s, d);
		}

		private sealed class _Statement_48 : Statement
		{
			public _Statement_48(Statement s, Description d)
			{
				this.s = s;
				this.d = d;
			}

			/// <exception cref="System.Exception"></exception>
			public override void Evaluate()
			{
				s.Evaluate();
				Exception problem = null;
				try
				{
					LuceneTestCase.AssertSaneFieldCaches(d.GetDisplayName());
				}
				catch (Exception t)
				{
					problem = t;
				}
				FieldCache.DEFAULT.PurgeAllCaches();
				if (problem != null)
				{
					Rethrow.Rethrow(problem);
				}
			}

			private readonly Statement s;

			private readonly Description d;
		}
	}
}
