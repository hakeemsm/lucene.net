/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene41
{
	/// <summary>Tests special cases of BlockPostingsFormat</summary>
	public class TestBlockPostingsFormat2 : LuceneTestCase
	{
		internal Directory dir;

		internal RandomIndexWriter iw;

		internal IndexWriterConfig iwc;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewFSDirectory(CreateTempDir("testDFBlockSize"));
			iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
			iwc.SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat()));
			iw = new RandomIndexWriter(Random(), dir, iwc.Clone());
			iw.SetDoRandomForceMerge(false);
		}

		// we will ourselves
		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			iw.Close();
			TestUtil.CheckIndex(dir);
			// for some extra coverage, checkIndex before we forceMerge
			iwc.SetOpenMode(IndexWriterConfig.OpenMode.APPEND);
			IndexWriter iw = new IndexWriter(dir, iwc.Clone());
			iw.ForceMerge(1);
			iw.Close();
			dir.Close();
			// just force a checkindex for now
			base.TearDown();
		}

		private Lucene.Net.Document.Document NewDocument()
		{
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			foreach (FieldInfo.IndexOptions option in FieldInfo.IndexOptions.Values())
			{
				FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
				// turn on tvs for a cross-check, since we rely upon checkindex in this test (for now)
				ft.SetStoreTermVectors(true);
				ft.SetStoreTermVectorOffsets(true);
				ft.SetStoreTermVectorPositions(true);
				ft.SetStoreTermVectorPayloads(true);
				ft.SetIndexOptions(option);
				doc.Add(new Field(option.ToString(), string.Empty, ft));
			}
			return doc;
		}

		/// <summary>tests terms with df = blocksize</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDFBlockSize()
		{
			Lucene.Net.Document.Document doc = NewDocument();
			for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE; i++)
			{
				foreach (IndexableField f in doc.GetFields())
				{
					((Field)f).SetStringValue(f.Name() + " " + f.Name() + "_2");
				}
				iw.AddDocument(doc);
			}
		}

		/// <summary>tests terms with df % blocksize = 0</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDFBlockSizeMultiple()
		{
			Lucene.Net.Document.Document doc = NewDocument();
			for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE * 16; i++)
			{
				foreach (IndexableField f in doc.GetFields())
				{
					((Field)f).SetStringValue(f.Name() + " " + f.Name() + "_2");
				}
				iw.AddDocument(doc);
			}
		}

		/// <summary>tests terms with ttf = blocksize</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTTFBlockSize()
		{
			Lucene.Net.Document.Document doc = NewDocument();
			for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE / 2; i++)
			{
				foreach (IndexableField f in doc.GetFields())
				{
					((Field)f).SetStringValue(f.Name() + " " + f.Name() + " " + f.Name() + "_2 " + f.
						Name() + "_2");
				}
				iw.AddDocument(doc);
			}
		}

		/// <summary>tests terms with ttf % blocksize = 0</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTTFBlockSizeMultiple()
		{
			Lucene.Net.Document.Document doc = NewDocument();
			for (int i = 0; i < Lucene41PostingsFormat.BLOCK_SIZE / 2; i++)
			{
				foreach (IndexableField f in doc.GetFields())
				{
					string proto = (f.Name() + " " + f.Name() + " " + f.Name() + " " + f.Name() + " "
						 + f.Name() + "_2 " + f.Name() + "_2 " + f.Name() + "_2 " + f.Name() + "_2");
					StringBuilder val = new StringBuilder();
					for (int j = 0; j < 16; j++)
					{
						val.Append(proto);
						val.Append(" ");
					}
					((Field)f).SetStringValue(val.ToString());
				}
				iw.AddDocument(doc);
			}
		}
	}
}
