/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// Some tests for
	/// <see cref="ParallelAtomicReader">ParallelAtomicReader</see>
	/// s with empty indexes
	/// </summary>
	public class TestParallelReaderEmptyIndex : LuceneTestCase
	{
		/// <summary>Creates two empty indexes and wraps a ParallelReader around.</summary>
		/// <remarks>
		/// Creates two empty indexes and wraps a ParallelReader around. Adding this
		/// reader to a new index should not throw any exception.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEmptyIndex()
		{
			Directory rd1 = NewDirectory();
			IndexWriter iw = new IndexWriter(rd1, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			iw.Dispose();
			// create a copy:
			Directory rd2 = NewDirectory(rd1);
			Directory rdOut = NewDirectory();
			IndexWriter iwOut = new IndexWriter(rdOut, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			ParallelAtomicReader apr = new ParallelAtomicReader(SlowCompositeReaderWrapper.Wrap
				(DirectoryReader.Open(rd1)), SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open
				(rd2)));
			// When unpatched, Lucene crashes here with a NoSuchElementException (caused by ParallelTermEnum)
			iwOut.AddIndexes(apr);
			iwOut.ForceMerge(1);
			// 2nd try with a readerless parallel reader
			iwOut.AddIndexes(new ParallelAtomicReader());
			iwOut.ForceMerge(1);
			ParallelCompositeReader cpr = new ParallelCompositeReader(DirectoryReader.Open(rd1
				), DirectoryReader.Open(rd2));
			// When unpatched, Lucene crashes here with a NoSuchElementException (caused by ParallelTermEnum)
			iwOut.AddIndexes(cpr);
			iwOut.ForceMerge(1);
			// 2nd try with a readerless parallel reader
			iwOut.AddIndexes(new ParallelCompositeReader());
			iwOut.ForceMerge(1);
			iwOut.Dispose();
			rdOut.Dispose();
			rd1.Dispose();
			rd2.Dispose();
		}

		/// <summary>
		/// This method creates an empty index (numFields=0, numDocs=0) but is marked
		/// to have TermVectors.
		/// </summary>
		/// <remarks>
		/// This method creates an empty index (numFields=0, numDocs=0) but is marked
		/// to have TermVectors. Adding this index to another index should not throw
		/// any exception.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEmptyIndexWithVectors()
		{
			Directory rd1 = NewDirectory();
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: make 1st writer");
				}
				IndexWriter iw = new IndexWriter(rd1, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
					new MockAnalyzer(Random())));
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				Field idField = NewTextField("id", string.Empty, Field.Store.NO);
				doc.Add(idField);
				FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
				customType.StoreTermVectors = true;
				doc.Add(NewField("test", string.Empty, customType));
				idField.StringValue = "1");
				iw.AddDocument(doc);
				doc.Add(NewTextField("test", string.Empty, Field.Store.NO));
				idField.StringValue = "2");
				iw.AddDocument(doc);
				iw.Dispose();
				IndexWriterConfig dontMergeConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, new 
					MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: make 2nd writer");
				}
				IndexWriter writer = new IndexWriter(rd1, dontMergeConfig);
				writer.DeleteDocuments(new Term("id", "1"));
				writer.Dispose();
				IndexReader ir = DirectoryReader.Open(rd1);
				AreEqual(2, ir.MaxDoc);
				AreEqual(1, ir.NumDocs);
				ir.Dispose();
				iw = new IndexWriter(rd1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
				iw.ForceMerge(1);
				iw.Dispose();
			}
			Directory rd2 = NewDirectory();
			{
				IndexWriter iw = new IndexWriter(rd2, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
					new MockAnalyzer(Random())));
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				iw.AddDocument(doc);
				iw.Dispose();
			}
			Directory rdOut = NewDirectory();
			IndexWriter iwOut = new IndexWriter(rdOut, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			DirectoryReader reader1;
			DirectoryReader reader2;
			ParallelAtomicReader pr = new ParallelAtomicReader(SlowCompositeReaderWrapper.Wrap
				(reader1 = DirectoryReader.Open(rd1)), SlowCompositeReaderWrapper.Wrap(reader2 =
				 DirectoryReader.Open(rd2)));
			// When unpatched, Lucene crashes here with an ArrayIndexOutOfBoundsException (caused by TermVectorsWriter)
			iwOut.AddIndexes(pr);
			// ParallelReader closes any IndexReader you added to it:
			pr.Dispose();
			// 
			//HM:revisit 
			//assert subreaders were closed
			AreEqual(0, reader1.GetRefCount());
			AreEqual(0, reader2.GetRefCount());
			rd1.Dispose();
			rd2.Dispose();
			iwOut.ForceMerge(1);
			iwOut.Dispose();
			rdOut.Dispose();
		}
	}
}
