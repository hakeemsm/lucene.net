/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestIndexWriterMerging : LuceneTestCase
	{
		/// <summary>
		/// Tests that index merging (specifically addIndexes(Directory...)) doesn't
		/// change the index order of documents.
		/// </summary>
		/// <remarks>
		/// Tests that index merging (specifically addIndexes(Directory...)) doesn't
		/// change the index order of documents.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLucene()
		{
			int num = 100;
			Directory indexA = NewDirectory();
			Directory indexB = NewDirectory();
			FillIndex(Random(), indexA, 0, num);
			bool fail = VerifyIndex(indexA, 0);
			if (fail)
			{
				Fail("Index a is invalid");
			}
			FillIndex(Random(), indexB, num, num);
			fail = VerifyIndex(indexB, num);
			if (fail)
			{
				Fail("Index b is invalid");
			}
			Directory merged = NewDirectory();
			IndexWriter writer = new IndexWriter(merged, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy(2)));
			writer.AddIndexes(indexA, indexB);
			writer.ForceMerge(1);
			writer.Dispose();
			fail = VerifyIndex(merged, 0);
			IsFalse("The merged index is invalid", fail);
			indexA.Dispose();
			indexB.Dispose();
			merged.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool VerifyIndex(Directory directory, int startAt)
		{
			bool fail = false;
			IndexReader reader = DirectoryReader.Open(directory);
			int max = reader.MaxDoc;
			for (int i = 0; i < max; i++)
			{
				Lucene.Net.Documents.Document temp = reader.Document(i);
				//System.out.println("doc "+i+"="+temp.getField("count").stringValue());
				//compare the index doc number to the value that it should be
				if (!temp.GetField("count").StringValue = ).Equals((i + startAt) + string.Empty))
				{
					fail = true;
					System.Console.Out.WriteLine("Document " + (i + startAt) + " is returning document "
						 + temp.GetField("count").StringValue = ));
				}
			}
			reader.Dispose();
			return fail;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void FillIndex(Random random, Directory dir, int start, int numDocs)
		{
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetOpenMode(IndexWriterConfig.OpenMode
				.CREATE).SetMaxBufferedDocs(2)).SetMergePolicy(NewLogMergePolicy(2)));
			for (int i = start; i < (start + numDocs); i++)
			{
				Lucene.Net.Documents.Document temp = new Lucene.Net.Documents.Document
					();
				temp.Add(NewStringField("count", (string.Empty + i), Field.Store.YES));
				writer.AddDocument(temp);
			}
			writer.Dispose();
		}

		// LUCENE-325: test forceMergeDeletes, when 2 singular merges
		// are required
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestForceMergeDeletes()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(2)).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH)));
			Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType();
			customType.Stored = (true);
			FieldType customType1 = new FieldType(TextField.TYPE_NOT_STORED);
			customType1.SetTokenized(false);
			customType1.StoreTermVectors = true;
			customType1.StoreTermVectorPositions = true;
			customType1.StoreTermVectorOffsets = true;
			Field idField = NewStringField("id", string.Empty, Field.Store.NO);
			document.Add(idField);
			Field storedField = NewField("stored", "stored", customType);
			document.Add(storedField);
			Field termVectorField = NewField("termVector", "termVector", customType1);
			document.Add(termVectorField);
			for (int i = 0; i < 10; i++)
			{
				idField.StringValue = string.Empty + i);
				writer.AddDocument(document);
			}
			writer.Dispose();
			IndexReader ir = DirectoryReader.Open(dir);
			AreEqual(10, ir.MaxDoc);
			AreEqual(10, ir.NumDocs);
			ir.Dispose();
			IndexWriterConfig dontMergeConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			writer = new IndexWriter(dir, dontMergeConfig);
			writer.DeleteDocuments(new Term("id", "0"));
			writer.DeleteDocuments(new Term("id", "7"));
			writer.Dispose();
			ir = DirectoryReader.Open(dir);
			AreEqual(8, ir.NumDocs);
			ir.Dispose();
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NewLogMergePolicy()));
			AreEqual(8, writer.NumDocs);
			AreEqual(10, writer.MaxDoc);
			writer.ForceMergeDeletes();
			AreEqual(8, writer.NumDocs);
			writer.Dispose();
			ir = DirectoryReader.Open(dir);
			AreEqual(8, ir.MaxDoc);
			AreEqual(8, ir.NumDocs);
			ir.Dispose();
			dir.Dispose();
		}

		// LUCENE-325: test forceMergeDeletes, when many adjacent merges are required
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestForceMergeDeletes2()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(2)).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergePolicy(NewLogMergePolicy
				(50)));
			Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType();
			customType.Stored = (true);
			FieldType customType1 = new FieldType(TextField.TYPE_NOT_STORED);
			customType1.SetTokenized(false);
			customType1.StoreTermVectors = true;
			customType1.StoreTermVectorPositions = true;
			customType1.StoreTermVectorOffsets = true;
			Field storedField = NewField("stored", "stored", customType);
			document.Add(storedField);
			Field termVectorField = NewField("termVector", "termVector", customType1);
			document.Add(termVectorField);
			Field idField = NewStringField("id", string.Empty, Field.Store.NO);
			document.Add(idField);
			for (int i = 0; i < 98; i++)
			{
				idField.StringValue = string.Empty + i);
				writer.AddDocument(document);
			}
			writer.Dispose();
			IndexReader ir = DirectoryReader.Open(dir);
			AreEqual(98, ir.MaxDoc);
			AreEqual(98, ir.NumDocs);
			ir.Dispose();
			IndexWriterConfig dontMergeConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			writer = new IndexWriter(dir, dontMergeConfig);
			for (int i_1 = 0; i_1 < 98; i_1 += 2)
			{
				writer.DeleteDocuments(new Term("id", string.Empty + i_1));
			}
			writer.Dispose();
			ir = DirectoryReader.Open(dir);
			AreEqual(49, ir.NumDocs);
			ir.Dispose();
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NewLogMergePolicy(3)));
			AreEqual(49, writer.NumDocs);
			writer.ForceMergeDeletes();
			writer.Dispose();
			ir = DirectoryReader.Open(dir);
			AreEqual(49, ir.MaxDoc);
			AreEqual(49, ir.NumDocs);
			ir.Dispose();
			dir.Dispose();
		}

		// LUCENE-325: test forceMergeDeletes without waiting, when
		// many adjacent merges are required
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestForceMergeDeletes3()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(2)).SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetMergePolicy(NewLogMergePolicy
				(50)));
			FieldType customType = new FieldType();
			customType.Stored = (true);
			FieldType customType1 = new FieldType(TextField.TYPE_NOT_STORED);
			customType1.SetTokenized(false);
			customType1.StoreTermVectors = true;
			customType1.StoreTermVectorPositions = true;
			customType1.StoreTermVectorOffsets = true;
			Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
				();
			Field storedField = NewField("stored", "stored", customType);
			document.Add(storedField);
			Field termVectorField = NewField("termVector", "termVector", customType1);
			document.Add(termVectorField);
			Field idField = NewStringField("id", string.Empty, Field.Store.NO);
			document.Add(idField);
			for (int i = 0; i < 98; i++)
			{
				idField.StringValue = string.Empty + i);
				writer.AddDocument(document);
			}
			writer.Dispose();
			IndexReader ir = DirectoryReader.Open(dir);
			AreEqual(98, ir.MaxDoc);
			AreEqual(98, ir.NumDocs);
			ir.Dispose();
			IndexWriterConfig dontMergeConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			writer = new IndexWriter(dir, dontMergeConfig);
			for (int i_1 = 0; i_1 < 98; i_1 += 2)
			{
				writer.DeleteDocuments(new Term("id", string.Empty + i_1));
			}
			writer.Dispose();
			ir = DirectoryReader.Open(dir);
			AreEqual(49, ir.NumDocs);
			ir.Dispose();
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NewLogMergePolicy(3)));
			writer.ForceMergeDeletes(false);
			writer.Dispose();
			ir = DirectoryReader.Open(dir);
			AreEqual(49, ir.MaxDoc);
			AreEqual(49, ir.NumDocs);
			ir.Dispose();
			dir.Dispose();
		}

		private class MyMergeScheduler : MergeScheduler
		{
			// Just intercepts all merges & verifies that we are never
			// merging a segment with >= 20 (maxMergeDocs) docs
			/// <exception cref="System.IO.IOException"></exception>
			public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound
				)
			{
				lock (this)
				{
					while (true)
					{
						MergePolicy.OneMerge merge = writer.GetNextMerge();
						if (merge == null)
						{
							break;
						}
						for (int i = 0; i < merge.segments.Count; i++)
						{
						}
						//HM:revisit 
						//assert merge.segments.get(i).info.getDocCount() < 20;
						writer.Merge(merge);
					}
				}
			}

			public override void Close()
			{
			}

			internal MyMergeScheduler(TestIndexWriterMerging _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestIndexWriterMerging _enclosing;
		}

		// LUCENE-1013
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSetMaxMergeDocs()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergeScheduler(new TestIndexWriterMerging.MyMergeScheduler
				(this)).SetMaxBufferedDocs(2)).SetMergePolicy(NewLogMergePolicy());
			LogMergePolicy lmp = (LogMergePolicy)conf.MergePolicy;
			lmp.SetMaxMergeDocs(20);
			lmp.MergeFactor = (2);
			IndexWriter iw = new IndexWriter(dir, conf);
			Lucene.Net.Documents.Document document = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			document.Add(NewField("tvtest", "a b c", customType));
			for (int i = 0; i < 177; i++)
			{
				iw.AddDocument(document);
			}
			iw.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoWaitClose()
		{
			Directory directory = NewDirectory();
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetTokenized(false);
			Field idField = NewField("id", string.Empty, customType);
			doc.Add(idField);
			for (int pass = 0; pass < 2; pass++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: pass=" + pass);
				}
				IndexWriterConfig conf = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMaxBufferedDocs
					(2)).SetMergePolicy(NewLogMergePolicy());
				if (pass == 2)
				{
					conf.SetMergeScheduler(new SerialMergeScheduler());
				}
				IndexWriter writer = new IndexWriter(directory, conf);
				((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = (100);
				for (int iter = 0; iter < 10; iter++)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: iter=" + iter);
					}
					for (int j = 0; j < 199; j++)
					{
						idField.StringValue = Sharpen.Extensions.ToString(iter * 201 + j));
						writer.AddDocument(doc);
					}
					int delID = iter * 199;
					for (int j_1 = 0; j_1 < 20; j_1++)
					{
						writer.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(delID)));
						delID += 5;
					}
					// Force a bunch of merge threads to kick off so we
					// stress out aborting them on close:
					((LogMergePolicy)writer.Config.MergePolicy).MergeFactor = (2);
					IndexWriter finalWriter = writer;
					AList<Exception> failure = new AList<Exception>();
					Sharpen.Thread t1 = new _Thread_404(finalWriter, doc, failure);
					if (failure.Count > 0)
					{
						throw failure[0];
					}
					t1.Start();
					writer.Close(false);
					t1.Join();
					// Make sure reader can read
					IndexReader reader = DirectoryReader.Open(directory);
					reader.Dispose();
					// Reopen
					writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
						MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy
						(NewLogMergePolicy()));
				}
				writer.Dispose();
			}
			directory.Dispose();
		}

		private sealed class _Thread_404 : Sharpen.Thread
		{
			public _Thread_404(IndexWriter finalWriter, Lucene.Net.Documents.Document doc
				, AList<Exception> failure)
			{
				this.finalWriter = finalWriter;
				this.Doc = doc;
				this.failure = failure;
			}

			public override void Run()
			{
				bool done = false;
				while (!done)
				{
					for (int i = 0; i < 100; i++)
					{
						try
						{
							finalWriter.AddDocument(doc);
						}
						catch (AlreadyClosedException)
						{
							done = true;
							break;
						}
						catch (ArgumentNullException)
						{
							done = true;
							break;
						}
						catch (Exception e)
						{
							Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
							failure.AddItem(e);
							done = true;
							break;
						}
					}
					Sharpen.Thread.Yield();
				}
			}

			private readonly IndexWriter finalWriter;

			private readonly Lucene.Net.Documents.Document doc;

			private readonly AList<Exception> failure;
		}
	}
}
