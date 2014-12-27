/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	/// <summary>tests for writing term vectors</summary>
	public class TestTermVectorsWriter : LuceneTestCase
	{
		// LUCENE-1442
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDoubleOffsetCounting()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorOffsets = true;
			Field f = NewField("field", "abcd", customType);
			doc.Add(f);
			doc.Add(f);
			Field f2 = NewField("field", string.Empty, customType);
			doc.Add(f2);
			doc.Add(f);
			w.AddDocument(doc);
			w.Dispose();
			IndexReader r = DirectoryReader.Open(dir);
			Terms vector = r.GetTermVectors(0).Terms("field");
			IsNotNull(vector);
			TermsEnum termsEnum = vector.Iterator(null);
			IsNotNull(termsEnum.Next());
			AreEqual(string.Empty, termsEnum.Term().Utf8ToString());
			// Token "" occurred once
			AreEqual(1, termsEnum.TotalTermFreq);
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(8, dpEnum.StartOffset());
			AreEqual(8, dpEnum.EndOffset());
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			// Token "abcd" occurred three times
			AreEqual(new BytesRef("abcd"), termsEnum.Next());
			dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
			AreEqual(3, termsEnum.TotalTermFreq);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(0, dpEnum.StartOffset());
			AreEqual(4, dpEnum.EndOffset());
			dpEnum.NextPosition();
			AreEqual(4, dpEnum.StartOffset());
			AreEqual(8, dpEnum.EndOffset());
			dpEnum.NextPosition();
			AreEqual(8, dpEnum.StartOffset());
			AreEqual(12, dpEnum.EndOffset());
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			IsNull(termsEnum.Next());
			r.Dispose();
			dir.Dispose();
		}

		// LUCENE-1442
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDoubleOffsetCounting2()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorOffsets = true;
			Field f = NewField("field", "abcd", customType);
			doc.Add(f);
			doc.Add(f);
			w.AddDocument(doc);
			w.Dispose();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			AreEqual(2, termsEnum.TotalTermFreq);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(0, dpEnum.StartOffset());
			AreEqual(4, dpEnum.EndOffset());
			dpEnum.NextPosition();
			AreEqual(5, dpEnum.StartOffset());
			AreEqual(9, dpEnum.EndOffset());
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			r.Dispose();
			dir.Dispose();
		}

		// LUCENE-1448
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEndOffsetPositionCharAnalyzer()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorOffsets = true;
			Field f = NewField("field", "abcd   ", customType);
			doc.Add(f);
			doc.Add(f);
			w.AddDocument(doc);
			w.Dispose();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			AreEqual(2, termsEnum.TotalTermFreq);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(0, dpEnum.StartOffset());
			AreEqual(4, dpEnum.EndOffset());
			dpEnum.NextPosition();
			AreEqual(8, dpEnum.StartOffset());
			AreEqual(12, dpEnum.EndOffset());
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			r.Dispose();
			dir.Dispose();
		}

		// LUCENE-1448
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEndOffsetPositionWithCachingTokenFilter()
		{
			Directory dir = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer
				));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			stream.Reset();
			// TODO: weird to reset before wrapping with CachingTokenFilter... correct?
			TokenStream cachedStream = new CachingTokenFilter(stream);
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorOffsets = true;
			Field f = new Field("field", cachedStream, customType);
			doc.Add(f);
			doc.Add(f);
			w.AddDocument(doc);
			w.Dispose();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			AreEqual(2, termsEnum.TotalTermFreq);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(0, dpEnum.StartOffset());
			AreEqual(4, dpEnum.EndOffset());
			dpEnum.NextPosition();
			AreEqual(8, dpEnum.StartOffset());
			AreEqual(12, dpEnum.EndOffset());
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			r.Dispose();
			dir.Dispose();
		}

		// LUCENE-1448
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEndOffsetPositionStopFilter()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET
				)));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorOffsets = true;
			Field f = NewField("field", "abcd the", customType);
			doc.Add(f);
			doc.Add(f);
			w.AddDocument(doc);
			w.Dispose();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			AreEqual(2, termsEnum.TotalTermFreq);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(0, dpEnum.StartOffset());
			AreEqual(4, dpEnum.EndOffset());
			dpEnum.NextPosition();
			AreEqual(9, dpEnum.StartOffset());
			AreEqual(13, dpEnum.EndOffset());
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			r.Dispose();
			dir.Dispose();
		}

		// LUCENE-1448
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEndOffsetPositionStandard()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorOffsets = true;
			Field f = NewField("field", "abcd the  ", customType);
			Field f2 = NewField("field", "crunch man", customType);
			doc.Add(f);
			doc.Add(f2);
			w.AddDocument(doc);
			w.Dispose();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(0, dpEnum.StartOffset());
			AreEqual(4, dpEnum.EndOffset());
			IsNotNull(termsEnum.Next());
			dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(11, dpEnum.StartOffset());
			AreEqual(17, dpEnum.EndOffset());
			IsNotNull(termsEnum.Next());
			dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(18, dpEnum.StartOffset());
			AreEqual(21, dpEnum.EndOffset());
			r.Dispose();
			dir.Dispose();
		}

		// LUCENE-1448
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEndOffsetPositionStandardEmptyField()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorOffsets = true;
			Field f = NewField("field", string.Empty, customType);
			Field f2 = NewField("field", "crunch man", customType);
			doc.Add(f);
			doc.Add(f2);
			w.AddDocument(doc);
			w.Dispose();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			AreEqual(1, (int)termsEnum.TotalTermFreq);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(1, dpEnum.StartOffset());
			AreEqual(7, dpEnum.EndOffset());
			IsNotNull(termsEnum.Next());
			dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(8, dpEnum.StartOffset());
			AreEqual(11, dpEnum.EndOffset());
			r.Dispose();
			dir.Dispose();
		}

		// LUCENE-1448
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEndOffsetPositionStandardEmptyField2()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorOffsets = true;
			Field f = NewField("field", "abcd", customType);
			doc.Add(f);
			doc.Add(NewField("field", string.Empty, customType));
			Field f2 = NewField("field", "crunch", customType);
			doc.Add(f2);
			w.AddDocument(doc);
			w.Dispose();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			AreEqual(1, (int)termsEnum.TotalTermFreq);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(0, dpEnum.StartOffset());
			AreEqual(4, dpEnum.EndOffset());
			IsNotNull(termsEnum.Next());
			dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
			IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			AreEqual(6, dpEnum.StartOffset());
			AreEqual(12, dpEnum.EndOffset());
			r.Dispose();
			dir.Dispose();
		}

		// LUCENE-1168
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTermVectorCorruption()
		{
			Directory dir = NewDirectory();
			for (int iter = 0; iter < 2; iter++)
			{
				IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
					)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
					(2)).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergeScheduler
					(new SerialMergeScheduler()).SetMergePolicy(new LogDocMergePolicy()));
				Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
					();
				FieldType customType = new FieldType();
				customType.Stored = (true);
				Field storedField = NewField("stored", "stored", customType);
				document.Add(storedField);
				writer.AddDocument(document);
				writer.AddDocument(document);
				document = new Lucene.Net.Documents.Document();
				document.Add(storedField);
				FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
				customType2.StoreTermVectors = true;
				customType2.StoreTermVectorPositions = true;
				customType2.StoreTermVectorOffsets = true;
				Field termVectorField = NewField("termVector", "termVector", customType2);
				document.Add(termVectorField);
				writer.AddDocument(document);
				writer.ForceMerge(1);
				writer.Dispose();
				IndexReader reader = DirectoryReader.Open(dir);
				for (int i = 0; i < reader.NumDocs; i++)
				{
					reader.Document(i);
					reader.GetTermVectors(i);
				}
				reader.Dispose();
				writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)NewIndexWriterConfig
					(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetRAMBufferSizeMB
					(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergeScheduler(new SerialMergeScheduler
					()).SetMergePolicy(new LogDocMergePolicy()));
				Directory[] indexDirs = new Directory[] { new MockDirectoryWrapper(Random(), new 
					RAMDirectory(dir, NewIOContext(Random()))) };
				writer.AddIndexes(indexDirs);
				writer.ForceMerge(1);
				writer.Dispose();
			}
			dir.Dispose();
		}

		// LUCENE-1168
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTermVectorCorruption2()
		{
			Directory dir = NewDirectory();
			for (int iter = 0; iter < 2; iter++)
			{
				IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
					)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
					(2)).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergeScheduler
					(new SerialMergeScheduler()).SetMergePolicy(new LogDocMergePolicy()));
				Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
					();
				FieldType customType = new FieldType();
				customType.Stored = (true);
				Field storedField = NewField("stored", "stored", customType);
				document.Add(storedField);
				writer.AddDocument(document);
				writer.AddDocument(document);
				document = new Lucene.Net.Documents.Document();
				document.Add(storedField);
				FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
				customType2.StoreTermVectors = true;
				customType2.StoreTermVectorPositions = true;
				customType2.StoreTermVectorOffsets = true;
				Field termVectorField = NewField("termVector", "termVector", customType2);
				document.Add(termVectorField);
				writer.AddDocument(document);
				writer.ForceMerge(1);
				writer.Dispose();
				IndexReader reader = DirectoryReader.Open(dir);
				IsNull(reader.GetTermVectors(0));
				IsNull(reader.GetTermVectors(1));
				IsNotNull(reader.GetTermVectors(2));
				reader.Dispose();
			}
			dir.Dispose();
		}

		// LUCENE-1168
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTermVectorCorruption3()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(2)).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergeScheduler
				(new SerialMergeScheduler()).SetMergePolicy(new LogDocMergePolicy()));
			Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType();
			customType.Stored = (true);
			Field storedField = NewField("stored", "stored", customType);
			document.Add(storedField);
			FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
			customType2.StoreTermVectors = true;
			customType2.StoreTermVectorPositions = true;
			customType2.StoreTermVectorOffsets = true;
			Field termVectorField = NewField("termVector", "termVector", customType2);
			document.Add(termVectorField);
			for (int i = 0; i < 10; i++)
			{
				writer.AddDocument(document);
			}
			writer.Dispose();
			writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetRAMBufferSizeMB
				(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergeScheduler(new SerialMergeScheduler
				()).SetMergePolicy(new LogDocMergePolicy()));
			for (int i_1 = 0; i_1 < 6; i_1++)
			{
				writer.AddDocument(document);
			}
			writer.ForceMerge(1);
			writer.Dispose();
			IndexReader reader = DirectoryReader.Open(dir);
			for (int i_2 = 0; i_2 < 10; i_2++)
			{
				reader.GetTermVectors(i_2);
				reader.Document(i_2);
			}
			reader.Dispose();
			dir.Dispose();
		}

		// LUCENE-1008
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNoTermVectorAfterTermVector()
		{
			Directory dir = NewDirectory();
			IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
				();
			FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
			customType2.StoreTermVectors = true;
			customType2.StoreTermVectorPositions = true;
			customType2.StoreTermVectorOffsets = true;
			document.Add(NewField("tvtest", "a b c", customType2));
			iw.AddDocument(document);
			document = new Lucene.Net.Documents.Document();
			document.Add(NewTextField("tvtest", "x y z", Field.Store.NO));
			iw.AddDocument(document);
			// Make first segment
			iw.Commit();
			FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			document.Add(NewField("tvtest", "a b c", customType));
			iw.AddDocument(document);
			// Make 2nd segment
			iw.Commit();
			iw.ForceMerge(1);
			iw.Dispose();
			dir.Dispose();
		}

		// LUCENE-1010
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNoTermVectorAfterTermVectorMerge()
		{
			Directory dir = NewDirectory();
			IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			document.Add(NewField("tvtest", "a b c", customType));
			iw.AddDocument(document);
			iw.Commit();
			document = new Lucene.Net.Documents.Document();
			document.Add(NewTextField("tvtest", "x y z", Field.Store.NO));
			iw.AddDocument(document);
			// Make first segment
			iw.Commit();
			iw.ForceMerge(1);
			FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
			customType2.StoreTermVectors = true;
			document.Add(NewField("tvtest", "a b c", customType2));
			iw.AddDocument(document);
			// Make 2nd segment
			iw.Commit();
			iw.ForceMerge(1);
			iw.Dispose();
			dir.Dispose();
		}
	}
}
