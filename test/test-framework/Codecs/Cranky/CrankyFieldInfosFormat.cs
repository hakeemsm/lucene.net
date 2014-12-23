using System;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Directory = System.IO.Directory;

namespace Lucene.Net.Codecs.Cranky.TestFramework
{
	internal class CrankyFieldInfosFormat : FieldInfosFormat
	{
		internal readonly FieldInfosFormat fiFormat;

		internal readonly Random random;

		internal CrankyFieldInfosFormat(FieldInfosFormat delegate_, Random random)
		{
			this.fiFormat = delegate_;
			this.random = random;
		}

		
		public override FieldInfosReader FieldInfosReader
		{
		    get { return fiFormat.FieldInfosReader; }
		}

		
		public override FieldInfosWriter FieldInfosWriter
		{
		    get
		    {
		        if (random.Next(100) == 0)
		        {
		            throw new IOException("Fake IOException from FieldInfosFormat.getFieldInfosWriter()");
		        }
		        return new CrankyFieldInfosWriter(fiFormat.FieldInfosWriter, random);
		    }
		}

		internal class CrankyFieldInfosWriter : FieldInfosWriter
		{
			internal readonly FieldInfosWriter fiWriter;

			internal readonly Random random;

			internal CrankyFieldInfosWriter(FieldInfosWriter fiwInput, Random random)
			{
				this.fiWriter = fiwInput;
				this.random = random;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(Lucene.Net.Store.Directory directory, string segmentName, string segmentSuffix
				, FieldInfos infos, IOContext context)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from FieldInfosWriter.write()");
				}
				fiWriter.Write(directory, segmentName, segmentSuffix, infos, context);
			}
		}
	}
}
