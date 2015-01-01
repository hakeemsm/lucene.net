using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;
using ZipFile = System.IO.Compression.ZipFile;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestBackwardsCompatibility : LuceneTestCase
	{
		internal static readonly string[] oldNames =
		{ "40.cfs", "40.nocfs", 
		    "41.cfs", "41.nocfs", "42.cfs", "42.nocfs", "45.cfs", "45.nocfs", "461.cfs", "461.nocfs"
		};

		internal readonly string[] unsupportedNames =
		{ "19.cfs", "19.nocfs"
		    , "20.cfs", "20.nocfs", "21.cfs", "21.nocfs", "22.cfs", "22.nocfs", "23.cfs", "23.nocfs"
		    , "24.cfs", "24.nocfs", "29.cfs", "29.nocfs" };

		internal static readonly string[] oldSingleSegmentNames =
		{ "40.optimized.cfs"
		    , "40.optimized.nocfs" };

		internal static IDictionary<string, Directory> oldIndexDirs;

		// note: add this if we make a 4.x impersonator
		// TODO: don't use 4.x codec, its unrealistic since it means
		// we won't even be running the actual code, only the impostor
		// @SuppressCodecs("Lucene4x")
		// Sep codec cannot yet handle the offsets in our 4.x index!
		// Uncomment these cases & run them on an older Lucene version,
		// to generate indexes to test backwards compatibility.  These
		// indexes will be created under directory /tmp/idx/.
		//
		// However, you must first disable the Lucene TestSecurityManager,
		// which will otherwise disallow writing outside of the build/
		// directory - to do this, comment out the "java.security.manager"
		// <sysproperty> under the "test-macro" <macrodef>.
		//
		// Be sure to create the indexes with the actual format:
		//  ant test -Dtestcase=TestBackwardsCompatibility -Dversion=x.y.z
		//      -Dtests.codec=LuceneXY -Dtests.postingsformat=LuceneXY -Dtests.docvaluesformat=LuceneXY
		//
		// Zip up the generated indexes:
		//
		//    cd /tmp/idx/index.cfs   ; zip index.<VERSION>.cfs.zip *
		//    cd /tmp/idx/index.nocfs ; zip index.<VERSION>.nocfs.zip *
		//
		// Then move those 2 zip files to your trunk checkout and add them
		// to the oldNames array.
		/// <summary>Randomizes the use of some of hte constructor variations</summary>
		private static IndexUpgrader NewIndexUpgrader(Directory dir)
		{
			bool streamType = Random().NextBoolean();
			int choice = Random().NextInt(0, 2);
			switch (choice)
			{
				case 0:
				{
					return new IndexUpgrader(dir, TEST_VERSION_CURRENT);
				}

				case 1:
				{
					return new IndexUpgrader(dir, TEST_VERSION_CURRENT, streamType ? null : System.Console.Error
						, false);
				}

				case 2:
				{
					return new IndexUpgrader(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null), false
						);
				}

				default:
				{
					Fail("case statement didn't get updated when random bounds changed"
						);
					break;
				}
			}
			return null;
		}

		// never get here
		/// <exception cref="System.Exception"></exception>
		[SetUp]
		public static void Setup()
		{
			IsFalse(OLD_FORMAT_IMPERSONATION_IS_ACTIVE, "test infra is broken!");
			IList<string> names = new List<string>(oldNames.Length + oldSingleSegmentNames.Length);
			names.AddRange(Arrays.AsList(oldNames));
			names.AddRange(Arrays.AsList(oldSingleSegmentNames));
			oldIndexDirs = new Dictionary<string, Directory>();
			foreach (string name in names)
			{
				var dir = CreateTempDir(name);
				var dataFile = (typeof(TestBackwardsCompatibility).Assembly.GetManifestResourceStream("index."+ name + ".zip"));
				new ZipArchive(dataFile).ExtractToDirectory(dir.FullName);
				oldIndexDirs[name] = NewFSDirectory(dir);
			}
		}

		/// <exception cref="System.Exception"></exception>
		[TearDown]
		public static void TearDown()
		{
			foreach (Directory d in oldIndexDirs.Values)
			{
				d.Dispose();
			}
			oldIndexDirs = null;
		}

		/// <summary>This test checks that *only* IndexFormatTooOldExceptions are thrown when you open and operate on too old indexes!
		/// 	</summary>
		[Test]
		public virtual void TestUnsupportedOldIndexes()
		{
			for (int i = 0; i < unsupportedNames.Length; i++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: index " + unsupportedNames[i]);
				}
				var oldIndxeDir = CreateTempDir(unsupportedNames[i]);
				
                string unsupportedZip = "unsupported." + unsupportedNames[i] + ".zip";
			    new ZipArchive(typeof(TestBackwardsCompatibility).Assembly.GetManifestResourceStream(unsupportedZip)).ExtractToDirectory(oldIndxeDir.FullName);
				
				BaseDirectoryWrapper dir = NewFSDirectory(oldIndxeDir);
				// don't checkindex, these are intentionally not supported
				dir.SetCheckIndexOnClose(false);
				IndexReader reader = null;
				IndexWriter writer = null;
				try
				{
					reader = DirectoryReader.Open(dir);
					Fail("DirectoryReader.open should not pass for " + unsupportedNames
						[i]);
				}
				catch (IndexFormatTooOldException)
				{
				}
				finally
				{
					// pass
					if (reader != null)
					{
						reader.Dispose();
					}
					reader = null;
				}
				try
				{
					writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
						(Random())));
					Fail("IndexWriter creation should not pass for " + unsupportedNames
						[i]);
				}
				catch (IndexFormatTooOldException e)
				{
					// pass
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: got expected exc:");
						System.Console.Out.WriteLine(e.StackTrace);
						
					}
					// Make sure exc message includes a path=
					IsTrue(e.Message.IndexOf("path=\"") != -1, "got exc message: " + e.Message);
				}
				finally
				{
					// we should fail to open IW, and so it should be null when we get here.
					// However, if the test fails (i.e., IW did not fail on open), we need
					// to close IW. However, if merges are run, IW may throw
					// IndexFormatTooOldException, and we don't want to mask the fail()
					// above, so close without waiting for merges.
					if (writer != null)
					{
						writer.Dispose(false);
					}
					writer = null;
				}

			    var bos = new MemoryStream(1024);
				CheckIndex checker = new CheckIndex(dir);
				checker.SetInfoStream(new StreamWriter(bos, Encoding.UTF8));
			    CheckIndex.Status indexStatus = checker.CheckIndex_Renamed_Method();
				IsFalse(indexStatus.clean);
				IsTrue(bos.ToString().Contains(typeof(IndexFormatTooOldException).FullName));
				dir.Dispose();
                oldIndxeDir.Delete(true);
				
			}
		}

		[Test]
		public virtual void TestFullyMergeOldIndex()
		{
			foreach (string name in oldNames)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: index=" + name);
				}
				Directory dir = NewDirectory(oldIndexDirs[name]);
				IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
					new MockAnalyzer(Random())));
				w.ForceMerge(1);
				w.Dispose();
				dir.Dispose();
			}
		}

		[Test]
		public virtual void TestAddOldIndexes()
		{
			foreach (string name in oldNames)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: old index " + name);
				}
				Directory targetDir = NewDirectory();
				IndexWriter w = new IndexWriter(targetDir, NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())));
				w.AddIndexes(oldIndexDirs[name]);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: done adding indices; now close");
				}
				w.Dispose();
				targetDir.Dispose();
			}
		}

		[Test]
		public virtual void TestAddOldIndexesReader()
		{
			foreach (string name in oldNames)
			{
				IndexReader reader = DirectoryReader.Open(oldIndexDirs[name]);
				Directory targetDir = NewDirectory();
				IndexWriter w = new IndexWriter(targetDir, NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())));
				w.AddIndexes(reader);
				w.Dispose();
				reader.Dispose();
				targetDir.Dispose();
			}
		}

		[Test]
		public virtual void TestSearchOldIndex()
		{
			foreach (string name in oldNames)
			{
				SearchIndex(oldIndexDirs[name], name);
			}
		}

		[Test]
		public virtual void TestIndexOldIndexNoAdds()
		{
			foreach (string name in oldNames)
			{
				Directory dir = NewDirectory(oldIndexDirs[name]);
				ChangeIndexNoAdds(Random(), dir);
				dir.Dispose();
			}
		}

		[Test]
		public virtual void TestIndexOldIndex()
		{
			foreach (string name in oldNames)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: oldName=" + name);
				}
				Directory dir = NewDirectory(oldIndexDirs[name]);
				ChangeIndexWithAdds(Random(), dir, name);
				dir.Dispose();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DoTestHits(ScoreDoc[] hits, int expectedCount, IndexReader reader)
		{
			int hitCount = hits.Length;
			AreEqual(expectedCount, hitCount, "wrong number of hits");
			for (int i = 0; i < hitCount; i++)
			{
				reader.Document(hits[i].Doc);
				reader.GetTermVectors(hits[i].Doc);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void SearchIndex(Directory dir, string oldName)
		{
			//QueryParser parser = new QueryParser("contents", new MockAnalyzer(random));
			//Query query = parser.parse("handle:1");
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			TestUtil.CheckIndex(dir);
			// true if this is a 4.0+ index
			bool is40Index = MultiFields.GetMergedFieldInfos(reader).FieldInfo("content5") !=
				 null;
			// true if this is a 4.2+ index
			bool is42Index = MultiFields.GetMergedFieldInfos(reader).FieldInfo("dvSortedSet")
				 != null;
			//HM:revisit 
			//assert is40Index; // NOTE: currently we can only do this on trunk!
			IBits liveDocs = MultiFields.GetLiveDocs(reader);
			for (int i = 0; i < 35; i++)
			{
				if (liveDocs[i])
				{
					Lucene.Net.Documents.Document d = reader.Document(i);
					IList<IIndexableField> fields = d.GetFields();
					bool isProxDoc = d.GetField("content3") == null;
					if (isProxDoc)
					{
						int numFields = is40Index ? 7 : 5;
						AreEqual(numFields, fields.Count);
						IIndexableField f = d.GetField("id");
						AreEqual(string.Empty + i, f.StringValue);
						f = d.GetField("utf8");
						AreEqual("Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd"
							, f.StringValue);
						f = d.GetField("autf8");
						AreEqual("Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd"
							, f.StringValue);
						f = d.GetField("content2");
						AreEqual("here is more content with aaa aaa aaa", f.StringValue);
						f = d.GetField("fie\u2C77ld");
						AreEqual("field with non-ascii name", f.StringValue);
					}
					Fields tfvFields = reader.GetTermVectors(i);
					IsNotNull(tfvFields, "i=" + i);
					Terms tfv = tfvFields.Terms("utf8");
					IsNotNull(tfv, "docID=" + i + " index=" + oldName);
				}
				else
				{
					// Only ID 7 is deleted
					AreEqual(7, i);
				}
			}
			if (is40Index)
			{
				// check docvalues fields
				NumericDocValues dvByte = MultiDocValues.GetNumericValues(reader, "dvByte");
				BinaryDocValues dvBytesDerefFixed = MultiDocValues.GetBinaryValues(reader, "dvBytesDerefFixed"
					);
				BinaryDocValues dvBytesDerefVar = MultiDocValues.GetBinaryValues(reader, "dvBytesDerefVar"
					);
				SortedDocValues dvBytesSortedFixed = MultiDocValues.GetSortedValues(reader, "dvBytesSortedFixed"
					);
				SortedDocValues dvBytesSortedVar = MultiDocValues.GetSortedValues(reader, "dvBytesSortedVar"
					);
				BinaryDocValues dvBytesStraightFixed = MultiDocValues.GetBinaryValues(reader, "dvBytesStraightFixed"
					);
				BinaryDocValues dvBytesStraightVar = MultiDocValues.GetBinaryValues(reader, "dvBytesStraightVar"
					);
				NumericDocValues dvDouble = MultiDocValues.GetNumericValues(reader, "dvDouble");
				NumericDocValues dvFloat = MultiDocValues.GetNumericValues(reader, "dvFloat");
				NumericDocValues dvInt = MultiDocValues.GetNumericValues(reader, "dvInt");
				NumericDocValues dvLong = MultiDocValues.GetNumericValues(reader, "dvLong");
				NumericDocValues dvPacked = MultiDocValues.GetNumericValues(reader, "dvPacked");
				NumericDocValues dvShort = MultiDocValues.GetNumericValues(reader, "dvShort");
				SortedSetDocValues dvSortedSet = null;
				if (is42Index)
				{
					dvSortedSet = MultiDocValues.GetSortedSetValues(reader, "dvSortedSet");
				}
				for (int i = 0; i < 35; i++)
				{
					int id = System.Convert.ToInt32(reader.Document(i).Get("id"));
					AreEqual(id, dvByte.Get(i));
					byte[] bytes = new byte[] { unchecked((byte)((int)(((uint)id) >> 24))), unchecked(
						(byte)((int)(((uint)id) >> 16))), unchecked((byte)((int)(((uint)id) >> 8))), unchecked(
						(byte)id) };
					BytesRef expectedRef = new BytesRef(bytes.ToSbytes());
					BytesRef scratch = new BytesRef();
					dvBytesDerefFixed.Get(i, scratch);
					AreEqual(expectedRef, scratch);
					dvBytesDerefVar.Get(i, scratch);
					AreEqual(expectedRef, scratch);
					dvBytesSortedFixed.Get(i, scratch);
					AreEqual(expectedRef, scratch);
					dvBytesSortedVar.Get(i, scratch);
					AreEqual(expectedRef, scratch);
					dvBytesStraightFixed.Get(i, scratch);
					AreEqual(expectedRef, scratch);
					dvBytesStraightVar.Get(i, scratch);
					AreEqual(expectedRef, scratch);
                    AreEqual((double)id, dvDouble.Get(i).LongBitsToDouble(), 0D);
                    AreEqual((float)id, ((int)dvFloat.Get(i)).IntBitsToFloat(), 0F);
					AreEqual(id, dvInt.Get(i));
					AreEqual(id, dvLong.Get(i));
					AreEqual(id, dvPacked.Get(i));
					AreEqual(id, dvShort.Get(i));
					if (is42Index)
					{
						dvSortedSet.SetDocument(i);
						long ord = dvSortedSet.NextOrd();
						AreEqual(SortedSetDocValues.NO_MORE_ORDS, dvSortedSet.NextOrd
							());
						dvSortedSet.LookupOrd(ord, scratch);
						AreEqual(expectedRef, scratch);
					}
				}
			}
			ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).ScoreDocs;
			// First document should be #0
			Lucene.Net.Documents.Document d_1 = searcher.IndexReader.Document(hits
				[0].Doc);
			AreEqual("didn't get the right document first", "0", d_1.Get
				("id"));
			DoTestHits(hits, 34, searcher.IndexReader);
			if (is40Index)
			{
				hits = searcher.Search(new TermQuery(new Term("content5", "aaa")), null, 1000).ScoreDocs;
				DoTestHits(hits, 34, searcher.IndexReader);
				hits = searcher.Search(new TermQuery(new Term("content6", "aaa")), null, 1000).ScoreDocs;
				DoTestHits(hits, 34, searcher.IndexReader);
			}
			hits = searcher.Search(new TermQuery(new Term("utf8", "\u0000")), null, 1000).ScoreDocs;
			AreEqual(34, hits.Length);
			hits = searcher.Search(new TermQuery(new Term("utf8", "lu\uD834\uDD1Ece\uD834\uDD60ne"
				)), null, 1000).ScoreDocs;
			AreEqual(34, hits.Length);
			hits = searcher.Search(new TermQuery(new Term("utf8", "ab\ud917\udc17cd")), null, 
				1000).ScoreDocs;
			AreEqual(34, hits.Length);
			reader.Dispose();
		}

		private int Compare(string name, string v)
		{
			int v0 = System.Convert.ToInt32(name.Substring(0, 2));
			int v1 = System.Convert.ToInt32(v);
			return v0 - v1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void ChangeIndexWithAdds(Random random, Directory dir, string origOldName
			)
		{
			// open writer
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(random)).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy
				(NewLogMergePolicy()));
			// add 10 docs
			for (int i = 0; i < 10; i++)
			{
				AddDoc(writer, 35 + i);
			}
			// make sure writer sees right total -- writer seems not to know about deletes in .del?
			int expected;
			if (Compare(origOldName, "24") < 0)
			{
				expected = 44;
			}
			else
			{
				expected = 45;
			}
			AreEqual(expected, writer.NumDocs, "wrong doc count");
			writer.Dispose();
			// make sure searching sees right # hits
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null
				, 1000).ScoreDocs;
			Lucene.Net.Documents.Document d = searcher.IndexReader.Document(hits[0
				].Doc);
			AreEqual("wrong first document", "0", d.Get("id"));
			DoTestHits(hits, 44, searcher.IndexReader);
			reader.Dispose();
			// fully merge
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(random)).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy(NewLogMergePolicy
				()));
			writer.ForceMerge(1);
			writer.Dispose();
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
			hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).ScoreDocs;
			AreEqual(44, hits.Length, "wrong number of hits");
			d = searcher.Doc(hits[0].Doc);
			DoTestHits(hits, 44, searcher.IndexReader);
			AreEqual("wrong first document", "0", d.Get("id"));
			reader.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void ChangeIndexNoAdds(Random random, Directory dir)
		{
			// make sure searching sees right # hits
			DirectoryReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null
				, 1000).ScoreDocs;
			AreEqual(34, hits.Length, "wrong number of hits");
			Lucene.Net.Documents.Document d = searcher.Doc(hits[0].Doc);
			AreEqual("wrong first document", "0", d.Get("id"));
			reader.Dispose();
			// fully merge
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(random)).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			writer.ForceMerge(1);
			writer.Dispose();
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
			hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).ScoreDocs;
			AreEqual(34, hits.Length, "wrong number of hits");
			DoTestHits(hits, 34, searcher.IndexReader);
			reader.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual DirectoryInfo CreateIndex(string dirName, bool doCFS, bool fullyMerged)
		{
			// we use a real directory name that is not cleaned up, because this method is only used to create backwards indexes:
			
		    string oldIdxPath = Path.Combine(Path.GetTempPath(), "\\idx");
		    var indexDir = new DirectoryInfo(oldIdxPath);
            indexDir.Delete(true);
		    
			Directory dir = NewFSDirectory(indexDir);
			LogByteSizeMergePolicy mp = new LogByteSizeMergePolicy();
			mp.SetNoCFSRatio(doCFS ? 1.0 : 0.0);
			mp.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
			// TODO: remove randomness
			IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
			{
			    UseCompoundFile = doCFS
			};
		    conf.SetMaxBufferedDocs(10);
            conf.SetMergePolicy(mp);
			IndexWriter writer = new IndexWriter(dir, conf);
			for (int i = 0; i < 35; i++)
			{
				AddDoc(writer, i);
			}
			AreEqual(35, writer.MaxDoc, "wrong doc count");
			if (fullyMerged)
			{
				writer.ForceMerge(1);
			}
			writer.Dispose();
			if (!fullyMerged)
			{
				// open fresh writer so we get no prx file in the added segment
				mp = new LogByteSizeMergePolicy();
				mp.SetNoCFSRatio(doCFS ? 1.0 : 0.0);
				// TODO: remove randomness
				conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
                conf.UseCompoundFile = doCFS;
                conf.SetMaxBufferedDocs(10);
				conf.SetMergePolicy(mp);
				writer = new IndexWriter(dir, conf);
				AddNoProxDoc(writer);
				writer.Dispose();
				conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
                conf.UseCompoundFile = doCFS;
                conf.SetMaxBufferedDocs(10);
				conf.SetMergePolicy(doCFS ? NoMergePolicy.COMPOUND_FILES : NoMergePolicy.NO_COMPOUND_FILES
					);
				writer = new IndexWriter(dir, conf);
				Term searchTerm = new Term("id", "7");
				writer.DeleteDocuments(searchTerm);
				writer.Dispose();
			}
			dir.Dispose();
			return indexDir;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(IndexWriter writer, int id)
		{
			var doc = new Lucene.Net.Documents.Document
			{
			    new TextField("content", "aaa", Field.Store.NO),
			    new StringField("id", id.ToString(), Field.Store.YES)
			};
		    FieldType customType2 = new FieldType(TextField.TYPE_STORED)
		    {
		        StoreTermVectors = true,
		        StoreTermVectorPositions = true,
		        StoreTermVectorOffsets = true
		    };
		    doc.Add(new Field("autf8", "Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd"
				, customType2));
			doc.Add(new Field("utf8", "Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd"
				, customType2));
			doc.Add(new Field("content2", "here is more content with aaa aaa aaa", customType2
				));
			doc.Add(new Field("fie\u2C77ld", "field with non-ascii name", customType2));
			// add numeric fields, to test if flex preserves encoding
			doc.Add(new IntField("trieInt", id, Field.Store.NO));
			doc.Add(new LongField("trieLong", (long)id, Field.Store.NO));
			// add docvalues fields
			doc.Add(new NumericDocValuesField("dvByte", unchecked((byte)id)));
			byte[] bytes = new byte[] { unchecked((byte)((int)(((uint)id) >> 24))), unchecked(
				(byte)((int)(((uint)id) >> 16))), unchecked((byte)((int)(((uint)id) >> 8))), unchecked(
				(byte)id) };
			BytesRef bytesRef = new BytesRef(bytes.ToSbytes());
			doc.Add(new BinaryDocValuesField("dvBytesDerefFixed", bytesRef));
			doc.Add(new BinaryDocValuesField("dvBytesDerefVar", bytesRef));
			doc.Add(new SortedDocValuesField("dvBytesSortedFixed", bytesRef));
			doc.Add(new SortedDocValuesField("dvBytesSortedVar", bytesRef));
			doc.Add(new BinaryDocValuesField("dvBytesStraightFixed", bytesRef));
			doc.Add(new BinaryDocValuesField("dvBytesStraightVar", bytesRef));
			doc.Add(new DoubleDocValuesField("dvDouble", (double)id));
			doc.Add(new FloatDocValuesField("dvFloat", (float)id));
			doc.Add(new NumericDocValuesField("dvInt", id));
			doc.Add(new NumericDocValuesField("dvLong", id));
			doc.Add(new NumericDocValuesField("dvPacked", id));
			doc.Add(new NumericDocValuesField("dvShort", (short)id));
			doc.Add(new SortedSetDocValuesField("dvSortedSet", bytesRef));
			// a field with both offsets and term vectors for a cross-check
			FieldType customType3 = new FieldType(TextField.TYPE_STORED);
			customType3.StoreTermVectors = true;
			customType3.StoreTermVectorPositions = true;
			customType3.StoreTermVectorOffsets = true;
			customType3.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
				);
			doc.Add(new Field("content5", "here is more content with aaa aaa aaa", customType3
				));
			// a field that omits only positions
			FieldType customType4 = new FieldType(TextField.TYPE_STORED);
			customType4.StoreTermVectors = true;
			customType4.StoreTermVectorPositions = (false);
			customType4.StoreTermVectorOffsets = true;
			customType4.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS);
			doc.Add(new Field("content6", "here is more content with aaa aaa aaa", customType4
				));
			// TODO: 
			//   index different norms types via similarity (we use a random one currently?!)
			//   remove any analyzer randomness, explicitly add payloads for certain fields.
			writer.AddDocument(doc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddNoProxDoc(IndexWriter writer)
		{
			var doc = new Lucene.Net.Documents.Document();
			var customType = new FieldType(TextField.TYPE_STORED) {IndexOptions = (FieldInfo.IndexOptions.DOCS_ONLY)};
		    Field f = new Field("content3", "aaa", customType);
			doc.Add(f);
			var customType2 = new FieldType {Stored = (true), IndexOptions = (FieldInfo.IndexOptions.DOCS_ONLY)};
		    f = new Field("content4", "aaa", customType2);
			doc.Add(f);
			writer.AddDocument(doc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int CountDocs(DocsEnum docs)
		{
			int count = 0;
			while ((docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			{
				count++;
			}
			return count;
		}

		// flex: test basics of TermsEnum api on non-flex index
		[Test]
		public virtual void TestNextIntoWrongField()
		{
			foreach (string name in oldNames)
			{
				Directory dir = oldIndexDirs[name];
				IndexReader r = DirectoryReader.Open(dir);
				TermsEnum terms = MultiFields.GetFields(r).Terms("content").Iterator(null);
				BytesRef t = terms.Next();
				IsNotNull(t);
				// content field only has term aaa:
				AreEqual("aaa", t.Utf8ToString());
				IsNull(terms.Next());
				BytesRef aaaTerm = new BytesRef("aaa");
				// should be found exactly
				AreEqual(TermsEnum.SeekStatus.FOUND, terms.SeekCeil(aaaTerm
					));
				AreEqual(35, CountDocs(TestUtil.Docs(Random(), terms, null
					, null, DocsEnum.FLAG_NONE)));
				IsNull(terms.Next());
				// should hit end of field
				AreEqual(TermsEnum.SeekStatus.END, terms.SeekCeil(new BytesRef
					("bbb")));
				IsNull(terms.Next());
				// should seek to aaa
				AreEqual(TermsEnum.SeekStatus.NOT_FOUND, terms.SeekCeil(new 
					BytesRef("a")));
				IsTrue(terms.Term.BytesEquals(aaaTerm));
				AreEqual(35, CountDocs(TestUtil.Docs(Random(), terms, null
					, null, DocsEnum.FLAG_NONE)));
				IsNull(terms.Next());
				AreEqual(TermsEnum.SeekStatus.FOUND, terms.SeekCeil(aaaTerm
					));
				AreEqual(35, CountDocs(TestUtil.Docs(Random(), terms, null
					, null, DocsEnum.FLAG_NONE)));
				IsNull(terms.Next());
				r.Dispose();
			}
		}

		/// <summary>Test that we didn't forget to bump the current Constants.LUCENE_MAIN_VERSION.
		/// 	</summary>
		/// <remarks>
		/// Test that we didn't forget to bump the current Constants.LUCENE_MAIN_VERSION.
		/// This is important so that we can determine which version of lucene wrote the segment.
		/// </remarks>
		[Test]
		public virtual void TestOldVersions()
		{
			// first create a little index with the current code and get the version
			Directory currentDir = NewDirectory();
			RandomIndexWriter riw = new RandomIndexWriter(Random(), currentDir);
			riw.AddDocument(new Lucene.Net.Documents.Document());
			riw.Close();
			DirectoryReader ir = DirectoryReader.Open(currentDir);
			SegmentReader air = (SegmentReader)((AtomicReader)ir.Leaves[0].Reader);
			string currentVersion = air.SegmentInfo.info.Version;
			IsNotNull(currentVersion);
			// only 3.0 segments can have a null version
			ir.Dispose();
			currentDir.Dispose();
			IComparer<string> comparator = StringHelper.VersionComparator;
			// now check all the old indexes, their version should be < the current version
			foreach (string name in oldNames)
			{
				Directory dir = oldIndexDirs[name];
				DirectoryReader r = DirectoryReader.Open(dir);
				foreach (AtomicReaderContext context in r.Leaves)
				{
					air = (SegmentReader)((AtomicReader)context.Reader);
					string oldVersion = air.SegmentInfo.info.Version;
					IsNotNull(oldVersion);
					// only 3.0 segments can have a null version
					IsTrue(comparator.Compare(oldVersion, currentVersion) < 0
						, "current Constants.LUCENE_MAIN_VERSION is <= an old index: did you forget to bump it?!");
				}
				r.Dispose();
			}
		}

		[Test]
		public virtual void TestNumericFields()
		{
			foreach (string name in oldNames)
			{
				Directory dir = oldIndexDirs[name];
				IndexReader reader = DirectoryReader.Open(dir);
				IndexSearcher searcher = NewSearcher(reader);
				for (int id = 10; id < 15; id++)
				{
					ScoreDoc[] hits = searcher.Search(NumericRangeQuery.NewIntRange("trieInt", 4, id, id, true, true), 100).ScoreDocs;
					AreEqual(1, hits.Length, "wrong number of hits");
					Lucene.Net.Documents.Document d = searcher.Doc(hits[0].Doc);
					AreEqual(id.ToString(), d.Get("id"));
					hits = searcher.Search(NumericRangeQuery.NewLongRange("trieLong", 4, id,id, true, true), 100).ScoreDocs;
					AreEqual(1, hits.Length, "wrong number of hits");
					d = searcher.Doc(hits[0].Doc);
					AreEqual(id.ToString(), d.Get("id"));
				}
				// check that also lower-precision fields are ok
				ScoreDoc[] hits_1 = searcher.Search(NumericRangeQuery.NewIntRange("trieInt", 4, int.MinValue
					, int.MaxValue, false, false), 100).ScoreDocs;
				AreEqual(34, hits_1.Length, "wrong number of hits");
				hits_1 = searcher.Search(NumericRangeQuery.NewLongRange("trieLong", 4, long.MinValue
					, long.MaxValue, false, false), 100).ScoreDocs;
				AreEqual(34, hits_1.Length, "wrong number of hits");
				// check decoding into field cache
				FieldCache.Ints fci = FieldCache.DEFAULT.GetInts(SlowCompositeReaderWrapper.Wrap(
					searcher.IndexReader), "trieInt", false);
				int maxDoc = searcher.IndexReader.MaxDoc;
				for (int doc = 0; doc < maxDoc; doc++)
				{
					int val = fci.Get(doc);
					IsTrue(val >= 0 && val < 35, "value in id bounds");
				}
				FieldCache.Longs fcl = FieldCache.DEFAULT.GetLongs(SlowCompositeReaderWrapper.Wrap
					(searcher.IndexReader), "trieLong", false);
				for (int doc_1 = 0; doc_1 < maxDoc; doc_1++)
				{
					long val = fcl.Get(doc_1);
					IsTrue(val >= 0L && val < 35L, "value in id bounds");
				}
				reader.Dispose();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int CheckAllSegmentsUpgraded(Directory dir)
		{
			SegmentInfos infos = new SegmentInfos();
			infos.Read(dir);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("checkAllSegmentsUpgraded: " + infos);
			}
			foreach (SegmentCommitInfo si in infos)
			{
				AreEqual(Constants.LUCENE_MAIN_VERSION, si.info.Version);
			}
			return infos.Count;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int GetNumberOfSegments(Directory dir)
		{
			SegmentInfos infos = new SegmentInfos();
			infos.Read(dir);
			return infos.Count;
		}

        [Test]
		public virtual void TestUpgradeOldIndex()
		{
			IList<string> names = new List<string>(oldNames.Length + oldSingleSegmentNames.Length
				);
			names.AddRange(Arrays.AsList(oldNames));
			names.AddRange(Arrays.AsList(oldSingleSegmentNames));
			foreach (string name in names)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("testUpgradeOldIndex: index=" + name);
				}
				Directory dir = NewDirectory(oldIndexDirs[name]);
				NewIndexUpgrader(dir).Upgrade();
				CheckAllSegmentsUpgraded(dir);
				dir.Dispose();
			}
		}

		[Test]
		public virtual void TestCommandLineArgs()
		{
			foreach (string name in oldIndexDirs.Keys)
			{
				DirectoryInfo dir = CreateTempDir(name);
			    var dataFile = new ZipArchive(typeof (TestBackwardsCompatibility).Assembly.
			        GetManifestResourceStream("index." + name + ".zip"));
				dataFile.ExtractToDirectory(dir.FullName);
                
				string path = dir.FullName;
				IList<string> args = new List<string>();
				if (Random().NextBoolean())
				{
					args.Add("-verbose");
				}
				if (Random().NextBoolean())
				{
					args.Add("-delete-prior-commits");
				}
				if (Random().NextBoolean())
				{
					// TODO: need to better randomize this, but ...
					//  - LuceneTestCase.FS_DIRECTORIES is private
					//  - newFSDirectory returns BaseDirectoryWrapper
					//  - BaseDirectoryWrapper doesn't expose delegate
					Type dirImpl = Random().NextBoolean() ? typeof(SimpleFSDirectory) : typeof(NIOFSDirectory
						);
					args.Add("-dir-impl");
					args.Add(dirImpl.FullName);
				}
				args.Add(path);
				IndexUpgrader upgrader = null;
				try
				{
					upgrader = IndexUpgrader.ParseArgs(Collections.ToArray(args, new string[0
						]));
				}
				catch (Exception e)
				{
					throw new SystemException("unable to parse args: " + args, e);
				}
				upgrader.Upgrade();
				Directory upgradedDir = NewFSDirectory(dir);
				try
				{
					CheckAllSegmentsUpgraded(upgradedDir);
				}
				finally
				{
					upgradedDir.Dispose();
				}
			}
		}

		[Test]
		public virtual void TestUpgradeOldSingleSegmentIndexWithAdditions()
		{
			foreach (string name in oldSingleSegmentNames)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("testUpgradeOldSingleSegmentIndexWithAdditions: index="
						 + name);
				}
				Directory dir = NewDirectory(oldIndexDirs[name]);
				AreEqual(1, GetNumberOfSegments(dir), "Original index must be single segment");
				// create a bunch of dummy segments
				int id = 40;
				RAMDirectory ramDir = new RAMDirectory();
				for (int i = 0; i < 3; i++)
				{
					// only use Log- or TieredMergePolicy, to make document addition predictable and not suddenly merge:
					MergePolicy mp = Random().NextBoolean() ? (MergePolicy) NewLogMergePolicy() : NewTieredMergePolicy();
					IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
						(Random())).SetMergePolicy(mp);
					IndexWriter w = new IndexWriter(ramDir, iwc);
					// add few more docs:
					for (int j = 0; j < RANDOM_MULTIPLIER * Random().Next(30); j++)
					{
						AddDoc(w, id++);
					}
					w.Dispose(false);
				}
				// add dummy segments (which are all in current
				// version) to single segment index
				MergePolicy mp_1 = Random().NextBoolean() ? (MergePolicy) NewLogMergePolicy() : NewTieredMergePolicy
					();
				IndexWriterConfig iwc_1 = new IndexWriterConfig(TEST_VERSION_CURRENT, null).SetMergePolicy
					(mp_1);
				IndexWriter w_1 = new IndexWriter(dir, iwc_1);
				w_1.AddIndexes(ramDir);
				w_1.Dispose(false);
				// determine count of segments in modified index
				int origSegCount = GetNumberOfSegments(dir);
				NewIndexUpgrader(dir).Upgrade();
				int segCount = CheckAllSegmentsUpgraded(dir);
				AreEqual(origSegCount
					, segCount, "Index must still contain the same number of segments, as only one segment was upgraded and nothing else merged");
				dir.Dispose();
			}
		}

		public static readonly string moreTermsIndex = "moreterms.40.zip";

		[Test]
		public virtual void TestMoreTerms()
		{
			DirectoryInfo oldIndexDir = CreateTempDir("moreterms");
			TestUtil.Unzip(GetDataFile(moreTermsIndex), oldIndexDir);
			Directory dir = NewFSDirectory(oldIndexDir);
			// TODO: more tests
			TestUtil.CheckIndex(dir);
			dir.Dispose();
		}
	}
}
