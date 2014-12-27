/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestLogMergePolicy : BaseMergePolicyTestCase
	{
		protected override Lucene.Net.Index.MergePolicy MergePolicy()
		{
			return NewLogMergePolicy(Random());
		}
	}
}
