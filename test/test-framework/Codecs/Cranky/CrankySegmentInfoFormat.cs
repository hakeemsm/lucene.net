using System;
using System.IO;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Directory = System.IO.Directory;

namespace Lucene.Net.Codecs.Cranky.TestFramework
{
	internal class CrankySegmentInfoFormat : SegmentInfoFormat
	{
		internal readonly SegmentInfoFormat segFormat;

		internal readonly Random random;

		internal CrankySegmentInfoFormat(SegmentInfoFormat del, Random random)
		{
			this.segFormat = del;
			this.random = random;
		}

		public override SegmentInfoReader SegmentInfoReader
		{
		    get { return segFormat.SegmentInfoReader; }
		}

		public override SegmentInfoWriter SegmentInfoWriter
		{
		    get
		    {
		        return new CrankySegmentInfoWriter(segFormat.SegmentInfoWriter, random);
		    }
		}

		internal class CrankySegmentInfoWriter : SegmentInfoWriter
		{
			internal readonly SegmentInfoWriter delegate_;

			internal readonly Random random;

			internal CrankySegmentInfoWriter(SegmentInfoWriter delegate_, Random random)
			{
				this.delegate_ = delegate_;
				this.random = random;
			}

			
			public override void Write(Lucene.Net.Store.Directory dir, SegmentInfo info, FieldInfos fis, IOContext
				 ioContext)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from SegmentInfoWriter.write()");
				}
				delegate_.Write(dir, info, fis, ioContext);
			}
		}
	}
}
