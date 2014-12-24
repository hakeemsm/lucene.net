using System;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Lucene41
{
	/// <summary>Tests special cases of BlockPostingsFormat</summary>
	[TestFixture]
    public class TestBlockPostingsFormat2 : LuceneTestCase
	{
		internal Directory dir;

		internal RandomIndexWriter iw;

		internal IndexWriterConfig iwc;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			dir = NewFSDirectory(CreateTempDir("testDFBlockSize"));
			iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
			iwc.SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat()));
			iw = new RandomIndexWriter(Random(), dir, (IndexWriterConfig) iwc.Clone());
			iw.SetDoRandomForceMerge(false);
		}

		// we will ourselves
		[TearDown]
		public override void TearDown()
		{
			iw.Close();
			TestUtil.CheckIndex(dir);
			// for some extra coverage, checkIndex before we forceMerge
			iwc.SetOpenMode(IndexWriterConfig.OpenMode.APPEND);
			IndexWriter iw2 = new IndexWriter(dir, (IndexWriterConfig) iwc.Clone());
			iw2.ForceMerge(1);
			iw2.Dispose();
			dir.Dispose();
			// just force a checkindex for now
			base.TearDown();
		}

		private Lucene.Net.Documents.Document NewDocument()
		{
			var doc = new Lucene.Net.Documents.Document();
			foreach (var option in Enum.GetNames(typeof(FieldInfo.IndexOptions)))
			{
				FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
				// turn on tvs for a cross-check, since we rely upon checkindex in this test (for now)
				ft.StoreTermVectors = true;
				ft.StoreTermVectorOffsets = true;
				ft.StoreTermVectorPositions = true;
				ft.StoreTermVectorPayloads = true;
				ft.IndexOptions = (FieldInfo.IndexOptions) Enum.Parse(typeof(FieldInfo.IndexOptions), option);
				doc.Add(new Field(option, string.Empty, ft));
			}
			return doc;
		}

		/// <summary>tests terms with df = blocksize</summary>
		[Test]
		public virtual void TestDFBlockSize()
		{
			Lucene.Net.Documents.Document doc = NewDocument();
			for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE; i++)
			{
				foreach (IIndexableField f in doc.GetFields())
				{
					((Field)f).StringValue = f.Name + " " + f.Name + "_2";
				}
				iw.AddDocument(doc);
			}
		}

		/// <summary>tests terms with df % blocksize = 0</summary>
		[Test]
		public virtual void TestDFBlockSizeMultiple()
		{
			Lucene.Net.Documents.Document doc = NewDocument();
			for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE * 16; i++)
			{
				foreach (IIndexableField f in doc.GetFields())
				{
					((Field)f).StringValue = f.Name + " " + f.Name + "_2";
				}
				iw.AddDocument(doc);
			}
		}

		/// <summary>tests terms with ttf = blocksize</summary>
		[Test]
		public virtual void TestTTFBlockSize()
		{
			Lucene.Net.Documents.Document doc = NewDocument();
			for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE / 2; i++)
			{
				foreach (IIndexableField f in doc.GetFields())
				{
					((Field)f).StringValue = f.Name + " " + f.Name + " " + f.Name + "_2 " + f.Name + "_2";
				}
				iw.AddDocument(doc);
			}
		}

		/// <summary>tests terms with ttf % blocksize = 0</summary>
		[Test]
		public virtual void TestTTFBlockSizeMultiple()
		{
			Lucene.Net.Documents.Document doc = NewDocument();
			for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE / 2; i++)
			{
				foreach (IIndexableField f in doc.GetFields())
				{
					string proto = (f.Name + " " + f.Name + " " + f.Name + " " + f.Name + " "
						 + f.Name + "_2 " + f.Name + "_2 " + f.Name + "_2 " + f.Name + "_2");
					StringBuilder val = new StringBuilder();
					for (int j = 0; j < 16; j++)
					{
						val.Append(proto);
						val.Append(" ");
					}
					((Field)f).StringValue = val.ToString();
				}
				iw.AddDocument(doc);
			}
		}
	}
}
