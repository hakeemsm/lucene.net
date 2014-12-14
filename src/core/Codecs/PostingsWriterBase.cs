using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public abstract class PostingsWriterBase : PostingsConsumer, IDisposable
    {
        protected PostingsWriterBase()
        {
        }

		// TODO: find a better name; this defines the API that the
		// terms dict impls use to talk to a postings impl.
		// TermsDict + PostingsReader/WriterBase == PostingsConsumer/Producer
		/// <summary>
		/// Called once after startup, before any terms have been
		/// added.
		/// </summary>
		/// <remarks>
		/// Called once after startup, before any terms have been
		/// added.  Implementations typically write a header to
		/// the provided
		/// <code>termsOut</code>
		/// .
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract void Init(IndexOutput termsOut);

		/// <summary>Return a newly created empty TermState</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract BlockTermState NewTermState();

		/// <summary>Start a new term.</summary>
		/// <remarks>
		/// Start a new term.  Note that a matching call to
		/// <see cref="FinishTerm(BlockTermState)">FinishTerm(BlockTermState)</see>
		/// is done, only if the term has at least one
		/// document.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
        public abstract void StartTerm();

		/// <summary>Finishes the current term.</summary>
		/// <remarks>
		/// Finishes the current term.  The provided
		/// <see cref="BlockTermState">BlockTermState</see>
		/// contains the term's summary statistics,
		/// and will holds metadata from PBF when returned
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract void FinishTerm(BlockTermState state);

		/// <summary>Encode metadata as long[] and byte[].</summary>
		/// <remarks>
		/// Encode metadata as long[] and byte[].
		/// <code>absolute</code>
		/// controls whether
		/// current term is delta encoded according to latest term.
		/// Usually elements in
		/// <code>longs</code>
		/// are file pointers, so each one always
		/// increases when a new term is consumed.
		/// <code>out</code>
		/// is used to write generic
		/// bytes, which are not monotonic.
		/// NOTE: sometimes long[] might contain "don't care" values that are unused, e.g.
		/// the pointer to postings list may not be defined for some terms but is defined
		/// for others, if it is designed to inline  some postings data in term dictionary.
		/// In this case, the postings writer should always use the last value, so that each
		/// element in metadata long[] remains monotonic.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract void EncodeTerm(long[] longs, DataOutput @out, FieldInfo fieldInfo
			, BlockTermState state, bool absolute);
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

		/// <summary>
		/// Sets the current field for writing, and returns the
		/// fixed length of long[] metadata (which is fixed per
		/// field), called when the writing switches to another field.
		/// </summary>
		/// <remarks>
		/// Sets the current field for writing, and returns the
		/// fixed length of long[] metadata (which is fixed per
		/// field), called when the writing switches to another field.
		/// </remarks>
		public abstract int SetField(FieldInfo fieldInfo);
        protected abstract void Dispose(bool disposing);
    }
}
