/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Lucene42;
using Org.Apache.Lucene.Codecs.Lucene45;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene45
{
	/// <summary>
	/// Read-write version of
	/// <see cref="Lucene45Codec">Lucene45Codec</see>
	/// for testing.
	/// </summary>
	public class Lucene45RWCodec : Lucene45Codec
	{
		private sealed class _Lucene42FieldInfosFormat_34 : Lucene42FieldInfosFormat
		{
			public _Lucene42FieldInfosFormat_34()
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

		private readonly Org.Apache.Lucene.Codecs.FieldInfosFormat fieldInfosFormat = new 
			_Lucene42FieldInfosFormat_34();

		public override Org.Apache.Lucene.Codecs.FieldInfosFormat FieldInfosFormat()
		{
			return fieldInfosFormat;
		}
	}
}
