/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.Valuesource;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Queries.Function.Valuesource
{
	/// <summary>
	/// Function that returns
	/// <see cref="Lucene.Net.Search.Similarities.TFIDFSimilarity.Idf(long, long)"
	/// 	>Lucene.Net.Search.Similarities.TFIDFSimilarity.Idf(long, long)</see>
	/// for every document.
	/// <p>
	/// Note that the configured Similarity for the field must be
	/// a subclass of
	/// <see cref="Lucene.Net.Search.Similarities.TFIDFSimilarity">Lucene.Net.Search.Similarities.TFIDFSimilarity
	/// 	</see>
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public class IDFValueSource : DocFreqValueSource
	{
		public IDFValueSource(string field, string val, string indexedField, BytesRef indexedBytes
			) : base(field, val, indexedField, indexedBytes)
		{
		}

		public override string Name()
		{
			return "idf";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			IndexSearcher searcher = (IndexSearcher)context.Get("searcher");
			TFIDFSimilarity sim = AsTFIDF(searcher.GetSimilarity(), field);
			if (sim == null)
			{
				throw new NotSupportedException("requires a TFIDFSimilarity (such as DefaultSimilarity)"
					);
			}
			int docfreq = searcher.GetIndexReader().DocFreq(new Term(indexedField, indexedBytes
				));
			float idf = sim.Idf(docfreq, searcher.GetIndexReader().MaxDoc());
			return new ConstDoubleDocValues(idf, this);
		}

		// tries extra hard to cast the sim to TFIDFSimilarity
		internal static TFIDFSimilarity AsTFIDF(Similarity sim, string field)
		{
			while (sim is PerFieldSimilarityWrapper)
			{
				sim = ((PerFieldSimilarityWrapper)sim).Get(field);
			}
			if (sim is TFIDFSimilarity)
			{
				return (TFIDFSimilarity)sim;
			}
			else
			{
				return null;
			}
		}
	}
}
