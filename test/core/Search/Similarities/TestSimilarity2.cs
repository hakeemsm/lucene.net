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
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Search.Similarities
{
	/// <summary>Tests against all the similarities we have</summary>
	public class TestSimilarity2 : LuceneTestCase
	{
		internal IList<Similarity> sims;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			sims = new List<Similarity>();
			sims.Add(new DefaultSimilarity());
			sims.Add(new BM25Similarity());
			// TODO: not great that we dup this all with TestSimilarityBase
			foreach (BasicModel basicModel in TestSimilarityBase.BASIC_MODELS)
			{
				foreach (AfterEffect afterEffect in TestSimilarityBase.AFTER_EFFECTS)
				{
					foreach (Normalization normalization in TestSimilarityBase.NORMALIZATIONS)
					{
						sims.Add(new DFRSimilarity(basicModel, afterEffect, normalization));
					}
				}
			}
			foreach (Distribution distribution in TestSimilarityBase.DISTRIBUTIONS)
			{
				foreach (Lambda lambda in TestSimilarityBase.LAMBDAS)
				{
					foreach (Normalization normalization in TestSimilarityBase.NORMALIZATIONS)
					{
						sims.Add(new IBSimilarity(distribution, lambda, normalization));
					}
				}
			}
			sims.Add(new LMDirichletSimilarity());
			sims.Add(new LMJelinekMercerSimilarity(0.1f));
			sims.Add(new LMJelinekMercerSimilarity(0.7f));
		}

		/// <summary>
		/// because of stupid things like querynorm, its possible we computeStats on a field that doesnt exist at all
		/// test this against a totally empty index, to make sure sims handle it
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmptyIndex()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher @is = NewSearcher(ir);
			foreach (Similarity sim in sims)
			{
				@is.SetSimilarity(sim);
				AreEqual(0, @is.Search(new TermQuery(new Term("foo", "bar"
					)), 10).TotalHits);
			}
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>similar to the above, but ORs the query with a real field</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmptyField()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("foo", "bar", Field.Store.NO));
			iw.AddDocument(doc);
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher @is = NewSearcher(ir);
			foreach (Similarity sim in sims)
			{
				@is.SetSimilarity(sim);
				BooleanQuery query = new BooleanQuery(true);
				query.Add(new TermQuery(new Term("foo", "bar")), BooleanClause.Occur.SHOULD);
				query.Add(new TermQuery(new Term("bar", "baz")), BooleanClause.Occur.SHOULD);
				AreEqual(1, @is.Search(query, 10).TotalHits);
			}
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>similar to the above, however the field exists, but we query with a term that doesnt exist too
		/// 	</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmptyTerm()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("foo", "bar", Field.Store.NO));
			iw.AddDocument(doc);
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher @is = NewSearcher(ir);
			foreach (Similarity sim in sims)
			{
				@is.SetSimilarity(sim);
				BooleanQuery query = new BooleanQuery(true);
				query.Add(new TermQuery(new Term("foo", "bar")), BooleanClause.Occur.SHOULD);
				query.Add(new TermQuery(new Term("foo", "baz")), BooleanClause.Occur.SHOULD);
				AreEqual(1, @is.Search(query, 10).TotalHits);
			}
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>make sure we can retrieve when norms are disabled</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoNorms()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.OmitsNorms = (true);
			ft.Freeze();
			doc.Add(NewField("foo", "bar", ft));
			iw.AddDocument(doc);
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher @is = NewSearcher(ir);
			foreach (Similarity sim in sims)
			{
				@is.SetSimilarity(sim);
				BooleanQuery query = new BooleanQuery(true);
				query.Add(new TermQuery(new Term("foo", "bar")), BooleanClause.Occur.SHOULD);
				AreEqual(1, @is.Search(query, 10).TotalHits);
			}
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>make sure all sims work if TF is omitted</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestOmitTF()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.IndexOptions = (FieldInfo.IndexOptions.DOCS_ONLY);
			ft.Freeze();
			Field f = NewField("foo", "bar", ft);
			doc.Add(f);
			iw.AddDocument(doc);
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher @is = NewSearcher(ir);
			foreach (Similarity sim in sims)
			{
				@is.SetSimilarity(sim);
				BooleanQuery query = new BooleanQuery(true);
				query.Add(new TermQuery(new Term("foo", "bar")), BooleanClause.Occur.SHOULD);
				AreEqual(1, @is.Search(query, 10).TotalHits);
			}
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>make sure all sims work if TF and norms is omitted</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestOmitTFAndNorms()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.IndexOptions = (FieldInfo.IndexOptions.DOCS_ONLY);
			ft.OmitsNorms = (true);
			ft.Freeze();
			Field f = NewField("foo", "bar", ft);
			doc.Add(f);
			iw.AddDocument(doc);
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher @is = NewSearcher(ir);
			foreach (Similarity sim in sims)
			{
				@is.SetSimilarity(sim);
				BooleanQuery query = new BooleanQuery(true);
				query.Add(new TermQuery(new Term("foo", "bar")), BooleanClause.Occur.SHOULD);
				AreEqual(1, @is.Search(query, 10).TotalHits);
			}
			ir.Dispose();
			dir.Dispose();
		}

		/// <summary>make sure all sims work with spanOR(termX, termY) where termY does not exist
		/// 	</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCrazySpans()
		{
			// The problem: "normal" lucene queries create scorers, returning null if terms dont exist
			// This means they never score a term that does not exist.
			// however with spans, there is only one scorer for the whole hierarchy:
			// inner queries are not real queries, their boosts are ignored, etc.
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			doc.Add(NewField("foo", "bar", ft));
			iw.AddDocument(doc);
			IndexReader ir = iw.Reader;
			iw.Dispose();
			IndexSearcher @is = NewSearcher(ir);
			foreach (Similarity sim in sims)
			{
				@is.SetSimilarity(sim);
				SpanTermQuery s1 = new SpanTermQuery(new Term("foo", "bar"));
				SpanTermQuery s2 = new SpanTermQuery(new Term("foo", "baz"));
				Query query = new SpanOrQuery(s1, s2);
				TopDocs td = @is.Search(query, 10);
				AreEqual(1, td.TotalHits);
				float score = td.ScoreDocs[0].score;
				IsTrue(score >= 0.0f);
				IsFalse("inf score for " + sim, float.IsInfinite(score));
			}
			ir.Dispose();
			dir.Dispose();
		}
	}
}
