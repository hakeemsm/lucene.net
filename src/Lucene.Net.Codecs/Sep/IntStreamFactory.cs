/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs.Sep;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Codecs.Sep
{
	/// <summary>Provides int reader and writer to specified files.</summary>
	/// <remarks>Provides int reader and writer to specified files.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class IntStreamFactory
	{
		/// <summary>
		/// Create an
		/// <see cref="IntIndexInput">IntIndexInput</see>
		/// on the provided
		/// fileName.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract IntIndexInput OpenInput(Directory dir, string fileName, IOContext
			 context);

		/// <summary>
		/// Create an
		/// <see cref="IntIndexOutput">IntIndexOutput</see>
		/// on the provided
		/// fileName.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract IntIndexOutput CreateOutput(Directory dir, string fileName, IOContext
			 context);
	}
}
