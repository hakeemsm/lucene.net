/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Lucene.Net.Queries.Function;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.Valuesource;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queries.Function.Valuesource
{
	/// <summary>
	/// Returns the value of
	/// <see cref="Lucene.Net.Index.IndexReader.MaxDoc()">Lucene.Net.Index.IndexReader.MaxDoc()
	/// 	</see>
	/// for every document. This is the number of documents
	/// including deletions.
	/// </summary>
	public class MaxDocValueSource : ValueSource
	{
		// javadocs
		public virtual string Name()
		{
			return "maxdoc";
		}

		public override string Description()
		{
			return Name() + "()";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			context.Put("searcher", searcher);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			IndexSearcher searcher = (IndexSearcher)context.Get("searcher");
			return new ConstIntDocValues(searcher.GetIndexReader().MaxDoc(), this);
		}

		public override bool Equals(object o)
		{
			return this.GetType() == o.GetType();
		}

		public override int GetHashCode()
		{
			return this.GetType().GetHashCode();
		}
	}
}
