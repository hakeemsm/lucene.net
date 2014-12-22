/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>Prints nothing.</summary>
	/// <remarks>
	/// Prints nothing. Just to make sure tests pass w/ and without enabled InfoStream
	/// without actually making noise.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class NullInfoStream : InfoStream
	{
		public override void Message(string component, string message)
		{
		}

		 
		//assert component != null;
		 
		//assert message != null;
		public override bool IsEnabled(string component)
		{
			 
			//assert component != null;
			return true;
		}

		// to actually enable logging, we just ignore on message()
		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
		}
	}
}
