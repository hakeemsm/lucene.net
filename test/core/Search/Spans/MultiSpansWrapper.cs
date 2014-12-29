/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Sharpen;

namespace Lucene.Net.Search.Spans
{
	/// <summary>
	/// A wrapper to perform span operations on a non-leaf reader context
	/// <p>
	/// NOTE: This should be used for testing purposes only
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public class MultiSpansWrapper : Lucene.Net.Search.Spans.Spans
	{
		private SpanQuery query;

		private IList<AtomicReaderContext> leaves;

		private int leafOrd = 0;

		private Lucene.Net.Search.Spans.Spans current;

		private IDictionary<Term, TermContext> termContexts;

		private readonly int numLeaves;

		private MultiSpansWrapper(IList<AtomicReaderContext> leaves, SpanQuery query, IDictionary
			<Term, TermContext> termContexts)
		{
			// can't be package private due to payloads
			this.query = query;
			this.leaves = leaves;
			this.numLeaves = leaves.Count;
			this.termContexts = termContexts;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static Lucene.Net.Search.Spans.Spans Wrap(IndexReaderContext topLevelReaderContext
			, SpanQuery query)
		{
			IDictionary<Term, TermContext> termContexts = new Dictionary<Term, TermContext>();
			TreeSet<Term> terms = new TreeSet<Term>();
			query.ExtractTerms(terms);
			foreach (Term term in terms)
			{
				termContexts.Put(term, TermContext.Build(topLevelReaderContext, term));
			}
			IList<AtomicReaderContext> leaves = topLevelReaderContext.Leaves;
			if (leaves.Count == 1)
			{
				AtomicReaderContext ctx = leaves[0];
				return query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, termContexts
					);
			}
			return new Lucene.Net.Search.Spans.MultiSpansWrapper(leaves, query, termContexts
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool Next()
		{
			if (leafOrd >= numLeaves)
			{
				return false;
			}
			if (current == null)
			{
				AtomicReaderContext ctx = leaves[leafOrd];
				current = query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, termContexts
					);
			}
			while (true)
			{
				if (current.Next())
				{
					return true;
				}
				if (++leafOrd < numLeaves)
				{
					AtomicReaderContext ctx = leaves[leafOrd];
					current = query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, termContexts
						);
				}
				else
				{
					current = null;
					break;
				}
			}
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool SkipTo(int target)
		{
			if (leafOrd >= numLeaves)
			{
				return false;
			}
			int subIndex = ReaderUtil.SubIndex(target, leaves);
			//HM:revisit 
			//assert subIndex >= leafOrd;
			if (subIndex != leafOrd)
			{
				AtomicReaderContext ctx = leaves[subIndex];
				current = query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, termContexts
					);
				leafOrd = subIndex;
			}
			else
			{
				if (current == null)
				{
					AtomicReaderContext ctx = leaves[leafOrd];
					current = query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, termContexts
						);
				}
			}
			while (true)
			{
				if (target < leaves[leafOrd].docBase)
				{
					// target was in the previous slice
					if (current.Next())
					{
						return true;
					}
				}
				else
				{
					if (current.SkipTo(target - leaves[leafOrd].docBase))
					{
						return true;
					}
				}
				if (++leafOrd < numLeaves)
				{
					AtomicReaderContext ctx = leaves[leafOrd];
					current = query.GetSpans(ctx, ((AtomicReader)ctx.Reader).LiveDocs, termContexts
						);
				}
				else
				{
					current = null;
					break;
				}
			}
			return false;
		}

		public override int Doc()
		{
			if (current == null)
			{
				return DocIdSetIterator.NO_MORE_DOCS;
			}
			return current.Doc() + leaves[leafOrd].docBase;
		}

		public override int Start()
		{
			if (current == null)
			{
				return DocIdSetIterator.NO_MORE_DOCS;
			}
			return current.Start();
		}

		public override int End()
		{
			if (current == null)
			{
				return DocIdSetIterator.NO_MORE_DOCS;
			}
			return current.End();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override ICollection<byte[]> GetPayload()
		{
			if (current == null)
			{
				return Sharpen.Collections.EmptyList();
			}
			return current.Payload;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IsPayloadAvailable()
		{
			if (current == null)
			{
				return false;
			}
			return current.IsPayloadAvailable();
		}

		public override long Cost()
		{
			return int.MaxValue;
		}
		// just for tests
	}
}
