/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>tests BooleanScorer2's minShouldMatch</summary>
	public class TestMinShouldMatch2 : LuceneTestCase
	{
		internal static Directory dir;

		internal static DirectoryReader r;

		internal static AtomicReader reader;

		internal static IndexSearcher searcher;

		internal static readonly string alwaysTerms = new string[] { "a" };

		internal static readonly string commonTerms = new string[] { "b", "c", "d" };

		internal static readonly string mediumTerms = new string[] { "e", "f", "g" };

		internal static readonly string rareTerms = new string[] { "h", "i", "j", "k", "l"
			, "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			int numDocs = AtLeast(300);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				AddSome(doc, alwaysTerms);
				if (Random().Next(100) < 90)
				{
					AddSome(doc, commonTerms);
				}
				if (Random().Next(100) < 50)
				{
					AddSome(doc, mediumTerms);
				}
				if (Random().Next(100) < 10)
				{
					AddSome(doc, rareTerms);
				}
				iw.AddDocument(doc);
			}
			iw.ForceMerge(1);
			iw.Dispose();
			r = DirectoryReader.Open(dir);
			reader = GetOnlySegmentReader(r);
			searcher = new IndexSearcher(reader);
			searcher.SetSimilarity(new _DefaultSimilarity_89());
		}

		private sealed class _DefaultSimilarity_89 : DefaultSimilarity
		{
			public _DefaultSimilarity_89()
			{
			}

			public override float QueryNorm(float sumOfSquaredWeights)
			{
				return 1;
			}
		}

		// we disable queryNorm, both for debugging and ease of impl
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			reader.Dispose();
			dir.Dispose();
			searcher = null;
			reader = null;
			r = null;
			dir = null;
		}

		private static void AddSome(Lucene.Net.Documents.Document doc, string[] values
			)
		{
			IList<string> list = Arrays.AsList(values);
			Sharpen.Collections.Shuffle(list, Random());
			int howMany = TestUtil.NextInt(Random(), 1, list.Count);
			for (int i = 0; i < howMany; i++)
			{
				doc.Add(new StringField("field", list[i], Field.Store.NO));
				doc.Add(new SortedSetDocValuesField("dv", new BytesRef(list[i])));
			}
		}

		/// <exception cref="System.Exception"></exception>
		private Lucene.Net.Search.Scorer Scorer(string[] values, int minShouldMatch
			, bool slow)
		{
			BooleanQuery bq = new BooleanQuery();
			foreach (string value in values)
			{
				bq.Add(new TermQuery(new Term("field", value)), BooleanClause.Occur.SHOULD);
			}
			bq.SetMinimumNumberShouldMatch(minShouldMatch);
			BooleanQuery.BooleanWeight weight = (BooleanQuery.BooleanWeight)searcher.CreateNormalizedWeight
				(bq);
			if (slow)
			{
				return new TestMinShouldMatch2.SlowMinShouldMatchScorer(weight, reader, searcher);
			}
			else
			{
				return weight.Scorer(((AtomicReaderContext)reader.GetContext()), null);
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertNext(Lucene.Net.Search.Scorer expected, Lucene.Net.Search.Scorer
			 actual)
		{
			if (actual == null)
			{
				AreEqual(DocIdSetIterator.NO_MORE_DOCS, expected.NextDoc()
					);
				return;
			}
			int doc;
			while ((doc = expected.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			{
				AreEqual(doc, actual.NextDoc());
				AreEqual(expected.Freq, actual.Freq);
				float expectedScore = expected.Score();
				float actualScore = actual.Score();
				AreEqual(expectedScore, actualScore, CheckHits.ExplainToleranceDelta
					(expectedScore, actualScore));
			}
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, actual.NextDoc());
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertAdvance(Lucene.Net.Search.Scorer expected, Lucene.Net.Search.Scorer
			 actual, int amount)
		{
			if (actual == null)
			{
				AreEqual(DocIdSetIterator.NO_MORE_DOCS, expected.NextDoc()
					);
				return;
			}
			int prevDoc = 0;
			int doc;
			while ((doc = expected.Advance(prevDoc + amount)) != DocIdSetIterator.NO_MORE_DOCS
				)
			{
				AreEqual(doc, actual.Advance(prevDoc + amount));
				AreEqual(expected.Freq, actual.Freq);
				float expectedScore = expected.Score();
				float actualScore = actual.Score();
				AreEqual(expectedScore, actualScore, CheckHits.ExplainToleranceDelta
					(expectedScore, actualScore));
				prevDoc = doc;
			}
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, actual.Advance(prevDoc
				 + amount));
		}

		/// <summary>simple test for next(): minShouldMatch=2 on 3 terms (one common, one medium, one rare)
		/// 	</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNextCMR2()
		{
			for (int common = 0; common < commonTerms.Length; common++)
			{
				for (int medium = 0; medium < mediumTerms.Length; medium++)
				{
					for (int rare = 0; rare < rareTerms.Length; rare++)
					{
						Lucene.Net.Search.Scorer expected = Scorer(new string[] { commonTerms[common
							], mediumTerms[medium], rareTerms[rare] }, 2, true);
						Lucene.Net.Search.Scorer actual = Scorer(new string[] { commonTerms[common
							], mediumTerms[medium], rareTerms[rare] }, 2, false);
						AssertNext(expected, actual);
					}
				}
			}
		}

		/// <summary>simple test for advance(): minShouldMatch=2 on 3 terms (one common, one medium, one rare)
		/// 	</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestAdvanceCMR2()
		{
			for (int amount = 25; amount < 200; amount += 25)
			{
				for (int common = 0; common < commonTerms.Length; common++)
				{
					for (int medium = 0; medium < mediumTerms.Length; medium++)
					{
						for (int rare = 0; rare < rareTerms.Length; rare++)
						{
							Lucene.Net.Search.Scorer expected = Scorer(new string[] { commonTerms[common
								], mediumTerms[medium], rareTerms[rare] }, 2, true);
							Lucene.Net.Search.Scorer actual = Scorer(new string[] { commonTerms[common
								], mediumTerms[medium], rareTerms[rare] }, 2, false);
							AssertAdvance(expected, actual, amount);
						}
					}
				}
			}
		}

		/// <summary>test next with giant bq of all terms with varying minShouldMatch</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNextAllTerms()
		{
			IList<string> termsList = new AList<string>();
			Sharpen.Collections.AddAll(termsList, Arrays.AsList(commonTerms));
			Sharpen.Collections.AddAll(termsList, Arrays.AsList(mediumTerms));
			Sharpen.Collections.AddAll(termsList, Arrays.AsList(rareTerms));
			string[] terms = Sharpen.Collections.ToArray(termsList, new string[0]);
			for (int minNrShouldMatch = 1; minNrShouldMatch <= terms.Length; minNrShouldMatch
				++)
			{
				Lucene.Net.Search.Scorer expected = Scorer(terms, minNrShouldMatch, true);
				Lucene.Net.Search.Scorer actual = Scorer(terms, minNrShouldMatch, false);
				AssertNext(expected, actual);
			}
		}

		/// <summary>test advance with giant bq of all terms with varying minShouldMatch</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestAdvanceAllTerms()
		{
			IList<string> termsList = new AList<string>();
			Sharpen.Collections.AddAll(termsList, Arrays.AsList(commonTerms));
			Sharpen.Collections.AddAll(termsList, Arrays.AsList(mediumTerms));
			Sharpen.Collections.AddAll(termsList, Arrays.AsList(rareTerms));
			string[] terms = Sharpen.Collections.ToArray(termsList, new string[0]);
			for (int amount = 25; amount < 200; amount += 25)
			{
				for (int minNrShouldMatch = 1; minNrShouldMatch <= terms.Length; minNrShouldMatch
					++)
				{
					Lucene.Net.Search.Scorer expected = Scorer(terms, minNrShouldMatch, true);
					Lucene.Net.Search.Scorer actual = Scorer(terms, minNrShouldMatch, false);
					AssertAdvance(expected, actual, amount);
				}
			}
		}

		/// <summary>test next with varying numbers of terms with varying minShouldMatch</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNextVaryingNumberOfTerms()
		{
			IList<string> termsList = new AList<string>();
			Sharpen.Collections.AddAll(termsList, Arrays.AsList(commonTerms));
			Sharpen.Collections.AddAll(termsList, Arrays.AsList(mediumTerms));
			Sharpen.Collections.AddAll(termsList, Arrays.AsList(rareTerms));
			Sharpen.Collections.Shuffle(termsList, Random());
			for (int numTerms = 2; numTerms <= termsList.Count; numTerms++)
			{
				string[] terms = Sharpen.Collections.ToArray(termsList.SubList(0, numTerms), new 
					string[0]);
				for (int minNrShouldMatch = 1; minNrShouldMatch <= terms.Length; minNrShouldMatch
					++)
				{
					Lucene.Net.Search.Scorer expected = Scorer(terms, minNrShouldMatch, true);
					Lucene.Net.Search.Scorer actual = Scorer(terms, minNrShouldMatch, false);
					AssertNext(expected, actual);
				}
			}
		}

		/// <summary>test advance with varying numbers of terms with varying minShouldMatch</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestAdvanceVaryingNumberOfTerms()
		{
			IList<string> termsList = new AList<string>();
			Sharpen.Collections.AddAll(termsList, Arrays.AsList(commonTerms));
			Sharpen.Collections.AddAll(termsList, Arrays.AsList(mediumTerms));
			Sharpen.Collections.AddAll(termsList, Arrays.AsList(rareTerms));
			Sharpen.Collections.Shuffle(termsList, Random());
			for (int amount = 25; amount < 200; amount += 25)
			{
				for (int numTerms = 2; numTerms <= termsList.Count; numTerms++)
				{
					string[] terms = Sharpen.Collections.ToArray(termsList.SubList(0, numTerms), new 
						string[0]);
					for (int minNrShouldMatch = 1; minNrShouldMatch <= terms.Length; minNrShouldMatch
						++)
					{
						Lucene.Net.Search.Scorer expected = Scorer(terms, minNrShouldMatch, true);
						Lucene.Net.Search.Scorer actual = Scorer(terms, minNrShouldMatch, false);
						AssertAdvance(expected, actual, amount);
					}
				}
			}
		}

		internal class SlowMinShouldMatchScorer : Scorer
		{
			internal int currentDoc = -1;

			internal int currentMatched = -1;

			internal readonly SortedSetDocValues dv;

			internal readonly int maxDoc;

			internal readonly ICollection<long> ords = new HashSet<long>();

			internal readonly Similarity.SimScorer[] sims;

			internal readonly int minNrShouldMatch;

			internal double score = float.NaN;

			/// <exception cref="System.IO.IOException"></exception>
			internal SlowMinShouldMatchScorer(BooleanQuery.BooleanWeight weight, AtomicReader
				 reader, IndexSearcher searcher) : base(weight)
			{
				// TODO: more tests
				// a slow min-should match scorer that uses a docvalues field.
				// later, we can make debugging easier as it can record the set of ords it currently matched
				// and e.g. print out their values and so on for the document
				// current docid
				// current number of terms matched
				this.dv = reader.GetSortedSetDocValues("dv");
				this.maxDoc = reader.MaxDoc;
				BooleanQuery bq = (BooleanQuery)weight.GetQuery();
				this.minNrShouldMatch = bq.GetMinimumNumberShouldMatch();
				this.sims = new Similarity.SimScorer[(int)dv.GetValueCount()];
				foreach (BooleanClause clause in bq.GetClauses())
				{
					//HM:revisit 
					//assert !clause.isProhibited();
					//HM:revisit 
					//assert !clause.isRequired();
					Term term = ((TermQuery)clause.GetQuery()).GetTerm();
					long ord = dv.LookupTerm(term.Bytes());
					if (ord >= 0)
					{
						bool success = ords.AddItem(ord);
						//HM:revisit 
						//assert success; // no dups
						TermContext context = TermContext.Build(((AtomicReaderContext)reader.GetContext()
							), term);
						Similarity.SimWeight w = weight.similarity.ComputeWeight(1f, searcher.CollectionStatistics
							("field"), searcher.TermStatistics(term, context));
						w.GetValueForNormalization();
						// ignored
						w.Normalize(1F, 1F);
						sims[(int)ord] = weight.similarity.SimScorer(w, ((AtomicReaderContext)reader.GetContext
							()));
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override float Score()
			{
				//HM:revisit 
				//assert score != 0 : currentMatched;
				return (float)score * ((BooleanQuery.BooleanWeight)weight).Coord(currentMatched, 
					((BooleanQuery.BooleanWeight)weight).maxCoord);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq
			{
				return currentMatched;
			}

			public override int DocID
			{
				return currentDoc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				//HM:revisit 
				//assert currentDoc != NO_MORE_DOCS;
				for (currentDoc = currentDoc + 1; currentDoc < maxDoc; currentDoc++)
				{
					currentMatched = 0;
					score = 0;
					dv.SetDocument(currentDoc);
					long ord;
					while ((ord = dv.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
					{
						if (ords.Contains(ord))
						{
							currentMatched++;
							score += sims[(int)ord].Score(currentDoc, 1);
						}
					}
					if (currentMatched >= minNrShouldMatch)
					{
						return currentDoc;
					}
				}
				return currentDoc = NO_MORE_DOCS;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				int doc;
				while ((doc = NextDoc()) < target)
				{
				}
				return doc;
			}

			public override long Cost()
			{
				return maxDoc;
			}
		}
	}
}
