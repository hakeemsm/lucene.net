using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Directory = System.IO.Directory;

namespace Lucene.Net.Codecs.Cranky.TestFramework
{
	internal class CrankyLiveDocsFormat : LiveDocsFormat
	{
		internal readonly LiveDocsFormat liveDocs;

		internal readonly Random random;

		internal CrankyLiveDocsFormat(LiveDocsFormat del, Random random)
		{
			this.liveDocs = del;
			this.random = random;
		}

		
		public override IMutableBits NewLiveDocs(int size)
		{
			return liveDocs.NewLiveDocs(size);
		}

		
		public override IMutableBits NewLiveDocs(IBits existing)
		{
			return liveDocs.NewLiveDocs(existing);
		}

		
		public override IBits ReadLiveDocs(Lucene.Net.Store.Directory dir, SegmentCommitInfo info, IOContext context)
		{
			return liveDocs.ReadLiveDocs(dir, info, context);
		}

		
		public override void WriteLiveDocs(IMutableBits bits, Lucene.Net.Store.Directory dir, SegmentCommitInfo info, int newDelCount, IOContext context)
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from LiveDocsFormat.writeLiveDocs()");
			}
			liveDocs.WriteLiveDocs(bits, dir, info, newDelCount, context);
		}

		
		public override void Files(SegmentCommitInfo info, ICollection<string> files)
		{
			// TODO: is this called only from write? if so we should throw exception!
			liveDocs.Files(info, files);
		}
	}
}
