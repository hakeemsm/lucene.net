using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs
{
    public abstract class DocValuesProducer : IDisposable
    {
        protected DocValuesProducer()
        {
        }

        public abstract NumericDocValues GetNumeric(FieldInfo field);

        public abstract BinaryDocValues GetBinary(FieldInfo field);

        public abstract SortedDocValues GetSorted(FieldInfo field);

        public abstract SortedSetDocValues GetSortedSet(FieldInfo field);

		/// <summary>
		/// Returns a
		/// <see cref="Lucene.Net.Util.Bits">Lucene.Net.Util.Bits</see>
		/// at the size of <code>reader.maxDoc()</code>,
		/// with turned on bits for each docid that does have a value for this field.
		/// The returned instance need not be thread-safe: it will only be
		/// used by a single thread.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract IBits GetDocsWithField(FieldInfo field);

		/// <summary>Returns approximate RAM bytes used</summary>
		public abstract long RamBytesUsed { get; }

		/// <summary>
		/// Checks consistency of this producer
		/// <p>
		/// Note that this may be costly in terms of I/O, e.g.
		/// </summary>
		/// <remarks>
		/// Checks consistency of this producer
		/// <p>
		/// Note that this may be costly in terms of I/O, e.g.
		/// may involve computing a checksum value against large data files.
		/// </remarks>
		/// <lucene.internal></lucene.internal>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract void CheckIntegrity();
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}
