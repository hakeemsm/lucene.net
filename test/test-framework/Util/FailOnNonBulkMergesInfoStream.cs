/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>Hackidy-HÃ¤ck-Hack to cause a test to fail on non-bulk merges</summary>
	public class FailOnNonBulkMergesInfoStream : InfoStream
	{
		// TODO: we should probably be a wrapper so verbose still works...
		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
		}

		public override bool IsEnabled(string component)
		{
			return true;
		}

		public override void Message(string component, string message)
		{
		}
		 
		//assert !message.contains("non-bulk merges");
	}
}
