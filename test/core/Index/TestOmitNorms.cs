/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestOmitNorms : LuceneTestCase
	{
		// Tests whether the DocumentWriter correctly enable the
		// omitNorms bit in the FieldInfo
		/// <exception cref="System.Exception"></exception>
		public virtual void TestOmitNorms()
		{
			Directory ram = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer));
			Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
			// this field will have norms
			Field f1 = NewTextField("f1", "This field has norms", Field.Store.NO);
			d.Add(f1);
			// this field will NOT have norms
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetOmitNorms(true);
			Field f2 = NewField("f2", "This field has NO norms in all docs", customType);
			d.Add(f2);
			writer.AddDocument(d);
			writer.ForceMerge(1);
			// now we add another document which has term freq for field f2 and not for f1 and verify if the SegmentMerger
			// keep things constant
			d = new Lucene.Net.Document.Document();
			// Reverse
			d.Add(NewField("f1", "This field has norms", customType));
			d.Add(NewTextField("f2", "This field has NO norms in all docs", Field.Store.NO));
			writer.AddDocument(d);
			// force merge
			writer.ForceMerge(1);
			// flush
			writer.Close();
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
			FieldInfos fi = reader.GetFieldInfos();
			NUnit.Framework.Assert.IsTrue("OmitNorms field bit should be set.", fi.FieldInfo(
				"f1").OmitsNorms());
			NUnit.Framework.Assert.IsTrue("OmitNorms field bit should be set.", fi.FieldInfo(
				"f2").OmitsNorms());
			reader.Close();
			ram.Close();
		}

		// Tests whether merging of docs that have different
		// omitNorms for the same field works
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixedMerge()
		{
			Directory ram = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriter writer = new IndexWriter(ram, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(3)).SetMergePolicy(NewLogMergePolicy
				(2)));
			Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
			// this field will have norms
			Field f1 = NewTextField("f1", "This field has norms", Field.Store.NO);
			d.Add(f1);
			// this field will NOT have norms
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetOmitNorms(true);
			Field f2 = NewField("f2", "This field has NO norms in all docs", customType);
			d.Add(f2);
			for (int i = 0; i < 30; i++)
			{
				writer.AddDocument(d);
			}
			// now we add another document which has norms for field f2 and not for f1 and verify if the SegmentMerger
			// keep things constant
			d = new Lucene.Net.Document.Document();
			// Reverese
			d.Add(NewField("f1", "This field has norms", customType));
			d.Add(NewTextField("f2", "This field has NO norms in all docs", Field.Store.NO));
			for (int i_1 = 0; i_1 < 30; i_1++)
			{
				writer.AddDocument(d);
			}
			// force merge
			writer.ForceMerge(1);
			// flush
			writer.Close();
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
			FieldInfos fi = reader.GetFieldInfos();
			NUnit.Framework.Assert.IsTrue("OmitNorms field bit should be set.", fi.FieldInfo(
				"f1").OmitsNorms());
			NUnit.Framework.Assert.IsTrue("OmitNorms field bit should be set.", fi.FieldInfo(
				"f2").OmitsNorms());
			reader.Close();
			ram.Close();
		}

		// Make sure first adding docs that do not omitNorms for
		// field X, then adding docs that do omitNorms for that same
		// field, 
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixedRAM()
		{
			Directory ram = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriter writer = new IndexWriter(ram, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(10)).SetMergePolicy(NewLogMergePolicy
				(2)));
			Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
			// this field will have norms
			Field f1 = NewTextField("f1", "This field has norms", Field.Store.NO);
			d.Add(f1);
			// this field will NOT have norms
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetOmitNorms(true);
			Field f2 = NewField("f2", "This field has NO norms in all docs", customType);
			d.Add(f2);
			for (int i = 0; i < 5; i++)
			{
				writer.AddDocument(d);
			}
			for (int i_1 = 0; i_1 < 20; i_1++)
			{
				writer.AddDocument(d);
			}
			// force merge
			writer.ForceMerge(1);
			// flush
			writer.Close();
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
			FieldInfos fi = reader.GetFieldInfos();
			NUnit.Framework.Assert.IsTrue("OmitNorms field bit should not be set.", !fi.FieldInfo
				("f1").OmitsNorms());
			NUnit.Framework.Assert.IsTrue("OmitNorms field bit should be set.", fi.FieldInfo(
				"f2").OmitsNorms());
			reader.Close();
			ram.Close();
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertNoNrm(Directory dir)
		{
			string[] files = dir.ListAll();
			for (int i = 0; i < files.Length; i++)
			{
				// TODO: this relies upon filenames
				NUnit.Framework.Assert.IsFalse(files[i].EndsWith(".nrm") || files[i].EndsWith(".len"
					));
			}
		}

		// Verifies no *.nrm exists when all fields omit norms:
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoNrmFile()
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
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetOmitNorms(true);
			Field f1 = NewField("f1", "This field has no norms", customType);
			d.Add(f1);
			for (int i = 0; i < 30; i++)
			{
				writer.AddDocument(d);
			}
			writer.Commit();
			AssertNoNrm(ram);
			// force merge
			writer.ForceMerge(1);
			// flush
			writer.Close();
			AssertNoNrm(ram);
			ram.Close();
		}

		/// <summary>
		/// Tests various combinations of omitNorms=true/false, the field not existing at all,
		/// ensuring that only omitNorms is 'viral'.
		/// </summary>
		/// <remarks>
		/// Tests various combinations of omitNorms=true/false, the field not existing at all,
		/// ensuring that only omitNorms is 'viral'.
		/// Internally checks that MultiNorms.norms() is consistent (returns the same bytes)
		/// as the fully merged equivalent.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestOmitNormsCombos()
		{
			// indexed with norms
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			Field norms = new Field("foo", "a", customType);
			// indexed without norms
			FieldType customType1 = new FieldType(TextField.TYPE_STORED);
			customType1.SetOmitNorms(true);
			Field noNorms = new Field("foo", "a", customType1);
			// not indexed, but stored
			FieldType customType2 = new FieldType();
			customType2.SetStored(true);
			Field noIndex = new Field("foo", "a", customType2);
			// not indexed but stored, omitNorms is set
			FieldType customType3 = new FieldType();
			customType3.SetStored(true);
			customType3.SetOmitNorms(true);
			Field noNormsNoIndex = new Field("foo", "a", customType3);
			// not indexed nor stored (doesnt exist at all, we index a different field instead)
			Field emptyNorms = new Field("bar", "a", customType);
			NUnit.Framework.Assert.IsNotNull(GetNorms("foo", norms, norms));
			NUnit.Framework.Assert.IsNull(GetNorms("foo", norms, noNorms));
			NUnit.Framework.Assert.IsNotNull(GetNorms("foo", norms, noIndex));
			NUnit.Framework.Assert.IsNotNull(GetNorms("foo", norms, noNormsNoIndex));
			NUnit.Framework.Assert.IsNotNull(GetNorms("foo", norms, emptyNorms));
			NUnit.Framework.Assert.IsNull(GetNorms("foo", noNorms, noNorms));
			NUnit.Framework.Assert.IsNull(GetNorms("foo", noNorms, noIndex));
			NUnit.Framework.Assert.IsNull(GetNorms("foo", noNorms, noNormsNoIndex));
			NUnit.Framework.Assert.IsNull(GetNorms("foo", noNorms, emptyNorms));
			NUnit.Framework.Assert.IsNull(GetNorms("foo", noIndex, noIndex));
			NUnit.Framework.Assert.IsNull(GetNorms("foo", noIndex, noNormsNoIndex));
			NUnit.Framework.Assert.IsNull(GetNorms("foo", noIndex, emptyNorms));
			NUnit.Framework.Assert.IsNull(GetNorms("foo", noNormsNoIndex, noNormsNoIndex));
			NUnit.Framework.Assert.IsNull(GetNorms("foo", noNormsNoIndex, emptyNorms));
			NUnit.Framework.Assert.IsNull(GetNorms("foo", emptyNorms, emptyNorms));
		}

		/// <summary>Indexes at least 1 document with f1, and at least 1 document with f2.</summary>
		/// <remarks>
		/// Indexes at least 1 document with f1, and at least 1 document with f2.
		/// returns the norms for "field".
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		internal virtual NumericDocValues GetNorms(string field, Field f1, Field f2)
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, iwc);
			// add f1
			Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
			d.Add(f1);
			riw.AddDocument(d);
			// add f2
			d = new Lucene.Net.Document.Document();
			d.Add(f2);
			riw.AddDocument(d);
			// add a mix of f1's and f2's
			int numExtraDocs = TestUtil.NextInt(Random(), 1, 1000);
			for (int i = 0; i < numExtraDocs; i++)
			{
				d = new Lucene.Net.Document.Document();
				d.Add(Random().NextBoolean() ? f1 : f2);
				riw.AddDocument(d);
			}
			IndexReader ir1 = riw.GetReader();
			// todo: generalize
			NumericDocValues norms1 = MultiDocValues.GetNormValues(ir1, field);
			// fully merge and validate MultiNorms against single segment.
			riw.ForceMerge(1);
			DirectoryReader ir2 = riw.GetReader();
			NumericDocValues norms2 = GetOnlySegmentReader(ir2).GetNormValues(field);
			if (norms1 == null)
			{
				NUnit.Framework.Assert.IsNull(norms2);
			}
			else
			{
				for (int docID = 0; docID < ir1.MaxDoc(); docID++)
				{
					NUnit.Framework.Assert.AreEqual(norms1.Get(docID), norms2.Get(docID));
				}
			}
			ir1.Close();
			ir2.Close();
			riw.Close();
			dir.Close();
			return norms1;
		}
	}
}
