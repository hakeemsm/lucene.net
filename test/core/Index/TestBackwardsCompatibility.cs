/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestBackwardsCompatibility : LuceneTestCase
	{
		internal static readonly string[] oldNames = new string[] { "40.cfs", "40.nocfs", 
			"41.cfs", "41.nocfs", "42.cfs", "42.nocfs", "45.cfs", "45.nocfs", "461.cfs", "461.nocfs"
			 };

		internal readonly string[] unsupportedNames = new string[] { "19.cfs", "19.nocfs"
			, "20.cfs", "20.nocfs", "21.cfs", "21.nocfs", "22.cfs", "22.nocfs", "23.cfs", "23.nocfs"
			, "24.cfs", "24.nocfs", "29.cfs", "29.nocfs" };

		internal static readonly string[] oldSingleSegmentNames = new string[] { "40.optimized.cfs"
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
			int choice = TestUtil.NextInt(Random(), 0, 2);
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
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			IsFalse("test infra is broken!", LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE
				);
			IList<string> names = new AList<string>(oldNames.Length + oldSingleSegmentNames.Length
				);
			Sharpen.Collections.AddAll(names, Arrays.AsList(oldNames));
			Sharpen.Collections.AddAll(names, Arrays.AsList(oldSingleSegmentNames));
			oldIndexDirs = new Dictionary<string, Directory>();
			foreach (string name in names)
			{
				FilePath dir = CreateTempDir(name);
				FilePath dataFile = new FilePath(typeof(TestBackwardsCompatibility).GetResource("index."
					 + name + ".zip").ToURI());
				TestUtil.Unzip(dataFile, dir);
				oldIndexDirs.Put(name, NewFSDirectory(dir));
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			foreach (Directory d in oldIndexDirs.Values)
			{
				d.Close();
			}
			oldIndexDirs = null;
		}

		/// <summary>This test checks that *only* IndexFormatTooOldExceptions are thrown when you open and operate on too old indexes!
		/// 	</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestUnsupportedOldIndexes()
		{
			for (int i = 0; i < unsupportedNames.Length; i++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: index " + unsupportedNames[i]);
				}
				FilePath oldIndxeDir = CreateTempDir(unsupportedNames[i]);
				TestUtil.Unzip(GetDataFile("unsupported." + unsupportedNames[i] + ".zip"), oldIndxeDir
					);
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
						reader.Close();
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
						Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
					}
					// Make sure exc message includes a path=
					IsTrue("got exc message: " + e.Message, e.Message.IndexOf(
						"path=\"") != -1);
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
						writer.Close(false);
					}
					writer = null;
				}
				ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
				CheckIndex checker = new CheckIndex(dir);
				checker.SetInfoStream(new TextWriter(bos, false, IOUtils.UTF_8));
				CheckIndex.Status indexStatus = checker.CheckIndex();
				IsFalse(indexStatus.clean);
				IsTrue(bos.ToString(IOUtils.UTF_8).Contains(typeof(IndexFormatTooOldException
					).FullName));
				dir.Close();
				TestUtil.Rm(oldIndxeDir);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFullyMergeOldIndex()
		{
			foreach (string name in oldNames)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: index=" + name);
				}
				Directory dir = NewDirectory(oldIndexDirs.Get(name));
				IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
					new MockAnalyzer(Random())));
				w.ForceMerge(1);
				w.Close();
				dir.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
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
				w.AddIndexes(oldIndexDirs.Get(name));
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: done adding indices; now close");
				}
				w.Close();
				targetDir.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestAddOldIndexesReader()
		{
			foreach (string name in oldNames)
			{
				IndexReader reader = DirectoryReader.Open(oldIndexDirs.Get(name));
				Directory targetDir = NewDirectory();
				IndexWriter w = new IndexWriter(targetDir, NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())));
				w.AddIndexes(reader);
				w.Close();
				reader.Close();
				targetDir.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSearchOldIndex()
		{
			foreach (string name in oldNames)
			{
				SearchIndex(oldIndexDirs.Get(name), name);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIndexOldIndexNoAdds()
		{
			foreach (string name in oldNames)
			{
				Directory dir = NewDirectory(oldIndexDirs.Get(name));
				ChangeIndexNoAdds(Random(), dir);
				dir.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIndexOldIndex()
		{
			foreach (string name in oldNames)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: oldName=" + name);
				}
				Directory dir = NewDirectory(oldIndexDirs.Get(name));
				ChangeIndexWithAdds(Random(), dir, name);
				dir.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DoTestHits(ScoreDoc[] hits, int expectedCount, IndexReader reader)
		{
			int hitCount = hits.Length;
			AreEqual("wrong number of hits", expectedCount, hitCount);
			for (int i = 0; i < hitCount; i++)
			{
				reader.Document(hits[i].doc);
				reader.GetTermVectors(hits[i].doc);
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
			Bits liveDocs = MultiFields.GetLiveDocs(reader);
			for (int i = 0; i < 35; i++)
			{
				if (liveDocs.Get(i))
				{
					Lucene.Net.Documents.Document d = reader.Document(i);
					IList<IIndexableField> fields = d.GetFields();
					bool isProxDoc = d.GetField("content3") == null;
					if (isProxDoc)
					{
						int numFields = is40Index ? 7 : 5;
						AreEqual(numFields, fields.Count);
						IIndexableField f = d.GetField("id");
						AreEqual(string.Empty + i, f.StringValue = ));
						f = d.GetField("utf8");
						AreEqual("Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd"
							, f.StringValue = ));
						f = d.GetField("autf8");
						AreEqual("Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd"
							, f.StringValue = ));
						f = d.GetField("content2");
						AreEqual("here is more content with aaa aaa aaa", f.StringValue
							());
						f = d.GetField("fie\u2C77ld");
						AreEqual("field with non-ascii name", f.StringValue = ));
					}
					Fields tfvFields = reader.GetTermVectors(i);
					IsNotNull("i=" + i, tfvFields);
					Terms tfv = tfvFields.Terms("utf8");
					IsNotNull("docID=" + i + " index=" + oldName, tfv);
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
				for (int i_1 = 0; i_1 < 35; i_1++)
				{
					int id = System.Convert.ToInt32(reader.Document(i_1).Get("id"));
					AreEqual(id, dvByte.Get(i_1));
					byte[] bytes = new byte[] { unchecked((byte)((int)(((uint)id) >> 24))), unchecked(
						(byte)((int)(((uint)id) >> 16))), unchecked((byte)((int)(((uint)id) >> 8))), unchecked(
						(byte)id) };
					BytesRef expectedRef = new BytesRef(bytes);
					BytesRef scratch = new BytesRef();
					dvBytesDerefFixed.Get(i_1, scratch);
					AreEqual(expectedRef, scratch);
					dvBytesDerefVar.Get(i_1, scratch);
					AreEqual(expectedRef, scratch);
					dvBytesSortedFixed.Get(i_1, scratch);
					AreEqual(expectedRef, scratch);
					dvBytesSortedVar.Get(i_1, scratch);
					AreEqual(expectedRef, scratch);
					dvBytesStraightFixed.Get(i_1, scratch);
					AreEqual(expectedRef, scratch);
					dvBytesStraightVar.Get(i_1, scratch);
					AreEqual(expectedRef, scratch);
					AreEqual((double)id, double.LongBitsToDouble(dvDouble.Get(
						i_1)), 0D);
					AreEqual((float)id, Sharpen.Runtime.IntBitsToFloat((int)dvFloat
						.Get(i_1)), 0F);
					AreEqual(id, dvInt.Get(i_1));
					AreEqual(id, dvLong.Get(i_1));
					AreEqual(id, dvPacked.Get(i_1));
					AreEqual(id, dvShort.Get(i_1));
					if (is42Index)
					{
						dvSortedSet.SetDocument(i_1);
						long ord = dvSortedSet.NextOrd();
						AreEqual(SortedSetDocValues.NO_MORE_ORDS, dvSortedSet.NextOrd
							());
						dvSortedSet.LookupOrd(ord, scratch);
						AreEqual(expectedRef, scratch);
					}
				}
			}
			ScoreDoc[] hits = searcher.Search(new TermQuery(new Term(new string("content"), "aaa"
				)), null, 1000).scoreDocs;
			// First document should be #0
			Lucene.Net.Documents.Document d_1 = searcher.GetIndexReader().Document(hits
				[0].doc);
			AreEqual("didn't get the right document first", "0", d_1.Get
				("id"));
			DoTestHits(hits, 34, searcher.GetIndexReader());
			if (is40Index)
			{
				hits = searcher.Search(new TermQuery(new Term(new string("content5"), "aaa")), null
					, 1000).scoreDocs;
				DoTestHits(hits, 34, searcher.GetIndexReader());
				hits = searcher.Search(new TermQuery(new Term(new string("content6"), "aaa")), null
					, 1000).scoreDocs;
				DoTestHits(hits, 34, searcher.GetIndexReader());
			}
			hits = searcher.Search(new TermQuery(new Term("utf8", "\u0000")), null, 1000).scoreDocs;
			AreEqual(34, hits.Length);
			hits = searcher.Search(new TermQuery(new Term(new string("utf8"), "lu\uD834\uDD1Ece\uD834\uDD60ne"
				)), null, 1000).scoreDocs;
			AreEqual(34, hits.Length);
			hits = searcher.Search(new TermQuery(new Term("utf8", "ab\ud917\udc17cd")), null, 
				1000).scoreDocs;
			AreEqual(34, hits.Length);
			reader.Close();
		}

		private int Compare(string name, string v)
		{
			int v0 = System.Convert.ToInt32(Sharpen.Runtime.Substring(name, 0, 2));
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
			AreEqual("wrong doc count", expected, writer.NumDocs());
			writer.Close();
			// make sure searching sees right # hits
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null
				, 1000).scoreDocs;
			Lucene.Net.Documents.Document d = searcher.GetIndexReader().Document(hits[0
				].doc);
			AreEqual("wrong first document", "0", d.Get("id"));
			DoTestHits(hits, 44, searcher.GetIndexReader());
			reader.Close();
			// fully merge
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(random)).SetOpenMode(IndexWriterConfig.OpenMode.APPEND).SetMergePolicy(NewLogMergePolicy
				()));
			writer.ForceMerge(1);
			writer.Close();
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
			hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).scoreDocs;
			AreEqual("wrong number of hits", 44, hits.Length);
			d = searcher.Doc(hits[0].doc);
			DoTestHits(hits, 44, searcher.GetIndexReader());
			AreEqual("wrong first document", "0", d.Get("id"));
			reader.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void ChangeIndexNoAdds(Random random, Directory dir)
		{
			// make sure searching sees right # hits
			DirectoryReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null
				, 1000).scoreDocs;
			AreEqual("wrong number of hits", 34, hits.Length);
			Lucene.Net.Documents.Document d = searcher.Doc(hits[0].doc);
			AreEqual("wrong first document", "0", d.Get("id"));
			reader.Close();
			// fully merge
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(random)).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			writer.ForceMerge(1);
			writer.Close();
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
			hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).scoreDocs;
			AreEqual("wrong number of hits", 34, hits.Length);
			DoTestHits(hits, 34, searcher.GetIndexReader());
			reader.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual FilePath CreateIndex(string dirName, bool doCFS, bool fullyMerged)
		{
			// we use a real directory name that is not cleaned up, because this method is only used to create backwards indexes:
			FilePath indexDir = new FilePath("/tmp/idx", dirName);
			TestUtil.Rm(indexDir);
			Directory dir = NewFSDirectory(indexDir);
			LogByteSizeMergePolicy mp = new LogByteSizeMergePolicy();
			mp.SetNoCFSRatio(doCFS ? 1.0 : 0.0);
			mp.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
			// TODO: remove randomness
			IndexWriterConfig conf = ((IndexWriterConfig)((IndexWriterConfig)new IndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetUseCompoundFile(doCFS)).SetMaxBufferedDocs
				(10)).SetMergePolicy(mp);
			IndexWriter writer = new IndexWriter(dir, conf);
			for (int i = 0; i < 35; i++)
			{
				AddDoc(writer, i);
			}
			AreEqual("wrong doc count", 35, writer.MaxDoc);
			if (fullyMerged)
			{
				writer.ForceMerge(1);
			}
			writer.Close();
			if (!fullyMerged)
			{
				// open fresh writer so we get no prx file in the added segment
				mp = new LogByteSizeMergePolicy();
				mp.SetNoCFSRatio(doCFS ? 1.0 : 0.0);
				// TODO: remove randomness
				conf = ((IndexWriterConfig)((IndexWriterConfig)new IndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetUseCompoundFile(doCFS)).SetMaxBufferedDocs(10))
					.SetMergePolicy(mp);
				writer = new IndexWriter(dir, conf);
				AddNoProxDoc(writer);
				writer.Close();
				conf = ((IndexWriterConfig)((IndexWriterConfig)new IndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetUseCompoundFile(doCFS)).SetMaxBufferedDocs(10))
					.SetMergePolicy(doCFS ? NoMergePolicy.COMPOUND_FILES : NoMergePolicy.NO_COMPOUND_FILES
					);
				writer = new IndexWriter(dir, conf);
				Term searchTerm = new Term("id", "7");
				writer.DeleteDocuments(searchTerm);
				writer.Close();
			}
			dir.Close();
			return indexDir;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(IndexWriter writer, int id)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField("content", "aaa", Field.Store.NO));
			doc.Add(new StringField("id", Sharpen.Extensions.ToString(id), Field.Store.YES));
			FieldType customType2 = new FieldType(TextField.TYPE_STORED);
			customType2.StoreTermVectors = true;
			customType2.StoreTermVectorPositions = true;
			customType2.StoreTermVectorOffsets = true;
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
			BytesRef @ref = new BytesRef(bytes);
			doc.Add(new BinaryDocValuesField("dvBytesDerefFixed", @ref));
			doc.Add(new BinaryDocValuesField("dvBytesDerefVar", @ref));
			doc.Add(new SortedDocValuesField("dvBytesSortedFixed", @ref));
			doc.Add(new SortedDocValuesField("dvBytesSortedVar", @ref));
			doc.Add(new BinaryDocValuesField("dvBytesStraightFixed", @ref));
			doc.Add(new BinaryDocValuesField("dvBytesStraightVar", @ref));
			doc.Add(new DoubleDocValuesField("dvDouble", (double)id));
			doc.Add(new FloatDocValuesField("dvFloat", (float)id));
			doc.Add(new NumericDocValuesField("dvInt", id));
			doc.Add(new NumericDocValuesField("dvLong", id));
			doc.Add(new NumericDocValuesField("dvPacked", id));
			doc.Add(new NumericDocValuesField("dvShort", (short)id));
			doc.Add(new SortedSetDocValuesField("dvSortedSet", @ref));
			// a field with both offsets and term vectors for a cross-check
			FieldType customType3 = new FieldType(TextField.TYPE_STORED);
			customType3.StoreTermVectors = true;
			customType3.StoreTermVectorPositions = true;
			customType3.StoreTermVectorOffsets = true;
			customType3.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
				);
			doc.Add(new Field("content5", "here is more content with aaa aaa aaa", customType3
				));
			// a field that omits only positions
			FieldType customType4 = new FieldType(TextField.TYPE_STORED);
			customType4.StoreTermVectors = true;
			customType4.SetStoreTermVectorPositions(false);
			customType4.StoreTermVectorOffsets = true;
			customType4.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS);
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
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY);
			Field f = new Field("content3", "aaa", customType);
			doc.Add(f);
			FieldType customType2 = new FieldType();
			customType2.SetStored(true);
			customType2.SetIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY);
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
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNextIntoWrongField()
		{
			foreach (string name in oldNames)
			{
				Directory dir = oldIndexDirs.Get(name);
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
				IsTrue(terms.Term().BytesEquals(aaaTerm));
				AreEqual(35, CountDocs(TestUtil.Docs(Random(), terms, null
					, null, DocsEnum.FLAG_NONE)));
				IsNull(terms.Next());
				AreEqual(TermsEnum.SeekStatus.FOUND, terms.SeekCeil(aaaTerm
					));
				AreEqual(35, CountDocs(TestUtil.Docs(Random(), terms, null
					, null, DocsEnum.FLAG_NONE)));
				IsNull(terms.Next());
				r.Close();
			}
		}

		/// <summary>Test that we didn't forget to bump the current Constants.LUCENE_MAIN_VERSION.
		/// 	</summary>
		/// <remarks>
		/// Test that we didn't forget to bump the current Constants.LUCENE_MAIN_VERSION.
		/// This is important so that we can determine which version of lucene wrote the segment.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestOldVersions()
		{
			// first create a little index with the current code and get the version
			Directory currentDir = NewDirectory();
			RandomIndexWriter riw = new RandomIndexWriter(Random(), currentDir);
			riw.AddDocument(new Lucene.Net.Documents.Document());
			riw.Close();
			DirectoryReader ir = DirectoryReader.Open(currentDir);
			SegmentReader air = (SegmentReader)((AtomicReader)ir.Leaves()[0].Reader());
			string currentVersion = air.GetSegmentInfo().info.GetVersion();
			IsNotNull(currentVersion);
			// only 3.0 segments can have a null version
			ir.Close();
			currentDir.Close();
			IComparer<string> comparator = StringHelper.GetVersionComparator();
			// now check all the old indexes, their version should be < the current version
			foreach (string name in oldNames)
			{
				Directory dir = oldIndexDirs.Get(name);
				DirectoryReader r = DirectoryReader.Open(dir);
				foreach (AtomicReaderContext context in r.Leaves())
				{
					air = (SegmentReader)((AtomicReader)context.Reader());
					string oldVersion = air.GetSegmentInfo().info.GetVersion();
					IsNotNull(oldVersion);
					// only 3.0 segments can have a null version
					IsTrue("current Constants.LUCENE_MAIN_VERSION is <= an old index: did you forget to bump it?!"
						, comparator.Compare(oldVersion, currentVersion) < 0);
				}
				r.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNumericFields()
		{
			foreach (string name in oldNames)
			{
				Directory dir = oldIndexDirs.Get(name);
				IndexReader reader = DirectoryReader.Open(dir);
				IndexSearcher searcher = NewSearcher(reader);
				for (int id = 10; id < 15; id++)
				{
					ScoreDoc[] hits = searcher.Search(NumericRangeQuery.NewIntRange("trieInt", 4, Sharpen.Extensions.ValueOf
						(id), Sharpen.Extensions.ValueOf(id), true, true), 100).scoreDocs;
					AreEqual("wrong number of hits", 1, hits.Length);
					Lucene.Net.Documents.Document d = searcher.Doc(hits[0].doc);
					AreEqual(id.ToString(), d.Get("id"));
					hits = searcher.Search(NumericRangeQuery.NewLongRange("trieLong", 4, Sharpen.Extensions.ValueOf
						(id), Sharpen.Extensions.ValueOf(id), true, true), 100).scoreDocs;
					AreEqual("wrong number of hits", 1, hits.Length);
					d = searcher.Doc(hits[0].doc);
					AreEqual(id.ToString(), d.Get("id"));
				}
				// check that also lower-precision fields are ok
				ScoreDoc[] hits_1 = searcher.Search(NumericRangeQuery.NewIntRange("trieInt", 4, int.MinValue
					, int.MaxValue, false, false), 100).scoreDocs;
				AreEqual("wrong number of hits", 34, hits_1.Length);
				hits_1 = searcher.Search(NumericRangeQuery.NewLongRange("trieLong", 4, long.MinValue
					, long.MaxValue, false, false), 100).scoreDocs;
				AreEqual("wrong number of hits", 34, hits_1.Length);
				// check decoding into field cache
				FieldCache.Ints fci = FieldCache.DEFAULT.GetInts(SlowCompositeReaderWrapper.Wrap(
					searcher.GetIndexReader()), "trieInt", false);
				int maxDoc = searcher.GetIndexReader().MaxDoc;
				for (int doc = 0; doc < maxDoc; doc++)
				{
					int val = fci.Get(doc);
					IsTrue("value in id bounds", val >= 0 && val < 35);
				}
				FieldCache.Longs fcl = FieldCache.DEFAULT.GetLongs(SlowCompositeReaderWrapper.Wrap
					(searcher.GetIndexReader()), "trieLong", false);
				for (int doc_1 = 0; doc_1 < maxDoc; doc_1++)
				{
					long val = fcl.Get(doc_1);
					IsTrue("value in id bounds", val >= 0L && val < 35L);
				}
				reader.Close();
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
				AreEqual(Constants.LUCENE_MAIN_VERSION, si.info.GetVersion
					());
			}
			return infos.Size();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int GetNumberOfSegments(Directory dir)
		{
			SegmentInfos infos = new SegmentInfos();
			infos.Read(dir);
			return infos.Size();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestUpgradeOldIndex()
		{
			IList<string> names = new AList<string>(oldNames.Length + oldSingleSegmentNames.Length
				);
			Sharpen.Collections.AddAll(names, Arrays.AsList(oldNames));
			Sharpen.Collections.AddAll(names, Arrays.AsList(oldSingleSegmentNames));
			foreach (string name in names)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("testUpgradeOldIndex: index=" + name);
				}
				Directory dir = NewDirectory(oldIndexDirs.Get(name));
				NewIndexUpgrader(dir).Upgrade();
				CheckAllSegmentsUpgraded(dir);
				dir.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCommandLineArgs()
		{
			foreach (string name in oldIndexDirs.Keys)
			{
				FilePath dir = CreateTempDir(name);
				FilePath dataFile = new FilePath(typeof(TestBackwardsCompatibility).GetResource("index."
					 + name + ".zip").ToURI());
				TestUtil.Unzip(dataFile, dir);
				string path = dir.GetAbsolutePath();
				IList<string> args = new AList<string>();
				if (Random().NextBoolean())
				{
					args.AddItem("-verbose");
				}
				if (Random().NextBoolean())
				{
					args.AddItem("-delete-prior-commits");
				}
				if (Random().NextBoolean())
				{
					// TODO: need to better randomize this, but ...
					//  - LuceneTestCase.FS_DIRECTORIES is private
					//  - newFSDirectory returns BaseDirectoryWrapper
					//  - BaseDirectoryWrapper doesn't expose delegate
					Type dirImpl = Random().NextBoolean() ? typeof(SimpleFSDirectory) : typeof(NIOFSDirectory
						);
					args.AddItem("-dir-impl");
					args.AddItem(dirImpl.FullName);
				}
				args.AddItem(path);
				IndexUpgrader upgrader = null;
				try
				{
					upgrader = IndexUpgrader.ParseArgs(Sharpen.Collections.ToArray(args, new string[0
						]));
				}
				catch (Exception e)
				{
					throw new RuntimeException("unable to parse args: " + args, e);
				}
				upgrader.Upgrade();
				Directory upgradedDir = NewFSDirectory(dir);
				try
				{
					CheckAllSegmentsUpgraded(upgradedDir);
				}
				finally
				{
					upgradedDir.Close();
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestUpgradeOldSingleSegmentIndexWithAdditions()
		{
			foreach (string name in oldSingleSegmentNames)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("testUpgradeOldSingleSegmentIndexWithAdditions: index="
						 + name);
				}
				Directory dir = NewDirectory(oldIndexDirs.Get(name));
				AreEqual("Original index must be single segment", 1, GetNumberOfSegments
					(dir));
				// create a bunch of dummy segments
				int id = 40;
				RAMDirectory ramDir = new RAMDirectory();
				for (int i = 0; i < 3; i++)
				{
					// only use Log- or TieredMergePolicy, to make document addition predictable and not suddenly merge:
					MergePolicy mp = Random().NextBoolean() ? NewLogMergePolicy() : NewTieredMergePolicy
						();
					IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
						(Random())).SetMergePolicy(mp);
					IndexWriter w = new IndexWriter(ramDir, iwc);
					// add few more docs:
					for (int j = 0; j < RANDOM_MULTIPLIER * Random().Next(30); j++)
					{
						AddDoc(w, id++);
					}
					w.Close(false);
				}
				// add dummy segments (which are all in current
				// version) to single segment index
				MergePolicy mp_1 = Random().NextBoolean() ? NewLogMergePolicy() : NewTieredMergePolicy
					();
				IndexWriterConfig iwc_1 = new IndexWriterConfig(TEST_VERSION_CURRENT, null).SetMergePolicy
					(mp_1);
				IndexWriter w_1 = new IndexWriter(dir, iwc_1);
				w_1.AddIndexes(ramDir);
				w_1.Close(false);
				// determine count of segments in modified index
				int origSegCount = GetNumberOfSegments(dir);
				NewIndexUpgrader(dir).Upgrade();
				int segCount = CheckAllSegmentsUpgraded(dir);
				AreEqual("Index must still contain the same number of segments, as only one segment was upgraded and nothing else merged"
					, origSegCount, segCount);
				dir.Close();
			}
		}

		public static readonly string moreTermsIndex = "moreterms.40.zip";

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMoreTerms()
		{
			FilePath oldIndexDir = CreateTempDir("moreterms");
			TestUtil.Unzip(GetDataFile(moreTermsIndex), oldIndexDir);
			Directory dir = NewFSDirectory(oldIndexDir);
			// TODO: more tests
			TestUtil.CheckIndex(dir);
			dir.Close();
		}
	}
}
