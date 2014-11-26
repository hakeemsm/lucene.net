/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Queries.Mlt;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Classification
{
	/// <summary>
	/// A k-Nearest Neighbor classifier (see <code>http://en.wikipedia.org/wiki/K-nearest_neighbors</code>) based
	/// on
	/// <see cref="Lucene.Net.Queries.Mlt.MoreLikeThis">Lucene.Net.Queries.Mlt.MoreLikeThis
	/// 	</see>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class KNearestNeighborClassifier : Classifier<BytesRef>
	{
		private MoreLikeThis mlt;

		private string[] textFieldNames;

		private string classFieldName;

		private IndexSearcher indexSearcher;

		private readonly int k;

		private Query query;

		private int minDocsFreq;

		private int minTermFreq;

		/// <summary>
		/// Create a
		/// <see cref="Classifier{T}">Classifier&lt;T&gt;</see>
		/// using kNN algorithm
		/// </summary>
		/// <param name="k">the number of neighbors to analyze as an <code>int</code></param>
		public KNearestNeighborClassifier(int k)
		{
			this.k = k;
		}

		/// <summary>
		/// Create a
		/// <see cref="Classifier{T}">Classifier&lt;T&gt;</see>
		/// using kNN algorithm
		/// </summary>
		/// <param name="k">the number of neighbors to analyze as an <code>int</code></param>
		/// <param name="minDocsFreq">
		/// the minimum number of docs frequency for MLT to be set with
		/// <see cref="Lucene.Net.Queries.Mlt.MoreLikeThis.SetMinDocFreq(int)">Lucene.Net.Queries.Mlt.MoreLikeThis.SetMinDocFreq(int)
		/// 	</see>
		/// </param>
		/// <param name="minTermFreq">
		/// the minimum number of term frequency for MLT to be set with
		/// <see cref="Lucene.Net.Queries.Mlt.MoreLikeThis.SetMinTermFreq(int)">Lucene.Net.Queries.Mlt.MoreLikeThis.SetMinTermFreq(int)
		/// 	</see>
		/// </param>
		public KNearestNeighborClassifier(int k, int minDocsFreq, int minTermFreq)
		{
			this.k = k;
			this.minDocsFreq = minDocsFreq;
			this.minTermFreq = minTermFreq;
		}

		/// <summary><inheritDoc></inheritDoc></summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual ClassificationResult<BytesRef> AssignClass(string text)
		{
			if (mlt == null)
			{
				throw new IOException("You must first call Classifier#train");
			}
			BooleanQuery mltQuery = new BooleanQuery();
			foreach (string textFieldName in textFieldNames)
			{
				mltQuery.Add(new BooleanClause(mlt.Like(new StringReader(text), textFieldName), Occur
					.SHOULD));
			}
			Query classFieldQuery = new WildcardQuery(new Term(classFieldName, "*"));
			mltQuery.Add(new BooleanClause(classFieldQuery, BooleanClause.Occur.MUST));
			if (query != null)
			{
				mltQuery.Add(query, BooleanClause.Occur.MUST);
			}
			TopDocs topDocs = indexSearcher.Search(mltQuery, k);
			return SelectClassFromNeighbors(topDocs);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ClassificationResult<BytesRef> SelectClassFromNeighbors(TopDocs topDocs)
		{
			// TODO : improve the nearest neighbor selection
			IDictionary<BytesRef, int> classCounts = new Dictionary<BytesRef, int>();
			foreach (ScoreDoc scoreDoc in topDocs.scoreDocs)
			{
				BytesRef cl = new BytesRef(indexSearcher.Doc(scoreDoc.doc).GetField(classFieldName
					).StringValue());
				int count = classCounts.Get(cl);
				if (count != null)
				{
					classCounts.Put(cl, count + 1);
				}
				else
				{
					classCounts.Put(cl, 1);
				}
			}
			double max = 0;
			BytesRef assignedClass = new BytesRef();
			foreach (KeyValuePair<BytesRef, int> entry in classCounts.EntrySet())
			{
				int count = entry.Value;
				if (count > max)
				{
					max = count;
					assignedClass = entry.Key.Clone();
				}
			}
			double score = max / (double)k;
			return new ClassificationResult<BytesRef>(assignedClass, score);
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
			this.textFieldNames = textFieldNames;
			this.classFieldName = classFieldName;
			mlt = new MoreLikeThis(atomicReader);
			mlt.SetAnalyzer(analyzer);
			mlt.SetFieldNames(textFieldNames);
			indexSearcher = new IndexSearcher(atomicReader);
			if (minDocsFreq > 0)
			{
				mlt.SetMinDocFreq(minDocsFreq);
			}
			if (minTermFreq > 0)
			{
				mlt.SetMinTermFreq(minTermFreq);
			}
			this.query = query;
		}
	}
}
