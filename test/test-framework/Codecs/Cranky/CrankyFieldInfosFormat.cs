/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Cranky;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Cranky
{
	internal class CrankyFieldInfosFormat : FieldInfosFormat
	{
		internal readonly FieldInfosFormat delegate_;

		internal readonly Random random;

		internal CrankyFieldInfosFormat(FieldInfosFormat delegate_, Random random)
		{
			this.delegate_ = delegate_;
			this.random = random;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldInfosReader GetFieldInfosReader()
		{
			return delegate_.GetFieldInfosReader();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldInfosWriter GetFieldInfosWriter()
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from FieldInfosFormat.getFieldInfosWriter()"
					);
			}
			return new CrankyFieldInfosFormat.CrankyFieldInfosWriter(delegate_.GetFieldInfosWriter
				(), random);
		}

		internal class CrankyFieldInfosWriter : FieldInfosWriter
		{
			internal readonly FieldInfosWriter delegate_;

			internal readonly Random random;

			internal CrankyFieldInfosWriter(FieldInfosWriter delegate_, Random random)
			{
				this.delegate_ = delegate_;
				this.random = random;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(Directory directory, string segmentName, string segmentSuffix
				, FieldInfos infos, IOContext context)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from FieldInfosWriter.write()");
				}
				delegate_.Write(directory, segmentName, segmentSuffix, infos, context);
			}
		}
	}
}
