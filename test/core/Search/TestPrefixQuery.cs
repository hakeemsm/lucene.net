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

using WhitespaceAnalyzer = Lucene.Net.Test.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary> Tests {@link PrefixQuery} class.
	/// 
	/// </summary>
    [TestFixture]
	public class TestPrefixQuery:LuceneTestCase
	{
		[Test]
		public virtual void  TestPrefixQuery_Renamed()
		{
			Directory directory = NewDirectory();
			
			System.String[] categories = new System.String[]{"/Computers", "/Computers/Mac", "/Computers/Windows"};
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory);
			for (int i = 0; i < categories.Length; i++)
			{
				Document doc = new Document();
				doc.Add(NewStringField("category", categories[i], Field.Store.YES));
				writer.AddDocument(doc);
			}
			IndexReader reader = writer.GetReader();
			PrefixQuery query = new PrefixQuery(new Term("category", "/Computers"));
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(3, hits.Length, "All documents in /Computers category and below");
			
			query = new PrefixQuery(new Term("category", "/Computers/Mac"));
			hits = searcher.Search(query, null, 1000).ScoreDocs;
			Assert.AreEqual(1, hits.Length, "One in /Computers/Mac");
			query = new PrefixQuery(new Term("category", string.Empty));
			Terms terms = MultiFields.GetTerms(searcher.GetIndexReader(), "category");
			IsFalse(query.GetTermsEnum(terms) is PrefixTermsEnum);
			hits = searcher.Search(query, null, 1000).scoreDocs;
			AreEqual("everything", 3, hits.Length);
			writer.Close();
			reader.Close();
			directory.Close();
		}
	}
}