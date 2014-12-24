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

using Lucene.Net.Test.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Store;
using Lucene.Net.Search;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net
{
	
	/// <summary>JUnit adaptation of an older test case SearchTest.</summary>
	[TestFixture]
	public class TestSearch:LuceneTestCase
	{
		
		/// <summary>Main for running test case by itself. </summary>
		public virtual void TestNegativeQueryBoost()
		{
			Query q = new TermQuery(new Term("foo", "bar"));
			q.SetBoost(-42f);
			AreEqual(-42f, q.GetBoost(), 0.0f);
			Directory directory = NewDirectory();
			try
			{
				Analyzer analyzer = new MockAnalyzer(Random());
				IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
				IndexWriter writer = new IndexWriter(directory, conf);
				try
				{
					Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
					d.Add(NewTextField("foo", "bar", Field.Store.YES));
					writer.AddDocument(d);
				}
				finally
				{
					writer.Close();
				}
				IndexReader reader = DirectoryReader.Open(directory);
				try
				{
					IndexSearcher searcher = NewSearcher(reader);
					ScoreDoc[] hits = searcher.Search(q, null, 1000).scoreDocs;
					AreEqual(1, hits.Length);
					IsTrue("score is not negative: " + hits[0].score, hits[0].
						score < 0);
					Explanation explain = searcher.Explain(q, hits[0].doc);
					AreEqual("score doesn't match explanation", hits[0].score, 
						explain.GetValue(), 0.001f);
					IsTrue("explain doesn't think doc is a match", explain.IsMatch
						());
				}
				finally
				{
					reader.Close();
				}
			}
			finally
			{
				directory.Close();
			}
		}
		
		/// <summary>This test performs a number of searches. It also compares output
		/// of searches using multi-file index segments with single-file
		/// index segments.
		/// 
		/// TODO: someone should check that the results of the searches are
		/// still correct by adding assert statements. Right now, the test
		/// passes if the results are the same between multi-file and
		/// single-file formats, even if the results are wrong.
		/// </summary>
        [Test]
        public virtual void TestSearch_Renamed()
		{
			System.IO.MemoryStream sw = new System.IO.MemoryStream();
			System.IO.StreamWriter pw = new System.IO.StreamWriter(sw);
			DoTestSearch(pw, false);
			pw.Close();
			sw.Close();
			System.String multiFileOutput = System.Text.ASCIIEncoding.ASCII.GetString(sw.ToArray());
			//System.out.println(multiFileOutput);
			
			sw = new System.IO.MemoryStream();
			pw = new System.IO.StreamWriter(sw);
			DoTestSearch(pw, true);
			pw.Close();
			sw.Close();
			System.String singleFileOutput = System.Text.ASCIIEncoding.ASCII.GetString(sw.ToArray());
			
			Assert.AreEqual(multiFileOutput, singleFileOutput);
		}
		
		
		private void  DoTestSearch(System.IO.StreamWriter out_Renamed, bool useCompoundFile)
		{
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(random);
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			MergePolicy mp = conf.GetMergePolicy();
			mp.SetNoCFSRatio(useCompoundFile ? 1.0 : 0.0);
			IndexWriter writer = new IndexWriter(directory, conf);
			System.String[] docs = new System.String[]{"a b c d e", "a b c d e a b c d e", "a b c d e f g h i j", "a c e", "e c a", "a c e a c e", "a c e a b c"};
			for (int j = 0; j < docs.Length; j++)
			{
				Document d = new Document();
				d.Add(NewTextField("contents", docs[j], Field.Store.YES));
				d.Add(NewStringField("id", string.Empty + j, Field.Store.NO));
				writer.AddDocument(d);
			}
			writer.Close();
			IndexReader reader = DirectoryReader.Open(directory);
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = null;
			Sort sort = new Sort(SortField.FIELD_SCORE, new SortField("id", SortField.Type.INT
				));
			foreach (Query query in BuildQueries())
			{
				out_Renamed.WriteLine("Query: " + query.ToString("contents"));
				
				//DateFilter filter =
				//  new DateFilter("modified", Time(1997,0,1), Time(1998,0,1));
				//DateFilter filter = DateFilter.Before("modified", Time(1997,00,01));
				//System.out.println(filter);
				
				hits = searcher.Search(query, null, 1000, sort).scoreDocs;
				
				out_Renamed.WriteLine(hits.Length + " total results");
				for (int i = 0; i < hits.Length && i < 10; i++)
				{
					Document d = searcher.Doc(hits[i].Doc);
					out_Renamed.WriteLine(i + " " + hits[i].Score + " " + d.Get("contents"));
				}
			}
			reader.Close();
			directory.Close();
		}
		private IList<Query> BuildQueries()
		{
			IList<Query> queries = new AList<Query>();
			BooleanQuery booleanAB = new BooleanQuery();
			booleanAB.Add(new TermQuery(new Term("contents", "a")), BooleanClause.Occur.SHOULD
				);
			booleanAB.Add(new TermQuery(new Term("contents", "b")), BooleanClause.Occur.SHOULD
				);
			queries.AddItem(booleanAB);
			PhraseQuery phraseAB = new PhraseQuery();
			phraseAB.Add(new Term("contents", "a"));
			phraseAB.Add(new Term("contents", "b"));
			queries.AddItem(phraseAB);
			PhraseQuery phraseABC = new PhraseQuery();
			phraseABC.Add(new Term("contents", "a"));
			phraseABC.Add(new Term("contents", "b"));
			phraseABC.Add(new Term("contents", "c"));
			queries.AddItem(phraseABC);
			BooleanQuery booleanAC = new BooleanQuery();
			booleanAC.Add(new TermQuery(new Term("contents", "a")), BooleanClause.Occur.SHOULD
				);
			booleanAC.Add(new TermQuery(new Term("contents", "c")), BooleanClause.Occur.SHOULD
				);
			queries.AddItem(booleanAC);
			PhraseQuery phraseAC = new PhraseQuery();
			phraseAC.Add(new Term("contents", "a"));
			phraseAC.Add(new Term("contents", "c"));
			queries.AddItem(phraseAC);
			PhraseQuery phraseACE = new PhraseQuery();
			phraseACE.Add(new Term("contents", "a"));
			phraseACE.Add(new Term("contents", "c"));
			phraseACE.Add(new Term("contents", "e"));
			queries.AddItem(phraseACE);
			return queries;
		}
		internal static long Time(int year, int month, int day)
		{
			System.DateTime calendar = new System.DateTime(year, month, day, 0, 0, 0, 0, new System.Globalization.GregorianCalendar());
			return calendar.Ticks;
		}
	}
}