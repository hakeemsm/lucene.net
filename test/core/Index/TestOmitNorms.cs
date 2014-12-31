using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	public class TestOmitNorms : LuceneTestCase
	{
		// Tests whether the DocumentWriter correctly enable the
		// omitNorms bit in the FieldInfo
		[Test]
		public virtual void TestDocOmitNorms()
		{
			Directory ram = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriter writer = new IndexWriter(ram, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, analyzer));
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			// this field will have norms
			Field f1 = NewTextField("f1", "This field has norms", Field.Store.NO);
			d.Add(f1);
			// this field will NOT have norms
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.OmitNorms = (true);
			Field f2 = NewField("f2", "This field has NO norms in all docs", customType);
			d.Add(f2);
			writer.AddDocument(d);
			writer.ForceMerge(1);
			// now we add another document which has term freq for field f2 and not for f1 and verify if the SegmentMerger
			// keep things constant
			d = new Lucene.Net.Documents.Document();
			// Reverse
			d.Add(NewField("f1", "This field has norms", customType));
			d.Add(NewTextField("f2", "This field has NO norms in all docs", Field.Store.NO));
			writer.AddDocument(d);
			// force merge
			writer.ForceMerge(1);
			// flush
			writer.Dispose();
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
			FieldInfos fi = reader.FieldInfos;
			AssertTrue("OmitNorms field bit should be set.", fi.FieldInfo(
				"f1").OmitsNorms);
			AssertTrue("OmitNorms field bit should be set.", fi.FieldInfo(
				"f2").OmitsNorms);
			reader.Dispose();
			ram.Dispose();
		}

		// Tests whether merging of docs that have different
		// omitNorms for the same field works
		[Test]
		public virtual void TestMixedMerge()
		{
			Directory ram = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriter writer = new IndexWriter(ram, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(3)).SetMergePolicy(NewLogMergePolicy
				(2)));
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			// this field will have norms
			Field f1 = NewTextField("f1", "This field has norms", Field.Store.NO);
			d.Add(f1);
			// this field will NOT have norms
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.OmitNorms = (true);
			Field f2 = NewField("f2", "This field has NO norms in all docs", customType);
			d.Add(f2);
			for (int i = 0; i < 30; i++)
			{
				writer.AddDocument(d);
			}
			// now we add another document which has norms for field f2 and not for f1 and verify if the SegmentMerger
			// keep things constant
			d = new Lucene.Net.Documents.Document();
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
			writer.Dispose();
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
			FieldInfos fi = reader.FieldInfos;
			AssertTrue("OmitNorms field bit should be set.", fi.FieldInfo(
				"f1").OmitsNorms);
			AssertTrue("OmitNorms field bit should be set.", fi.FieldInfo(
				"f2").OmitsNorms);
			reader.Dispose();
			ram.Dispose();
		}

		// Make sure first adding docs that do not omitNorms for
		// field X, then adding docs that do omitNorms for that same
		// field, 
		[Test]
		public virtual void TestMixedRAM()
		{
			Directory ram = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriter writer = new IndexWriter(ram, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(10)).SetMergePolicy(NewLogMergePolicy
				(2)));
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			// this field will have norms
			Field f1 = NewTextField("f1", "This field has norms", Field.Store.NO);
			d.Add(f1);
			// this field will NOT have norms
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.OmitNorms = (true);
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
			writer.Dispose();
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(ram));
			FieldInfos fi = reader.FieldInfos;
			AssertTrue("OmitNorms field bit should not be set.", !fi.FieldInfo
				("f1").OmitsNorms);
			AssertTrue("OmitNorms field bit should be set.", fi.FieldInfo(
				"f2").OmitsNorms);
			reader.Dispose();
			ram.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertNoNrm(Directory dir)
		{
			string[] files = dir.ListAll();
			for (int i = 0; i < files.Length; i++)
			{
				// TODO: this relies upon filenames
				IsFalse(files[i].EndsWith(".nrm") || files[i].EndsWith(".len"
					));
			}
		}

		// Verifies no *.nrm exists when all fields omit norms:
		[Test]
		public virtual void TestNoNrmFile()
		{
			Directory ram = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriter writer = new IndexWriter(ram, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(3)).SetMergePolicy(NewLogMergePolicy
				()));
			LogMergePolicy lmp = (LogMergePolicy)writer.Config.MergePolicy;
			lmp.MergeFactor = (2);
			lmp.SetNoCFSRatio(0.0);
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.OmitNorms = (true);
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
			writer.Dispose();
			AssertNoNrm(ram);
			ram.Dispose();
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
		[Test]
		public virtual void TestOmitNormsCombos()
		{
			// indexed with norms
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			Field norms = new Field("foo", "a", customType);
			// indexed without norms
			FieldType customType1 = new FieldType(TextField.TYPE_STORED);
			customType1.OmitNorms = (true);
			Field noNorms = new Field("foo", "a", customType1);
			// not indexed, but stored
			FieldType customType2 = new FieldType();
			customType2.Stored = (true);
			Field noIndex = new Field("foo", "a", customType2);
			// not indexed but stored, omitNorms is set
			FieldType customType3 = new FieldType();
			customType3.Stored = (true);
			customType3.OmitNorms = (true);
			Field noNormsNoIndex = new Field("foo", "a", customType3);
			// not indexed nor stored (doesnt exist at all, we index a different field instead)
			Field emptyNorms = new Field("bar", "a", customType);
			IsNotNull(GetNorms("foo", norms, norms));
			IsNull(GetNorms("foo", norms, noNorms));
			IsNotNull(GetNorms("foo", norms, noIndex));
			IsNotNull(GetNorms("foo", norms, noNormsNoIndex));
			IsNotNull(GetNorms("foo", norms, emptyNorms));
			IsNull(GetNorms("foo", noNorms, noNorms));
			IsNull(GetNorms("foo", noNorms, noIndex));
			IsNull(GetNorms("foo", noNorms, noNormsNoIndex));
			IsNull(GetNorms("foo", noNorms, emptyNorms));
			IsNull(GetNorms("foo", noIndex, noIndex));
			IsNull(GetNorms("foo", noIndex, noNormsNoIndex));
			IsNull(GetNorms("foo", noIndex, emptyNorms));
			IsNull(GetNorms("foo", noNormsNoIndex, noNormsNoIndex));
			IsNull(GetNorms("foo", noNormsNoIndex, emptyNorms));
			IsNull(GetNorms("foo", emptyNorms, emptyNorms));
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
			Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
			d.Add(f1);
			riw.AddDocument(d);
			// add f2
			d = new Lucene.Net.Documents.Document();
			d.Add(f2);
			riw.AddDocument(d);
			// add a mix of f1's and f2's
			int numExtraDocs = TestUtil.NextInt(Random(), 1, 1000);
			for (int i = 0; i < numExtraDocs; i++)
			{
				d = new Lucene.Net.Documents.Document();
				d.Add(Random().NextBoolean() ? f1 : f2);
				riw.AddDocument(d);
			}
			IndexReader ir1 = riw.Reader;
			// todo: generalize
			NumericDocValues norms1 = MultiDocValues.GetNormValues(ir1, field);
			// fully merge and validate MultiNorms against single segment.
			riw.ForceMerge(1);
			DirectoryReader ir2 = riw.Reader;
			NumericDocValues norms2 = GetOnlySegmentReader(ir2).GetNormValues(field);
			if (norms1 == null)
			{
				IsNull(norms2);
			}
			else
			{
				for (int docID = 0; docID < ir1.MaxDoc; docID++)
				{
					AreEqual(norms1.Get(docID), norms2.Get(docID));
				}
			}
			ir1.Dispose();
			ir2.Dispose();
			riw.Dispose();
			dir.Dispose();
			return norms1;
		}
	}
}
