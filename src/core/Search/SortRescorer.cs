/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>
	/// A
	/// <see cref="Rescorer">Rescorer</see>
	/// that re-sorts according to a provided
	/// Sort.
	/// </summary>
	public class SortRescorer : Rescorer
	{
		private readonly Sort sort;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public SortRescorer(Sort sort)
		{
			this.sort = sort;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override TopDocs Rescore(IndexSearcher searcher, TopDocs firstPassTopDocs, 
			int topN)
		{
			// Copy ScoreDoc[] and sort by ascending docID:
			ScoreDoc[] hits = firstPassTopDocs.scoreDocs.Clone();
			Arrays.Sort(hits, new _IComparer_47());
			IList<AtomicReaderContext> leaves = searcher.GetIndexReader().Leaves();
			TopFieldCollector collector = TopFieldCollector.Create(sort, topN, true, true, true
				, false);
			// Now merge sort docIDs from hits, with reader's leaves:
			int hitUpto = 0;
			int readerUpto = -1;
			int endDoc = 0;
			int docBase = 0;
			FakeScorer fakeScorer = new FakeScorer();
			while (hitUpto < hits.Length)
			{
				ScoreDoc hit = hits[hitUpto];
				int docID = hit.doc;
				AtomicReaderContext readerContext = null;
				while (docID >= endDoc)
				{
					readerUpto++;
					readerContext = leaves[readerUpto];
					endDoc = readerContext.docBase + ((AtomicReader)readerContext.Reader()).MaxDoc();
				}
				if (readerContext != null)
				{
					// We advanced to another segment:
					collector.SetNextReader(readerContext);
					collector.SetScorer(fakeScorer);
					docBase = readerContext.docBase;
				}
				fakeScorer.score = hit.score;
				fakeScorer.doc = docID - docBase;
				collector.Collect(fakeScorer.doc);
				hitUpto++;
			}
			return collector.TopDocs();
		}

		private sealed class _IComparer_47 : IComparer<ScoreDoc>
		{
			public _IComparer_47()
			{
			}

			public int Compare(ScoreDoc a, ScoreDoc b)
			{
				return a.doc - b.doc;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Explanation Explain(IndexSearcher searcher, Explanation firstPassExplanation
			, int docID)
		{
			TopDocs oneHit = new TopDocs(1, new ScoreDoc[] { new ScoreDoc(docID, firstPassExplanation
				.GetValue()) });
			TopDocs hits = Rescore(searcher, oneHit, 1);
			//HM:revisit 
			//assert hits.totalHits == 1;
			// TODO: if we could ask the Sort to explain itself then
			// we wouldn't need the separate ExpressionRescorer...
			Explanation result = new Explanation(0.0f, "sort field values for sort=" + sort.ToString
				());
			// Add first pass:
			Explanation first = new Explanation(firstPassExplanation.GetValue(), "first pass score"
				);
			first.AddDetail(firstPassExplanation);
			result.AddDetail(first);
			FieldDoc fieldDoc = (FieldDoc)hits.scoreDocs[0];
			// Add sort values:
			SortField[] sortFields = sort.GetSort();
			for (int i = 0; i < sortFields.Length; i++)
			{
				result.AddDetail(new Explanation(0.0f, "sort field " + sortFields[i].ToString() +
					 " value=" + fieldDoc.fields[i]));
			}
			return result;
		}
	}
}
