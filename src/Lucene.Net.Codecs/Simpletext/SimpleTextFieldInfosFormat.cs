/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	/// <summary>
	/// plaintext field infos format
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextFieldInfosFormat : FieldInfosFormat
	{
		private readonly FieldInfosReader reader = new SimpleTextFieldInfosReader();

		private readonly FieldInfosWriter writer = new SimpleTextFieldInfosWriter();

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldInfosReader GetFieldInfosReader()
		{
			return reader;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldInfosWriter GetFieldInfosWriter()
		{
			return writer;
		}
	}
}
