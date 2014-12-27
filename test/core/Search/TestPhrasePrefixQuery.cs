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

using SimpleAnalyzer = Lucene.Net.Test.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using TermEnum = Lucene.Net.Index.TermEnum;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{
	
	/// <summary>This class tests PhrasePrefixQuery class.</summary>
    [TestFixture]
	public class TestPhrasePrefixQuery:LuceneTestCase
	{
		/*public TestPhrasePrefixQuery(System.String name):base(name)
		{
		}*/
		
		/// <summary> </summary>
		[Test]
		public virtual void  TestPhrasePrefix()
		{
			Directory indexStore = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), indexStore);
			Document doc1 = new Document();
			Document doc2 = new Document();
			Document doc3 = new Document();
			Document doc4 = new Document();
			Document doc5 = new Document();
			doc1.Add(NewTextField("body", "blueberry pie", Field.Store.YES));
			doc2.Add(NewTextField("body", "blueberry strudel", Field.Store.YES));
			doc3.Add(NewTextField("body", "blueberry pizza", Field.Store.YES));
			doc4.Add(NewTextField("body", "blueberry chewing gum", Field.Store.YES));
			doc5.Add(NewTextField("body", "piccadilly circus", Field.Store.YES));
			writer.AddDocument(doc1);
			writer.AddDocument(doc2);
			writer.AddDocument(doc3);
			writer.AddDocument(doc4);
			writer.AddDocument(doc5);
			IndexReader reader = writer.GetReader();
			writer.Dispose();

			IndexSearcher searcher = NewSearcher(reader);
			
			//PhrasePrefixQuery query1 = new PhrasePrefixQuery();
			MultiPhraseQuery query1 = new MultiPhraseQuery();
			//PhrasePrefixQuery query2 = new PhrasePrefixQuery();
			MultiPhraseQuery query2 = new MultiPhraseQuery();
			query1.Add(new Term("body", "blueberry"));
			query2.Add(new Term("body", "strawberry"));
			List<Term> termsWithPrefix = new List<Term>();
			
			// this TermEnum gives "piccadilly", "pie" and "pizza".
			System.String prefix = "pi";
			TermsEnum te = MultiFields.GetFields(reader).Terms("body").Iterator(null);
			te.SeekCeil(new BytesRef(prefix));
			do 
			{
				string s = te.Term().Utf8ToString();
				if (s.StartsWith(prefix))
				{
					termsWithPrefix.AddItem(new Term("body", s));
				}
				else
				{
					break;
				}
			}
			while (te.Next() != null);
			
			query1.Add((Term[]) termsWithPrefix.ToArray(typeof(Term)));
			query2.Add((Term[]) termsWithPrefix.ToArray(typeof(Term)));
			
			ScoreDoc[] result;
			result = searcher.Search(query1, null, 1000).ScoreDocs;
			Assert.AreEqual(2, result.Length);
			
			result = searcher.Search(query2, null, 1000).ScoreDocs;
			Assert.AreEqual(0, result.Length);
			reader.Dispose();
			indexStore.Dispose();
		}
	}
}