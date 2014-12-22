/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene42;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene42
{
	/// <summary>
	/// Read-write version of
	/// <see cref="Lucene42Codec">Lucene42Codec</see>
	/// for testing.
	/// </summary>
	public class Lucene42RWCodec : Lucene42Codec
	{
		private static readonly DocValuesFormat dv = new Lucene42RWDocValuesFormat();

		private static readonly Lucene.Net.Codecs.NormsFormat norms = new Lucene42NormsFormat
			();

		private sealed class _Lucene42FieldInfosFormat_37 : Lucene42FieldInfosFormat
		{
			public _Lucene42FieldInfosFormat_37()
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
					return new Lucene42FieldInfosWriter();
				}
			}
		}

		private readonly Lucene.Net.Codecs.FieldInfosFormat fieldInfosFormat = new 
			_Lucene42FieldInfosFormat_37();

		public override DocValuesFormat GetDocValuesFormatForField(string field)
		{
			return dv;
		}

		public override Lucene.Net.Codecs.NormsFormat NormsFormat()
		{
			return norms;
		}

		public override Lucene.Net.Codecs.FieldInfosFormat FieldInfosFormat()
		{
			return fieldInfosFormat;
		}
	}
}
