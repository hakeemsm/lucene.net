/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries
{
	/// <summary>Filter to remove duplicate values from search results.</summary>
	/// <remarks>
	/// Filter to remove duplicate values from search results.
	/// <p>
	/// WARNING: for this to work correctly, you may have to wrap
	/// your reader as it cannot current deduplicate across different
	/// index segments.
	/// </remarks>
	/// <seealso cref="Lucene.Net.Index.SlowCompositeReaderWrapper">Lucene.Net.Index.SlowCompositeReaderWrapper
	/// 	</seealso>
	public class DuplicateFilter : Filter
	{
		/// <summary>
		/// KeepMode determines which document id to consider as the master, all others being
		/// identified as duplicates.
		/// </summary>
		/// <remarks>
		/// KeepMode determines which document id to consider as the master, all others being
		/// identified as duplicates. Selecting the "first occurrence" can potentially save on IO.
		/// </remarks>
		public enum KeepMode
		{
			KM_USE_FIRST_OCCURRENCE,
			KM_USE_LAST_OCCURRENCE
		}

		private DuplicateFilter.KeepMode keepMode;

		/// <summary>
		/// "Full" processing mode starts by setting all bits to false and only setting bits
		/// for documents that contain the given field and are identified as none-duplicates.
		/// </summary>
		/// <remarks>
		/// "Full" processing mode starts by setting all bits to false and only setting bits
		/// for documents that contain the given field and are identified as none-duplicates.
		/// <p/>
		/// "Fast" processing sets all bits to true then unsets all duplicate docs found for the
		/// given field. This approach avoids the need to read DocsEnum for terms that are seen
		/// to have a document frequency of exactly "1" (i.e. no duplicates). While a potentially
		/// faster approach , the downside is that bitsets produced will include bits set for
		/// documents that do not actually contain the field given.
		/// </remarks>
		public enum ProcessingMode
		{
			PM_FULL_VALIDATION,
			PM_FAST_INVALIDATION
		}

		private DuplicateFilter.ProcessingMode processingMode;

		private string fieldName;

		public DuplicateFilter(string fieldName) : this(fieldName, DuplicateFilter.KeepMode
			.KM_USE_LAST_OCCURRENCE, DuplicateFilter.ProcessingMode.PM_FULL_VALIDATION)
		{
		}

		public DuplicateFilter(string fieldName, DuplicateFilter.KeepMode keepMode, DuplicateFilter.ProcessingMode
			 processingMode)
		{
			// TODO: make duplicate filter aware of ReaderContext such that we can
			// filter duplicates across segments
			this.fieldName = fieldName;
			this.keepMode = keepMode;
			this.processingMode = processingMode;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
			)
		{
			if (processingMode == DuplicateFilter.ProcessingMode.PM_FAST_INVALIDATION)
			{
				return FastBits(((AtomicReader)context.Reader()), acceptDocs);
			}
			else
			{
				return CorrectBits(((AtomicReader)context.Reader()), acceptDocs);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private FixedBitSet CorrectBits(AtomicReader reader, Bits acceptDocs)
		{
			FixedBitSet bits = new FixedBitSet(reader.MaxDoc());
			//assume all are INvalid
			Terms terms = reader.Fields().Terms(fieldName);
			if (terms == null)
			{
				return bits;
			}
			TermsEnum termsEnum = terms.Iterator(null);
			DocsEnum docs = null;
			while (true)
			{
				BytesRef currTerm = termsEnum.Next();
				if (currTerm == null)
				{
					break;
				}
				else
				{
					docs = termsEnum.Docs(acceptDocs, docs, DocsEnum.FLAG_NONE);
					int doc = docs.NextDoc();
					if (doc != DocIdSetIterator.NO_MORE_DOCS)
					{
						if (keepMode == DuplicateFilter.KeepMode.KM_USE_FIRST_OCCURRENCE)
						{
							bits.Set(doc);
						}
						else
						{
							int lastDoc = doc;
							while (true)
							{
								lastDoc = doc;
								doc = docs.NextDoc();
								if (doc == DocIdSetIterator.NO_MORE_DOCS)
								{
									break;
								}
							}
							bits.Set(lastDoc);
						}
					}
				}
			}
			return bits;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private FixedBitSet FastBits(AtomicReader reader, Bits acceptDocs)
		{
			FixedBitSet bits = new FixedBitSet(reader.MaxDoc());
			bits.Set(0, reader.MaxDoc());
			//assume all are valid
			Terms terms = reader.Fields().Terms(fieldName);
			if (terms == null)
			{
				return bits;
			}
			TermsEnum termsEnum = terms.Iterator(null);
			DocsEnum docs = null;
			while (true)
			{
				BytesRef currTerm = termsEnum.Next();
				if (currTerm == null)
				{
					break;
				}
				else
				{
					if (termsEnum.DocFreq() > 1)
					{
						// unset potential duplicates
						docs = termsEnum.Docs(acceptDocs, docs, DocsEnum.FLAG_NONE);
						int doc = docs.NextDoc();
						if (doc != DocIdSetIterator.NO_MORE_DOCS)
						{
							if (keepMode == DuplicateFilter.KeepMode.KM_USE_FIRST_OCCURRENCE)
							{
								doc = docs.NextDoc();
							}
						}
						int lastDoc = -1;
						while (true)
						{
							lastDoc = doc;
							bits.Clear(lastDoc);
							doc = docs.NextDoc();
							if (doc == DocIdSetIterator.NO_MORE_DOCS)
							{
								break;
							}
						}
						if (keepMode == DuplicateFilter.KeepMode.KM_USE_LAST_OCCURRENCE)
						{
							// restore the last bit
							bits.Set(lastDoc);
						}
					}
				}
			}
			return bits;
		}

		public virtual string GetFieldName()
		{
			return fieldName;
		}

		public virtual void SetFieldName(string fieldName)
		{
			this.fieldName = fieldName;
		}

		public virtual DuplicateFilter.KeepMode GetKeepMode()
		{
			return keepMode;
		}

		public virtual void SetKeepMode(DuplicateFilter.KeepMode keepMode)
		{
			this.keepMode = keepMode;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if ((obj == null) || (obj.GetType() != this.GetType()))
			{
				return false;
			}
			Lucene.Net.Sandbox.Queries.DuplicateFilter other = (Lucene.Net.Sandbox.Queries.DuplicateFilter
				)obj;
			return keepMode == other.keepMode && processingMode == other.processingMode && fieldName
				 != null && fieldName.Equals(other.fieldName);
		}

		public override int GetHashCode()
		{
			int hash = 217;
			hash = 31 * hash + keepMode.GetHashCode();
			hash = 31 * hash + processingMode.GetHashCode();
			hash = 31 * hash + fieldName.GetHashCode();
			return hash;
		}

		public virtual DuplicateFilter.ProcessingMode GetProcessingMode()
		{
			return processingMode;
		}

		public virtual void SetProcessingMode(DuplicateFilter.ProcessingMode processingMode
			)
		{
			this.processingMode = processingMode;
		}
	}
}
