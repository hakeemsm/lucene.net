/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Classification
{
	/// <summary>A simplistic Lucene based NaiveBayes classifier, see <code>http://en.wikipedia.org/wiki/Naive_Bayes_classifier</code>
	/// 	</summary>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleNaiveBayesClassifier : Classifier<BytesRef>
	{
		private AtomicReader atomicReader;

		private string[] textFieldNames;

		private string classFieldName;

		private int docsWithClassSize;

		private Analyzer analyzer;

		private IndexSearcher indexSearcher;

		private Query query;

		/// <summary>Creates a new NaiveBayes classifier.</summary>
		/// <remarks>
		/// Creates a new NaiveBayes classifier.
		/// Note that you must call
		/// <see cref="Train(Lucene.Net.Index.AtomicReader, string, string, Lucene.Net.Analysis.Analyzer)
		/// 	">train()</see>
		/// before you can
		/// classify any documents.
		/// </remarks>
		public SimpleNaiveBayesClassifier()
		{
		}

		/// <summary><inheritDoc></inheritDoc></summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Train(AtomicReader atomicReader, string textFieldName, string
			 classFieldName, Analyzer analyzer)
		{
			Train(atomicReader, textFieldName, classFieldName, analyzer, null);
		}

		/// <summary><inheritDoc></inheritDoc></summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Train(AtomicReader atomicReader, string textFieldName, string
			 classFieldName, Analyzer analyzer, Query query)
		{
			Train(atomicReader, new string[] { textFieldName }, classFieldName, analyzer, query
				);
		}

		/// <summary><inheritDoc></inheritDoc></summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Train(AtomicReader atomicReader, string[] textFieldNames, string
			 classFieldName, Analyzer analyzer, Query query)
		{
			this.atomicReader = atomicReader;
			this.indexSearcher = new IndexSearcher(this.atomicReader);
			this.textFieldNames = textFieldNames;
			this.classFieldName = classFieldName;
			this.analyzer = analyzer;
			this.query = query;
			this.docsWithClassSize = CountDocsWithClass();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int CountDocsWithClass()
		{
			int docCount = MultiFields.GetTerms(this.atomicReader, this.classFieldName).GetDocCount
				();
			if (docCount == -1)
			{
				// in case codec doesn't support getDocCount
				TotalHitCountCollector totalHitCountCollector = new TotalHitCountCollector();
				BooleanQuery q = new BooleanQuery();
				q.Add(new BooleanClause(new WildcardQuery(new Term(classFieldName, WildcardQuery.
					WILDCARD_STRING.ToString())), BooleanClause.Occur.MUST));
				if (query != null)
				{
					q.Add(query, BooleanClause.Occur.MUST);
				}
				indexSearcher.Search(q, totalHitCountCollector);
				docCount = totalHitCountCollector.GetTotalHits();
			}
			return docCount;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private string[] TokenizeDoc(string doc)
		{
			ICollection<string> result = new List<string>();
			foreach (string textFieldName in textFieldNames)
			{
				TokenStream tokenStream = analyzer.TokenStream(textFieldName, doc);
				try
				{
					CharTermAttribute charTermAttribute = tokenStream.AddAttribute<CharTermAttribute>
						();
					tokenStream.Reset();
					while (tokenStream.IncrementToken())
					{
						result.AddItem(charTermAttribute.ToString());
					}
					tokenStream.End();
				}
				finally
				{
					IOUtils.CloseWhileHandlingException(tokenStream);
				}
			}
			return Sharpen.Collections.ToArray(result, new string[result.Count]);
		}

		/// <summary><inheritDoc></inheritDoc></summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual ClassificationResult<BytesRef> AssignClass(string inputDocument)
		{
			if (atomicReader == null)
			{
				throw new IOException("You must first call Classifier#train");
			}
			double max = -double.MaxValue;
			BytesRef foundClass = new BytesRef();
			Terms terms = MultiFields.GetTerms(atomicReader, classFieldName);
			TermsEnum termsEnum = terms.Iterator(null);
			BytesRef next;
			string[] tokenizedDoc = TokenizeDoc(inputDocument);
			while ((next = termsEnum.Next()) != null)
			{
				double clVal = CalculateLogPrior(next) + CalculateLogLikelihood(tokenizedDoc, next
					);
				if (clVal > max)
				{
					max = clVal;
					foundClass = BytesRef.DeepCopyOf(next);
				}
			}
			double score = 10 / Math.Abs(max);
			return new ClassificationResult<BytesRef>(foundClass, score);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private double CalculateLogLikelihood(string[] tokenizedDoc, BytesRef c)
		{
			// for each word
			double result = 0d;
			foreach (string word in tokenizedDoc)
			{
				// search with text:word AND class:c
				int hits = GetWordFreqForClass(word, c);
				// num : count the no of times the word appears in documents of class c (+1)
				double num = hits + 1;
				// +1 is added because of add 1 smoothing
				// den : for the whole dictionary, count the no of times a word appears in documents of class c (+|V|)
				double den = GetTextTermFreqForClass(c) + docsWithClassSize;
				// P(w|c) = num/den
				double wordProbability = num / den;
				result += Math.Log(wordProbability);
			}
			// log(P(d|c)) = log(P(w1|c))+...+log(P(wn|c))
			return result;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private double GetTextTermFreqForClass(BytesRef c)
		{
			double avgNumberOfUniqueTerms = 0;
			foreach (string textFieldName in textFieldNames)
			{
				Terms terms = MultiFields.GetTerms(atomicReader, textFieldName);
				long numPostings = terms.GetSumDocFreq();
				// number of term/doc pairs
				avgNumberOfUniqueTerms += numPostings / (double)terms.GetDocCount();
			}
			// avg # of unique terms per doc
			int docsWithC = atomicReader.DocFreq(new Term(classFieldName, c));
			return avgNumberOfUniqueTerms * docsWithC;
		}

		// avg # of unique terms in text fields per doc * # docs with c
		/// <exception cref="System.IO.IOException"></exception>
		private int GetWordFreqForClass(string word, BytesRef c)
		{
			BooleanQuery booleanQuery = new BooleanQuery();
			BooleanQuery subQuery = new BooleanQuery();
			foreach (string textFieldName in textFieldNames)
			{
				subQuery.Add(new BooleanClause(new TermQuery(new Term(textFieldName, word)), BooleanClause.Occur
					.SHOULD));
			}
			booleanQuery.Add(new BooleanClause(subQuery, BooleanClause.Occur.MUST));
			booleanQuery.Add(new BooleanClause(new TermQuery(new Term(classFieldName, c)), BooleanClause.Occur
				.MUST));
			if (query != null)
			{
				booleanQuery.Add(query, BooleanClause.Occur.MUST);
			}
			TotalHitCountCollector totalHitCountCollector = new TotalHitCountCollector();
			indexSearcher.Search(booleanQuery, totalHitCountCollector);
			return totalHitCountCollector.GetTotalHits();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private double CalculateLogPrior(BytesRef currentClass)
		{
			return Math.Log((double)DocCount(currentClass)) - Math.Log(docsWithClassSize);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int DocCount(BytesRef countedClass)
		{
			return atomicReader.DocFreq(new Term(classFieldName, countedClass));
		}
	}
}
