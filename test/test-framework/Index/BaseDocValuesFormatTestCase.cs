using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene42;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.TestFramework.Util;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>Abstract class to do basic tests for a docvalues format.</summary>
	/// <remarks>
	/// Abstract class to do basic tests for a docvalues format.
	/// NOTE: This test focuses on the docvalues impl, nothing else.
	/// The [stretch] goal is for this test to be
	/// so thorough in testing a new DocValuesFormat that if this
	/// test passes, then all Lucene/Solr tests should also pass.  Ie,
	/// if there is some bug in a given DocValuesFormat that this
	/// test fails to catch then this test needs to be improved!
	/// </remarks>
	public abstract class BaseDocValuesFormatTestCase : BaseIndexFileFormatTestCase
	{
		protected internal override void AddRandomFields(Lucene.Net.Documents.Document
			 doc)
		{
			if (Usually())
			{
				doc.Add(new NumericDocValuesField("ndv", Random().Next(1 << 12)));
				doc.Add(new BinaryDocValuesField("bdv", new BytesRef(TestUtil.RandomSimpleString(
					Random()))));
				doc.Add(new SortedDocValuesField("sdv", new BytesRef(TestUtil.RandomSimpleString(
					Random(), 2))));
			}
			if (DefaultCodecSupportsSortedSet())
			{
				int numValues = Random().Next(5);
				for (int i = 0; i < numValues; ++i)
				{
					doc.Add(new SortedSetDocValuesField("ssdv", new BytesRef(TestUtil.RandomSimpleString
						(Random(), 2))));
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestOneNumber()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
			var doc = new Document();
			string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
			string text = "This is the text to be indexed. " + longTerm;
			doc.Add(NewTextField("fieldname", text, Field.Store.YES));
			doc.Add(new NumericDocValuesField("dv", 5));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			IndexSearcher isearcher = new IndexSearcher(ireader);
			AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname"
				, longTerm)), 1).TotalHits);
			Query query = new TermQuery(new Term("fieldname", "text"));
			TopDocs hits = isearcher.Search(query, null, 1);
			AreEqual(1, hits.TotalHits);
			// Iterate through the results:
			for (int i = 0; i < hits.ScoreDocs.Length; i++)
			{
				Lucene.Net.Documents.Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
				AreEqual(text, hitDoc.Get("fieldname"));
				 
				//assert ireader.Leaves.size() == 1;
				NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues
					("dv");
				AreEqual(5, dv.Get(hits.ScoreDocs[i].Doc));
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestOneFloat()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
			string text = "This is the text to be indexed. " + longTerm;
			doc.Add(NewTextField("fieldname", text, Field.Store.YES));
			doc.Add(new FloatDocValuesField("dv", 5.7f));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			IndexSearcher isearcher = new IndexSearcher(ireader);
			AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname"
				, longTerm)), 1).TotalHits);
			Query query = new TermQuery(new Term("fieldname", "text"));
			TopDocs hits = isearcher.Search(query, null, 1);
			AreEqual(1, hits.TotalHits);
			// Iterate through the results:
			for (int i = 0; i < hits.ScoreDocs.Length; i++)
			{
				Lucene.Net.Documents.Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
				AreEqual(text, hitDoc.Get("fieldname"));
				 
				//assert ireader.Leaves.size() == 1;
				NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues
					("dv");
				AreEqual(float.FloatToRawIntBits(5.7f), dv.Get(hits.ScoreDocs
					[i].Doc));
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTwoNumbers()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
			string text = "This is the text to be indexed. " + longTerm;
			doc.Add(NewTextField("fieldname", text, Field.Store.YES));
			doc.Add(new NumericDocValuesField("dv1", 5));
			doc.Add(new NumericDocValuesField("dv2", 17));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			IndexSearcher isearcher = new IndexSearcher(ireader);
			AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname"
				, longTerm)), 1).TotalHits);
			Query query = new TermQuery(new Term("fieldname", "text"));
			TopDocs hits = isearcher.Search(query, null, 1);
			AreEqual(1, hits.TotalHits);
			// Iterate through the results:
			for (int i = 0; i < hits.ScoreDocs.Length; i++)
			{
				Lucene.Net.Documents.Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
				AreEqual(text, hitDoc.Get("fieldname"));
				 
				//assert ireader.Leaves.size() == 1;
				NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues
					("dv1");
				AreEqual(5, dv.Get(hits.ScoreDocs[i].Doc));
				dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues("dv2");
				AreEqual(17, dv.Get(hits.ScoreDocs[i].Doc));
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTwoBinaryValues()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
			string text = "This is the text to be indexed. " + longTerm;
			doc.Add(NewTextField("fieldname", text, Field.Store.YES));
			doc.Add(new BinaryDocValuesField("dv1", new BytesRef(longTerm)));
			doc.Add(new BinaryDocValuesField("dv2", new BytesRef(text)));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			IndexSearcher isearcher = new IndexSearcher(ireader);
			AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname"
				, longTerm)), 1).TotalHits);
			Query query = new TermQuery(new Term("fieldname", "text"));
			TopDocs hits = isearcher.Search(query, null, 1);
			AreEqual(1, hits.TotalHits);
			// Iterate through the results:
			for (int i = 0; i < hits.ScoreDocs.Length; i++)
			{
				Lucene.Net.Documents.Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
				AreEqual(text, hitDoc.Get("fieldname"));
				 
				//assert ireader.Leaves.size() == 1;
				BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues
					("dv1");
				BytesRef scratch = new BytesRef();
				dv.Get(hits.ScoreDocs[i].Doc, scratch);
				AreEqual(new BytesRef(longTerm), scratch);
				dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues("dv2");
				dv.Get(hits.ScoreDocs[i].Doc, scratch);
				AreEqual(new BytesRef(text), scratch);
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTwoFieldsMixed()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
			string text = "This is the text to be indexed. " + longTerm;
			doc.Add(NewTextField("fieldname", text, Field.Store.YES));
			doc.Add(new NumericDocValuesField("dv1", 5));
			doc.Add(new BinaryDocValuesField("dv2", new BytesRef("hello world")));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			IndexSearcher isearcher = new IndexSearcher(ireader);
			AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname"
				, longTerm)), 1).TotalHits);
			Query query = new TermQuery(new Term("fieldname", "text"));
			TopDocs hits = isearcher.Search(query, null, 1);
			AreEqual(1, hits.TotalHits);
			BytesRef scratch = new BytesRef();
			// Iterate through the results:
			for (int i = 0; i < hits.ScoreDocs.Length; i++)
			{
				Lucene.Net.Documents.Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
				AreEqual(text, hitDoc.Get("fieldname"));
				 
				//assert ireader.Leaves.size() == 1;
				NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues
					("dv1");
				AreEqual(5, dv.Get(hits.ScoreDocs[i].Doc));
				BinaryDocValues dv2 = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues
					("dv2");
				dv2.Get(hits.ScoreDocs[i].Doc, scratch);
				AreEqual(new BytesRef("hello world"), scratch);
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestThreeFieldsMixed()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
			string text = "This is the text to be indexed. " + longTerm;
			doc.Add(NewTextField("fieldname", text, Field.Store.YES));
			doc.Add(new SortedDocValuesField("dv1", new BytesRef("hello hello")));
			doc.Add(new NumericDocValuesField("dv2", 5));
			doc.Add(new BinaryDocValuesField("dv3", new BytesRef("hello world")));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			IndexSearcher isearcher = new IndexSearcher(ireader);
			AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname"
				, longTerm)), 1).TotalHits);
			Query query = new TermQuery(new Term("fieldname", "text"));
			TopDocs hits = isearcher.Search(query, null, 1);
			AreEqual(1, hits.TotalHits);
			BytesRef scratch = new BytesRef();
			// Iterate through the results:
			for (int i = 0; i < hits.ScoreDocs.Length; i++)
			{
				Lucene.Net.Documents.Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
				AreEqual(text, hitDoc.Get("fieldname"));
				 
				//assert ireader.Leaves.size() == 1;
				SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues
					("dv1");
				int ord = dv.GetOrd(0);
				dv.LookupOrd(ord, scratch);
				AreEqual(new BytesRef("hello hello"), scratch);
				NumericDocValues dv2 = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues
					("dv2");
				AreEqual(5, dv2.Get(hits.ScoreDocs[i].Doc));
				BinaryDocValues dv3 = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues
					("dv3");
				dv3.Get(hits.ScoreDocs[i].Doc, scratch);
				AreEqual(new BytesRef("hello world"), scratch);
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestThreeFieldsMixed2()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
			string text = "This is the text to be indexed. " + longTerm;
			doc.Add(NewTextField("fieldname", text, Field.Store.YES));
			doc.Add(new BinaryDocValuesField("dv1", new BytesRef("hello world")));
			doc.Add(new SortedDocValuesField("dv2", new BytesRef("hello hello")));
			doc.Add(new NumericDocValuesField("dv3", 5));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			IndexSearcher isearcher = new IndexSearcher(ireader);
			AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname"
				, longTerm)), 1).TotalHits);
			Query query = new TermQuery(new Term("fieldname", "text"));
			TopDocs hits = isearcher.Search(query, null, 1);
			AreEqual(1, hits.TotalHits);
			BytesRef scratch = new BytesRef();
			// Iterate through the results:
			for (int i = 0; i < hits.ScoreDocs.Length; i++)
			{
				Lucene.Net.Documents.Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
				AreEqual(text, hitDoc.Get("fieldname"));
				 
				//assert ireader.Leaves.size() == 1;
				SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues
					("dv2");
				int ord = dv.GetOrd(0);
				dv.LookupOrd(ord, scratch);
				AreEqual(new BytesRef("hello hello"), scratch);
				NumericDocValues dv2 = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues
					("dv3");
				AreEqual(5, dv2.Get(hits.ScoreDocs[i].Doc));
				BinaryDocValues dv3 = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues
					("dv1");
				dv3.Get(hits.ScoreDocs[i].Doc, scratch);
				AreEqual(new BytesRef("hello world"), scratch);
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTwoDocumentsNumeric()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", 1));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("dv", 2));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues
				("dv");
			AreEqual(1, dv.Get(0));
			AreEqual(2, dv.Get(1));
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTwoDocumentsMerged()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewField("id", "0", StringField.TYPE_STORED));
			doc.Add(new NumericDocValuesField("dv", -10));
			iwriter.AddDocument(doc);
			iwriter.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewField("id", "1", StringField.TYPE_STORED));
			doc.Add(new NumericDocValuesField("dv", 99));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues
				("dv");
			for (int i = 0; i < 2; i++)
			{
				Lucene.Net.Documents.Document doc2 = ((AtomicReader)ireader.Leaves[0].Reader).Document(i);
				long expected;
				if (doc2.Get("id").Equals("0"))
				{
					expected = -10;
				}
				else
				{
					expected = 99;
				}
				AreEqual(expected, dv.Get(i));
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBigNumericRange()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", long.MinValue));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("dv", long.MaxValue));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues
				("dv");
			AreEqual(long.MinValue, dv.Get(0));
			AreEqual(long.MaxValue, dv.Get(1));
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBigNumericRange2()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new NumericDocValuesField("dv", -8841491950446638677L));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new NumericDocValuesField("dv", 9062230939892376225L));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues
				("dv");
			AreEqual(-8841491950446638677L, dv.Get(0));
			AreEqual(9062230939892376225L, dv.Get(1));
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBytes()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
			string text = "This is the text to be indexed. " + longTerm;
			doc.Add(NewTextField("fieldname", text, Field.Store.YES));
			doc.Add(new BinaryDocValuesField("dv", new BytesRef("hello world")));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			IndexSearcher isearcher = new IndexSearcher(ireader);
			AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname"
				, longTerm)), 1).TotalHits);
			Query query = new TermQuery(new Term("fieldname", "text"));
			TopDocs hits = isearcher.Search(query, null, 1);
			AreEqual(1, hits.TotalHits);
			BytesRef scratch = new BytesRef();
			// Iterate through the results:
			for (int i = 0; i < hits.ScoreDocs.Length; i++)
			{
				Lucene.Net.Documents.Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
				AreEqual(text, hitDoc.Get("fieldname"));
				 
				//assert ireader.Leaves.size() == 1;
				BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues
					("dv");
				dv.Get(hits.ScoreDocs[i].Doc, scratch);
				AreEqual(new BytesRef("hello world"), scratch);
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBytesTwoDocumentsMerged()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewField("id", "0", StringField.TYPE_STORED));
			doc.Add(new BinaryDocValuesField("dv", new BytesRef("hello world 1")));
			iwriter.AddDocument(doc);
			iwriter.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewField("id", "1", StringField.TYPE_STORED));
			doc.Add(new BinaryDocValuesField("dv", new BytesRef("hello 2")));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues
				("dv");
			BytesRef scratch = new BytesRef();
			for (int i = 0; i < 2; i++)
			{
				Lucene.Net.Documents.Document doc2 = ((AtomicReader)ireader.Leaves[0].Reader).Document(i);
				string expected;
				if (doc2.Get("id").Equals("0"))
				{
					expected = "hello world 1";
				}
				else
				{
					expected = "hello 2";
				}
				dv.Get(i, scratch);
				AreEqual(expected, scratch.Utf8ToString());
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedBytes()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
			string text = "This is the text to be indexed. " + longTerm;
			doc.Add(NewTextField("fieldname", text, Field.Store.YES));
			doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world")));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			IndexSearcher isearcher = new IndexSearcher(ireader);
			AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname"
				, longTerm)), 1).TotalHits);
			Query query = new TermQuery(new Term("fieldname", "text"));
			TopDocs hits = isearcher.Search(query, null, 1);
			AreEqual(1, hits.TotalHits);
			BytesRef scratch = new BytesRef();
			// Iterate through the results:
			for (int i = 0; i < hits.ScoreDocs.Length; i++)
			{
				Lucene.Net.Documents.Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
				AreEqual(text, hitDoc.Get("fieldname"));
				 
				//assert ireader.Leaves.size() == 1;
				SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues
					("dv");
				dv.LookupOrd(dv.GetOrd(hits.ScoreDocs[i].Doc), scratch);
				AreEqual(new BytesRef("hello world"), scratch);
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedBytesTwoDocuments()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues
				("dv");
			BytesRef scratch = new BytesRef();
			dv.LookupOrd(dv.GetOrd(0), scratch);
			AreEqual("hello world 1", scratch.Utf8ToString());
			dv.LookupOrd(dv.GetOrd(1), scratch);
			AreEqual("hello world 2", scratch.Utf8ToString());
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedBytesThreeDocuments()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues
				("dv");
			AreEqual(2, dv.ValueCount);
			BytesRef scratch = new BytesRef();
			AreEqual(0, dv.GetOrd(0));
			dv.LookupOrd(0, scratch);
			AreEqual("hello world 1", scratch.Utf8ToString());
			AreEqual(1, dv.GetOrd(1));
			dv.LookupOrd(1, scratch);
			AreEqual("hello world 2", scratch.Utf8ToString());
			AreEqual(0, dv.GetOrd(2));
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedBytesTwoDocumentsMerged()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(NewField("id", "0", StringField.TYPE_STORED));
			doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
			iwriter.AddDocument(doc);
			iwriter.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewField("id", "1", StringField.TYPE_STORED));
			doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues
				("dv");
			AreEqual(2, dv.ValueCount);
			// 2 ords
			BytesRef scratch = new BytesRef();
			dv.LookupOrd(0, scratch);
			AreEqual(new BytesRef("hello world 1"), scratch);
			dv.LookupOrd(1, scratch);
			AreEqual(new BytesRef("hello world 2"), scratch);
			for (int i = 0; i < 2; i++)
			{
				Lucene.Net.Documents.Document doc2 = ((AtomicReader)ireader.Leaves[0].Reader).Document(i);
				string expected;
				if (doc2.Get("id").Equals("0"))
				{
					expected = "hello world 1";
				}
				else
				{
					expected = "hello world 2";
				}
				dv.LookupOrd(dv.GetOrd(i), scratch);
				AreEqual(expected, scratch.Utf8ToString());
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedMergeAwayAllValues()
		{
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "0", Field.Store.NO));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "1", Field.Store.NO));
			doc.Add(new SortedDocValuesField("field", new BytesRef("hello")));
			iwriter.AddDocument(doc);
			iwriter.Commit();
			iwriter.DeleteDocuments(new Term("id", "1"));
			iwriter.ForceMerge(1);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedDocValues dv = GetOnlySegmentReader(ireader).GetSortedDocValues("field");
			if (DefaultCodecSupportsDocsWithField())
			{
				AreEqual(-1, dv.GetOrd(0));
				AreEqual(0, dv.ValueCount);
			}
			else
			{
				AreEqual(0, dv.GetOrd(0));
				AreEqual(1, dv.ValueCount);
				BytesRef @ref = new BytesRef();
				dv.LookupOrd(0, @ref);
				AreEqual(new BytesRef(), @ref);
			}
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBytesWithNewline()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new BinaryDocValuesField("dv", new BytesRef("hello\nworld\r1")));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues
				("dv");
			BytesRef scratch = new BytesRef();
			dv.Get(0, scratch);
			AreEqual(new BytesRef("hello\nworld\r1"), scratch);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMissingSortedBytes()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
			iwriter.AddDocument(doc);
			// 2nd doc missing the DV field
			iwriter.AddDocument(new Lucene.Net.Documents.Document());
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues
				("dv");
			BytesRef scratch = new BytesRef();
			dv.LookupOrd(dv.GetOrd(0), scratch);
			AreEqual(new BytesRef("hello world 2"), scratch);
			if (DefaultCodecSupportsDocsWithField())
			{
				AreEqual(-1, dv.GetOrd(1));
			}
			dv.Get(1, scratch);
			AreEqual(new BytesRef(string.Empty), scratch);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedTermsEnum()
		{
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedDocValuesField("field", new BytesRef("hello")));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("field", new BytesRef("world")));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("field", new BytesRef("beer")));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedDocValues dv = GetOnlySegmentReader(ireader).GetSortedDocValues("field");
			AreEqual(3, dv.ValueCount);
			TermsEnum termsEnum = dv.TermsEnum;
			// next()
			AreEqual("beer", termsEnum.Next().Utf8ToString());
			AreEqual(0, termsEnum.Ord);
			AreEqual("hello", termsEnum.Next().Utf8ToString());
			AreEqual(1, termsEnum.Ord);
			AreEqual("world", termsEnum.Next().Utf8ToString());
			AreEqual(2, termsEnum.Ord);
			// seekCeil()
			AreEqual(TermsEnum.SeekStatus.NOT_FOUND, termsEnum.SeekCeil
				(new BytesRef("ha!")));
			AreEqual("hello", termsEnum.Term.Utf8ToString());
			AreEqual(1, termsEnum.Ord);
			AreEqual(TermsEnum.SeekStatus.FOUND, termsEnum.SeekCeil(new 
				BytesRef("beer")));
			AreEqual("beer", termsEnum.Term.Utf8ToString());
			AreEqual(0, termsEnum.Ord);
			AreEqual(TermsEnum.SeekStatus.END, termsEnum.SeekCeil(new 
				BytesRef("zzz")));
			// seekExact()
			IsTrue(termsEnum.SeekExact(new BytesRef("beer")));
			AreEqual("beer", termsEnum.Term.Utf8ToString());
			AreEqual(0, termsEnum.Ord);
			IsTrue(termsEnum.SeekExact(new BytesRef("hello")));
			AreEqual(Codec.Default.ToString(), "hello", termsEnum.Term.Utf8ToString());
			AreEqual(1, termsEnum.Ord);
			IsTrue(termsEnum.SeekExact(new BytesRef("world")));
			AreEqual("world", termsEnum.Term.Utf8ToString());
			AreEqual(2, termsEnum.Ord);
			IsFalse(termsEnum.SeekExact(new BytesRef("bogus")));
			// seek(ord)
			termsEnum.SeekExact(0);
			AreEqual("beer", termsEnum.Term.Utf8ToString());
			AreEqual(0, termsEnum.Ord);
			termsEnum.SeekExact(1);
			AreEqual("hello", termsEnum.Term.Utf8ToString());
			AreEqual(1, termsEnum.Ord);
			termsEnum.SeekExact(2);
			AreEqual("world", termsEnum.Term.Utf8ToString());
			AreEqual(2, termsEnum.Ord);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEmptySortedBytes()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedDocValuesField("dv", new BytesRef(string.Empty)));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef(string.Empty)));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			SortedDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues
				("dv");
			BytesRef scratch = new BytesRef();
			AreEqual(0, dv.GetOrd(0));
			AreEqual(0, dv.GetOrd(1));
			dv.LookupOrd(dv.GetOrd(0), scratch);
			AreEqual(string.Empty, scratch.Utf8ToString());
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEmptyBytes()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new BinaryDocValuesField("dv", new BytesRef(string.Empty)));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new BinaryDocValuesField("dv", new BytesRef(string.Empty)));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues
				("dv");
			BytesRef scratch = new BytesRef();
			dv.Get(0, scratch);
			AreEqual(string.Empty, scratch.Utf8ToString());
			dv.Get(1, scratch);
			AreEqual(string.Empty, scratch.Utf8ToString());
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestVeryLargeButLegalBytes()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			var doc = new Document();
			var bytes = new sbyte[32766];
			BytesRef b = new BytesRef(bytes);
			Random().NextBytes(bytes.ToBytes());
			doc.Add(new BinaryDocValuesField("dv", b));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues
				("dv");
			BytesRef scratch = new BytesRef();
			dv.Get(0, scratch);
			AreEqual(new BytesRef(bytes), scratch);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestVeryLargeButLegalSortedBytes()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			byte[] bytes = new byte[32766];
			BytesRef b = new BytesRef(bytes.ToSbytes());
			Random().NextBytes(bytes);
			doc.Add(new SortedDocValuesField("dv", b));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues
				("dv");
			BytesRef scratch = new BytesRef();
			dv.Get(0, scratch);
			AreEqual(new BytesRef(bytes.ToSbytes()), scratch);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCodecUsesOwnBytes()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new BinaryDocValuesField("dv", new BytesRef("boo!")));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues
				("dv");
			var mybytes = new sbyte[20];
			BytesRef scratch = new BytesRef(mybytes);
			dv.Get(0, scratch);
			AreEqual("boo!", scratch.Utf8ToString());
			IsFalse(scratch.bytes == mybytes);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCodecUsesOwnSortedBytes()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			var iwriter = new RandomIndexWriter(Random(), directory, conf);
			var doc = new Document {new SortedDocValuesField("dv", new BytesRef("boo!"))};
		    iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues
				("dv");
			var mybytes = new sbyte[20];
			BytesRef scratch = new BytesRef(mybytes);
			dv.Get(0, scratch);
			AreEqual("boo!", scratch.Utf8ToString());
			IsFalse(scratch.bytes == mybytes);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCodecUsesOwnBytesEachTime()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			var doc = new Document {new BinaryDocValuesField("dv", new BytesRef("foo!"))};
		    iwriter.AddDocument(doc);
			doc = new Document {new BinaryDocValuesField("dv", new BytesRef("bar!"))};
		    iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues
				("dv");
			BytesRef scratch = new BytesRef();
			dv.Get(0, scratch);
			AreEqual("foo!", scratch.Utf8ToString());
			BytesRef scratch2 = new BytesRef();
			dv.Get(1, scratch2);
			AreEqual("bar!", scratch2.Utf8ToString());
			// check scratch is still valid
			AreEqual("foo!", scratch.Utf8ToString());
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCodecUsesOwnSortedBytesEachTime()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
			var doc = new Document {new SortedDocValuesField("dv", new BytesRef("foo!"))};
		    iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedDocValuesField("dv", new BytesRef("bar!")));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			 
			//assert ireader.Leaves.size() == 1;
			BinaryDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetSortedDocValues
				("dv");
			BytesRef scratch = new BytesRef();
			dv.Get(0, scratch);
			AreEqual("foo!", scratch.Utf8ToString());
			BytesRef scratch2 = new BytesRef();
			dv.Get(1, scratch2);
			AreEqual("bar!", scratch2.Utf8ToString());
			// check scratch is still valid
			AreEqual("foo!", scratch.Utf8ToString());
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDocValuesSimple()
		{
			Directory dir = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			conf.SetMergePolicy(NewLogMergePolicy());
			IndexWriter writer = new IndexWriter(dir, conf);
			for (int i = 0; i < 5; i++)
			{
				var doc = new Document
				{
				    new NumericDocValuesField("docId", i),
				    new TextField("docId", string.Empty + i, Field.Store.NO)
				};
			    writer.AddDocument(doc);
			}
			writer.Commit();
			writer.ForceMerge(1, true);
			writer.Dispose(true);
			DirectoryReader reader = DirectoryReader.Open(dir, 1);
			AreEqual(1, reader.Leaves.Count);
			IndexSearcher searcher = new IndexSearcher(reader);
			BooleanQuery query = new BooleanQuery();
			query.Add(new TermQuery(new Term("docId", "0")), Occur.SHOULD);
			query.Add(new TermQuery(new Term("docId", "1")), Occur.SHOULD);
			query.Add(new TermQuery(new Term("docId", "2")), Occur.SHOULD);
			query.Add(new TermQuery(new Term("docId", "3")), Occur.SHOULD);
			query.Add(new TermQuery(new Term("docId", "4")), Occur.SHOULD);
			TopDocs search = searcher.Search(query, 10);
			AreEqual(5, search.TotalHits);
			ScoreDoc[] ScoreDocs = search.ScoreDocs;
			NumericDocValues docValues = GetOnlySegmentReader(reader).GetNumericDocValues("docId"
				);
			for (int i_1 = 0; i_1 < ScoreDocs.Length; i_1++)
			{
				AreEqual(i_1, ScoreDocs[i_1].Doc);
				AreEqual(i_1, docValues.Get(ScoreDocs[i_1].Doc));
			}
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRandomSortedBytes()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig cfg = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			if (!DefaultCodecSupportsDocsWithField())
			{
				// if the codec doesnt support missing, we expect missing to be mapped to byte[]
				// by the impersonator, but we have to give it a chance to merge them to this
				cfg.SetMergePolicy(NewLogMergePolicy());
			}
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, cfg);
			int numDocs = AtLeast(100);
			BytesRefHash hash = new BytesRefHash();
			IDictionary<string, string> docToString = new Dictionary<string, string>();
			int maxLength = TestUtil.NextInt(Random(), 1, 50);
			for (int i = 0; i < numDocs; i++)
			{
				var doc = new Document {NewTextField("id", string.Empty + i, Field.Store.YES)};
			    string @string = TestUtil.RandomRealisticUnicodeString(Random(), 1, maxLength);
				BytesRef br = new BytesRef(@string);
				doc.Add(new SortedDocValuesField("field", br));
				hash.Add(br);
				docToString[string.Empty + i] = @string;
				w.AddDocument(doc);
			}
			if (Rarely())
			{
				w.Commit();
			}
			int numDocsNoValue = AtLeast(10);
			for (int i_1 = 0; i_1 < numDocsNoValue; i_1++)
			{
				var doc = new Document {NewTextField("id", "noValue", Field.Store.YES)};
			    w.AddDocument(doc);
			}
			if (!DefaultCodecSupportsDocsWithField())
			{
				BytesRef bytesRef = new BytesRef();
				hash.Add(bytesRef);
			}
			// add empty value for the gaps
			if (Rarely())
			{
				w.Commit();
			}
			if (!DefaultCodecSupportsDocsWithField())
			{
				// if the codec doesnt support missing, we expect missing to be mapped to byte[]
				// by the impersonator, but we have to give it a chance to merge them to this
				w.ForceMerge(1);
			}
			for (int i_2 = 0; i_2 < numDocs; i_2++)
			{
				var doc = new Document();
				string id = string.Empty + i_2 + numDocs;
				doc.Add(NewTextField("id", id, Field.Store.YES));
				string @string = TestUtil.RandomRealisticUnicodeString(Random(), 1, maxLength);
				BytesRef br = new BytesRef(@string);
				hash.Add(br);
				docToString[id] = @string;
				doc.Add(new SortedDocValuesField("field", br));
				w.AddDocument(doc);
			}
			w.Commit();
			IndexReader reader = w.GetReader();
			SortedDocValues docValues = MultiDocValues.GetSortedValues(reader, "field");
			int[] sort = hash.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
			BytesRef expected = new BytesRef();
			BytesRef actual = new BytesRef();
			AreEqual(hash.Size, docValues.ValueCount);
			for (int i_3 = 0; i_3 < hash.Size; i_3++)
			{
				hash.Get(sort[i_3], expected);
				docValues.LookupOrd(i_3, actual);
				AreEqual(expected.Utf8ToString(), actual.Utf8ToString());
				int ord = docValues.LookupTerm(expected);
				AreEqual(i_3, ord);
			}
			AtomicReader slowR = SlowCompositeReaderWrapper.Wrap(reader);
			ICollection<KeyValuePair<string, string>> entrySet = docToString;
			foreach (KeyValuePair<string, string> entry in entrySet)
			{
				// pk lookup
				DocsEnum termDocsEnum = slowR.TermDocsEnum(new Term("id", entry.Key));
				int docId = termDocsEnum.NextDoc();
				expected = new BytesRef(entry.Value);
				docValues.Get(docId, actual);
				AreEqual(expected, actual);
			}
			reader.Dispose();
			w.Close();
			dir.Dispose();
		}

		internal abstract class LongProducer
		{
			internal abstract long Next();
		}

		/// <exception cref="System.Exception"></exception>
		private void DoTestNumericsVsStoredFields(long minValue, long maxValue)
		{
			DoTestNumericsVsStoredFields(new AnonymousLongProducer(minValue, maxValue));
		}

		private sealed class AnonymousLongProducer : LongProducer
		{
			public AnonymousLongProducer(long minValue, long maxValue)
			{
				this.minValue = minValue;
				this.maxValue = maxValue;
			}

			internal override long Next()
			{
				return TestUtil.NextLong(LuceneTestCase.Random(), minValue, maxValue);
			}

			private readonly long minValue;

			private readonly long maxValue;
		}

		/// <exception cref="System.Exception"></exception>
		private void DoTestNumericsVsStoredFields(BaseDocValuesFormatTestCase.LongProducer
			 longs)
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
			var doc = new Document();
			Field idField = new StringField("id", string.Empty, Field.Store.NO);
			Field storedField = NewStringField("stored", string.Empty, Field.Store.YES);
			Field dvField = new NumericDocValuesField("dv", 0);
			doc.Add(idField);
			doc.Add(storedField);
			doc.Add(dvField);
			// index some docs
			int numDocs = AtLeast(300);
			// numDocs should be always > 256 so that in case of a codec that optimizes
			// for numbers of values <= 256, all storage layouts are tested
			 
			//assert numDocs > 256;
			for (int i = 0; i < numDocs; i++)
			{
			    idField.StringValue = i.ToString();
				long value = longs.Next();
				storedField.StringValue = Convert.ToString(value);
				dvField.SetLongValue(value);
				writer.AddDocument(doc);
				if (Random().Next(31) == 0)
				{
					writer.Commit();
				}
			}
			// delete some docs
			int numDeletions = Random().Next(numDocs / 10);
			for (int i_1 = 0; i_1 < numDeletions; i_1++)
			{
				int id = Random().Next(numDocs);
				writer.DeleteDocuments(new Term("id", id.ToString()));
			}
			// merge some segments and ensure that at least one of them has more than
			// 256 values
			writer.ForceMerge(numDocs / 256);
			writer.Close();
			// compare
			DirectoryReader ir = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext context in ir.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader);
				NumericDocValues docValues = r.GetNumericDocValues("dv");
				for (int i_2 = 0; i_2 < r.MaxDoc; i_2++)
				{
					long storedValue = long.Parse(r.Document(i_2).Get("stored"));
					AreEqual(storedValue, docValues.Get(i_2));
				}
			}
			ir.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		private void DoTestMissingVsFieldCache(long minValue, long maxValue)
		{
			DoTestMissingVsFieldCache(new AnonymousLongProducer2(minValue, maxValue));
		}

		private sealed class AnonymousLongProducer2 : LongProducer
		{
			public AnonymousLongProducer2(long minValue, long maxValue)
			{
				this.minValue = minValue;
				this.maxValue = maxValue;
			}

			internal override long Next()
			{
				return LuceneTestCase.Random().NextLong(minValue, maxValue);
			}

			private readonly long minValue;

			private readonly long maxValue;
		}

		/// <exception cref="System.Exception"></exception>
		private void DoTestMissingVsFieldCache(BaseDocValuesFormatTestCase.LongProducer longs
			)
		{
			AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField
				());
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
			Field idField = new StringField("id", string.Empty, Field.Store.NO);
			Field indexedField = NewStringField("indexed", string.Empty, Field.Store.NO);
			Field dvField = new NumericDocValuesField("dv", 0);
			// index some docs
			int numDocs = AtLeast(300);
			// numDocs should be always > 256 so that in case of a codec that optimizes
			// for numbers of values <= 256, all storage layouts are tested
			 
			//assert numDocs > 256;
			for (int i = 0; i < numDocs; i++)
			{
				idField.StringValue = i.ToString();
				long value = longs.Next();
				indexedField.StringValue = System.Convert.ToString(value));
				dvField.SetLongValue(value);
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(idField);
				// 1/4 of the time we neglect to add the fields
				if (Random().Next(4) > 0)
				{
					doc.Add(indexedField);
					doc.Add(dvField);
				}
				writer.AddDocument(doc);
				if (Random().Next(31) == 0)
				{
					writer.Commit();
				}
			}
			// delete some docs
			int numDeletions = Random().Next(numDocs / 10);
			for (int i_1 = 0; i_1 < numDeletions; i_1++)
			{
				int id = Random().Next(numDocs);
				writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(id)));
			}
			// merge some segments and ensure that at least one of them has more than
			// 256 values
			writer.ForceMerge(numDocs / 256);
			writer.Close();
			// compare
			DirectoryReader ir = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext context in ir.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader());
				Bits expected = FieldCache.DEFAULT.GetDocsWithField(r, "indexed");
				Bits actual = FieldCache.DEFAULT.GetDocsWithField(r, "dv");
				AssertEquals(expected, actual);
			}
			ir.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBooleanNumericsVsStoredFields()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestNumericsVsStoredFields(0, 1);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestByteNumericsVsStoredFields()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestNumericsVsStoredFields(byte.MinValue, byte.MaxValue);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestByteMissingVsFieldCache()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestMissingVsFieldCache(byte.MinValue, byte.MaxValue);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestShortNumericsVsStoredFields()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestNumericsVsStoredFields(short.MinValue, short.MaxValue);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestShortMissingVsFieldCache()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestMissingVsFieldCache(short.MinValue, short.MaxValue);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIntNumericsVsStoredFields()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestNumericsVsStoredFields(int.MinValue, int.MaxValue);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIntMissingVsFieldCache()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestMissingVsFieldCache(int.MinValue, int.MaxValue);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLongNumericsVsStoredFields()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestNumericsVsStoredFields(long.MinValue, long.MaxValue);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLongMissingVsFieldCache()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestMissingVsFieldCache(long.MinValue, long.MaxValue);
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void DoTestBinaryVsStoredFields(int minLength, int maxLength)
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field idField = new StringField("id", string.Empty, Field.Store.NO);
			Field storedField = new StoredField("stored", new byte[0]);
			Field dvField = new BinaryDocValuesField("dv", new BytesRef());
			doc.Add(idField);
			doc.Add(storedField);
			doc.Add(dvField);
			// index some docs
			int numDocs = AtLeast(300);
			for (int i = 0; i < numDocs; i++)
			{
				idField.StringValue = Sharpen.Extensions.ToString(i));
				int length;
				if (minLength == maxLength)
				{
					length = minLength;
				}
				else
				{
					// fixed length
					length = TestUtil.NextInt(Random(), minLength, maxLength);
				}
				byte[] buffer = new byte[length];
				Random().NextBytes(buffer);
				storedField.SetBytesValue(buffer);
				dvField.SetBytesValue(buffer);
				writer.AddDocument(doc);
				if (Random().Next(31) == 0)
				{
					writer.Commit();
				}
			}
			// delete some docs
			int numDeletions = Random().Next(numDocs / 10);
			for (int i_1 = 0; i_1 < numDeletions; i_1++)
			{
				int id = Random().Next(numDocs);
				writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(id)));
			}
			writer.Close();
			// compare
			DirectoryReader ir = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext context in ir.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader());
				BinaryDocValues docValues = r.GetBinaryDocValues("dv");
				for (int i_2 = 0; i_2 < r.MaxDoc(); i_2++)
				{
					BytesRef binaryValue = r.Document(i_2).GetBinaryValue("stored");
					BytesRef scratch = new BytesRef();
					docValues.Get(i_2, scratch);
					AreEqual(binaryValue, scratch);
				}
			}
			ir.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBinaryFixedLengthVsStoredFields()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				int fixedLength = TestUtil.NextInt(Random(), 0, 10);
				DoTestBinaryVsStoredFields(fixedLength, fixedLength);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBinaryVariableLengthVsStoredFields()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestBinaryVsStoredFields(0, 10);
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void DoTestSortedVsStoredFields(int minLength, int maxLength)
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field idField = new StringField("id", string.Empty, Field.Store.NO);
			Field storedField = new StoredField("stored", new byte[0]);
			Field dvField = new SortedDocValuesField("dv", new BytesRef());
			doc.Add(idField);
			doc.Add(storedField);
			doc.Add(dvField);
			// index some docs
			int numDocs = AtLeast(300);
			for (int i = 0; i < numDocs; i++)
			{
				idField.StringValue = Sharpen.Extensions.ToString(i));
				int length;
				if (minLength == maxLength)
				{
					length = minLength;
				}
				else
				{
					// fixed length
					length = TestUtil.NextInt(Random(), minLength, maxLength);
				}
				byte[] buffer = new byte[length];
				Random().NextBytes(buffer);
				storedField.SetBytesValue(buffer);
				dvField.SetBytesValue(buffer);
				writer.AddDocument(doc);
				if (Random().Next(31) == 0)
				{
					writer.Commit();
				}
			}
			// delete some docs
			int numDeletions = Random().Next(numDocs / 10);
			for (int i_1 = 0; i_1 < numDeletions; i_1++)
			{
				int id = Random().Next(numDocs);
				writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(id)));
			}
			writer.Close();
			// compare
			DirectoryReader ir = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext context in ir.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader());
				BinaryDocValues docValues = r.GetSortedDocValues("dv");
				for (int i_2 = 0; i_2 < r.MaxDoc(); i_2++)
				{
					BytesRef binaryValue = r.Document(i_2).GetBinaryValue("stored");
					BytesRef scratch = new BytesRef();
					docValues.Get(i_2, scratch);
					AreEqual(binaryValue, scratch);
				}
			}
			ir.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		private void DoTestSortedVsFieldCache(int minLength, int maxLength)
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field idField = new StringField("id", string.Empty, Field.Store.NO);
			Field indexedField = new StringField("indexed", string.Empty, Field.Store.NO);
			Field dvField = new SortedDocValuesField("dv", new BytesRef());
			doc.Add(idField);
			doc.Add(indexedField);
			doc.Add(dvField);
			// index some docs
			int numDocs = AtLeast(300);
			for (int i = 0; i < numDocs; i++)
			{
				idField.StringValue = Sharpen.Extensions.ToString(i));
				int length;
				if (minLength == maxLength)
				{
					length = minLength;
				}
				else
				{
					// fixed length
					length = TestUtil.NextInt(Random(), minLength, maxLength);
				}
				string value = TestUtil.RandomSimpleString(Random(), length);
				indexedField.StringValue = value);
				dvField.SetBytesValue(new BytesRef(value));
				writer.AddDocument(doc);
				if (Random().Next(31) == 0)
				{
					writer.Commit();
				}
			}
			// delete some docs
			int numDeletions = Random().Next(numDocs / 10);
			for (int i_1 = 0; i_1 < numDeletions; i_1++)
			{
				int id = Random().Next(numDocs);
				writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(id)));
			}
			writer.Close();
			// compare
			DirectoryReader ir = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext context in ir.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader());
				SortedDocValues expected = FieldCache.DEFAULT.GetTermsIndex(r, "indexed");
				SortedDocValues actual = r.GetSortedDocValues("dv");
				AssertEquals(r.MaxDoc(), expected, actual);
			}
			ir.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortedFixedLengthVsStoredFields()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				int fixedLength = TestUtil.NextInt(Random(), 1, 10);
				DoTestSortedVsStoredFields(fixedLength, fixedLength);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortedFixedLengthVsFieldCache()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				int fixedLength = TestUtil.NextInt(Random(), 1, 10);
				DoTestSortedVsFieldCache(fixedLength, fixedLength);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortedVariableLengthVsFieldCache()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestSortedVsFieldCache(1, 10);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortedVariableLengthVsStoredFields()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestSortedVsStoredFields(1, 10);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedSetOneValue()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory directory = NewDirectory();
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			iwriter.AddDocument(doc);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field"
				);
			dv.SetDocument(0);
			AreEqual(0, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			BytesRef bytes = new BytesRef();
			dv.LookupOrd(0, bytes);
			AreEqual(new BytesRef("hello"), bytes);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedSetTwoFields()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory directory = NewDirectory();
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			doc.Add(new SortedSetDocValuesField("field2", new BytesRef("world")));
			iwriter.AddDocument(doc);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field"
				);
			dv.SetDocument(0);
			AreEqual(0, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			BytesRef bytes = new BytesRef();
			dv.LookupOrd(0, bytes);
			AreEqual(new BytesRef("hello"), bytes);
			dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field2");
			dv.SetDocument(0);
			AreEqual(0, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			dv.LookupOrd(0, bytes);
			AreEqual(new BytesRef("world"), bytes);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedSetTwoDocumentsMerged()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			iwriter.AddDocument(doc);
			iwriter.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field"
				);
			AreEqual(2, dv.ValueCount);
			dv.SetDocument(0);
			AreEqual(0, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			BytesRef bytes = new BytesRef();
			dv.LookupOrd(0, bytes);
			AreEqual(new BytesRef("hello"), bytes);
			dv.SetDocument(1);
			AreEqual(1, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			dv.LookupOrd(1, bytes);
			AreEqual(new BytesRef("world"), bytes);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedSetTwoValues()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory directory = NewDirectory();
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
			iwriter.AddDocument(doc);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field"
				);
			dv.SetDocument(0);
			AreEqual(0, dv.NextOrd());
			AreEqual(1, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			BytesRef bytes = new BytesRef();
			dv.LookupOrd(0, bytes);
			AreEqual(new BytesRef("hello"), bytes);
			dv.LookupOrd(1, bytes);
			AreEqual(new BytesRef("world"), bytes);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedSetTwoValuesUnordered()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory directory = NewDirectory();
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			iwriter.AddDocument(doc);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field"
				);
			dv.SetDocument(0);
			AreEqual(0, dv.NextOrd());
			AreEqual(1, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			BytesRef bytes = new BytesRef();
			dv.LookupOrd(0, bytes);
			AreEqual(new BytesRef("hello"), bytes);
			dv.LookupOrd(1, bytes);
			AreEqual(new BytesRef("world"), bytes);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedSetThreeValuesTwoDocs()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
			iwriter.AddDocument(doc);
			iwriter.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("beer")));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field"
				);
			AreEqual(3, dv.ValueCount);
			dv.SetDocument(0);
			AreEqual(1, dv.NextOrd());
			AreEqual(2, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			dv.SetDocument(1);
			AreEqual(0, dv.NextOrd());
			AreEqual(1, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			BytesRef bytes = new BytesRef();
			dv.LookupOrd(0, bytes);
			AreEqual(new BytesRef("beer"), bytes);
			dv.LookupOrd(1, bytes);
			AreEqual(new BytesRef("hello"), bytes);
			dv.LookupOrd(2, bytes);
			AreEqual(new BytesRef("world"), bytes);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedSetTwoDocumentsLastMissing()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field"
				);
			AreEqual(1, dv.ValueCount);
			dv.SetDocument(0);
			AreEqual(0, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			BytesRef bytes = new BytesRef();
			dv.LookupOrd(0, bytes);
			AreEqual(new BytesRef("hello"), bytes);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedSetTwoDocumentsLastMissingMerge()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			iwriter.AddDocument(doc);
			iwriter.Commit();
			doc = new Lucene.Net.Documents.Document();
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field"
				);
			AreEqual(1, dv.ValueCount);
			dv.SetDocument(0);
			AreEqual(0, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			BytesRef bytes = new BytesRef();
			dv.LookupOrd(0, bytes);
			AreEqual(new BytesRef("hello"), bytes);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedSetTwoDocumentsFirstMissing()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field"
				);
			AreEqual(1, dv.ValueCount);
			dv.SetDocument(1);
			AreEqual(0, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			BytesRef bytes = new BytesRef();
			dv.LookupOrd(0, bytes);
			AreEqual(new BytesRef("hello"), bytes);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedSetTwoDocumentsFirstMissingMerge()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			iwriter.AddDocument(doc);
			iwriter.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field"
				);
			AreEqual(1, dv.ValueCount);
			dv.SetDocument(1);
			AreEqual(0, dv.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, dv.NextOrd());
			BytesRef bytes = new BytesRef();
			dv.LookupOrd(0, bytes);
			AreEqual(new BytesRef("hello"), bytes);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedSetMergeAwayAllValues()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "0", Field.Store.NO));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "1", Field.Store.NO));
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			iwriter.AddDocument(doc);
			iwriter.Commit();
			iwriter.DeleteDocuments(new Term("id", "1"));
			iwriter.ForceMerge(1);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field"
				);
			AreEqual(0, dv.ValueCount);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSortedSetTermsEnum()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("hello")));
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("world")));
			doc.Add(new SortedSetDocValuesField("field", new BytesRef("beer")));
			iwriter.AddDocument(doc);
			DirectoryReader ireader = iwriter.GetReader();
			iwriter.Close();
			SortedSetDocValues dv = GetOnlySegmentReader(ireader).GetSortedSetDocValues("field"
				);
			AreEqual(3, dv.ValueCount);
			TermsEnum termsEnum = dv.TermsEnum;
			// next()
			AreEqual("beer", termsEnum.Next().Utf8ToString());
			AreEqual(0, termsEnum.Ord);
			AreEqual("hello", termsEnum.Next().Utf8ToString());
			AreEqual(1, termsEnum.Ord);
			AreEqual("world", termsEnum.Next().Utf8ToString());
			AreEqual(2, termsEnum.Ord);
			// seekCeil()
			AreEqual(TermsEnum.SeekStatus.NOT_FOUND, termsEnum.SeekCeil
				(new BytesRef("ha!")));
			AreEqual("hello", termsEnum.Term.Utf8ToString());
			AreEqual(1, termsEnum.Ord);
			AreEqual(TermsEnum.SeekStatus.FOUND, termsEnum.SeekCeil(new 
				BytesRef("beer")));
			AreEqual("beer", termsEnum.Term.Utf8ToString());
			AreEqual(0, termsEnum.Ord);
			AreEqual(TermsEnum.SeekStatus.END, termsEnum.SeekCeil(new 
				BytesRef("zzz")));
			// seekExact()
			IsTrue(termsEnum.SeekExact(new BytesRef("beer")));
			AreEqual("beer", termsEnum.Term.Utf8ToString());
			AreEqual(0, termsEnum.Ord);
			IsTrue(termsEnum.SeekExact(new BytesRef("hello")));
			AreEqual("hello", termsEnum.Term.Utf8ToString());
			AreEqual(1, termsEnum.Ord);
			IsTrue(termsEnum.SeekExact(new BytesRef("world")));
			AreEqual("world", termsEnum.Term.Utf8ToString());
			AreEqual(2, termsEnum.Ord);
			IsFalse(termsEnum.SeekExact(new BytesRef("bogus")));
			// seek(ord)
			termsEnum.SeekExact(0);
			AreEqual("beer", termsEnum.Term.Utf8ToString());
			AreEqual(0, termsEnum.Ord);
			termsEnum.SeekExact(1);
			AreEqual("hello", termsEnum.Term.Utf8ToString());
			AreEqual(1, termsEnum.Ord);
			termsEnum.SeekExact(2);
			AreEqual("world", termsEnum.Term.Utf8ToString());
			AreEqual(2, termsEnum.Ord);
			ireader.Dispose();
			directory.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		private void DoTestSortedSetVsStoredFields(int minLength, int maxLength, int maxValuesPerDoc
			)
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
			// index some docs
			int numDocs = AtLeast(300);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				Field idField = new StringField("id", Sharpen.Extensions.ToString(i), Field.Store
					.NO);
				doc.Add(idField);
				int length;
				if (minLength == maxLength)
				{
					length = minLength;
				}
				else
				{
					// fixed length
					length = TestUtil.NextInt(Random(), minLength, maxLength);
				}
				int numValues = TestUtil.NextInt(Random(), 0, maxValuesPerDoc);
				// create a random set of strings
				ICollection<string> values = new TreeSet<string>();
				for (int v = 0; v < numValues; v++)
				{
					values.AddItem(TestUtil.RandomSimpleString(Random(), length));
				}
				// add ordered to the stored field
				foreach (string v_1 in values)
				{
					doc.Add(new StoredField("stored", v_1));
				}
				// add in any order to the dv field
				AList<string> unordered = new AList<string>(values);
				Sharpen.Collections.Shuffle(unordered, Random());
				foreach (string v_2 in unordered)
				{
					doc.Add(new SortedSetDocValuesField("dv", new BytesRef(v_2)));
				}
				writer.AddDocument(doc);
				if (Random().Next(31) == 0)
				{
					writer.Commit();
				}
			}
			// delete some docs
			int numDeletions = Random().Next(numDocs / 10);
			for (int i_1 = 0; i_1 < numDeletions; i_1++)
			{
				int id = Random().Next(numDocs);
				writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(id)));
			}
			writer.Close();
			// compare
			DirectoryReader ir = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext context in ir.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader());
				SortedSetDocValues docValues = r.GetSortedSetDocValues("dv");
				BytesRef scratch = new BytesRef();
				for (int i_2 = 0; i_2 < r.MaxDoc(); i_2++)
				{
					string[] stringValues = r.Document(i_2).GetValues("stored");
					if (docValues != null)
					{
						docValues.SetDocument(i_2);
					}
					for (int j = 0; j < stringValues.Length; j++)
					{
						 
						//assert docValues != null;
						long ord = docValues.NextOrd();
						 
						//assert ord != NO_MORE_ORDS;
						docValues.LookupOrd(ord, scratch);
						AreEqual(stringValues[j], scratch.Utf8ToString());
					}
				}
			}
			 
			//assert docValues == null || docValues.nextOrd() == NO_MORE_ORDS;
			ir.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortedSetFixedLengthVsStoredFields()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				int fixedLength = TestUtil.NextInt(Random(), 1, 10);
				DoTestSortedSetVsStoredFields(fixedLength, fixedLength, 16);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortedSetVariableLengthVsStoredFields()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestSortedSetVsStoredFields(1, 10, 16);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortedSetFixedLengthSingleValuedVsStoredFields()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				int fixedLength = TestUtil.NextInt(Random(), 1, 10);
				DoTestSortedSetVsStoredFields(fixedLength, fixedLength, 1);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortedSetVariableLengthSingleValuedVsStoredFields()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestSortedSetVsStoredFields(1, 10, 1);
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertEquals(Bits expected, Bits actual)
		{
			AreEqual(expected.Length(), actual.Length());
			for (int i = 0; i < expected.Length(); i++)
			{
				AreEqual(expected.Get(i), actual.Get(i));
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertEquals(int maxDoc, SortedDocValues expected, SortedDocValues actual
			)
		{
			AssertEquals(maxDoc, new SingletonSortedSetDocValues(expected), new SingletonSortedSetDocValues
				(actual));
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertEquals(int maxDoc, SortedSetDocValues expected, SortedSetDocValues
			 actual)
		{
			// can be null for the segment if no docs actually had any SortedDocValues
			// in this case FC.getDocTermsOrds returns EMPTY
			if (actual == null)
			{
				AreEqual(DocValues.EMPTY_SORTED_SET, expected);
				return;
			}
			AreEqual(expected.ValueCount, actual.ValueCount);
			// compare ord lists
			for (int i = 0; i < maxDoc; i++)
			{
				expected.SetDocument(i);
				actual.SetDocument(i);
				long expectedOrd;
				while ((expectedOrd = expected.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
				{
					AreEqual(expectedOrd, actual.NextOrd());
				}
				AreEqual(SortedSetDocValues.NO_MORE_ORDS, actual.NextOrd()
					);
			}
			// compare ord dictionary
			BytesRef expectedBytes = new BytesRef();
			BytesRef actualBytes = new BytesRef();
			for (long i_1 = 0; i_1 < expected.ValueCount; i_1++)
			{
				expected.LookupTerm(expectedBytes);
				actual.LookupTerm(actualBytes);
				AreEqual(expectedBytes, actualBytes);
			}
			// compare termsenum
			AssertEquals(expected.ValueCount, expected.TermsEnum, actual.TermsEnum);
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertEquals(long numOrds, TermsEnum expected, TermsEnum actual)
		{
			BytesRef @ref;
			// sequential next() through all terms
			while ((@ref = expected.Next()) != null)
			{
				AreEqual(@ref, actual.Next());
				AreEqual(expected.Ord, actual.Ord);
				AreEqual(expected.Term, actual.Term);
			}
			IsNull(actual.Next());
			// sequential seekExact(ord) through all terms
			for (long i = 0; i < numOrds; i++)
			{
				expected.SeekExact(i);
				actual.SeekExact(i);
				AreEqual(expected.Ord, actual.Ord);
				AreEqual(expected.Term, actual.Term);
			}
			// sequential seekExact(BytesRef) through all terms
			for (long i_1 = 0; i_1 < numOrds; i_1++)
			{
				expected.SeekExact(i_1);
				IsTrue(actual.SeekExact(expected.Term));
				AreEqual(expected.Ord, actual.Ord);
				AreEqual(expected.Term, actual.Term);
			}
			// sequential seekCeil(BytesRef) through all terms
			for (long i_2 = 0; i_2 < numOrds; i_2++)
			{
				expected.SeekExact(i_2);
				AreEqual(TermsEnum.SeekStatus.FOUND, actual.SeekCeil(expected
					.Term));
				AreEqual(expected.Ord, actual.Ord);
				AreEqual(expected.Term, actual.Term);
			}
			// random seekExact(ord)
			for (long i_3 = 0; i_3 < numOrds; i_3++)
			{
				long randomOrd = TestUtil.NextLong(Random(), 0, numOrds - 1);
				expected.SeekExact(randomOrd);
				actual.SeekExact(randomOrd);
				AreEqual(expected.Ord, actual.Ord);
				AreEqual(expected.Term, actual.Term);
			}
			// random seekExact(BytesRef)
			for (long i_4 = 0; i_4 < numOrds; i_4++)
			{
				long randomOrd = TestUtil.NextLong(Random(), 0, numOrds - 1);
				expected.SeekExact(randomOrd);
				actual.SeekExact(expected.Term);
				AreEqual(expected.Ord, actual.Ord);
				AreEqual(expected.Term, actual.Term);
			}
			// random seekCeil(BytesRef)
			for (long i_5 = 0; i_5 < numOrds; i_5++)
			{
				BytesRef target = new BytesRef(TestUtil.RandomUnicodeString(Random()));
				TermsEnum.SeekStatus expectedStatus = expected.SeekCeil(target);
				AreEqual(expectedStatus, actual.SeekCeil(target));
				if (expectedStatus != TermsEnum.SeekStatus.END)
				{
					AreEqual(expected.Ord, actual.Ord);
					AreEqual(expected.Term, actual.Term);
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void DoTestSortedSetVsUninvertedField(int minLength, int maxLength)
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
			// index some docs
			int numDocs = AtLeast(300);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				Field idField = new StringField("id", Sharpen.Extensions.ToString(i), Field.Store
					.NO);
				doc.Add(idField);
				int length;
				if (minLength == maxLength)
				{
					length = minLength;
				}
				else
				{
					// fixed length
					length = TestUtil.NextInt(Random(), minLength, maxLength);
				}
				int numValues = Random().Next(17);
				// create a random list of strings
				IList<string> values = new AList<string>();
				for (int v = 0; v < numValues; v++)
				{
					values.AddItem(TestUtil.RandomSimpleString(Random(), length));
				}
				// add in any order to the indexed field
				AList<string> unordered = new AList<string>(values);
				Sharpen.Collections.Shuffle(unordered, Random());
				foreach (string v_1 in values)
				{
					doc.Add(NewStringField("indexed", v_1, Field.Store.NO));
				}
				// add in any order to the dv field
				AList<string> unordered2 = new AList<string>(values);
				Sharpen.Collections.Shuffle(unordered2, Random());
				foreach (string v_2 in unordered2)
				{
					doc.Add(new SortedSetDocValuesField("dv", new BytesRef(v_2)));
				}
				writer.AddDocument(doc);
				if (Random().Next(31) == 0)
				{
					writer.Commit();
				}
			}
			// delete some docs
			int numDeletions = Random().Next(numDocs / 10);
			for (int i_1 = 0; i_1 < numDeletions; i_1++)
			{
				int id = Random().Next(numDocs);
				writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(id)));
			}
			// compare per-segment
			DirectoryReader ir = writer.GetReader();
			foreach (AtomicReaderContext context in ir.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader());
				SortedSetDocValues expected = FieldCache.DEFAULT.GetDocTermOrds(r, "indexed");
				SortedSetDocValues actual = r.GetSortedSetDocValues("dv");
				AssertEquals(r.MaxDoc(), expected, actual);
			}
			ir.Close();
			writer.ForceMerge(1);
			// now compare again after the merge
			ir = writer.GetReader();
			AtomicReader ar = GetOnlySegmentReader(ir);
			SortedSetDocValues expected_1 = FieldCache.DEFAULT.GetDocTermOrds(ar, "indexed");
			SortedSetDocValues actual_1 = ar.GetSortedSetDocValues("dv");
			AssertEquals(ir.MaxDoc(), expected_1, actual_1);
			ir.Close();
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortedSetFixedLengthVsUninvertedField()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				int fixedLength = TestUtil.NextInt(Random(), 1, 10);
				DoTestSortedSetVsUninvertedField(fixedLength, fixedLength);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSortedSetVariableLengthVsUninvertedField()
		{
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				DoTestSortedSetVsUninvertedField(1, 10);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestGCDCompression()
		{
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				long min = -(((long)Random().Next(1 << 30)) << 32);
				long mul = Random().Next() & unchecked((long)(0xFFFFFFFFL));
				BaseDocValuesFormatTestCase.LongProducer longs = new _LongProducer_2445(min, mul);
				DoTestNumericsVsStoredFields(longs);
			}
		}

		private sealed class _LongProducer_2445 : BaseDocValuesFormatTestCase.LongProducer
		{
			public _LongProducer_2445(long min, long mul)
			{
				this.min = min;
				this.mul = mul;
			}

			internal override long Next()
			{
				return min + mul * LuceneTestCase.Random().Next(1 << 20);
			}

			private readonly long min;

			private readonly long mul;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestZeros()
		{
			DoTestNumericsVsStoredFields(0, 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestZeroOrMin()
		{
			// try to make GCD compression fail if the format did not anticipate that
			// the GCD of 0 and MIN_VALUE is negative
			int numIterations = AtLeast(1);
			for (int i = 0; i < numIterations; i++)
			{
				BaseDocValuesFormatTestCase.LongProducer longs = new _LongProducer_2464();
				DoTestNumericsVsStoredFields(longs);
			}
		}

		private sealed class _LongProducer_2464 : BaseDocValuesFormatTestCase.LongProducer
		{
			public _LongProducer_2464()
			{
			}

			internal override long Next()
			{
				return LuceneTestCase.Random().NextBoolean() ? 0 : long.MinValue;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTwoNumbersOneMissing()
		{
			AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField
				());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "0", Field.Store.YES));
			doc.Add(new NumericDocValuesField("dv1", 0));
			iw.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "1", Field.Store.YES));
			iw.AddDocument(doc);
			iw.ForceMerge(1);
			iw.Close();
			IndexReader ir = DirectoryReader.Open(directory);
			AreEqual(1, ir.Leaves.Count);
			AtomicReader ar = ((AtomicReader)ir.Leaves[0].Reader);
			NumericDocValues dv = ar.GetNumericDocValues("dv1");
			AreEqual(0, dv.Get(0));
			AreEqual(0, dv.Get(1));
			IBits docsWithField = ar.GetDocsWithField("dv1");
			IsTrue(docsWithField.Get(0));
			IsFalse(docsWithField.Get(1));
			ir.Close();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTwoNumbersOneMissingWithMerging()
		{
			AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField
				());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "0", Field.Store.YES));
			doc.Add(new NumericDocValuesField("dv1", 0));
			iw.AddDocument(doc);
			iw.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "1", Field.Store.YES));
			iw.AddDocument(doc);
			iw.ForceMerge(1);
			iw.Close();
			IndexReader ir = DirectoryReader.Open(directory);
			AreEqual(1, ir.Leaves.Count);
			AtomicReader ar = ((AtomicReader)ir.Leaves[0].Reader);
			NumericDocValues dv = ar.GetNumericDocValues("dv1");
			AreEqual(0, dv.Get(0));
			AreEqual(0, dv.Get(1));
			Bits docsWithField = ar.GetDocsWithField("dv1");
			IsTrue(docsWithField.Get(0));
			IsFalse(docsWithField.Get(1));
			ir.Close();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestThreeNumbersOneMissingWithMerging()
		{
			AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField
				());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "0", Field.Store.YES));
			doc.Add(new NumericDocValuesField("dv1", 0));
			iw.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "1", Field.Store.YES));
			iw.AddDocument(doc);
			iw.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "2", Field.Store.YES));
			doc.Add(new NumericDocValuesField("dv1", 5));
			iw.AddDocument(doc);
			iw.ForceMerge(1);
			iw.Close();
			IndexReader ir = DirectoryReader.Open(directory);
			AreEqual(1, ir.Leaves.Count);
			AtomicReader ar = ((AtomicReader)ir.Leaves[0].Reader);
			NumericDocValues dv = ar.GetNumericDocValues("dv1");
			AreEqual(0, dv.Get(0));
			AreEqual(0, dv.Get(1));
			AreEqual(5, dv.Get(2));
			Bits docsWithField = ar.GetDocsWithField("dv1");
			IsTrue(docsWithField.Get(0));
			IsFalse(docsWithField.Get(1));
			IsTrue(docsWithField.Get(2));
			ir.Close();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTwoBytesOneMissing()
		{
			AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField
				());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "0", Field.Store.YES));
			doc.Add(new BinaryDocValuesField("dv1", new BytesRef()));
			iw.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "1", Field.Store.YES));
			iw.AddDocument(doc);
			iw.ForceMerge(1);
			iw.Close();
			IndexReader ir = DirectoryReader.Open(directory);
			AreEqual(1, ir.Leaves.Count);
			AtomicReader ar = ((AtomicReader)ir.Leaves[0].Reader);
			BinaryDocValues dv = ar.GetBinaryDocValues("dv1");
			BytesRef @ref = new BytesRef();
			dv.Get(0, @ref);
			AreEqual(new BytesRef(), @ref);
			dv.Get(1, @ref);
			AreEqual(new BytesRef(), @ref);
			Bits docsWithField = ar.GetDocsWithField("dv1");
			IsTrue(docsWithField.Get(0));
			IsFalse(docsWithField.Get(1));
			ir.Close();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTwoBytesOneMissingWithMerging()
		{
			AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField
				());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "0", Field.Store.YES));
			doc.Add(new BinaryDocValuesField("dv1", new BytesRef()));
			iw.AddDocument(doc);
			iw.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "1", Field.Store.YES));
			iw.AddDocument(doc);
			iw.ForceMerge(1);
			iw.Close();
			IndexReader ir = DirectoryReader.Open(directory);
			AreEqual(1, ir.Leaves.Count);
			AtomicReader ar = ((AtomicReader)ir.Leaves[0].Reader);
			BinaryDocValues dv = ar.GetBinaryDocValues("dv1");
			BytesRef @ref = new BytesRef();
			dv.Get(0, @ref);
			AreEqual(new BytesRef(), @ref);
			dv.Get(1, @ref);
			AreEqual(new BytesRef(), @ref);
			Bits docsWithField = ar.GetDocsWithField("dv1");
			IsTrue(docsWithField.Get(0));
			IsFalse(docsWithField.Get(1));
			ir.Close();
			directory.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestThreeBytesOneMissingWithMerging()
		{
			AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField
				());
			Directory directory = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			conf.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("id", "0", Field.Store.YES));
			doc.Add(new BinaryDocValuesField("dv1", new BytesRef()));
			iw.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "1", Field.Store.YES));
			iw.AddDocument(doc);
			iw.Commit();
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("id", "2", Field.Store.YES));
			doc.Add(new BinaryDocValuesField("dv1", new BytesRef("boo")));
			iw.AddDocument(doc);
			iw.ForceMerge(1);
			iw.Close();
			IndexReader ir = DirectoryReader.Open(directory);
			AreEqual(1, ir.Leaves.Count);
			AtomicReader ar = ((AtomicReader)ir.Leaves[0].Reader);
			BinaryDocValues dv = ar.GetBinaryDocValues("dv1");
			BytesRef @ref = new BytesRef();
			dv.Get(0, @ref);
			AreEqual(new BytesRef(), @ref);
			dv.Get(1, @ref);
			AreEqual(new BytesRef(), @ref);
			dv.Get(2, @ref);
			AreEqual(new BytesRef("boo"), @ref);
			Bits docsWithField = ar.GetDocsWithField("dv1");
			IsTrue(docsWithField.Get(0));
			IsFalse(docsWithField.Get(1));
			IsTrue(docsWithField.Get(2));
			ir.Close();
			directory.Dispose();
		}

		// LUCENE-4853
		/// <exception cref="System.Exception"></exception>
		public virtual void TestHugeBinaryValues()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			// FSDirectory because SimpleText will consume gobbs of
			// space when storing big binary values:
			Directory d = NewFSDirectory(CreateTempDir("hugeBinaryValues"));
			bool doFixed = Random().NextBoolean();
			int numDocs;
			int fixedLength = 0;
			if (doFixed)
			{
				// Sometimes make all values fixed length since some
				// codecs have different code paths for this:
				numDocs = TestUtil.NextInt(Random(), 10, 20);
				fixedLength = TestUtil.NextInt(Random(), 65537, 256 * 1024);
			}
			else
			{
				numDocs = TestUtil.NextInt(Random(), 100, 200);
			}
			IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer
				));
			IList<byte[]> docBytes = new AList<byte[]>();
			long totalBytes = 0;
			for (int docID = 0; docID < numDocs; docID++)
			{
				// we don't use RandomIndexWriter because it might add
				// more docvalues than we expect !!!!
				// Must be > 64KB in size to ensure more than 2 pages in
				// PagedBytes would be needed:
				int numBytes;
				if (doFixed)
				{
					numBytes = fixedLength;
				}
				else
				{
					if (docID == 0 || Random().Next(5) == 3)
					{
						numBytes = TestUtil.NextInt(Random(), 65537, 3 * 1024 * 1024);
					}
					else
					{
						numBytes = TestUtil.NextInt(Random(), 1, 1024 * 1024);
					}
				}
				totalBytes += numBytes;
				if (totalBytes > 5 * 1024 * 1024)
				{
					break;
				}
				byte[] bytes = new byte[numBytes];
				Random().NextBytes(bytes);
				docBytes.AddItem(bytes);
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				BytesRef b = new BytesRef(bytes);
				b.length = bytes.Length;
				doc.Add(new BinaryDocValuesField("field", b));
				doc.Add(new StringField("id", string.Empty + docID, Field.Store.YES));
				try
				{
					w.AddDocument(doc);
				}
				catch (ArgumentException iae)
				{
					if (iae.Message.IndexOf("is too large") == -1)
					{
						throw;
					}
					else
					{
						// OK: some codecs can't handle binary DV > 32K
						IsFalse(CodecAcceptsHugeBinaryValues("field"));
						w.Rollback();
						d.Close();
						return;
					}
				}
			}
			DirectoryReader r;
			try
			{
				r = w.GetReader();
			}
			catch (ArgumentException iae)
			{
				if (iae.Message.IndexOf("is too large") == -1)
				{
					throw;
				}
				else
				{
					IsFalse(CodecAcceptsHugeBinaryValues("field"));
					// OK: some codecs can't handle binary DV > 32K
					w.Rollback();
					d.Close();
					return;
				}
			}
			w.Close();
			AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r);
			BinaryDocValues s = FieldCache.DEFAULT.GetTerms(ar, "field", false);
			for (int docID_1 = 0; docID_1 < docBytes.Count; docID_1++)
			{
				Lucene.Net.Documents.Document doc = ar.Document(docID_1);
				BytesRef bytes = new BytesRef();
				s.Get(docID_1, bytes);
				byte[] expected = docBytes[System.Convert.ToInt32(doc.Get("id"))];
				AreEqual(expected.Length, bytes.length);
				AreEqual(new BytesRef(expected), bytes);
			}
			IsTrue(CodecAcceptsHugeBinaryValues("field"));
			ar.Close();
			d.Close();
		}

		// TODO: get this out of here and into the deprecated codecs (4.0, 4.2)
		/// <exception cref="System.Exception"></exception>
		public virtual void TestHugeBinaryValueLimit()
		{
			// We only test DVFormats that have a limit
			AssumeFalse("test requires codec with limits on max binary field length", CodecAcceptsHugeBinaryValues
				("field"));
			Analyzer analyzer = new MockAnalyzer(Random());
			// FSDirectory because SimpleText will consume gobbs of
			// space when storing big binary values:
			Directory d = NewFSDirectory(CreateTempDir("hugeBinaryValues"));
			bool doFixed = Random().NextBoolean();
			int numDocs;
			int fixedLength = 0;
			if (doFixed)
			{
				// Sometimes make all values fixed length since some
				// codecs have different code paths for this:
				numDocs = TestUtil.NextInt(Random(), 10, 20);
				fixedLength = Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH;
			}
			else
			{
				numDocs = TestUtil.NextInt(Random(), 100, 200);
			}
			IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer
				));
			IList<byte[]> docBytes = new List<byte[]>();
			long totalBytes = 0;
			for (int docID = 0; docID < numDocs; docID++)
			{
				// we don't use RandomIndexWriter because it might add
				// more docvalues than we expect !!!!
				// Must be > 64KB in size to ensure more than 2 pages in
				// PagedBytes would be needed:
				int numBytes;
				if (doFixed)
				{
					numBytes = fixedLength;
				}
				else
				{
					if (docID == 0 || Random().Next(5) == 3)
					{
						numBytes = Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH;
					}
					else
					{
						numBytes = TestUtil.NextInt(Random(), 1, Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH
							);
					}
				}
				totalBytes += numBytes;
				if (totalBytes > 5 * 1024 * 1024)
				{
					break;
				}
				byte[] bytes = new byte[numBytes];
				Random().NextBytes(bytes);
				docBytes.Add(bytes);
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				BytesRef b = new BytesRef(bytes);
				b.length = bytes.Length;
				doc.Add(new BinaryDocValuesField("field", b));
				doc.Add(new StringField("id", string.Empty + docID, Field.Store.YES));
				w.AddDocument(doc);
			}
			DirectoryReader r = w.Reader();
			w.Close();
			AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r);
			BinaryDocValues s = FieldCache.DEFAULT.GetTerms(ar, "field", false);
			for (int docID_1 = 0; docID_1 < docBytes.Count; docID_1++)
			{
				Lucene.Net.Documents.Document doc = ar.Document(docID_1);
				BytesRef bytes = new BytesRef();
				s.Get(docID_1, bytes);
				byte[] expected = docBytes[System.Convert.ToInt32(doc.Get("id"))];
				AreEqual(expected.Length, bytes.length);
				AreEqual(new BytesRef(expected), bytes);
			}
			ar.Close();
			d.Close();
		}

		/// <summary>Tests dv against stored fields with threads (binary/numeric/sorted, no missing)
		/// 	</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestThreads()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field idField = new StringField("id", string.Empty, Field.Store.NO);
			Field storedBinField = new StoredField("storedBin", new byte[0]);
			Field dvBinField = new BinaryDocValuesField("dvBin", new BytesRef());
			Field dvSortedField = new SortedDocValuesField("dvSorted", new BytesRef());
			Field storedNumericField = new StoredField("storedNum", string.Empty);
			Field dvNumericField = new NumericDocValuesField("dvNum", 0);
			doc.Add(idField);
			doc.Add(storedBinField);
			doc.Add(dvBinField);
			doc.Add(dvSortedField);
			doc.Add(storedNumericField);
			doc.Add(dvNumericField);
			// index some docs
			int numDocs = AtLeast(300);
			for (int i = 0; i < numDocs; i++)
			{
				idField.StringValue = Sharpen.Extensions.ToString(i));
				int length = TestUtil.NextInt(Random(), 0, 8);
				byte[] buffer = new byte[length];
				Random().NextBytes(buffer);
				storedBinField.SetBytesValue(buffer);
				dvBinField.SetBytesValue(buffer);
				dvSortedField.SetBytesValue(buffer);
				long numericValue = Random().NextLong();
				storedNumericField.StringValue = System.Convert.ToString(numericValue));
				dvNumericField.SetLongValue(numericValue);
				writer.AddDocument(doc);
				if (Random().Next(31) == 0)
				{
					writer.Commit();
				}
			}
			// delete some docs
			int numDeletions = Random().Next(numDocs / 10);
			for (int i_1 = 0; i_1 < numDeletions; i_1++)
			{
				int id = Random().Next(numDocs);
				writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(id)));
			}
			writer.Close();
			// compare
			DirectoryReader ir = DirectoryReader.Open(dir);
			int numThreads = TestUtil.NextInt(Random(), 2, 7);
			Sharpen.Thread[] threads = new Sharpen.Thread[numThreads];
			CountDownLatch startingGun = new CountDownLatch(1);
			for (int i_2 = 0; i_2 < threads.Length; i_2++)
			{
				threads[i_2] = new _Thread_2893(startingGun, ir);
				threads[i_2].Start();
			}
			startingGun.CountDown();
			foreach (Sharpen.Thread t in threads)
			{
				t.Join();
			}
			ir.Close();
			dir.Close();
		}

		private sealed class _Thread_2893 : Sharpen.Thread
		{
			public _Thread_2893(CountDownLatch startingGun, DirectoryReader ir)
			{
				this.startingGun = startingGun;
				this.ir = ir;
			}

			public override void Run()
			{
				try
				{
					startingGun.Await();
					foreach (AtomicReaderContext context in ir.Leaves)
					{
						AtomicReader r = ((AtomicReader)context.Reader());
						BinaryDocValues binaries = r.GetBinaryDocValues("dvBin");
						SortedDocValues sorted = r.GetSortedDocValues("dvSorted");
						NumericDocValues numerics = r.GetNumericDocValues("dvNum");
						for (int j = 0; j < r.MaxDoc(); j++)
						{
							BytesRef binaryValue = r.Document(j).GetBinaryValue("storedBin");
							BytesRef scratch = new BytesRef();
							binaries.Get(j, scratch);
							AreEqual(binaryValue, scratch);
							sorted.Get(j, scratch);
							AreEqual(binaryValue, scratch);
							string expected = r.Document(j).Get("storedNum");
							AreEqual(long.Parse(expected), numerics.Get(j));
						}
					}
					TestUtil.CheckReader(ir);
				}
				catch (Exception e)
				{
					throw new RuntimeException(e);
				}
			}

			private readonly CountDownLatch startingGun;

			private readonly DirectoryReader ir;
		}

		/// <summary>Tests dv against stored fields with threads (all types + missing)</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestThreads2()
		{
			AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField
				());
			AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
			Field idField = new StringField("id", string.Empty, Field.Store.NO);
			Field storedBinField = new StoredField("storedBin", new byte[0]);
			Field dvBinField = new BinaryDocValuesField("dvBin", new BytesRef());
			Field dvSortedField = new SortedDocValuesField("dvSorted", new BytesRef());
			Field storedNumericField = new StoredField("storedNum", string.Empty);
			Field dvNumericField = new NumericDocValuesField("dvNum", 0);
			// index some docs
			int numDocs = AtLeast(300);
			for (int i = 0; i < numDocs; i++)
			{
				idField.StringValue = Sharpen.Extensions.ToString(i));
				int length = TestUtil.NextInt(Random(), 0, 8);
				byte[] buffer = new byte[length];
				Random().NextBytes(buffer);
				storedBinField.SetBytesValue(buffer);
				dvBinField.SetBytesValue(buffer);
				dvSortedField.SetBytesValue(buffer);
				long numericValue = Random().NextLong();
				storedNumericField.StringValue = System.Convert.ToString(numericValue));
				dvNumericField.SetLongValue(numericValue);
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(idField);
				if (Random().Next(4) > 0)
				{
					doc.Add(storedBinField);
					doc.Add(dvBinField);
					doc.Add(dvSortedField);
				}
				if (Random().Next(4) > 0)
				{
					doc.Add(storedNumericField);
					doc.Add(dvNumericField);
				}
				int numSortedSetFields = Random().Next(3);
				ICollection<string> values = new TreeSet<string>();
				for (int j = 0; j < numSortedSetFields; j++)
				{
					values.AddItem(TestUtil.RandomSimpleString(Random()));
				}
				foreach (string v in values)
				{
					doc.Add(new SortedSetDocValuesField("dvSortedSet", new BytesRef(v)));
					doc.Add(new StoredField("storedSortedSet", v));
				}
				writer.AddDocument(doc);
				if (Random().Next(31) == 0)
				{
					writer.Commit();
				}
			}
			// delete some docs
			int numDeletions = Random().Next(numDocs / 10);
			for (int i_1 = 0; i_1 < numDeletions; i_1++)
			{
				int id = Random().Next(numDocs);
				writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(id)));
			}
			writer.Close();
			// compare
			DirectoryReader ir = DirectoryReader.Open(dir);
			int numThreads = TestUtil.NextInt(Random(), 2, 7);
			Sharpen.Thread[] threads = new Sharpen.Thread[numThreads];
			CountDownLatch startingGun = new CountDownLatch(1);
			for (int i_2 = 0; i_2 < threads.Length; i_2++)
			{
				threads[i_2] = new _Thread_2998(startingGun, ir);
				threads[i_2].Start();
			}
			startingGun.CountDown();
			foreach (Sharpen.Thread t in threads)
			{
				t.Join();
			}
			ir.Close();
			dir.Close();
		}

		private sealed class _Thread_2998 : Sharpen.Thread
		{
			public _Thread_2998(CountDownLatch startingGun, DirectoryReader ir)
			{
				this.startingGun = startingGun;
				this.ir = ir;
			}

			public override void Run()
			{
				try
				{
					startingGun.Await();
					foreach (AtomicReaderContext context in ir.Leaves)
					{
						AtomicReader r = ((AtomicReader)context.Reader());
						BinaryDocValues binaries = r.GetBinaryDocValues("dvBin");
						Bits binaryBits = r.GetDocsWithField("dvBin");
						SortedDocValues sorted = r.GetSortedDocValues("dvSorted");
						Bits sortedBits = r.GetDocsWithField("dvSorted");
						NumericDocValues numerics = r.GetNumericDocValues("dvNum");
						Bits numericBits = r.GetDocsWithField("dvNum");
						SortedSetDocValues sortedSet = r.GetSortedSetDocValues("dvSortedSet");
						Bits sortedSetBits = r.GetDocsWithField("dvSortedSet");
						for (int j = 0; j < r.MaxDoc(); j++)
						{
							BytesRef binaryValue = r.Document(j).GetBinaryValue("storedBin");
							if (binaryValue != null)
							{
								if (binaries != null)
								{
									BytesRef scratch = new BytesRef();
									binaries.Get(j, scratch);
									AreEqual(binaryValue, scratch);
									sorted.Get(j, scratch);
									AreEqual(binaryValue, scratch);
									IsTrue(binaryBits.Get(j));
									IsTrue(sortedBits.Get(j));
								}
							}
							else
							{
								if (binaries != null)
								{
									IsFalse(binaryBits.Get(j));
									IsFalse(sortedBits.Get(j));
									AreEqual(-1, sorted.GetOrd(j));
								}
							}
							string number = r.Document(j).Get("storedNum");
							if (number != null)
							{
								if (numerics != null)
								{
									AreEqual(long.Parse(number), numerics.Get(j));
								}
							}
							else
							{
								if (numerics != null)
								{
									IsFalse(numericBits.Get(j));
									AreEqual(0, numerics.Get(j));
								}
							}
							string[] values = r.Document(j).GetValues("storedSortedSet");
							if (values.Length > 0)
							{
								IsNotNull(sortedSet);
								sortedSet.SetDocument(j);
								for (int k = 0; k < values.Length; k++)
								{
									long ord = sortedSet.NextOrd();
									IsTrue(ord != SortedSetDocValues.NO_MORE_ORDS);
									BytesRef value = new BytesRef();
									sortedSet.LookupOrd(ord, value);
									AreEqual(values[k], value.Utf8ToString());
								}
								AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd
									());
								IsTrue(sortedSetBits.Get(j));
							}
							else
							{
								if (sortedSet != null)
								{
									sortedSet.SetDocument(j);
									AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd
										());
									IsFalse(sortedSetBits.Get(j));
								}
							}
						}
					}
					TestUtil.CheckReader(ir);
				}
				catch (Exception e)
				{
					throw new RuntimeException(e);
				}
			}

			private readonly CountDownLatch startingGun;

			private readonly DirectoryReader ir;
		}

		// LUCENE-5218
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmptyBinaryValueOnPageSizes()
		{
			// Test larger and larger power-of-two sized values,
			// followed by empty string value:
			for (int i = 0; i < 20; i++)
			{
				if (i > 14 && CodecAcceptsHugeBinaryValues("field") == false)
				{
					break;
				}
				Directory dir = NewDirectory();
				RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
				BytesRef bytes = new BytesRef();
				bytes.bytes = new byte[1 << i];
				bytes.length = 1 << i;
				for (int j = 0; j < 4; j++)
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					doc.Add(new BinaryDocValuesField("field", bytes));
					w.AddDocument(doc);
				}
				Lucene.Net.Documents.Document doc_1 = new Lucene.Net.Documents.Document
					();
				doc_1.Add(new StoredField("id", "5"));
				doc_1.Add(new BinaryDocValuesField("field", new BytesRef()));
				w.AddDocument(doc_1);
				IndexReader r = w.GetReader();
				w.Close();
				AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r);
				BinaryDocValues values = ar.GetBinaryDocValues("field");
				BytesRef result = new BytesRef();
				for (int j_1 = 0; j_1 < 5; j_1++)
				{
					values.Get(0, result);
					IsTrue(result.length == 0 || result.length == 1 << i);
				}
				ar.Close();
				dir.Close();
			}
		}

		protected internal virtual bool CodecAcceptsHugeBinaryValues(string field)
		{
			return true;
		}
	}
}
