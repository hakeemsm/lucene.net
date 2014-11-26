/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	internal class BufferedUpdates
	{
		internal static readonly int BYTES_PER_DEL_TERM = 9 * RamUsageEstimator.NUM_BYTES_OBJECT_REF
			 + 7 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + 10 * RamUsageEstimator.NUM_BYTES_INT;

		internal static readonly int BYTES_PER_DEL_DOCID = 2 * RamUsageEstimator.NUM_BYTES_OBJECT_REF
			 + RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator.NUM_BYTES_INT;

		internal static readonly int BYTES_PER_DEL_QUERY = 5 * RamUsageEstimator.NUM_BYTES_OBJECT_REF
			 + 2 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + 2 * RamUsageEstimator.NUM_BYTES_INT
			 + 24;

		internal static readonly int BYTES_PER_NUMERIC_FIELD_ENTRY = 7 * RamUsageEstimator
			.NUM_BYTES_OBJECT_REF + 3 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator
			.NUM_BYTES_ARRAY_HEADER + 5 * RamUsageEstimator.NUM_BYTES_INT + RamUsageEstimator
			.NUM_BYTES_FLOAT;

		internal static readonly int BYTES_PER_NUMERIC_UPDATE_ENTRY = 7 * RamUsageEstimator
			.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator
			.NUM_BYTES_INT;

		internal static readonly int BYTES_PER_BINARY_FIELD_ENTRY = 7 * RamUsageEstimator
			.NUM_BYTES_OBJECT_REF + 3 * RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator
			.NUM_BYTES_ARRAY_HEADER + 5 * RamUsageEstimator.NUM_BYTES_INT + RamUsageEstimator
			.NUM_BYTES_FLOAT;

		internal static readonly int BYTES_PER_BINARY_UPDATE_ENTRY = 7 * RamUsageEstimator
			.NUM_BYTES_OBJECT_REF + RamUsageEstimator.NUM_BYTES_OBJECT_HEADER + RamUsageEstimator
			.NUM_BYTES_INT;

		internal readonly AtomicInteger numTermDeletes = new AtomicInteger();

		internal readonly AtomicInteger numNumericUpdates = new AtomicInteger();

		internal readonly AtomicInteger numBinaryUpdates = new AtomicInteger();

		internal readonly IDictionary<Term, int> terms = new Dictionary<Term, int>();

		internal readonly IDictionary<Query, int> queries = new Dictionary<Query, int>();

		internal readonly IList<int> docIDs = new AList<int>();

		internal readonly IDictionary<string, LinkedHashMap<Term, DocValuesUpdate.NumericDocValuesUpdate
			>> numericUpdates = new Dictionary<string, LinkedHashMap<Term, DocValuesUpdate.NumericDocValuesUpdate
			>>();

		internal readonly IDictionary<string, LinkedHashMap<Term, DocValuesUpdate.BinaryDocValuesUpdate
			>> binaryUpdates = new Dictionary<string, LinkedHashMap<Term, DocValuesUpdate.BinaryDocValuesUpdate
			>>();

		public static readonly int MAX_INT = Sharpen.Extensions.ValueOf(int.MaxValue);

		internal readonly AtomicLong bytesUsed;

		private const bool VERBOSE_DELETES = false;

		internal long gen;

		public BufferedUpdates()
		{
			// NOTE: instances of this class are accessed either via a private
			// instance on DocumentWriterPerThread, or via sync'd code by
			// DocumentsWriterDeleteQueue
			// Map<dvField,Map<updateTerm,NumericUpdate>>
			// For each field we keep an ordered list of NumericUpdates, key'd by the
			// update Term. LinkedHashMap guarantees we will later traverse the map in
			// insertion order (so that if two terms affect the same document, the last
			// one that came in wins), and helps us detect faster if the same Term is
			// used to update the same field multiple times (so we later traverse it
			// only once).
			// Map<dvField,Map<updateTerm,BinaryUpdate>>
			// For each field we keep an ordered list of BinaryUpdates, key'd by the
			// update Term. LinkedHashMap guarantees we will later traverse the map in
			// insertion order (so that if two terms affect the same document, the last
			// one that came in wins), and helps us detect faster if the same Term is
			// used to update the same field multiple times (so we later traverse it
			// only once).
			this.bytesUsed = new AtomicLong();
		}

		public override string ToString()
		{
			string s = "gen=" + gen;
			if (numTermDeletes.Get() != 0)
			{
				s += " " + numTermDeletes.Get() + " deleted terms (unique count=" + terms.Count +
					 ")";
			}
			if (queries.Count != 0)
			{
				s += " " + queries.Count + " deleted queries";
			}
			if (docIDs.Count != 0)
			{
				s += " " + docIDs.Count + " deleted docIDs";
			}
			if (numNumericUpdates.Get() != 0)
			{
				s += " " + numNumericUpdates.Get() + " numeric updates (unique count=" + numericUpdates
					.Count + ")";
			}
			if (numBinaryUpdates.Get() != 0)
			{
				s += " " + numBinaryUpdates.Get() + " binary updates (unique count=" + binaryUpdates
					.Count + ")";
			}
			if (bytesUsed.Get() != 0)
			{
				s += " bytesUsed=" + bytesUsed.Get();
			}
			return s;
		}

		public virtual void AddQuery(Query query, int docIDUpto)
		{
			int current = queries.Put(query, docIDUpto);
			// increment bytes used only if the query wasn't added so far.
			if (current == null)
			{
				bytesUsed.AddAndGet(BYTES_PER_DEL_QUERY);
			}
		}

		public virtual void AddDocID(int docID)
		{
			docIDs.AddItem(Sharpen.Extensions.ValueOf(docID));
			bytesUsed.AddAndGet(BYTES_PER_DEL_DOCID);
		}

		public virtual void AddTerm(Term term, int docIDUpto)
		{
			int current = terms.Get(term);
			if (current != null && docIDUpto < current)
			{
				// Only record the new number if it's greater than the
				// current one.  This is important because if multiple
				// threads are replacing the same doc at nearly the
				// same time, it's possible that one thread that got a
				// higher docID is scheduled before the other
				// threads.  If we blindly replace than we can
				// incorrectly get both docs indexed.
				return;
			}
			terms.Put(term, Sharpen.Extensions.ValueOf(docIDUpto));
			// note that if current != null then it means there's already a buffered
			// delete on that term, therefore we seem to over-count. this over-counting
			// is done to respect IndexWriterConfig.setMaxBufferedDeleteTerms.
			numTermDeletes.IncrementAndGet();
			if (current == null)
			{
				bytesUsed.AddAndGet(BYTES_PER_DEL_TERM + term.bytes.length + (RamUsageEstimator.NUM_BYTES_CHAR
					 * term.Field().Length));
			}
		}

		public virtual void AddNumericUpdate(DocValuesUpdate.NumericDocValuesUpdate update
			, int docIDUpto)
		{
			LinkedHashMap<Term, DocValuesUpdate.NumericDocValuesUpdate> fieldUpdates = numericUpdates
				.Get(update.field);
			if (fieldUpdates == null)
			{
				fieldUpdates = new LinkedHashMap<Term, DocValuesUpdate.NumericDocValuesUpdate>();
				numericUpdates.Put(update.field, fieldUpdates);
				bytesUsed.AddAndGet(BYTES_PER_NUMERIC_FIELD_ENTRY);
			}
			DocValuesUpdate.NumericDocValuesUpdate current = fieldUpdates.Get(update.term);
			if (current != null && docIDUpto < current.docIDUpto)
			{
				// Only record the new number if it's greater than or equal to the current
				// one. This is important because if multiple threads are replacing the
				// same doc at nearly the same time, it's possible that one thread that
				// got a higher docID is scheduled before the other threads.
				return;
			}
			update.docIDUpto = docIDUpto;
			// since it's a LinkedHashMap, we must first remove the Term entry so that
			// it's added last (we're interested in insertion-order).
			if (current != null)
			{
				Sharpen.Collections.Remove(fieldUpdates, update.term);
			}
			fieldUpdates.Put(update.term, update);
			numNumericUpdates.IncrementAndGet();
			if (current == null)
			{
				bytesUsed.AddAndGet(BYTES_PER_NUMERIC_UPDATE_ENTRY + update.SizeInBytes());
			}
		}

		public virtual void AddBinaryUpdate(DocValuesUpdate.BinaryDocValuesUpdate update, 
			int docIDUpto)
		{
			LinkedHashMap<Term, DocValuesUpdate.BinaryDocValuesUpdate> fieldUpdates = binaryUpdates
				.Get(update.field);
			if (fieldUpdates == null)
			{
				fieldUpdates = new LinkedHashMap<Term, DocValuesUpdate.BinaryDocValuesUpdate>();
				binaryUpdates.Put(update.field, fieldUpdates);
				bytesUsed.AddAndGet(BYTES_PER_BINARY_FIELD_ENTRY);
			}
			DocValuesUpdate.BinaryDocValuesUpdate current = fieldUpdates.Get(update.term);
			if (current != null && docIDUpto < current.docIDUpto)
			{
				// Only record the new number if it's greater than or equal to the current
				// one. This is important because if multiple threads are replacing the
				// same doc at nearly the same time, it's possible that one thread that
				// got a higher docID is scheduled before the other threads.
				return;
			}
			update.docIDUpto = docIDUpto;
			// since it's a LinkedHashMap, we must first remove the Term entry so that
			// it's added last (we're interested in insertion-order).
			if (current != null)
			{
				Sharpen.Collections.Remove(fieldUpdates, update.term);
			}
			fieldUpdates.Put(update.term, update);
			numBinaryUpdates.IncrementAndGet();
			if (current == null)
			{
				bytesUsed.AddAndGet(BYTES_PER_BINARY_UPDATE_ENTRY + update.SizeInBytes());
			}
		}

		internal virtual void Clear()
		{
			terms.Clear();
			queries.Clear();
			docIDs.Clear();
			numericUpdates.Clear();
			binaryUpdates.Clear();
			numTermDeletes.Set(0);
			numNumericUpdates.Set(0);
			numBinaryUpdates.Set(0);
			bytesUsed.Set(0);
		}

		internal virtual bool Any()
		{
			return terms.Count > 0 || docIDs.Count > 0 || queries.Count > 0 || numericUpdates
				.Count > 0 || binaryUpdates.Count > 0;
		}
	}
}
