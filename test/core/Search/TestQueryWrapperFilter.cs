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

using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Index = Lucene.Net.Documents.Field.Index;
using Store = Lucene.Net.Documents.Field.Store;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using Occur = Lucene.Net.Search.Occur;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
    [TestFixture]
	public class TestQueryWrapperFilter:LuceneTestCase
	{
		
        [Test]
		public virtual void  TestBasic()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Document doc = new Document();
			doc.Add(NewTextField("field", "value", Field.Store.NO));
			writer.AddDocument(doc);
			IndexReader reader = writer.GetReader();
			writer.Close();
			
			TermQuery termQuery = new TermQuery(new Term("field", "value"));
			
			// should not throw exception with primitive query
			QueryWrapperFilter qwf = new QueryWrapperFilter(termQuery);
			
			IndexSearcher searcher = NewSearcher(reader);
			TopDocs hits = searcher.Search(new MatchAllDocsQuery(), qwf, 10);
			Assert.AreEqual(1, hits.TotalHits);
			hits = searcher.Search(new MatchAllDocsQuery(), new CachingWrapperFilter(qwf), 10
				);
			NUnit.Framework.Assert.AreEqual(1, hits.totalHits);
			// should not throw exception with complex primitive query
			BooleanQuery booleanQuery = new BooleanQuery();
			booleanQuery.Add(termQuery, Occur.MUST);
			booleanQuery.Add(new TermQuery(new Term("field", "missing")), Occur.MUST_NOT);
			qwf = new QueryWrapperFilter(termQuery);
			
			hits = searcher.Search(new MatchAllDocsQuery(), qwf, 10);
			Assert.AreEqual(1, hits.TotalHits);
			hits = searcher.Search(new MatchAllDocsQuery(), new CachingWrapperFilter(qwf), 10
				);
			NUnit.Framework.Assert.AreEqual(1, hits.totalHits);
			// should not throw exception with non primitive Query (doesn't implement
			// Query#createWeight)
			qwf = new QueryWrapperFilter(new FuzzyQuery(new Term("field", "valu")));
			hits = searcher.Search(new MatchAllDocsQuery(), qwf, 10);
			NUnit.Framework.Assert.AreEqual(1, hits.totalHits);
			hits = searcher.Search(new MatchAllDocsQuery(), new CachingWrapperFilter(qwf), 10
				);
			NUnit.Framework.Assert.AreEqual(1, hits.totalHits);
			// test a query with no hits
			termQuery = new TermQuery(new Term("field", "not_exist"));
			qwf = new QueryWrapperFilter(termQuery);
			hits = searcher.Search(new MatchAllDocsQuery(), qwf, 10);
			Assert.AreEqual(1, hits.TotalHits);
			hits = searcher.Search(new MatchAllDocsQuery(), new CachingWrapperFilter(qwf), 10
				);
			NUnit.Framework.Assert.AreEqual(0, hits.totalHits);
			reader.Close();
			dir.Close();
		}
		public virtual void TestRandom()
		{
			Directory d = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d);
			w.w.GetConfig().SetMaxBufferedDocs(17);
			int numDocs = AtLeast(100);
			ICollection<string> aDocs = new HashSet<string>();
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				string v;
				if (Random().Next(5) == 4)
				{
					v = "a";
					aDocs.AddItem(string.Empty + i);
				}
				else
				{
					v = "b";
				}
				Field f = NewStringField("field", v, Field.Store.NO);
				doc.Add(f);
				doc.Add(NewStringField("id", string.Empty + i, Field.Store.YES));
				w.AddDocument(doc);
			}
			int numDelDocs = AtLeast(10);
			for (int i_1 = 0; i_1 < numDelDocs; i_1++)
			{
				string delID = string.Empty + Random().Next(numDocs);
				w.DeleteDocuments(new Term("id", delID));
				aDocs.Remove(delID);
			}
			IndexReader r = w.GetReader();
			w.Close();
			TopDocs hits = NewSearcher(r).Search(new MatchAllDocsQuery(), new QueryWrapperFilter
				(new TermQuery(new Term("field", "a"))), numDocs);
			NUnit.Framework.Assert.AreEqual(aDocs.Count, hits.totalHits);
			foreach (ScoreDoc sd in hits.scoreDocs)
			{
				NUnit.Framework.Assert.IsTrue(aDocs.Contains(r.Document(sd.doc).Get("id")));
			}
			r.Close();
			d.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestThousandDocuments()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			for (int i = 0; i < 1000; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(NewStringField("field", English.IntToEnglish(i), Field.Store.NO));
				writer.AddDocument(doc);
			}
			IndexReader reader = writer.GetReader();
			writer.Close();
			IndexSearcher searcher = NewSearcher(reader);
			for (int i_1 = 0; i_1 < 1000; i_1++)
			{
				TermQuery termQuery = new TermQuery(new Term("field", English.IntToEnglish(i_1)));
				QueryWrapperFilter qwf = new QueryWrapperFilter(termQuery);
				TopDocs td = searcher.Search(new MatchAllDocsQuery(), qwf, 10);
				NUnit.Framework.Assert.AreEqual(1, td.totalHits);
			}
			reader.Close();
			dir.Close();
		}
	}
}