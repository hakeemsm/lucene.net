/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs.Mocksep;
using Lucene.Net.Codecs.Sep;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Lucene.Net.Codecs.Mocksep
{
	/// <summary>
	/// Encodes ints directly as vInts with
	/// <see cref="MockSingleIntIndexOutput">MockSingleIntIndexOutput</see>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class MockSingleIntFactory : IntStreamFactory
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override IntIndexInput OpenInput(Directory dir, string fileName, IOContext
			 context)
		{
			return new MockSingleIntIndexInput(dir, fileName, context);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IntIndexOutput CreateOutput(Directory dir, string fileName, IOContext
			 context)
		{
			return new MockSingleIntIndexOutput(dir, fileName, context);
		}
	}
}
