/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Query;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Spatial
{
	/// <summary>
	/// A Spatial Filter implementing
	/// <see cref="Lucene.Net.Spatial.Query.SpatialOperation.IsDisjointTo">Lucene.Net.Spatial.Query.SpatialOperation.IsDisjointTo
	/// 	</see>
	/// in terms
	/// of a
	/// <see cref="SpatialStrategy">SpatialStrategy</see>
	/// 's support for
	/// <see cref="Lucene.Net.Spatial.Query.SpatialOperation.Intersects">Lucene.Net.Spatial.Query.SpatialOperation.Intersects
	/// 	</see>
	/// .
	/// A document is considered disjoint if it has spatial data that does not
	/// intersect with the query shape.  Another way of looking at this is that it's
	/// a way to invert a query shape.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class DisjointSpatialFilter : Filter
	{
		private readonly string field;

		private readonly Filter intersectsFilter;

		/// <param name="strategy">Needed to compute intersects</param>
		/// <param name="args">Used in spatial intersection</param>
		/// <param name="field">
		/// This field is used to determine which docs have spatial data via
		/// <see cref="Lucene.Net.Search.FieldCache.GetDocsWithField(Lucene.Net.Index.AtomicReader, string)
		/// 	">Lucene.Net.Search.FieldCache.GetDocsWithField(Lucene.Net.Index.AtomicReader, string)
		/// 	</see>
		/// .
		/// Passing null will assume all docs have spatial data.
		/// </param>
		public DisjointSpatialFilter(SpatialStrategy strategy, SpatialArgs args, string field
			)
		{
			//maybe null
			this.field = field;
			// TODO consider making SpatialArgs cloneable
			SpatialOperation origOp = args.GetOperation();
			//copy so we can restore
			args.SetOperation(SpatialOperation.Intersects);
			//temporarily set to intersects
			intersectsFilter = strategy.MakeFilter(args);
			args.SetOperation(origOp);
		}

		//restore so it looks like it was
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o == null || GetType() != o.GetType())
			{
				return false;
			}
			Lucene.Net.Spatial.DisjointSpatialFilter that = (Lucene.Net.Spatial.DisjointSpatialFilter
				)o;
			if (field != null ? !field.Equals(that.field) : that.field != null)
			{
				return false;
			}
			if (!intersectsFilter.Equals(that.intersectsFilter))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = field != null ? field.GetHashCode() : 0;
			result = 31 * result + intersectsFilter.GetHashCode();
			return result;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
			)
		{
			Bits docsWithField;
			if (field == null)
			{
				docsWithField = null;
			}
			else
			{
				//all docs
				//NOTE By using the FieldCache we re-use a cache
				// which is nice but loading it in this way might be slower than say using an
				// intersects filter against the world bounds. So do we add a method to the
				// strategy, perhaps?  But the strategy can't cache it.
				docsWithField = FieldCache.DEFAULT.GetDocsWithField(((AtomicReader)context.Reader
					()), field);
				int maxDoc = ((AtomicReader)context.Reader()).MaxDoc();
				if (docsWithField.Length() != maxDoc)
				{
					throw new InvalidOperationException("Bits length should be maxDoc (" + maxDoc + ") but wasn't: "
						 + docsWithField);
				}
				if (docsWithField is Bits.MatchNoBits)
				{
					return null;
				}
				else
				{
					//match nothing
					if (docsWithField is Bits.MatchAllBits)
					{
						docsWithField = null;
					}
				}
			}
			//all docs
			//not so much a chain but a way to conveniently invert the Filter
			DocIdSet docIdSet = new ChainedFilter(new Filter[] { intersectsFilter }, ChainedFilter
				.ANDNOT).GetDocIdSet(context, acceptDocs);
			return BitsFilteredDocIdSet.Wrap(docIdSet, docsWithField);
		}
	}
}
