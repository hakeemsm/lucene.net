using Lucene.Net.TestFramework.Index;

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
