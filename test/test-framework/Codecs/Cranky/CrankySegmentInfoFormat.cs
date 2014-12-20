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
	internal class CrankySegmentInfoFormat : SegmentInfoFormat
	{
		internal readonly SegmentInfoFormat delegate_;

		internal readonly Random random;

		internal CrankySegmentInfoFormat(SegmentInfoFormat delegate_, Random random)
		{
			this.delegate_ = delegate_;
			this.random = random;
		}

		public override SegmentInfoReader GetSegmentInfoReader()
		{
			return delegate_.GetSegmentInfoReader();
		}

		public override SegmentInfoWriter GetSegmentInfoWriter()
		{
			return new CrankySegmentInfoFormat.CrankySegmentInfoWriter(delegate_.GetSegmentInfoWriter
				(), random);
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

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(Directory dir, SegmentInfo info, FieldInfos fis, IOContext
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
