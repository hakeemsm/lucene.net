/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Spatial.Prefix
{
	/// <summary>Base class for Lucene Filters on SpatialPrefixTree fields.</summary>
	/// <remarks>Base class for Lucene Filters on SpatialPrefixTree fields.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class AbstractPrefixTreeFilter : Filter
	{
		protected internal readonly Com.Spatial4j.Core.Shape.Shape queryShape;

		protected internal readonly string fieldName;

		protected internal readonly SpatialPrefixTree grid;

		protected internal readonly int detailLevel;

		public AbstractPrefixTreeFilter(Com.Spatial4j.Core.Shape.Shape queryShape, string
			 fieldName, SpatialPrefixTree grid, int detailLevel)
		{
			//not in equals/hashCode since it's implied for a specific field
			this.queryShape = queryShape;
			this.fieldName = fieldName;
			this.grid = grid;
			this.detailLevel = detailLevel;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!GetType().Equals(o.GetType()))
			{
				return false;
			}
			Lucene.Net.Spatial.Prefix.AbstractPrefixTreeFilter that = (Lucene.Net.Spatial.Prefix.AbstractPrefixTreeFilter
				)o;
			if (detailLevel != that.detailLevel)
			{
				return false;
			}
			if (!fieldName.Equals(that.fieldName))
			{
				return false;
			}
			if (!queryShape.Equals(that.queryShape))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = queryShape.GetHashCode();
			result = 31 * result + fieldName.GetHashCode();
			result = 31 * result + detailLevel;
			return result;
		}

		/// <summary>
		/// Holds transient state and docid collecting utility methods as part of
		/// traversing a
		/// <see cref="Lucene.Net.Index.TermsEnum">Lucene.Net.Index.TermsEnum</see>
		/// .
		/// </summary>
		public abstract class BaseTermsEnumTraverser
		{
			protected internal readonly AtomicReaderContext context;

			protected internal Bits acceptDocs;

			protected internal readonly int maxDoc;

			protected internal TermsEnum termsEnum;

			protected internal DocsEnum docsEnum;

			/// <exception cref="System.IO.IOException"></exception>
			public BaseTermsEnumTraverser(AbstractPrefixTreeFilter _enclosing, AtomicReaderContext
				 context, Bits acceptDocs)
			{
				this._enclosing = _enclosing;
				//remember to check for null in getDocIdSet
				this.context = context;
				AtomicReader reader = ((AtomicReader)context.Reader());
				this.acceptDocs = acceptDocs;
				this.maxDoc = reader.MaxDoc();
				Terms terms = reader.Terms(this._enclosing.fieldName);
				if (terms != null)
				{
					this.termsEnum = terms.Iterator(null);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal virtual void CollectDocs(FixedBitSet bitSet)
			{
				//WARN: keep this specialization in sync
				//HM:revisit
				//assert termsEnum != null;
				this.docsEnum = this.termsEnum.Docs(this.acceptDocs, this.docsEnum, DocsEnum.FLAG_NONE
					);
				int docid;
				while ((docid = this.docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
				{
					bitSet.Set(docid);
				}
			}

			private readonly AbstractPrefixTreeFilter _enclosing;
		}
	}
}
