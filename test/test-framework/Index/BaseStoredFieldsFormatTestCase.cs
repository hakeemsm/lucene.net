using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.TestFramework.Util;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>
	/// Base class aiming at testing
	/// <see cref="Lucene.NetCodecs.StoredFieldsFormat">stored fields formats</see>
	/// .
	/// To test a new format, all you need is to register a new
	/// <see cref="Lucene.NetCodecs.Codec">Lucene.NetCodecs.Codec</see>
	/// which
	/// uses it and extend this class and override
	/// <see cref="BaseIndexFileFormatTestCase.GetCodec()">BaseIndexFileFormatTestCase.GetCodec()
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public abstract class BaseStoredFieldsFormatTestCase : BaseIndexFileFormatTestCase
	{
		protected internal override void AddRandomFields(Lucene.Net.Documents.Document
			 d)
		{
			int numValues = Random().Next(3);
			for (int i = 0; i < numValues; ++i)
			{
				d.Add(new StoredField("f", TestUtil.RandomSimpleString(Random(), 100)));
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRandomStoredFields()
		{
			Directory dir = NewDirectory();
			Random rand = Random();
			RandomIndexWriter w = new RandomIndexWriter(rand, dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(TestUtil.NextInt
				(rand, 5, 20))));
			//w.w.setNoCFSRatio(0.0);
			int docCount = AtLeast(200);
			int fieldCount = TestUtil.NextInt(rand, 1, 5);
			IList<int> fieldIDs = new AList<int>();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetTokenized(false);
			Field idField = NewField("id", string.Empty, customType);
			for (int i = 0; i < fieldCount; i++)
			{
				fieldIDs.AddItem(i);
			}
			IDictionary<string, Lucene.Net.Documents.Document> docs = new Dictionary<string
				, Lucene.Net.Documents.Document>();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: build index docCount=" + docCount);
			}
			FieldType customType2 = new FieldType();
			customType2.SetStored(true);
			for (int i_1 = 0; i_1 < docCount; i_1++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(idField);
				string id = string.Empty + i_1;
				idField.SetStringValue(id);
				docs.Put(id, doc);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: add doc id=" + id);
				}
				foreach (int field in fieldIDs)
				{
					string s;
					if (rand.Next(4) != 3)
					{
						s = TestUtil.RandomUnicodeString(rand, 1000);
						doc.Add(NewField("f" + field, s, customType2));
					}
					else
					{
						s = null;
					}
				}
				w.AddDocument(doc);
				if (rand.Next(50) == 17)
				{
					// mixup binding of field name -> Number every so often
					Sharpen.Collections.Shuffle(fieldIDs);
				}
				if (rand.Next(5) == 3 && i_1 > 0)
				{
					string delID = string.Empty + rand.Next(i_1);
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: delete doc id=" + delID);
					}
					w.DeleteDocuments(new Term("id", delID));
					Sharpen.Collections.Remove(docs, delID);
				}
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: " + docs.Count + " docs in index; now load fields"
					);
			}
			if (docs.Count > 0)
			{
				string[] idsList = Sharpen.Collections.ToArray(docs.Keys, new string[docs.Count]);
				for (int x = 0; x < 2; x++)
				{
					IndexReader r = w.GetReader();
					IndexSearcher s = NewSearcher(r);
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: cycle x=" + x + " r=" + r);
					}
					int num = AtLeast(1000);
					for (int iter = 0; iter < num; iter++)
					{
						string testID = idsList[rand.Next(idsList.Length)];
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: test id=" + testID);
						}
						TopDocs hits = s.Search(new TermQuery(new Term("id", testID)), 1);
						NUnit.Framework.Assert.AreEqual(1, hits.totalHits);
						Lucene.Net.Documents.Document doc = r.Document(hits.scoreDocs[0].doc);
						Lucene.Net.Documents.Document docExp = docs.Get(testID);
						for (int i_2 = 0; i_2 < fieldCount; i_2++)
						{
							NUnit.Framework.Assert.AreEqual("doc " + testID + ", field f" + fieldCount + " is wrong"
								, docExp.Get("f" + i_2), doc.Get("f" + i_2));
						}
					}
					r.Close();
					w.ForceMerge(1);
				}
			}
			w.Close();
			dir.Close();
		}

		// LUCENE-1727: make sure doc fields are stored in order
		/// <exception cref="System.Exception"></exception>
		public virtual void TestStoredFieldsOrder()
		{
			Directory d = NewDirectory();
			IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType();
			customType.SetStored(true);
			doc.Add(NewField("zzz", "a b c", customType));
			doc.Add(NewField("aaa", "a b c", customType));
			doc.Add(NewField("zzz", "1 2 3", customType));
			w.AddDocument(doc);
			IndexReader r = w.GetReader();
			Lucene.Net.Documents.Document doc2 = r.Document(0);
			Iterator<IndexableField> it = doc2.GetFields().Iterator();
			NUnit.Framework.Assert.IsTrue(it.HasNext());
			Field f = (Field)it.Next();
			NUnit.Framework.Assert.AreEqual(f.Name(), "zzz");
			NUnit.Framework.Assert.AreEqual(f.StringValue(), "a b c");
			NUnit.Framework.Assert.IsTrue(it.HasNext());
			f = (Field)it.Next();
			NUnit.Framework.Assert.AreEqual(f.Name(), "aaa");
			NUnit.Framework.Assert.AreEqual(f.StringValue(), "a b c");
			NUnit.Framework.Assert.IsTrue(it.HasNext());
			f = (Field)it.Next();
			NUnit.Framework.Assert.AreEqual(f.Name(), "zzz");
			NUnit.Framework.Assert.AreEqual(f.StringValue(), "1 2 3");
			NUnit.Framework.Assert.IsFalse(it.HasNext());
			r.Close();
			w.Close();
			d.Close();
		}

		// LUCENE-1219
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBinaryFieldOffsetLength()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			byte[] b = new byte[50];
			for (int i = 0; i < 50; i++)
			{
				b[i] = unchecked((byte)(i + 77));
			}
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field f = new StoredField("binary", b, 10, 17);
			byte[] bx = f.BinaryValue().bytes;
			NUnit.Framework.Assert.IsTrue(bx != null);
			NUnit.Framework.Assert.AreEqual(50, bx.Length);
			NUnit.Framework.Assert.AreEqual(10, f.BinaryValue().offset);
			NUnit.Framework.Assert.AreEqual(17, f.BinaryValue().length);
			doc.Add(f);
			w.AddDocument(doc);
			w.Close();
			IndexReader ir = DirectoryReader.Open(dir);
			Lucene.Net.Documents.Document doc2 = ir.Document(0);
			IndexableField f2 = doc2.GetField("binary");
			b = f2.BinaryValue().bytes;
			NUnit.Framework.Assert.IsTrue(b != null);
			NUnit.Framework.Assert.AreEqual(17, b.Length, 17);
			NUnit.Framework.Assert.AreEqual(87, b[0]);
			ir.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNumericField()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			int numDocs = AtLeast(500);
			Number[] answers = new Number[numDocs];
			FieldType.NumericType[] typeAnswers = new FieldType.NumericType[numDocs];
			for (int id = 0; id < numDocs; id++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				Field nf;
				Field sf;
				Number answer;
				FieldType.NumericType typeAnswer;
				if (Random().NextBoolean())
				{
					// float/double
					if (Random().NextBoolean())
					{
						float f = Random().NextFloat();
						answer = float.ValueOf(f);
						nf = new FloatField("nf", f, Field.Store.NO);
						sf = new StoredField("nf", f);
						typeAnswer = FieldType.NumericType.FLOAT;
					}
					else
					{
						double d = Random().NextDouble();
						answer = double.ValueOf(d);
						nf = new DoubleField("nf", d, Field.Store.NO);
						sf = new StoredField("nf", d);
						typeAnswer = FieldType.NumericType.DOUBLE;
					}
				}
				else
				{
					// int/long
					if (Random().NextBoolean())
					{
						int i = Random().Next();
						answer = Sharpen.Extensions.ValueOf(i);
						nf = new IntField("nf", i, Field.Store.NO);
						sf = new StoredField("nf", i);
						typeAnswer = FieldType.NumericType.INT;
					}
					else
					{
						long l = Random().NextLong();
						answer = Sharpen.Extensions.ValueOf(l);
						nf = new LongField("nf", l, Field.Store.NO);
						sf = new StoredField("nf", l);
						typeAnswer = FieldType.NumericType.LONG;
					}
				}
				doc.Add(nf);
				doc.Add(sf);
				answers[id] = answer;
				typeAnswers[id] = typeAnswer;
				FieldType ft = new FieldType(IntField.TYPE_STORED);
				ft.SetNumericPrecisionStep(int.MaxValue);
				doc.Add(new IntField("id", id, ft));
				w.AddDocument(doc);
			}
			DirectoryReader r = w.GetReader();
			w.Close();
			NUnit.Framework.Assert.AreEqual(numDocs, r.NumDocs());
			foreach (AtomicReaderContext ctx in r.Leaves())
			{
				AtomicReader sub = ((AtomicReader)ctx.Reader());
				FieldCache.Ints ids = FieldCache.DEFAULT.GetInts(sub, "id", false);
				for (int docID = 0; docID < sub.NumDocs(); docID++)
				{
					Lucene.Net.Documents.Document doc = sub.Document(docID);
					Field f = (Field)doc.GetField("nf");
					NUnit.Framework.Assert.IsTrue("got f=" + f, f is StoredField);
					NUnit.Framework.Assert.AreEqual(answers[ids.Get(docID)], f.NumericValue());
				}
			}
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIndexedBit()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType onlyStored = new FieldType();
			onlyStored.SetStored(true);
			doc.Add(new Field("field", "value", onlyStored));
			doc.Add(new StringField("field2", "value", Field.Store.YES));
			w.AddDocument(doc);
			IndexReader r = w.GetReader();
			w.Close();
			NUnit.Framework.Assert.IsFalse(r.Document(0).GetField("field").FieldType().Indexed
				());
			NUnit.Framework.Assert.IsTrue(r.Document(0).GetField("field2").FieldType().Indexed
				());
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestReadSkip()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwConf.SetMaxBufferedDocs(RandomInts.RandomIntBetween(Random(), 2, 30));
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);
			FieldType ft = new FieldType();
			ft.SetStored(true);
			ft.Freeze();
			string @string = TestUtil.RandomSimpleString(Random(), 50);
			byte[] bytes = Sharpen.Runtime.GetBytesForString(@string, StandardCharsets.UTF_8);
			long l = Random().NextBoolean() ? Random().Next(42) : Random().NextLong();
			int i = Random().NextBoolean() ? Random().Next(42) : Random().Next();
			float f = Random().NextFloat();
			double d = Random().NextDouble();
			IList<Field> fields = Arrays.AsList(new Field("bytes", bytes, ft), new Field("string"
				, @string, ft), new LongField("long", l, Field.Store.YES), new IntField("int", i
				, Field.Store.YES), new FloatField("float", f, Field.Store.YES), new DoubleField
				("double", d, Field.Store.YES));
			for (int k = 0; k < 100; ++k)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				foreach (Field fld in fields)
				{
					doc.Add(fld);
				}
				iw.w.AddDocument(doc);
			}
			iw.Commit();
			DirectoryReader reader = DirectoryReader.Open(dir);
			int docID = Random().Next(100);
			foreach (Field fld_1 in fields)
			{
				string fldName = fld_1.Name();
				Lucene.Net.Documents.Document sDoc = reader.Document(docID, Sharpen.Collections
					.Singleton(fldName));
				IndexableField sField = sDoc.GetField(fldName);
				if (typeof(Field).Equals(fld_1.GetType()))
				{
					NUnit.Framework.Assert.AreEqual(fld_1.BinaryValue(), sField.BinaryValue());
					NUnit.Framework.Assert.AreEqual(fld_1.StringValue(), sField.StringValue());
				}
				else
				{
					NUnit.Framework.Assert.AreEqual(fld_1.NumericValue(), sField.NumericValue());
				}
			}
			reader.Close();
			iw.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEmptyDocs()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwConf.SetMaxBufferedDocs(RandomInts.RandomIntBetween(Random(), 2, 30));
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);
			// make sure that the fact that documents might be empty is not a problem
			Lucene.Net.Documents.Document emptyDoc = new Lucene.Net.Documents.Document
				();
			int numDocs = Random().NextBoolean() ? 1 : AtLeast(1000);
			for (int i = 0; i < numDocs; ++i)
			{
				iw.AddDocument(emptyDoc);
			}
			iw.Commit();
			DirectoryReader rd = DirectoryReader.Open(dir);
			for (int i_1 = 0; i_1 < numDocs; ++i_1)
			{
				Lucene.Net.Documents.Document doc = rd.Document(i_1);
				NUnit.Framework.Assert.IsNotNull(doc);
				NUnit.Framework.Assert.IsTrue(doc.GetFields().IsEmpty());
			}
			rd.Close();
			iw.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestConcurrentReads()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwConf.SetMaxBufferedDocs(RandomInts.RandomIntBetween(Random(), 2, 30));
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);
			// make sure the readers are properly cloned
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field field = new StringField("fld", string.Empty, Field.Store.YES);
			doc.Add(field);
			int numDocs = AtLeast(1000);
			for (int i = 0; i < numDocs; ++i)
			{
				field.SetStringValue(string.Empty + i);
				iw.AddDocument(doc);
			}
			iw.Commit();
			DirectoryReader rd = DirectoryReader.Open(dir);
			IndexSearcher searcher = new IndexSearcher(rd);
			int concurrentReads = AtLeast(5);
			int readsPerThread = AtLeast(50);
			IList<Sharpen.Thread> readThreads = new AList<Sharpen.Thread>();
			AtomicReference<Exception> ex = new AtomicReference<Exception>();
			for (int i_1 = 0; i_1 < concurrentReads; ++i_1)
			{
				readThreads.AddItem(new _Thread_432(readsPerThread, numDocs, searcher, rd, ex));
			}
			foreach (Sharpen.Thread thread in readThreads)
			{
				thread.Start();
			}
			foreach (Sharpen.Thread thread_1 in readThreads)
			{
				thread_1.Join();
			}
			rd.Close();
			if (ex.Get() != null)
			{
				throw ex.Get();
			}
			iw.Close();
			dir.Close();
		}

		private sealed class _Thread_432 : Sharpen.Thread
		{
			public _Thread_432(int readsPerThread, int numDocs, IndexSearcher searcher, DirectoryReader
				 rd, AtomicReference<Exception> ex)
			{
				this.readsPerThread = readsPerThread;
				this.numDocs = numDocs;
				this.searcher = searcher;
				this.rd = rd;
				this.ex = ex;
				{
					this.queries = new int[readsPerThread];
					for (int i = 0; i < this.queries.Length; ++i)
					{
						this.queries[i] = LuceneTestCase.Random().Next(numDocs);
					}
				}
			}

			internal int[] queries;

			public override void Run()
			{
				foreach (int q in this.queries)
				{
					Query query = new TermQuery(new Term("fld", string.Empty + q));
					try
					{
						TopDocs topDocs = searcher.Search(query, 1);
						if (topDocs.totalHits != 1)
						{
							throw new InvalidOperationException("Expected 1 hit, got " + topDocs.totalHits);
						}
						Lucene.Net.Documents.Document sdoc = rd.Document(topDocs.scoreDocs[0].doc);
						if (sdoc == null || sdoc.Get("fld") == null)
						{
							throw new InvalidOperationException("Could not find document " + q);
						}
						if (!Sharpen.Extensions.ToString(q).Equals(sdoc.Get("fld")))
						{
							throw new InvalidOperationException("Expected " + q + ", but got " + sdoc.Get("fld"
								));
						}
					}
					catch (Exception e)
					{
						ex.CompareAndSet(null, e);
					}
				}
			}

			private readonly int readsPerThread;

			private readonly int numDocs;

			private readonly IndexSearcher searcher;

			private readonly DirectoryReader rd;

			private readonly AtomicReference<Exception> ex;
		}

		private byte[] RandomByteArray(int length, int max)
		{
			byte[] result = new byte[length];
			for (int i = 0; i < length; ++i)
			{
				result[i] = unchecked((byte)Random().Next(max));
			}
			return result;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestWriteReadMerge()
		{
			// get another codec, other than the default: so we are merging segments across different codecs
			Codec otherCodec;
			if ("SimpleText".Equals(Codec.GetDefault().GetName()))
			{
				otherCodec = new Lucene46Codec();
			}
			else
			{
				otherCodec = new SimpleTextCodec();
			}
			Directory dir = NewDirectory();
			IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwConf.SetMaxBufferedDocs(RandomInts.RandomIntBetween(Random(), 2, 30));
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf.Clone());
			int docCount = AtLeast(200);
			byte[][][] data = new byte[docCount][][];
			for (int i = 0; i < docCount; ++i)
			{
				int fieldCount = Rarely() ? RandomInts.RandomIntBetween(Random(), 1, 500) : RandomInts
					.RandomIntBetween(Random(), 1, 5);
				data[i] = new byte[fieldCount][];
				for (int j = 0; j < fieldCount; ++j)
				{
					int length = Rarely() ? Random().Next(1000) : Random().Next(10);
					int max = Rarely() ? 256 : 2;
					data[i][j] = RandomByteArray(length, max);
				}
			}
			FieldType type = new FieldType(StringField.TYPE_STORED);
			type.SetIndexed(false);
			type.Freeze();
			IntField id = new IntField("id", 0, Field.Store.YES);
			for (int i_1 = 0; i_1 < data.Length; ++i_1)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(id);
				id.SetIntValue(i_1);
				for (int j = 0; j < data[i_1].Length; ++j)
				{
					Field f = new Field("bytes" + j, data[i_1][j], type);
					doc.Add(f);
				}
				iw.w.AddDocument(doc);
				if (Random().NextBoolean() && (i_1 % (data.Length / 10) == 0))
				{
					iw.w.Close();
					// test merging against a non-compressing codec
					if (iwConf.GetCodec() == otherCodec)
					{
						iwConf.SetCodec(Codec.GetDefault());
					}
					else
					{
						iwConf.SetCodec(otherCodec);
					}
					iw = new RandomIndexWriter(Random(), dir, iwConf.Clone());
				}
			}
			for (int i_2 = 0; i_2 < 10; ++i_2)
			{
				int min = Random().Next(data.Length);
				int max = min + Random().Next(20);
				iw.DeleteDocuments(NumericRangeQuery.NewIntRange("id", min, max, true, false));
			}
			iw.ForceMerge(2);
			// force merges with deletions
			iw.Commit();
			DirectoryReader ir = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.IsTrue(ir.NumDocs() > 0);
			int numDocs = 0;
			for (int i_3 = 0; i_3 < ir.MaxDoc(); ++i_3)
			{
				Lucene.Net.Documents.Document doc = ir.Document(i_3);
				if (doc == null)
				{
					continue;
				}
				++numDocs;
				int docId = doc.GetField("id").NumericValue();
				NUnit.Framework.Assert.AreEqual(data[docId].Length + 1, doc.GetFields().Count);
				for (int j = 0; j < data[docId].Length; ++j)
				{
					byte[] arr = data[docId][j];
					BytesRef arr2Ref = doc.GetBinaryValue("bytes" + j);
					byte[] arr2 = Arrays.CopyOfRange(arr2Ref.bytes, arr2Ref.offset, arr2Ref.offset + 
						arr2Ref.length);
					AssertArrayEquals(arr, arr2);
				}
			}
			NUnit.Framework.Assert.IsTrue(ir.NumDocs() <= numDocs);
			ir.Close();
			iw.DeleteAll();
			iw.Commit();
			iw.ForceMerge(1);
			iw.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		[LuceneTestCase.Nightly]
		public virtual void TestBigDocuments()
		{
			// "big" as "much bigger than the chunk size"
			// for this test we force a FS dir
			// we can't just use newFSDirectory, because this test doesn't really index anything.
			// so if we get NRTCachingDir+SimpleText, we make massive stored fields and OOM (LUCENE-4484)
			Directory dir = new MockDirectoryWrapper(Random(), new MMapDirectory(CreateTempDir
				("testBigDocuments")));
			IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwConf.SetMaxBufferedDocs(RandomInts.RandomIntBetween(Random(), 2, 30));
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			Lucene.Net.Documents.Document emptyDoc = new Lucene.Net.Documents.Document
				();
			// emptyDoc
			Lucene.Net.Documents.Document bigDoc1 = new Lucene.Net.Documents.Document
				();
			// lot of small fields
			Lucene.Net.Documents.Document bigDoc2 = new Lucene.Net.Documents.Document
				();
			// 1 very big field
			Field idField = new StringField("id", string.Empty, Field.Store.NO);
			emptyDoc.Add(idField);
			bigDoc1.Add(idField);
			bigDoc2.Add(idField);
			FieldType onlyStored = new FieldType(StringField.TYPE_STORED);
			onlyStored.SetIndexed(false);
			Field smallField = new Field("fld", RandomByteArray(Random().Next(10), 256), onlyStored
				);
			int numFields = RandomInts.RandomIntBetween(Random(), 500000, 1000000);
			for (int i = 0; i < numFields; ++i)
			{
				bigDoc1.Add(smallField);
			}
			Field bigField = new Field("fld", RandomByteArray(RandomInts.RandomIntBetween(Random
				(), 1000000, 5000000), 2), onlyStored);
			bigDoc2.Add(bigField);
			int numDocs = AtLeast(5);
			Lucene.Net.Documents.Document[] docs = new Lucene.Net.Documents.Document
				[numDocs];
			for (int i_1 = 0; i_1 < numDocs; ++i_1)
			{
				docs[i_1] = RandomPicks.RandomFrom(Random(), Arrays.AsList(emptyDoc, bigDoc1, bigDoc2
					));
			}
			for (int i_2 = 0; i_2 < numDocs; ++i_2)
			{
				idField.SetStringValue(string.Empty + i_2);
				iw.AddDocument(docs[i_2]);
				if (Random().Next(numDocs) == 0)
				{
					iw.Commit();
				}
			}
			iw.Commit();
			iw.ForceMerge(1);
			// look at what happens when big docs are merged
			DirectoryReader rd = DirectoryReader.Open(dir);
			IndexSearcher searcher = new IndexSearcher(rd);
			for (int i_3 = 0; i_3 < numDocs; ++i_3)
			{
				Query query = new TermQuery(new Term("id", string.Empty + i_3));
				TopDocs topDocs = searcher.Search(query, 1);
				NUnit.Framework.Assert.AreEqual(string.Empty + i_3, 1, topDocs.totalHits);
				Lucene.Net.Documents.Document doc = rd.Document(topDocs.scoreDocs[0].doc);
				NUnit.Framework.Assert.IsNotNull(doc);
				IndexableField[] fieldValues = doc.GetFields("fld");
				NUnit.Framework.Assert.AreEqual(docs[i_3].GetFields("fld").Length, fieldValues.Length
					);
				if (fieldValues.Length > 0)
				{
					NUnit.Framework.Assert.AreEqual(docs[i_3].GetFields("fld")[0].BinaryValue(), fieldValues
						[0].BinaryValue());
				}
			}
			rd.Close();
			iw.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBulkMergeWithDeletes()
		{
			int numDocs = AtLeast(200);
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
			for (int i = 0; i < numDocs; ++i)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("id", Sharpen.Extensions.ToString(i), Field.Store.YES));
				doc.Add(new StoredField("f", TestUtil.RandomSimpleString(Random())));
				w.AddDocument(doc);
			}
			int deleteCount = TestUtil.NextInt(Random(), 5, numDocs);
			for (int i_1 = 0; i_1 < deleteCount; ++i_1)
			{
				int id = Random().Next(numDocs);
				w.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(id)));
			}
			w.Commit();
			w.Close();
			w = new RandomIndexWriter(Random(), dir);
			w.ForceMerge(TestUtil.NextInt(Random(), 1, 3));
			w.Commit();
			w.Close();
			TestUtil.CheckIndex(dir);
			dir.Close();
		}
	}
}
