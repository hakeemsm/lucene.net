using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestFieldsReader : LuceneTestCase
	{
		private static Directory dir;

		private static Lucene.Net.Documents.Document testDoc;

		private static FieldInfos.Builder fieldInfos = null;

		/// <exception cref="System.Exception"></exception>
		[SetUp]
		public void Setup()
		{
			testDoc = new Lucene.Net.Documents.Document();
			fieldInfos = new FieldInfos.Builder();
			DocHelper.SetupDoc(testDoc);
			foreach (IIndexableField field in testDoc)
			{
				fieldInfos.AddOrUpdate(field.Name, field.FieldTypeValue);
			}
			dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NewLogMergePolicy());
			conf.MergePolicy.SetNoCFSRatio(0.0);
			IndexWriter writer = new IndexWriter(dir, conf);
			writer.AddDocument(testDoc);
			writer.Dispose();
			TestFieldsReader.FaultyIndexInput.doFail = false;
		}

		[TearDown]
		public static void TearDown()
		{
			dir.Dispose();
			dir = null;
			fieldInfos = null;
			testDoc = null;
		}

		[Test]
		public virtual void TestFieldTypes()
		{
			IsTrue(dir != null);
			IsTrue(fieldInfos != null);
			IndexReader reader = DirectoryReader.Open(dir);
			Lucene.Net.Documents.Document doc = reader.Document(0);
			IsTrue(doc != null);
			IsTrue(doc.GetField(DocHelper.TEXT_FIELD_1_KEY) != null);
			Field field = (Field)doc.GetField(DocHelper.TEXT_FIELD_2_KEY);
			IsTrue(field != null);
			IsTrue(field.FieldTypeValue.StoreTermVectors);
			IsFalse(field.FieldTypeValue.OmitNorms);
			IsTrue(field.FieldTypeValue.IndexOptions == FieldInfo.IndexOptions
				.DOCS_AND_FREQS_AND_POSITIONS);
			field = (Field)doc.GetField(DocHelper.TEXT_FIELD_3_KEY);
			IsTrue(field != null);
			IsFalse(field.FieldTypeValue.StoreTermVectors);
			IsTrue(field.FieldTypeValue.OmitNorms);
			IsTrue(field.FieldTypeValue.IndexOptions == FieldInfo.IndexOptions
				.DOCS_AND_FREQS_AND_POSITIONS);
			field = (Field)doc.GetField(DocHelper.NO_TF_KEY);
			IsTrue(field != null);
			IsFalse(field.FieldTypeValue.StoreTermVectors);
			IsFalse(field.FieldTypeValue.OmitNorms);
			IsTrue(field.FieldTypeValue.IndexOptions == FieldInfo.IndexOptions
				.DOCS_ONLY);
			DocumentStoredFieldVisitor visitor = new DocumentStoredFieldVisitor(DocHelper.TEXT_FIELD_3_KEY
				);
			reader.Document(0, visitor);
			IList<IIndexableField> fields = visitor.Document.GetFields();
			AreEqual(1, fields.Count);
			AreEqual(DocHelper.TEXT_FIELD_3_KEY, fields[0].Name);
			reader.Dispose();
		}

		public class FaultyFSDirectory : BaseDirectory
		{
			internal Directory fsDir;

			public FaultyFSDirectory(DirectoryInfo dir)
			{
				fsDir = NewFSDirectory(dir);
				lockFactory = fsDir.LockFactory;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IndexInput OpenInput(string name, IOContext context)
			{
				return new TestFieldsReader.FaultyIndexInput(fsDir.OpenInput(name, context));
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override string[] ListAll()
			{
				return fsDir.ListAll();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool FileExists(string name)
			{
				return fsDir.FileExists(name);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void DeleteFile(string name)
			{
				fsDir.DeleteFile(name);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long FileLength(string name)
			{
				return fsDir.FileLength(name);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IndexOutput CreateOutput(string name, IOContext context)
			{
				return fsDir.CreateOutput(name, context);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Sync(ICollection<string> names)
			{
				fsDir.Sync(names);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void Dispose(bool disposing)
			{
				fsDir.Dispose();
			}
		}

		private class FaultyIndexInput : BufferedIndexInput
		{
			internal IndexInput delegate_;

			internal static bool doFail;

			internal int count;

		    internal FaultyIndexInput(IndexInput delegate_) : base("FaultyIndexInput(" + delegate_
				 + ")", BufferedIndexInput.BUFFER_SIZE)
			{
				this.delegate_ = delegate_;
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void SimOutage()
			{
				if (doFail && count++ % 2 == 1)
				{
					throw new IOException("Simulated network outage");
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void ReadInternal(byte[] b, int offset, int length)
			{
				SimOutage();
				delegate_.Seek(FilePointer);
				delegate_.ReadBytes(b, offset, length);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SeekInternal(long pos)
			{
			}

			public override long Length
			{
			    get { return delegate_.Length; }
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void Dispose(bool disposing)
			{
				delegate_.Dispose();
			}

			public override object Clone()
			{
				TestFieldsReader.FaultyIndexInput i = new TestFieldsReader.FaultyIndexInput(((IndexInput
					)delegate_.Clone()));
				// seek the clone to our current position
				try
				{
					i.Seek(FilePointer);
				}
				catch (IOException)
				{
					throw new SystemException();
				}
				return i;
			}
		}

		// LUCENE-1262
		[Test]
		public virtual void TestExceptions()
		{
			DirectoryInfo indexDir = CreateTempDir("testfieldswriterexceptions");
			try
			{
				Directory dir = new TestFieldsReader.FaultyFSDirectory(indexDir);
				IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE);
				IndexWriter writer = new IndexWriter(dir, iwc);
				for (int i = 0; i < 2; i++)
				{
					writer.AddDocument(testDoc);
				}
				writer.ForceMerge(1);
				writer.Dispose();
				IndexReader reader = DirectoryReader.Open(dir);
				TestFieldsReader.FaultyIndexInput.doFail = true;
				bool exc = false;
				for (int i_1 = 0; i_1 < 2; i_1++)
				{
					try
					{
						reader.Document(i_1);
					}
					catch (IOException)
					{
						// expected
						exc = true;
					}
					try
					{
						reader.Document(i_1);
					}
					catch (IOException)
					{
						// expected
						exc = true;
					}
				}
				IsTrue(exc);
				reader.Dispose();
				dir.Dispose();
			}
			finally
			{
				TestUtil.Rm(indexDir);
			}
		}
	}
}
