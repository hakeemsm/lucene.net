/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.TestFramework.Search;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>
	/// Prepares and restores
	/// <see cref="LuceneTestCase">LuceneTestCase</see>
	/// at instance level
	/// (fine grained junk that doesn't fit anywhere else).
	/// </summary>
	internal sealed class TestRuleSetupAndRestoreInstanceEnv : AbstractBeforeAfterRule
	{
		private int savedBoolMaxClauseCount;

		protected internal override void Before()
		{
			savedBoolMaxClauseCount = BooleanQuery.GetMaxClauseCount();
		}

		protected internal override void After()
		{
			BooleanQuery.SetMaxClauseCount(savedBoolMaxClauseCount);
		}
	}
}
