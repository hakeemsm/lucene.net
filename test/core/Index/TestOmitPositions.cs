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
	/// <lucene.experimental></lucene.experimental>
	public class TestOmitPositions : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestBasic()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS);
			Field f = NewField("foo", "this is a test test", ft);
			doc.Add(f);
			for (int i = 0; i < 100; i++)
			{
				w.AddDocument(doc);
			}
			IndexReader reader = w.GetReader();
			w.Close();
			NUnit.Framework.Assert.IsNull(MultiFields.GetTermPositionsEnum(reader, null, "foo"
				, new BytesRef("test")));
			DocsEnum de = TestUtil.Docs(Random(), reader, "foo", new BytesRef("test"), null, 
				null, DocsEnum.FLAG_FREQS);
			while (de.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
				NUnit.Framework.Assert.AreEqual(2, de.Freq());
			}
			reader.Close();
			dir.Close();
		}

		// Tests whether the DocumentWriter correctly enable the
		// omitTermFreqAndPositions bit in the FieldInfo
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPositions()
		{
			Directory ram = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer));
			Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
			// f1,f2,f3: docs only
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY);
			Field f1 = NewField("f1", "This field has docs only", ft);
			d.Add(f1);
			Field f2 = NewField("f2", "This field has docs only", ft);
			d.Add(f2);
			Field f3 = NewField("f3", "This field has docs only", ft);
			d.Add(f3);
			FieldType ft2 = new FieldType(TextField.TYPE_NOT_STORED);
			ft2.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS);
			// f4,f5,f6 docs and freqs
			Field f4 = NewField("f4", "This field has docs and freqs", ft2);
			d.Add(f4);
			Field f5 = NewField("f5", "This field has docs and freqs", ft2);
			d.Add(f5);
			Field f6 = NewField("f6", "This field has docs and freqs", ft2);
			d.Add(f6);
			FieldType ft3 = new FieldType(TextField.TYPE_NOT_STORED);
			ft3.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);
			// f7,f8,f9 docs/freqs/positions
			Field f7 = NewField("f7", "This field has docs and freqs and positions", ft3);
			d.Add(f7);
			Field f8 = NewField("f8", "This field has docs and freqs and positions", ft3);
			d.Add(f8);
			Field f9 = NewField("f9", "This field has docs and freqs and positions", ft3);
			d.Add(f9);
			writer.AddDocument(d);
			writer.ForceMerge(1);
			// now we add another document which has docs-only for f1, f4, f7, docs/freqs for f2, f5, f8, 
			// and docs/freqs/positions for f3, f6, f9
			d = new Lucene.Net.Document.Document();
			// f1,f4,f7: docs only
			f1 = NewField("f1", "This field has docs only", ft);
			d.Add(f1);
			f4 = NewField("f4", "This field has docs only", ft);
			d.Add(f4);
			f7 = NewField("f7", "This field has docs only", ft);
			d.Add(f7);
			// f2, f5, f8: docs and freqs
			f2 = NewField("f2", "This field has docs and freqs", ft2);
			d.Add(f2);
			f5 = NewField("f5", "This field has docs and freqs", ft2);
			d.Add(f5);
			f8 = NewField("f8", "This field has docs and freqs", ft2);
			d.Add(f8);
			// f3, f6, f9: docs and freqs and positions
			f3 = NewField("f3", "This field has docs and freqs and positions", ft3);
			d.Add(f3);
			f6 = NewField("f6", "This field has docs and freqs and positions", ft3);
			d.Add(f6);
			f9 = NewField("f9", "This field has docs and freqs and positions", ft3);
			d.Add(f9);
			writer.AddDocument(d);
			// force merge
			writer.ForceMerge(1);
			// flush
			writer.Close();
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
			FieldInfos fi = reader.GetFieldInfos();
			// docs + docs = docs
			NUnit.Framework.Assert.AreEqual(FieldInfo.IndexOptions.DOCS_ONLY, fi.FieldInfo("f1"
				).GetIndexOptions());
			// docs + docs/freqs = docs
			NUnit.Framework.Assert.AreEqual(FieldInfo.IndexOptions.DOCS_ONLY, fi.FieldInfo("f2"
				).GetIndexOptions());
			// docs + docs/freqs/pos = docs
			NUnit.Framework.Assert.AreEqual(FieldInfo.IndexOptions.DOCS_ONLY, fi.FieldInfo("f3"
				).GetIndexOptions());
			// docs/freqs + docs = docs
			NUnit.Framework.Assert.AreEqual(FieldInfo.IndexOptions.DOCS_ONLY, fi.FieldInfo("f4"
				).GetIndexOptions());
			// docs/freqs + docs/freqs = docs/freqs
			NUnit.Framework.Assert.AreEqual(FieldInfo.IndexOptions.DOCS_AND_FREQS, fi.FieldInfo
				("f5").GetIndexOptions());
			// docs/freqs + docs/freqs/pos = docs/freqs
			NUnit.Framework.Assert.AreEqual(FieldInfo.IndexOptions.DOCS_AND_FREQS, fi.FieldInfo
				("f6").GetIndexOptions());
			// docs/freqs/pos + docs = docs
			NUnit.Framework.Assert.AreEqual(FieldInfo.IndexOptions.DOCS_ONLY, fi.FieldInfo("f7"
				).GetIndexOptions());
			// docs/freqs/pos + docs/freqs = docs/freqs
			NUnit.Framework.Assert.AreEqual(FieldInfo.IndexOptions.DOCS_AND_FREQS, fi.FieldInfo
				("f8").GetIndexOptions());
			// docs/freqs/pos + docs/freqs/pos = docs/freqs/pos
			NUnit.Framework.Assert.AreEqual(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
				, fi.FieldInfo("f9").GetIndexOptions());
			reader.Close();
			ram.Close();
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertNoPrx(Directory dir)
		{
			string[] files = dir.ListAll();
			for (int i = 0; i < files.Length; i++)
			{
				NUnit.Framework.Assert.IsFalse(files[i].EndsWith(".prx"));
				NUnit.Framework.Assert.IsFalse(files[i].EndsWith(".pos"));
			}
		}

		// Verifies no *.prx exists when all fields omit term positions:
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoPrxFile()
		{
			Directory ram = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriter writer = new IndexWriter(ram, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(3)).SetMergePolicy(NewLogMergePolicy
				()));
			LogMergePolicy lmp = (LogMergePolicy)writer.GetConfig().GetMergePolicy();
			lmp.SetMergeFactor(2);
			lmp.SetNoCFSRatio(0.0);
			Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS);
			Field f1 = NewField("f1", "This field has term freqs", ft);
			d.Add(f1);
			for (int i = 0; i < 30; i++)
			{
				writer.AddDocument(d);
			}
			writer.Commit();
			AssertNoPrx(ram);
			// now add some documents with positions, and check there is no prox after optimization
			d = new Lucene.Net.Document.Document();
			f1 = NewTextField("f1", "This field has positions", Field.Store.NO);
			d.Add(f1);
			for (int i_1 = 0; i_1 < 30; i_1++)
			{
				writer.AddDocument(d);
			}
			// force merge
			writer.ForceMerge(1);
			// flush
			writer.Close();
			AssertNoPrx(ram);
			ram.Close();
		}

		/// <summary>make sure we downgrade positions and payloads correctly</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixing()
		{
			// no positions
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS);
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			for (int i = 0; i < 20; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				if (i < 19 && Random().NextBoolean())
				{
					for (int j = 0; j < 50; j++)
					{
						doc.Add(new TextField("foo", "i have positions", Field.Store.NO));
					}
				}
				else
				{
					for (int j = 0; j < 50; j++)
					{
						doc.Add(new Field("foo", "i have no positions", ft));
					}
				}
				iw.AddDocument(doc);
				iw.Commit();
			}
			if (Random().NextBoolean())
			{
				iw.ForceMerge(1);
			}
			DirectoryReader ir = iw.GetReader();
			FieldInfos fis = MultiFields.GetMergedFieldInfos(ir);
			NUnit.Framework.Assert.AreEqual(FieldInfo.IndexOptions.DOCS_AND_FREQS, fis.FieldInfo
				("foo").GetIndexOptions());
			NUnit.Framework.Assert.IsFalse(fis.FieldInfo("foo").HasPayloads());
			iw.Close();
			ir.Close();
			dir.Close();
		}
		// checkindex
	}
}
