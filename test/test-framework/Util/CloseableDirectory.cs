using System;
using Lucene.Net.Store;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>
	/// Attempts to close a
	/// <see cref="Lucene.Net.TestFramework.Store.BaseDirectoryWrapper">Lucene.Net.TestFramework.Store.BaseDirectoryWrapper
	/// 	</see>
	/// .
	/// </summary>
	/// <seealso cref="LuceneTestCase.NewDirectory(Sharpen.Random)">LuceneTestCase.NewDirectory(Sharpen.Random)
	/// 	</seealso>
	internal sealed class CloseableDirectory : IDisposable
	{
		private readonly BaseDirectoryWrapper dir;

		private readonly TestRuleMarkFailure failureMarker;

		public CloseableDirectory(BaseDirectoryWrapper dir, TestRuleMarkFailure failureMarker
			)
		{
			this.dir = dir;
			this.failureMarker = failureMarker;
		}

		public void Dispose()
		{
			// We only attempt to check open/closed state if there were no other test
			// failures.
			try
			{
				if (failureMarker.WasSuccessful() && dir.IsOpen())
				{
                    dir.Close();
				}
			}
			finally
			{
			}
		}
		
		//assert.fail("Directory not closed: " + dir);
		// TODO: perform real close of the delegate: LUCENE-4058
		// dir.close();
	}
}
