/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene40;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene40
{
	/// <summary>Read-write version of Lucene40Codec for testing</summary>
	public sealed class Lucene40RWCodec : Lucene40Codec
	{
		private sealed class _Lucene40FieldInfosFormat_33 : Lucene40FieldInfosFormat
		{
			public _Lucene40FieldInfosFormat_33()
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override FieldInfosWriter GetFieldInfosWriter()
			{
				if (!LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
				{
					return base.GetFieldInfosWriter();
				}
				else
				{
					return new Lucene40FieldInfosWriter();
				}
			}
		}

		private readonly Lucene.Net.Codecs.FieldInfosFormat fieldInfos = new _Lucene40FieldInfosFormat_33
			();

		private readonly Lucene.Net.Codecs.DocValuesFormat docValues = new Lucene40RWDocValuesFormat
			();

		private readonly Lucene.Net.Codecs.NormsFormat norms = new Lucene40RWNormsFormat
			();

		public override Lucene.Net.Codecs.FieldInfosFormat FieldInfosFormat()
		{
			return fieldInfos;
		}

		public override Lucene.Net.Codecs.DocValuesFormat DocValuesFormat()
		{
			return docValues;
		}

		public override Lucene.Net.Codecs.NormsFormat NormsFormat()
		{
			return norms;
		}
	}
}
