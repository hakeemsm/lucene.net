using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class StoredFieldsReader : ICloneable, IDisposable
    {
        public abstract void VisitDocument(int n, StoredFieldVisitor visitor);

        public abstract object Clone();

        public abstract long RamBytesUsed { get; }
		public abstract void CheckIntegrity();
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}
