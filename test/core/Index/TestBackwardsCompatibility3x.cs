using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
    public class TestBackwardsCompatibility3x : LuceneTestCase
    {
        internal static readonly string[] oldNames =
		{ "30.cfs", "30.nocfs", 
		    "31.cfs", "31.nocfs", "32.cfs", "32.nocfs", "34.cfs", "34.nocfs" };

        internal readonly string[] unsupportedNames =
		{ "19.cfs", "19.nocfs"
		    , "20.cfs", "20.nocfs", "21.cfs", "21.nocfs", "22.cfs", "22.nocfs", "23.cfs", "23.nocfs"
		    , "24.cfs", "24.nocfs", "29.cfs", "29.nocfs" };

        internal static readonly string[] oldSingleSegmentNames =
		{ "31.optimized.cfs"
		    , "31.optimized.nocfs" };

        internal static IDictionary<string, Directory> oldIndexDirs;

        // don't use 3.x codec, its unrealistic since it means
        // we won't even be running the actual code, only the impostor
        // Sep codec cannot yet handle the offsets we add when changing indexes!
        // Uncomment these cases & run them on an older Lucene
        // version, to generate an index to test backwards
        // compatibility.  Then, cd to build/test/index.cfs and
        // run "zip index.<VERSION>.cfs.zip *"; cd to
        // build/test/index.nocfs and run "zip
        // index.<VERSION>.nocfs.zip *".  Then move those 2 zip
        // files to your trunk checkout and add them to the
        // oldNames array.
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
                DirectoryInfo dir = CreateTempDir(name);
                var dataFile = new ZipArchive(typeof(TestBackwardsCompatibility3x).Assembly.GetManifestResourceStream
                    ("index." + name + ".zip"));
                dataFile.ExtractToDirectory(dir.FullName);

                oldIndexDirs[name] = NewFSDirectory(dir);
            }
        }


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
                DirectoryInfo oldIndexDir = CreateTempDir(unsupportedNames[i]);
                var unsupportedDir = new ZipArchive(
                    typeof(TestBackwardsCompatibility3x).Assembly.GetManifestResourceStream("unsupported." + unsupportedNames[i] + ".zip"));
                unsupportedDir.ExtractToDirectory(oldIndexDir.FullName);

                BaseDirectoryWrapper dir = NewFSDirectory(oldIndexDir);
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
                        Console.Out.WriteLine(e.StackTrace);

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
                IsTrue(bos.ToString().Contains(typeof(IndexFormatTooOldException
                    ).FullName));
                dir.Dispose();
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
                var w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
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
                var w = new IndexWriter(targetDir, NewIndexWriterConfig(TEST_VERSION_CURRENT
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



        [Obsolete(@"3.x transition mechanism")]
        [Test]
        public virtual void TestDeleteOldIndex()
        {
            foreach (string name in oldNames)
            {
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("TEST: oldName=" + name);
                }
                // Try one delete:
                Directory dir = NewDirectory(oldIndexDirs[name]);
                IndexReader ir = DirectoryReader.Open(dir);
                AreEqual(35, ir.NumDocs);
                ir.Dispose();
                var iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
                iw.DeleteDocuments(new Term("id", "3"));
                iw.Dispose();
                ir = DirectoryReader.Open(dir);
                AreEqual(34, ir.NumDocs);
                ir.Dispose();
                // Delete all but 1 document:
                iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
                for (int i = 0; i < 35; i++)
                {
                    iw.DeleteDocuments(new Term("id", string.Empty + i));
                }
                // Verify NRT reader takes:
                ir = DirectoryReader.Open(iw, true);
                iw.Dispose();
                AreEqual(1, ir.NumDocs, "index " + name);
                ir.Dispose();
                // Verify non-NRT reader takes:
                ir = DirectoryReader.Open(dir);
                AreEqual(1, ir.NumDocs, "index " + name);
                ir.Dispose();
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
            IndexSearcher searcher = new IndexSearcher(reader);
            TestUtil.CheckIndex(dir);
            // true if this is a 4.0+ index
            bool is40Index = MultiFields.GetMergedFieldInfos(reader).FieldInfo("content5") != null;
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
                for (int i = 0; i < 35; i++)
                {
                    int id = System.Convert.ToInt32(reader.Document(i).Get("id"));
                    AreEqual(id, dvByte.Get(i));
                    byte[] bytes = {(byte)(id >> 24), (byte)(id >> 16), (byte)(id >> 8), (byte)id };
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
                    AreEqual(id, dvDouble.Get(i).LongBitsToDouble(), 0D);
                    AreEqual(id, ((int)dvFloat.Get(i)).IntBitsToFloat(), 0F);
                    AreEqual(id, dvInt.Get(i));
                    AreEqual(id, dvLong.Get(i));
                    AreEqual(id, dvPacked.Get(i));
                    AreEqual(id, dvShort.Get(i));
                }
            }
            ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa"
                )), null, 1000).ScoreDocs;
            // First document should be #21 since it's norm was
            // increased:
            Lucene.Net.Documents.Document d_1 = searcher.IndexReader.Document(hits
                [0].Doc);
            AreEqual("didn't get the right document first", "21", d_1.
                Get("id"));
            DoTestHits(hits, 34, searcher.IndexReader);
            if (is40Index)
            {
                hits = searcher.Search(new TermQuery(new Term("content5", "aaa")), null
                    , 1000).ScoreDocs;
                DoTestHits(hits, 34, searcher.IndexReader);
                hits = searcher.Search(new TermQuery(new Term("content6", "aaa")), null
                    , 1000).ScoreDocs;
                DoTestHits(hits, 34, searcher.IndexReader);
            }
            hits = searcher.Search(new TermQuery(new Term("utf8", "\u0000")), null, 1000).ScoreDocs;
            AreEqual(34, hits.Length);
            hits = searcher.Search(new TermQuery(new Term("utf8", "Lu\uD834\uDD1Ece\uD834\uDD60ne"
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
                , new MockAnalyzer(random)).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
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
            IndexSearcher searcher = new IndexSearcher(reader);
            ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null
                , 1000).ScoreDocs;
            Lucene.Net.Documents.Document d = searcher.IndexReader.Document(hits[0
                ].Doc);
            AreEqual("wrong first document", "21", d.Get("id"));
            DoTestHits(hits, 44, searcher.IndexReader);
            reader.Dispose();
            // fully merge
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
                (random)).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
            writer.ForceMerge(1);
            writer.Dispose();
            reader = DirectoryReader.Open(dir);
            searcher = new IndexSearcher(reader);
            hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).ScoreDocs;
            AreEqual(44, hits.Length, "wrong number of hits");
            d = searcher.Doc(hits[0].Doc);
            DoTestHits(hits, 44, searcher.IndexReader);
            AreEqual("wrong first document", "21", d.Get("id"));
            reader.Dispose();
        }

        /// <exception cref="System.IO.IOException"></exception>
        public virtual void ChangeIndexNoAdds(Random random, Directory dir)
        {
            // make sure searching sees right # hits
            DirectoryReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = new IndexSearcher(reader);
            ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null
                , 1000).ScoreDocs;
            AreEqual(34, hits.Length, "wrong number of hits");
            Lucene.Net.Documents.Document d = searcher.Doc(hits[0].Doc);
            AreEqual("wrong first document", "21", d.Get("id"));
            reader.Dispose();
            // fully merge
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
                , new MockAnalyzer(random)).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
            writer.ForceMerge(1);
            writer.Dispose();
            reader = DirectoryReader.Open(dir);
            searcher = new IndexSearcher(reader);
            hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).ScoreDocs;
            AreEqual(34, hits.Length, "wrong number of hits");
            DoTestHits(hits, 34, searcher.IndexReader);
            reader.Dispose();
        }

        /// <exception cref="System.IO.IOException"></exception>
        public virtual DirectoryInfo CreateIndex(string dirName, bool doCFS, bool fullyMerged)
        {
            // we use a real directory name that is not cleaned up, because this method is only used to create backwards indexes:
            DirectoryInfo indexDir = CreateTempDir("\\4x" + dirName);
            TestUtil.Rm(indexDir);
            Directory dir = NewFSDirectory(indexDir);
            LogByteSizeMergePolicy mp = new LogByteSizeMergePolicy();
            mp.SetNoCFSRatio(doCFS ? 1.0 : 0.0);
            mp.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
            // TODO: remove randomness
            IndexWriterConfig conf = ((IndexWriterConfig)new IndexWriterConfig
                (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergePolicy
                (mp);
            conf.UseCompoundFile = doCFS;
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
                conf = ((IndexWriterConfig)new IndexWriterConfig(TEST_VERSION_CURRENT
                    , new MockAnalyzer(Random())).SetMaxBufferedDocs(10)).SetMergePolicy(mp);
                conf.UseCompoundFile = (doCFS);
                writer = new IndexWriter(dir, conf);
                AddNoProxDoc(writer);
                writer.Dispose();
                writer = new IndexWriter(dir, conf.SetMergePolicy(doCFS ? NoMergePolicy.COMPOUND_FILES
                     : NoMergePolicy.NO_COMPOUND_FILES));
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
            byte[] bytes = { (byte)(id >> 24), (byte)(id >> 16), (byte)(id >> 8), (byte)id };
            
            BytesRef bRef = new BytesRef(bytes.ToSbytes());
            doc.Add(new BinaryDocValuesField("dvBytesDerefFixed", bRef));
            doc.Add(new BinaryDocValuesField("dvBytesDerefVar", bRef));
            doc.Add(new SortedDocValuesField("dvBytesSortedFixed", bRef));
            doc.Add(new SortedDocValuesField("dvBytesSortedVar", bRef));
            doc.Add(new BinaryDocValuesField("dvBytesStraightFixed", bRef));
            doc.Add(new BinaryDocValuesField("dvBytesStraightVar", bRef));
            doc.Add(new DoubleDocValuesField("dvDouble", (double)id));
            doc.Add(new FloatDocValuesField("dvFloat", (float)id));
            doc.Add(new NumericDocValuesField("dvInt", id));
            doc.Add(new NumericDocValuesField("dvLong", id));
            doc.Add(new NumericDocValuesField("dvPacked", id));
            doc.Add(new NumericDocValuesField("dvShort", (short)id));
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
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.IndexOptions = (FieldInfo.IndexOptions.DOCS_ONLY);
            Field f = new Field("content3", "aaa", customType);
            doc.Add(f);
            FieldType customType2 = new FieldType();
            customType2.Stored = (true);
            customType2.IndexOptions = (FieldInfo.IndexOptions.DOCS_ONLY);
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
                    , null, 0)));
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
                    , null, 0)));
                IsNull(terms.Next());
                AreEqual(TermsEnum.SeekStatus.FOUND, terms.SeekCeil(aaaTerm
                    ));
                AreEqual(35, CountDocs(TestUtil.Docs(Random(), terms, null
                    , null, 0)));
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
                    // TODO: does preflex codec actually set "3.0" here? This is safe to do I think.
                    // assertNotNull(oldVersion);
                    IsTrue(oldVersion == null || comparator.Compare(oldVersion, currentVersion) < 0
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
                IndexSearcher searcher = new IndexSearcher(reader);
                for (int id = 10; id < 15; id++)
                {
                    ScoreDoc[] hits = searcher.Search(NumericRangeQuery.NewIntRange("trieInt", 4, id, id, true, true), 100).ScoreDocs;
                    AreEqual(1, hits.Length, "wrong number of hits");
                    Lucene.Net.Documents.Document d = searcher.Doc(hits[0].Doc);
                    AreEqual(id.ToString(), d.Get("id"));
                    hits = searcher.Search(NumericRangeQuery.NewLongRange("trieLong", 4, id, id, true, true), 100).ScoreDocs;
                    AreEqual(1, hits.Length, "wrong number of hits");
                    d = searcher.Doc(hits[0].Doc);
                    AreEqual(id.ToString(), d.Get("id"));
                }
                // check that also lower-precision fields are ok
                ScoreDoc[] hits2 = searcher.Search(NumericRangeQuery.NewIntRange("trieInt", 4, int.MinValue
                    , int.MaxValue, false, false), 100).ScoreDocs;
                AreEqual(34, hits2.Length, "wrong number of hits");
                hits2 = searcher.Search(NumericRangeQuery.NewLongRange("trieLong", 4, long.MinValue
                    , long.MaxValue, false, false), 100).ScoreDocs;
                AreEqual(34, hits2.Length, "wrong number of hits");
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
                for (int doc2 = 0; doc2 < maxDoc; doc2++)
                {
                    long val = fcl.Get(doc2);
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
            IList<string> names = new List<string>(oldNames.Length + oldSingleSegmentNames.Length);
            names.AddRange(Arrays.AsList(oldNames));
            names.AddRange(Arrays.AsList(oldSingleSegmentNames));
            foreach (string name in names)
            {
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("testUpgradeOldIndex: index=" + name);
                }
                Directory dir = NewDirectory(oldIndexDirs[name]);
                new IndexUpgrader(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null), false).Upgrade
                    ();
                CheckAllSegmentsUpgraded(dir);
                dir.Dispose();
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
                AreEqual(1, GetNumberOfSegments
                    (dir), "Original index must be single segment");
                // create a bunch of dummy segments
                int id = 40;
                RAMDirectory ramDir = new RAMDirectory();
                for (int i = 0; i < 3; i++)
                {
                    // only use Log- or TieredMergePolicy, to make document addition predictable and not suddenly merge:
                    MergePolicy mp = Random().NextBoolean() ? (MergePolicy) NewLogMergePolicy() : NewTieredMergePolicy
                        ();
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
                MergePolicy mp_1 = Random().NextBoolean() ? (MergePolicy) NewLogMergePolicy() : NewTieredMergePolicy();
                IndexWriterConfig iwc_1 = new IndexWriterConfig(TEST_VERSION_CURRENT, null).SetMergePolicy
                    (mp_1);
                IndexWriter w_1 = new IndexWriter(dir, iwc_1);
                w_1.AddIndexes(ramDir);
                w_1.Dispose(false);
                // determine count of segments in modified index
                int origSegCount = GetNumberOfSegments(dir);
                new IndexUpgrader(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null), false).Upgrade
                    ();
                int segCount = CheckAllSegmentsUpgraded(dir);
                AreEqual(origSegCount
                    , segCount, "Index must still contain the same number of segments, as only one segment was upgraded and nothing else merged");
                dir.Dispose();
            }
        }

        public static readonly string surrogatesIndexName = "index.36.surrogates.zip";

        [Test]
        public virtual void TestSurrogates()
        {
            DirectoryInfo oldIndexDir = CreateTempDir("surrogates");
            var zipArchive = new ZipArchive(typeof (TestBackwardsCompatibility3x).Assembly.GetManifestResourceStream(surrogatesIndexName));
            zipArchive.ExtractToDirectory(oldIndexDir.FullName);
            
            Directory dir = NewFSDirectory(oldIndexDir);
            // TODO: more tests
            TestUtil.CheckIndex(dir);
            dir.Dispose();
        }

        public static readonly string bogus24IndexName = "bogus24.upgraded.to.36.zip";

        [Test]
        public virtual void TestNegativePositions()
        {
            DirectoryInfo oldIndexDir = CreateTempDir("negatives");
            //TODO: refactor these to a common method
            var zipArchive = new ZipArchive(typeof(TestBackwardsCompatibility3x).Assembly.GetManifestResourceStream(bogus24IndexName));
            zipArchive.ExtractToDirectory(oldIndexDir.FullName);
            Directory dir = NewFSDirectory(oldIndexDir);
            DirectoryReader ir = DirectoryReader.Open(dir);
            IndexSearcher @is = new IndexSearcher(ir);
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("field3", "more"));
            pq.Add(new Term("field3", "text"));
            TopDocs td = @is.Search(pq, 10);
            AreEqual(1, td.TotalHits);
            AtomicReader wrapper = SlowCompositeReaderWrapper.Wrap(ir);
            DocsAndPositionsEnum de = wrapper.TermPositionsEnum(new Term("field3", "broken"));
            //HM:revisit 
            //assert de != null;
            AreEqual(0, de.NextDoc());
            AreEqual(0, de.NextPosition());
            ir.Dispose();
            TestUtil.CheckIndex(dir);
            dir.Dispose();
        }
    }
}
