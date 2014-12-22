/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene3x
{
	/// <lucene.internal></lucene.internal>
	/// <lucene.experimental></lucene.experimental>
	internal class PreFlexRWFieldInfosFormat : Lucene3xFieldInfosFormat
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override FieldInfosReader GetFieldInfosReader()
		{
			return new PreFlexRWFieldInfosReader();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldInfosWriter GetFieldInfosWriter()
		{
			return new PreFlexRWFieldInfosWriter();
		}
	}
}
