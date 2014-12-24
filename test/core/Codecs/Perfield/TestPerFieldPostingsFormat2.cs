using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Codecs.Mocksep;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Perfield
{
    [TestFixture]
	public class TestPerFieldPostingsFormat2 : LuceneTestCase
	{
		//TODO: would be better in this test to pull termsenums and instanceof or something?
		// this way we can verify PFPF is doing the right thing.
		// for now we do termqueries.
		/// <exception cref="System.IO.IOException"></exception>
		private IndexWriter NewWriter(Directory dir, IndexWriterConfig conf)
		{
			LogDocMergePolicy logByteSizeMergePolicy = new LogDocMergePolicy();
			logByteSizeMergePolicy.SetNoCFSRatio(0.0);
			// make sure we use plain
			// files
			conf.SetMergePolicy(logByteSizeMergePolicy);
			IndexWriter writer = new IndexWriter(dir, conf);
			return writer;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDocs(IndexWriter writer, int numDocs)
		{
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				{
				    NewTextField("content", "aaa", Field.Store.NO)
				};
			    writer.AddDocument(doc);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDocs2(IndexWriter writer, int numDocs)
		{
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewTextField("content", "bbb", Field.Store.NO));
				writer.AddDocument(doc);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDocs3(IndexWriter writer, int numDocs)
		{
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewTextField("content", "ccc", Field.Store.NO));
				doc.Add(NewStringField("id", string.Empty + i, Field.Store.YES));
				writer.AddDocument(doc);
			}
		}

		
		[NUnit.Framework.Test]
		public virtual void TestMergeUnusedPerFieldCodec()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwconf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetCodec(new MockCodec());
			IndexWriter writer = NewWriter(dir, iwconf);
			AddDocs(writer, 10);
			writer.Commit();
			AddDocs3(writer, 10);
			writer.Commit();
			AddDocs2(writer, 10);
			writer.Commit();
			AreEqual(30, writer.MaxDoc);
			TestUtil.CheckIndex(dir);
			writer.ForceMerge(1);
			AreEqual(30, writer.MaxDoc);
			writer.Dispose();
			dir.Dispose();
		}

		// TODO: not sure this test is that great, we should probably peek inside PerFieldPostingsFormat or something?!
		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestChangeCodecAndMerge()
		{
			Directory dir = NewDirectory();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: make new index");
			}
			IndexWriterConfig iwconf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetCodec(new MockCodec());
			iwconf.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
			//((LogMergePolicy) iwconf.getMergePolicy()).setMergeFactor(10);
			IndexWriter writer = NewWriter(dir, iwconf);
			AddDocs(writer, 10);
			writer.Commit();
			AssertQuery(new Term("content", "aaa"), dir, 10);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: addDocs3");
			}
			AddDocs3(writer, 10);
			writer.Commit();
			writer.Dispose();
			AssertQuery(new Term("content", "ccc"), dir, 10);
			AssertQuery(new Term("content", "aaa"), dir, 10);
			Codec codec = iwconf.Codec;
			iwconf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode
				(IndexWriterConfig.OpenMode.APPEND).SetCodec(codec);
			//((LogMergePolicy) iwconf.getMergePolicy()).setNoCFSRatio(0.0);
			//((LogMergePolicy) iwconf.getMergePolicy()).setMergeFactor(10);
			iwconf.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
			iwconf.SetCodec(new TestPerFieldPostingsFormat2.MockCodec2());
			// uses standard for field content
			writer = NewWriter(dir, iwconf);
			// swap in new codec for currently written segments
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: add docs w/ Standard codec for content field"
					);
			}
			AddDocs2(writer, 10);
			writer.Commit();
			codec = iwconf.Codec;
			AreEqual(30, writer.MaxDoc);
			AssertQuery(new Term("content", "bbb"), dir, 10);
			AssertQuery(new Term("content", "ccc"), dir, 10);
			////
			AssertQuery(new Term("content", "aaa"), dir, 10);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: add more docs w/ new codec");
			}
			AddDocs2(writer, 10);
			writer.Commit();
			AssertQuery(new Term("content", "ccc"), dir, 10);
			AssertQuery(new Term("content", "bbb"), dir, 20);
			AssertQuery(new Term("content", "aaa"), dir, 10);
			AreEqual(40, writer.MaxDoc);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: now optimize");
			}
			writer.ForceMerge(1);
			AreEqual(40, writer.MaxDoc);
			writer.Dispose();
			AssertQuery(new Term("content", "ccc"), dir, 10);
			AssertQuery(new Term("content", "bbb"), dir, 20);
			AssertQuery(new Term("content", "aaa"), dir, 10);
			dir.Dispose();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertQuery(Term t, Directory dir, int num)
		{
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: assertQuery " + t);
			}
			IndexReader reader = DirectoryReader.Open(dir, 1);
			IndexSearcher searcher = NewSearcher(reader);
			TopDocs search = searcher.Search(new TermQuery(t), num + 10);
			AreEqual(num, search.TotalHits);
			reader.Dispose();
		}

		public class MockCodec : Lucene46Codec
		{
			internal readonly PostingsFormat lucene40 = new Lucene41PostingsFormat();

			internal readonly PostingsFormat simpleText = new SimpleTextPostingsFormat();

			internal readonly PostingsFormat mockSep = new MockSepPostingsFormat();

			public override PostingsFormat GetPostingsFormatForField(string field)
			{
				if (field.Equals("id"))
				{
					return simpleText;
				}
				else
				{
					if (field.Equals("content"))
					{
						return mockSep;
					}
					else
					{
						return lucene40;
					}
				}
			}
		}

		public class MockCodec2 : Lucene46Codec
		{
			internal readonly PostingsFormat lucene40 = new Lucene41PostingsFormat();

			internal readonly PostingsFormat simpleText = new SimpleTextPostingsFormat();

			public override PostingsFormat GetPostingsFormatForField(string field)
			{
				if (field.Equals("id"))
				{
					return simpleText;
				}
				else
				{
					return lucene40;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestStressPerFieldCodec()
		{
			Directory dir = NewDirectory(Random());
			int docsPerRound = 97;
			int numRounds = AtLeast(1);
			for (int i = 0; i < numRounds; i++)
			{
				int num = TestUtil.NextInt(Random(), 30, 60);
				IndexWriterConfig config = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, new 
					MockAnalyzer(Random()));
				config.SetOpenMode(IndexWriterConfig.OpenMode.CREATE_OR_APPEND);
				IndexWriter writer = NewWriter(dir, config);
				for (int j = 0; j < docsPerRound; j++)
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					for (int k = 0; k < num; k++)
					{
						FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
						customType.Tokenized = Random().NextBoolean();
						customType.OmitNorms = Random().NextBoolean();
						Field field = NewField(string.Empty + k, TestUtil.RandomRealisticUnicodeString(Random
							(), 128), customType);
						doc.Add(field);
					}
					writer.AddDocument(doc);
				}
				if (Random().NextBoolean())
				{
					writer.ForceMerge(1);
				}
				writer.Commit();
				AreEqual((i + 1) * docsPerRound, writer.MaxDoc);
				writer.Dispose();
			}
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSameCodecDifferentInstance()
		{
			Codec codec = new AnonymousLucene46Codec2();
			DoTestMixedPostings(codec);
		}

		private sealed class AnonymousLucene46Codec2 : Lucene46Codec
		{
		    public override PostingsFormat GetPostingsFormatForField(string field)
			{
				if ("id".Equals(field))
				{
					return new Pulsing41PostingsFormat(1);
				}
				else
				{
					if ("date".Equals(field))
					{
						return new Pulsing41PostingsFormat(1);
					}
					else
					{
						return base.GetPostingsFormatForField(field);
					}
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSameCodecDifferentParams()
		{
			Codec codec = new AnonymousLucene46Codec();
			DoTestMixedPostings(codec);
		}

		private sealed class AnonymousLucene46Codec : Lucene46Codec
		{
		    public override PostingsFormat GetPostingsFormatForField(string field)
			{
				if ("id".Equals(field))
				{
					return new Pulsing41PostingsFormat(1);
				}
		        if ("date".Equals(field))
		        {
		            return new Pulsing41PostingsFormat(2);
		        }
		        return base.GetPostingsFormatForField(field);
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void DoTestMixedPostings(Codec codec)
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetCodec(codec);
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			// turn on vectors for the checkindex cross-check
			ft.StoreTermVectors = true;
			ft.StoreTermVectorOffsets = true;
			ft.StoreTermVectorPositions = true;
			Field idField = new Field("id", string.Empty, ft);
			Field dateField = new Field("date", string.Empty, ft);
			doc.Add(idField);
			doc.Add(dateField);
			for (int i = 0; i < 100; i++)
			{
				idField.StringValue = Random().Next(50).ToString();
			    dateField.StringValue = Random().Next(100).ToString();
				iw.AddDocument(doc);
			}
			iw.Close();
			dir.Dispose();
		}
		// checkindex
	}
}
