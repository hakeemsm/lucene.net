/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
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
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			Field f = NewField("field", "abcd", customType);
			doc.Add(f);
			doc.Add(f);
			Field f2 = NewField("field", string.Empty, customType);
			doc.Add(f2);
			doc.Add(f);
			w.AddDocument(doc);
			w.Close();
			IndexReader r = DirectoryReader.Open(dir);
			Terms vector = r.GetTermVectors(0).Terms("field");
			NUnit.Framework.Assert.IsNotNull(vector);
			TermsEnum termsEnum = vector.Iterator(null);
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			NUnit.Framework.Assert.AreEqual(string.Empty, termsEnum.Term().Utf8ToString());
			// Token "" occurred once
			NUnit.Framework.Assert.AreEqual(1, termsEnum.TotalTermFreq());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(8, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(8, dpEnum.EndOffset());
			NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			// Token "abcd" occurred three times
			NUnit.Framework.Assert.AreEqual(new BytesRef("abcd"), termsEnum.Next());
			dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
			NUnit.Framework.Assert.AreEqual(3, termsEnum.TotalTermFreq());
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(0, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(4, dpEnum.EndOffset());
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(4, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(8, dpEnum.EndOffset());
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(8, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(12, dpEnum.EndOffset());
			NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			NUnit.Framework.Assert.IsNull(termsEnum.Next());
			r.Close();
			dir.Close();
		}

		// LUCENE-1442
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDoubleOffsetCounting2()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			Field f = NewField("field", "abcd", customType);
			doc.Add(f);
			doc.Add(f);
			w.AddDocument(doc);
			w.Close();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			NUnit.Framework.Assert.AreEqual(2, termsEnum.TotalTermFreq());
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(0, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(4, dpEnum.EndOffset());
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(5, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(9, dpEnum.EndOffset());
			NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			r.Close();
			dir.Close();
		}

		// LUCENE-1448
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEndOffsetPositionCharAnalyzer()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			Field f = NewField("field", "abcd   ", customType);
			doc.Add(f);
			doc.Add(f);
			w.AddDocument(doc);
			w.Close();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			NUnit.Framework.Assert.AreEqual(2, termsEnum.TotalTermFreq());
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(0, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(4, dpEnum.EndOffset());
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(8, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(12, dpEnum.EndOffset());
			NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			r.Close();
			dir.Close();
		}

		// LUCENE-1448
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEndOffsetPositionWithCachingTokenFilter()
		{
			Directory dir = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer
				));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			stream.Reset();
			// TODO: weird to reset before wrapping with CachingTokenFilter... correct?
			TokenStream cachedStream = new CachingTokenFilter(stream);
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			Field f = new Field("field", cachedStream, customType);
			doc.Add(f);
			doc.Add(f);
			w.AddDocument(doc);
			w.Close();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			NUnit.Framework.Assert.AreEqual(2, termsEnum.TotalTermFreq());
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(0, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(4, dpEnum.EndOffset());
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(8, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(12, dpEnum.EndOffset());
			NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			r.Close();
			dir.Close();
		}

		// LUCENE-1448
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEndOffsetPositionStopFilter()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random(), MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET
				)));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			Field f = NewField("field", "abcd the", customType);
			doc.Add(f);
			doc.Add(f);
			w.AddDocument(doc);
			w.Close();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			NUnit.Framework.Assert.AreEqual(2, termsEnum.TotalTermFreq());
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(0, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(4, dpEnum.EndOffset());
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(9, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(13, dpEnum.EndOffset());
			NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.NextDoc());
			r.Close();
			dir.Close();
		}

		// LUCENE-1448
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEndOffsetPositionStandard()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			Field f = NewField("field", "abcd the  ", customType);
			Field f2 = NewField("field", "crunch man", customType);
			doc.Add(f);
			doc.Add(f2);
			w.AddDocument(doc);
			w.Close();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(0, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(4, dpEnum.EndOffset());
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(11, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(17, dpEnum.EndOffset());
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(18, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(21, dpEnum.EndOffset());
			r.Close();
			dir.Close();
		}

		// LUCENE-1448
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEndOffsetPositionStandardEmptyField()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			Field f = NewField("field", string.Empty, customType);
			Field f2 = NewField("field", "crunch man", customType);
			doc.Add(f);
			doc.Add(f2);
			w.AddDocument(doc);
			w.Close();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			NUnit.Framework.Assert.AreEqual(1, (int)termsEnum.TotalTermFreq());
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(1, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(7, dpEnum.EndOffset());
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(8, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(11, dpEnum.EndOffset());
			r.Close();
			dir.Close();
		}

		// LUCENE-1448
		/// <exception cref="System.Exception"></exception>
		public virtual void TestEndOffsetPositionStandardEmptyField2()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			Field f = NewField("field", "abcd", customType);
			doc.Add(f);
			doc.Add(NewField("field", string.Empty, customType));
			Field f2 = NewField("field", "crunch", customType);
			doc.Add(f2);
			w.AddDocument(doc);
			w.Close();
			IndexReader r = DirectoryReader.Open(dir);
			TermsEnum termsEnum = r.GetTermVectors(0).Terms("field").Iterator(null);
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			NUnit.Framework.Assert.AreEqual(1, (int)termsEnum.TotalTermFreq());
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(0, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(4, dpEnum.EndOffset());
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			dpEnum.NextPosition();
			NUnit.Framework.Assert.AreEqual(6, dpEnum.StartOffset());
			NUnit.Framework.Assert.AreEqual(12, dpEnum.EndOffset());
			r.Close();
			dir.Close();
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
				Lucene.Net.Document.Document document = new Lucene.Net.Document.Document
					();
				FieldType customType = new FieldType();
				customType.SetStored(true);
				Field storedField = NewField("stored", "stored", customType);
				document.Add(storedField);
				writer.AddDocument(document);
				writer.AddDocument(document);
				document = new Lucene.Net.Document.Document();
				document.Add(storedField);
				FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
				customType2.SetStoreTermVectors(true);
				customType2.SetStoreTermVectorPositions(true);
				customType2.SetStoreTermVectorOffsets(true);
				Field termVectorField = NewField("termVector", "termVector", customType2);
				document.Add(termVectorField);
				writer.AddDocument(document);
				writer.ForceMerge(1);
				writer.Close();
				IndexReader reader = DirectoryReader.Open(dir);
				for (int i = 0; i < reader.NumDocs(); i++)
				{
					reader.Document(i);
					reader.GetTermVectors(i);
				}
				reader.Close();
				writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)NewIndexWriterConfig
					(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetRAMBufferSizeMB
					(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergeScheduler(new SerialMergeScheduler
					()).SetMergePolicy(new LogDocMergePolicy()));
				Directory[] indexDirs = new Directory[] { new MockDirectoryWrapper(Random(), new 
					RAMDirectory(dir, NewIOContext(Random()))) };
				writer.AddIndexes(indexDirs);
				writer.ForceMerge(1);
				writer.Close();
			}
			dir.Close();
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
				Lucene.Net.Document.Document document = new Lucene.Net.Document.Document
					();
				FieldType customType = new FieldType();
				customType.SetStored(true);
				Field storedField = NewField("stored", "stored", customType);
				document.Add(storedField);
				writer.AddDocument(document);
				writer.AddDocument(document);
				document = new Lucene.Net.Document.Document();
				document.Add(storedField);
				FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
				customType2.SetStoreTermVectors(true);
				customType2.SetStoreTermVectorPositions(true);
				customType2.SetStoreTermVectorOffsets(true);
				Field termVectorField = NewField("termVector", "termVector", customType2);
				document.Add(termVectorField);
				writer.AddDocument(document);
				writer.ForceMerge(1);
				writer.Close();
				IndexReader reader = DirectoryReader.Open(dir);
				NUnit.Framework.Assert.IsNull(reader.GetTermVectors(0));
				NUnit.Framework.Assert.IsNull(reader.GetTermVectors(1));
				NUnit.Framework.Assert.IsNotNull(reader.GetTermVectors(2));
				reader.Close();
			}
			dir.Close();
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
			Lucene.Net.Document.Document document = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType();
			customType.SetStored(true);
			Field storedField = NewField("stored", "stored", customType);
			document.Add(storedField);
			FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
			customType2.SetStoreTermVectors(true);
			customType2.SetStoreTermVectorPositions(true);
			customType2.SetStoreTermVectorOffsets(true);
			Field termVectorField = NewField("termVector", "termVector", customType2);
			document.Add(termVectorField);
			for (int i = 0; i < 10; i++)
			{
				writer.AddDocument(document);
			}
			writer.Close();
			writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetRAMBufferSizeMB
				(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergeScheduler(new SerialMergeScheduler
				()).SetMergePolicy(new LogDocMergePolicy()));
			for (int i_1 = 0; i_1 < 6; i_1++)
			{
				writer.AddDocument(document);
			}
			writer.ForceMerge(1);
			writer.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			for (int i_2 = 0; i_2 < 10; i_2++)
			{
				reader.GetTermVectors(i_2);
				reader.Document(i_2);
			}
			reader.Close();
			dir.Close();
		}

		// LUCENE-1008
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNoTermVectorAfterTermVector()
		{
			Directory dir = NewDirectory();
			IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			Lucene.Net.Document.Document document = new Lucene.Net.Document.Document
				();
			FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
			customType2.SetStoreTermVectors(true);
			customType2.SetStoreTermVectorPositions(true);
			customType2.SetStoreTermVectorOffsets(true);
			document.Add(NewField("tvtest", "a b c", customType2));
			iw.AddDocument(document);
			document = new Lucene.Net.Document.Document();
			document.Add(NewTextField("tvtest", "x y z", Field.Store.NO));
			iw.AddDocument(document);
			// Make first segment
			iw.Commit();
			FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			document.Add(NewField("tvtest", "a b c", customType));
			iw.AddDocument(document);
			// Make 2nd segment
			iw.Commit();
			iw.ForceMerge(1);
			iw.Close();
			dir.Close();
		}

		// LUCENE-1010
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNoTermVectorAfterTermVectorMerge()
		{
			Directory dir = NewDirectory();
			IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			Lucene.Net.Document.Document document = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			document.Add(NewField("tvtest", "a b c", customType));
			iw.AddDocument(document);
			iw.Commit();
			document = new Lucene.Net.Document.Document();
			document.Add(NewTextField("tvtest", "x y z", Field.Store.NO));
			iw.AddDocument(document);
			// Make first segment
			iw.Commit();
			iw.ForceMerge(1);
			FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
			customType2.SetStoreTermVectors(true);
			document.Add(NewField("tvtest", "a b c", customType2));
			iw.AddDocument(document);
			// Make 2nd segment
			iw.Commit();
			iw.ForceMerge(1);
			iw.Close();
			dir.Close();
		}
	}
}
