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

using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Store;
using Lucene.Net.Search;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net
{
	
	
	/// <summary>JUnit adaptation of an older test case DocTest.</summary>
    [TestFixture]
    public class TestSearchForDuplicates : LuceneTestCase
	{
		
		/// <summary>Main for running test case by itself. </summary>
		[STAThread]
		public static void  Main(System.String[] args)
		{
			// TestRunner.run(new TestSuite(typeof(TestSearchForDuplicates))); {{Aroush-2.9}} how is this done in NUnit?
		}
		
		
		
		internal const System.String PRIORITY_FIELD = "priority";
		internal const System.String ID_FIELD = "id";
		internal const System.String HIGH_PRIORITY = "high";
		internal const System.String MED_PRIORITY = "medium";
		internal const System.String LOW_PRIORITY = "low";
		
		
		/// <summary>This test compares search results when using and not using compound
		/// files.
		/// 
		/// TODO: There is rudimentary search result validation as well, but it is
		/// simply based on asserting the output observed in the old test case,
		/// without really knowing if the output is correct. Someone needs to
		/// validate this output and make any changes to the checkHits method.
		/// </summary>
        [Test]
		public virtual void  TestRun()
		{
			System.IO.MemoryStream sw = new System.IO.MemoryStream();
			System.IO.StreamWriter pw = new System.IO.StreamWriter(sw);
			int MAX_DOCS = AtLeast(225);
			DoTest(Random(), pw, false, MAX_DOCS);
			pw.Close();
			sw.Close();
			System.String multiFileOutput = System.Text.ASCIIEncoding.ASCII.GetString(sw.ToArray());
			//System.out.println(multiFileOutput);
			
			sw = new System.IO.MemoryStream();
			pw = new System.IO.StreamWriter(sw);
			DoTest(Random(), pw, true, MAX_DOCS);
			pw.Close();
			sw.Close();
			System.String singleFileOutput = System.Text.ASCIIEncoding.ASCII.GetString(sw.ToArray());
			
			Assert.AreEqual(multiFileOutput, singleFileOutput);
		}
		
		
		private void  DoTest(Random random,System.IO.StreamWriter out_Renamed, bool useCompoundFiles, int MAX_DOCS)
		{
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(random);
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			MergePolicy mp = conf.GetMergePolicy();
			mp.SetNoCFSRatio(useCompoundFiles ? 1.0 : 0.0);
			IndexWriter writer = new IndexWriter(directory, conf);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: now build index MAX_DOCS=" + MAX_DOCS);
			}
			for (int j = 0; j < MAX_DOCS; j++)
			{
				Document d = new Document();
				d.Add(NewTextField(PRIORITY_FIELD, HIGH_PRIORITY, Field.Store.YES));
				d.Add(NewTextField(ID_FIELD, Sharpen.Extensions.ToString(j), Field.Store.YES));
				writer.AddDocument(d);
			}
			writer.Close();
			
			// try a search without OR
			IndexReader reader = DirectoryReader.Open(directory);
			IndexSearcher searcher = NewSearcher(reader);
			Query query = new TermQuery(new Term(PRIORITY_FIELD, HIGH_PRIORITY));
			@out.WriteLine("Query: " + query.ToString(PRIORITY_FIELD));
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: search query=" + query);
			}
			Sort sort = new Sort(SortField.FIELD_SCORE, new SortField(ID_FIELD, SortField.Type
				.INT));
			ScoreDoc[] hits = searcher.Search(query, null, MAX_DOCS).ScoreDocs;
			PrintHits(out_Renamed, hits, searcher);
			CheckHits(hits, MAX_DOCS, searcher);
			
			
			// try a new search with OR
			searcher = NewSearcher(reader);
			hits = null;
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(new TermQuery(new Term(PRIORITY_FIELD, HIGH_PRIORITY)), BooleanClause.Occur
				.SHOULD);
			booleanQuery.Add(new TermQuery(new Term(PRIORITY_FIELD, MED_PRIORITY)), BooleanClause.Occur
				.SHOULD);
			out_Renamed.WriteLine("Query: " + query.ToString(PRIORITY_FIELD));
			
			hits = searcher.Search(booleanQuery, null, MAX_DOCS, sort).scoreDocs;
			PrintHits(out_Renamed, hits, searcher);
			CheckHits(hits, MAX_DOCS, searcher);
			
			reader.Close();
			directory.Close();
		}
		
		
		private void  PrintHits(System.IO.StreamWriter out_Renamed, ScoreDoc[] hits, IndexSearcher searcher)
		{
			out_Renamed.WriteLine(hits.Length + " total results\n");
			for (int i = 0; i < hits.Length; i++)
			{
				if (i < 10 || (i > 94 && i < 105))
				{
					Document d = searcher.Doc(hits[i].Doc);
					out_Renamed.WriteLine(i + " " + d.Get(ID_FIELD));
				}
			}
		}
		
		private void  CheckHits(ScoreDoc[] hits, int expectedCount, IndexSearcher searcher)
		{
			Assert.AreEqual(expectedCount, hits.Length, "total results");
			for (int i = 0; i < hits.Length; i++)
			{
				if (i < 10 || (i > 94 && i < 105))
				{
					Document d = searcher.Doc(hits[i].Doc);
					Assert.AreEqual(System.Convert.ToString(i), d.Get(ID_FIELD), "check " + i);
				}
			}
		}
	}
}