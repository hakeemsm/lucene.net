/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Lucene.Net.Documents;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary>Document boost unit test.
	/// 
	/// 
	/// </summary>
	/// <version>  $Revision: 787772 $
	/// </version>
    [TestFixture]
	public class TestDocBoost:LuceneTestCase
	{
		private class AnonymousClassCollector:Collector
		{
			public AnonymousClassCollector(float[] scores, TestDocBoost enclosingInstance)
			{
				InitBlock(scores, enclosingInstance);
			}
			private void  InitBlock(float[] scores, TestDocBoost enclosingInstance)
			{
				this.scores = scores;
				this.enclosingInstance = enclosingInstance;
			}
			private float[] scores;
			private TestDocBoost enclosingInstance;
			public TestDocBoost Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private int base_Renamed = 0;
			private Scorer scorer;
			public override void  SetScorer(Scorer scorer)
			{
				this.scorer = scorer;
			}
			public override void  Collect(int doc)
			{
				scores[doc + base_Renamed] = scorer.Score();
			}
			public override void SetNextReader(AtomicReaderContext context)
			{
				base_Renamed = context.docBase;
			}

		    public override bool AcceptsDocsOutOfOrder
		    {
		        get { return true; }
		    }
		}
		
		[Test]
		public virtual void  TestDocBoost_Renamed()
		{
			Directory store = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), store, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				()));
			Field f1 = NewTextField("field", "word", Field.Store.YES);
			Field f2 = NewTextField("field", "word", Field.Store.YES);
			f2.Boost = 2.0f;
			
			Document d1 = new Document();
			Document d2 = new Document();
			
			d1.Add(f1); // boost = 1
			d2.Add(f2); // boost = 2
			
			writer.AddDocument(d1);
			writer.AddDocument(d2);
			IndexReader reader = writer.GetReader();
			writer.Close();
			
			float[] scores = new float[4];
			IndexSearcher searcher = NewSearcher(reader);
			searcher.Search(new TermQuery(new Term("field", "word")), new _Collector_62(scores
				));
			float lastScore = 0.0f;
			
			for (int i = 0; i < 2; i++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine(searcher.Explain(new TermQuery(new Term("field", "word"
						)), i));
				}
				NUnit.Framework.Assert.IsTrue("score: " + scores[i] + " should be > lastScore: " 
					+ lastScore, scores[i] > lastScore);
				lastScore = scores[i];
			}
			reader.Close();
			store.Close();
		}
	}
}