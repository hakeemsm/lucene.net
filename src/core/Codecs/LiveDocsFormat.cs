using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs
{
    public abstract class LiveDocsFormat
    {
        protected LiveDocsFormat()
        {
        }

        public abstract IMutableBits NewLiveDocs(int size);

        public abstract IMutableBits NewLiveDocs(IBits existing);

        public abstract IBits ReadLiveDocs(Directory dir, SegmentCommitInfo info, IOContext context);

        public abstract void WriteLiveDocs(IMutableBits bits, Directory dir, SegmentCommitInfo info, int newDelCount, IOContext context);

        public abstract void Files(SegmentCommitInfo info, ICollection<String> files);
    }
}
