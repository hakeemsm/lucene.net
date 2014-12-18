using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class PostingsReaderBase : IDisposable
    {
        protected PostingsReaderBase()
        {
        }

        public abstract void Init(IndexInput termsIn);

        public abstract BlockTermState NewTermState();

		/// <summary>Actually decode metadata for next term</summary>
		/// <seealso cref="PostingsWriterBase.EncodeTerm(long[], Lucene.Net.TestFramework.Store.DataOutput, Lucene.Net.TestFramework.Index.FieldInfo, BlockTermState, bool)
		/// 	"></seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract void DecodeTerm(long[] longs, DataInput @in, FieldInfo fieldInfo, 
			BlockTermState state, bool absolute);

        public abstract DocsEnum Docs(FieldInfo fieldInfo, BlockTermState state, IBits skipDocs, DocsEnum reuse, int flags);

        public abstract DocsAndPositionsEnum DocsAndPositions(FieldInfo fieldInfo, BlockTermState state, IBits skipDocs, DocsAndPositionsEnum reuse,
                                                        int flags);

		public abstract long RamBytesUsed();
		public abstract void CheckIntegrity();
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

    }
}
