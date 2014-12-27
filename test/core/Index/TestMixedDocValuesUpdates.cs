/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Com.Carrotsearch.Randomizedtesting.Generators;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestMixedDocValuesUpdates : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestManyReopensAndFields()
		{
			Directory dir = NewDirectory();
			Random random = Random();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(random));
			LogMergePolicy lmp = NewLogMergePolicy();
			lmp.MergeFactor = (3);
			// merge often
			conf.SetMergePolicy(lmp);
			IndexWriter writer = new IndexWriter(dir, conf);
			bool isNRT = random.NextBoolean();
			DirectoryReader reader;
			if (isNRT)
			{
				reader = DirectoryReader.Open(writer, true);
			}
			else
			{
				writer.Commit();
				reader = DirectoryReader.Open(dir);
			}
			int numFields = random.Next(4) + 3;
			// 3-7
			int numNDVFields = random.Next(numFields / 2) + 1;
			// 1-3
			long[] fieldValues = new long[numFields];
			bool[] fieldHasValue = new bool[numFields];
			Arrays.Fill(fieldHasValue, true);
			for (int i = 0; i < fieldValues.Length; i++)
			{
				fieldValues[i] = 1;
			}
			int numRounds = AtLeast(15);
			int docID = 0;
			for (int i_1 = 0; i_1 < numRounds; i_1++)
			{
				int numDocs = AtLeast(5);
				//      System.out.println("[" + Thread.currentThread().getName() + "]: round=" + i + ", numDocs=" + numDocs);
				for (int j = 0; j < numDocs; j++)
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					doc.Add(new StringField("id", "doc-" + docID, Field.Store.NO));
					doc.Add(new StringField("key", "all", Field.Store.NO));
					// update key
					// add all fields with their current value
					for (int f = 0; f < fieldValues.Length; f++)
					{
						if (f < numNDVFields)
						{
							doc.Add(new NumericDocValuesField("f" + f, fieldValues[f]));
						}
						else
						{
							doc.Add(new BinaryDocValuesField("f" + f, TestBinaryDocValuesUpdates.ToBytes(fieldValues
								[f])));
						}
					}
					writer.AddDocument(doc);
					++docID;
				}
				// if field's value was unset before, unset it from all new added documents too
				for (int field = 0; field < fieldHasValue.Length; field++)
				{
					if (!fieldHasValue[field])
					{
						if (field < numNDVFields)
						{
							writer.UpdateNumericDocValue(new Term("key", "all"), "f" + field, null);
						}
						else
						{
							writer.UpdateBinaryDocValue(new Term("key", "all"), "f" + field, null);
						}
					}
				}
				int fieldIdx = random.Next(fieldValues.Length);
				string updateField = "f" + fieldIdx;
				if (random.NextBoolean())
				{
					//        System.out.println("[" + Thread.currentThread().getName() + "]: unset field '" + updateField + "'");
					fieldHasValue[fieldIdx] = false;
					if (fieldIdx < numNDVFields)
					{
						writer.UpdateNumericDocValue(new Term("key", "all"), updateField, null);
					}
					else
					{
						writer.UpdateBinaryDocValue(new Term("key", "all"), updateField, null);
					}
				}
				else
				{
					fieldHasValue[fieldIdx] = true;
					if (fieldIdx < numNDVFields)
					{
						writer.UpdateNumericDocValue(new Term("key", "all"), updateField, ++fieldValues[fieldIdx
							]);
					}
					else
					{
						writer.UpdateBinaryDocValue(new Term("key", "all"), updateField, TestBinaryDocValuesUpdates
							.ToBytes(++fieldValues[fieldIdx]));
					}
				}
				//        System.out.println("[" + Thread.currentThread().getName() + "]: updated field '" + updateField + "' to value " + fieldValues[fieldIdx]);
				if (random.NextDouble() < 0.2)
				{
					int deleteDoc = random.Next(docID);
					// might also delete an already deleted document, ok!
					writer.DeleteDocuments(new Term("id", "doc-" + deleteDoc));
				}
				//        System.out.println("[" + Thread.currentThread().getName() + "]: deleted document: doc-" + deleteDoc);
				// verify reader
				if (!isNRT)
				{
					writer.Commit();
				}
				//      System.out.println("[" + Thread.currentThread().getName() + "]: reopen reader: " + reader);
				DirectoryReader newReader = DirectoryReader.OpenIfChanged(reader);
				IsNotNull(newReader);
				reader.Dispose();
				reader = newReader;
				//      System.out.println("[" + Thread.currentThread().getName() + "]: reopened reader: " + reader);
				IsTrue(reader.NumDocs > 0);
				// we delete at most one document per round
				BytesRef scratch = new BytesRef();
				foreach (AtomicReaderContext context in reader.Leaves)
				{
					AtomicReader r = ((AtomicReader)context.Reader);
					//        System.out.println(((SegmentReader) r).getSegmentName());
					Bits liveDocs = r.LiveDocs;
					for (int field_1 = 0; field_1 < fieldValues.Length; field_1++)
					{
						string f = "f" + field_1;
						BinaryDocValues bdv = r.GetBinaryDocValues(f);
						NumericDocValues ndv = r.GetNumericDocValues(f);
						Bits docsWithField = r.GetDocsWithField(f);
						if (field_1 < numNDVFields)
						{
							IsNotNull(ndv);
							IsNull(bdv);
						}
						else
						{
							IsNull(ndv);
							IsNotNull(bdv);
						}
						int maxDoc = r.MaxDoc;
						for (int doc = 0; doc < maxDoc; doc++)
						{
							if (liveDocs == null || liveDocs.Get(doc))
							{
								//              System.out.println("doc=" + (doc + context.docBase) + " f='" + f + "' vslue=" + getValue(bdv, doc, scratch));
								if (fieldHasValue[field_1])
								{
									IsTrue(docsWithField.Get(doc));
									if (field_1 < numNDVFields)
									{
										AreEqual("invalid value for doc=" + doc + ", field=" + f +
											 ", reader=" + r, fieldValues[field_1], ndv.Get(doc));
									}
									else
									{
										AreEqual("invalid value for doc=" + doc + ", field=" + f +
											 ", reader=" + r, fieldValues[field_1], TestBinaryDocValuesUpdates.GetValue(bdv, 
											doc, scratch));
									}
								}
								else
								{
									IsFalse(docsWithField.Get(doc));
								}
							}
						}
					}
				}
			}
			//      System.out.println();
			IOUtils.Close(writer, reader, dir);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestStressMultiThreading()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter writer = new IndexWriter(dir, conf);
			// create index
			int numThreads = TestUtil.NextInt(Random(), 3, 6);
			int numDocs = AtLeast(2000);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("id", "doc" + i, Field.Store.NO));
				double group = Random().NextDouble();
				string g;
				if (group < 0.1)
				{
					g = "g0";
				}
				else
				{
					if (group < 0.5)
					{
						g = "g1";
					}
					else
					{
						if (group < 0.8)
						{
							g = "g2";
						}
						else
						{
							g = "g3";
						}
					}
				}
				doc.Add(new StringField("updKey", g, Field.Store.NO));
				for (int j = 0; j < numThreads; j++)
				{
					long value = Random().Next();
					doc.Add(new BinaryDocValuesField("f" + j, TestBinaryDocValuesUpdates.ToBytes(value
						)));
					doc.Add(new NumericDocValuesField("cf" + j, value * 2));
				}
				// control, always updated to f * 2
				writer.AddDocument(doc);
			}
			CountDownLatch done = new CountDownLatch(numThreads);
			AtomicInteger numUpdates = new AtomicInteger(AtLeast(100));
			// same thread updates a field as well as reopens
			Sharpen.Thread[] threads = new Sharpen.Thread[numThreads];
			for (int i_1 = 0; i_1 < threads.Length; i_1++)
			{
				string f = "f" + i_1;
				string cf = "cf" + i_1;
				threads[i_1] = new _Thread_219(numUpdates, writer, f, cf, numDocs, done, "UpdateThread-"
					 + i_1);
			}
			//              System.out.println("[" + Thread.currentThread().getName() + "] numUpdates=" + numUpdates + " updateTerm=" + t);
			// sometimes unset a value
			//                System.err.println("[" + Thread.currentThread().getName() + "] t=" + t + ", f=" + f + ", updValue=UNSET");
			//                System.err.println("[" + Thread.currentThread().getName() + "] t=" + t + ", f=" + f + ", updValue=" + updValue);
			// delete a random document
			//                System.out.println("[" + Thread.currentThread().getName() + "] deleteDoc=doc" + doc);
			// commit every 20 updates on average
			//                  System.out.println("[" + Thread.currentThread().getName() + "] commit");
			// reopen NRT reader (apply updates), on average once every 10 updates
			//                  System.out.println("[" + Thread.currentThread().getName() + "] open NRT");
			//                  System.out.println("[" + Thread.currentThread().getName() + "] reopen NRT");
			//            System.out.println("[" + Thread.currentThread().getName() + "] DONE");
			// suppress this exception only if there was another exception
			foreach (Sharpen.Thread t in threads)
			{
				t.Start();
			}
			done.Await();
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			BytesRef scratch = new BytesRef();
			foreach (AtomicReaderContext context in reader.Leaves)
			{
				AtomicReader r = ((AtomicReader)context.Reader);
				for (int i_2 = 0; i_2 < numThreads; i_2++)
				{
					BinaryDocValues bdv = r.GetBinaryDocValues("f" + i_2);
					NumericDocValues control = r.GetNumericDocValues("cf" + i_2);
					Bits docsWithBdv = r.GetDocsWithField("f" + i_2);
					Bits docsWithControl = r.GetDocsWithField("cf" + i_2);
					Bits liveDocs = r.LiveDocs;
					for (int j = 0; j < r.MaxDoc; j++)
					{
						if (liveDocs == null || liveDocs.Get(j))
						{
							AreEqual(docsWithBdv.Get(j), docsWithControl.Get(j));
							if (docsWithBdv.Get(j))
							{
								long ctrlValue = control.Get(j);
								long bdvValue = TestBinaryDocValuesUpdates.GetValue(bdv, j, scratch) * 2;
								//              if (ctrlValue != bdvValue) {
								//                System.out.println("seg=" + r + ", f=f" + i + ", doc=" + j + ", group=" + r.document(j).get("updKey") + ", ctrlValue=" + ctrlValue + ", bdvBytes=" + scratch);
								//              }
								AreEqual(ctrlValue, bdvValue);
							}
						}
					}
				}
			}
			reader.Dispose();
			dir.Dispose();
		}

		private sealed class _Thread_219 : Sharpen.Thread
		{
			public _Thread_219(AtomicInteger numUpdates, IndexWriter writer, string f, string
				 cf, int numDocs, CountDownLatch done, string baseArg1) : base(baseArg1)
			{
				this.numUpdates = numUpdates;
				this.writer = writer;
				this.f = f;
				this.cf = cf;
				this.numDocs = numDocs;
				this.done = done;
			}

			public override void Run()
			{
				DirectoryReader reader = null;
				bool success = false;
				try
				{
					Random random = LuceneTestCase.Random();
					while (numUpdates.GetAndDecrement() > 0)
					{
						double group = random.NextDouble();
						Term t;
						if (group < 0.1)
						{
							t = new Term("updKey", "g0");
						}
						else
						{
							if (group < 0.5)
							{
								t = new Term("updKey", "g1");
							}
							else
							{
								if (group < 0.8)
								{
									t = new Term("updKey", "g2");
								}
								else
								{
									t = new Term("updKey", "g3");
								}
							}
						}
						if (random.NextBoolean())
						{
							writer.UpdateBinaryDocValue(t, f, null);
							writer.UpdateNumericDocValue(t, cf, null);
						}
						else
						{
							long updValue = random.Next();
							writer.UpdateBinaryDocValue(t, f, TestBinaryDocValuesUpdates.ToBytes(updValue));
							writer.UpdateNumericDocValue(t, cf, updValue * 2);
						}
						if (random.NextDouble() < 0.2)
						{
							int doc = random.Next(numDocs);
							writer.DeleteDocuments(new Term("id", "doc" + doc));
						}
						if (random.NextDouble() < 0.05)
						{
							writer.Commit();
						}
						if (random.NextDouble() < 0.1)
						{
							if (reader == null)
							{
								reader = DirectoryReader.Open(writer, true);
							}
							else
							{
								DirectoryReader r2 = DirectoryReader.OpenIfChanged(reader, writer, true);
								if (r2 != null)
								{
									reader.Dispose();
									reader = r2;
								}
							}
						}
					}
					success = true;
				}
				catch (IOException e)
				{
					throw new RuntimeException(e);
				}
				finally
				{
					if (reader != null)
					{
						try
						{
							reader.Dispose();
						}
						catch (IOException e)
						{
							if (success)
							{
								throw new RuntimeException(e);
							}
						}
					}
					done.CountDown();
				}
			}

			private readonly AtomicInteger numUpdates;

			private readonly IndexWriter writer;

			private readonly string f;

			private readonly string cf;

			private readonly int numDocs;

			private readonly CountDownLatch done;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestUpdateDifferentDocsInDifferentGens()
		{
			// update same document multiple times across generations
			Directory dir = NewDirectory();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			conf.SetMaxBufferedDocs(4);
			IndexWriter writer = new IndexWriter(dir, conf);
			int numDocs = AtLeast(10);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new StringField("id", "doc" + i, Field.Store.NO));
				long value = Random().Next();
				doc.Add(new BinaryDocValuesField("f", TestBinaryDocValuesUpdates.ToBytes(value)));
				doc.Add(new NumericDocValuesField("cf", value * 2));
				writer.AddDocument(doc);
			}
			int numGens = AtLeast(5);
			BytesRef scratch = new BytesRef();
			for (int i_1 = 0; i_1 < numGens; i_1++)
			{
				int doc = Random().Next(numDocs);
				Term t = new Term("id", "doc" + doc);
				long value = Random().NextLong();
				writer.UpdateBinaryDocValue(t, "f", TestBinaryDocValuesUpdates.ToBytes(value));
				writer.UpdateNumericDocValue(t, "cf", value * 2);
				DirectoryReader reader = DirectoryReader.Open(writer, true);
				foreach (AtomicReaderContext context in reader.Leaves)
				{
					AtomicReader r = ((AtomicReader)context.Reader);
					BinaryDocValues fbdv = r.GetBinaryDocValues("f");
					NumericDocValues cfndv = r.GetNumericDocValues("cf");
					for (int j = 0; j < r.MaxDoc; j++)
					{
						AreEqual(cfndv.Get(j), TestBinaryDocValuesUpdates.GetValue
							(fbdv, j, scratch) * 2);
					}
				}
				reader.Dispose();
			}
			writer.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTonsOfUpdates()
		{
			// LUCENE-5248: make sure that when there are many updates, we don't use too much RAM
			Directory dir = NewDirectory();
			Random random = Random();
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(random));
			conf.SetRAMBufferSizeMB(IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB);
			conf.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
			// don't flush by doc
			IndexWriter writer = new IndexWriter(dir, conf);
			// test data: lots of documents (few 10Ks) and lots of update terms (few hundreds)
			int numDocs = AtLeast(20000);
			int numBinaryFields = AtLeast(5);
			int numTerms = TestUtil.NextInt(random, 10, 100);
			// terms should affect many docs
			ICollection<string> updateTerms = new HashSet<string>();
			while (updateTerms.Count < numTerms)
			{
				updateTerms.AddItem(TestUtil.RandomSimpleString(random));
			}
			//    System.out.println("numDocs=" + numDocs + " numBinaryFields=" + numBinaryFields + " numTerms=" + numTerms);
			// build a large index with many BDV fields and update terms
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				int numUpdateTerms = TestUtil.NextInt(random, 1, numTerms / 10);
				for (int j = 0; j < numUpdateTerms; j++)
				{
					doc.Add(new StringField("upd", RandomPicks.RandomFrom(random, updateTerms), Field.Store
						.NO));
				}
				for (int j_1 = 0; j_1 < numBinaryFields; j_1++)
				{
					long val = random.Next();
					doc.Add(new BinaryDocValuesField("f" + j_1, TestBinaryDocValuesUpdates.ToBytes(val
						)));
					doc.Add(new NumericDocValuesField("cf" + j_1, val * 2));
				}
				writer.AddDocument(doc);
			}
			writer.Commit();
			// commit so there's something to apply to
			// set to flush every 2048 bytes (approximately every 12 updates), so we get
			// many flushes during binary updates
			writer.Config.SetRAMBufferSizeMB(2048.0 / 1024 / 1024);
			int numUpdates = AtLeast(100);
			//    System.out.println("numUpdates=" + numUpdates);
			for (int i_1 = 0; i_1 < numUpdates; i_1++)
			{
				int field = random.Next(numBinaryFields);
				Term updateTerm = new Term("upd", RandomPicks.RandomFrom(random, updateTerms));
				long value = random.Next();
				writer.UpdateBinaryDocValue(updateTerm, "f" + field, TestBinaryDocValuesUpdates.ToBytes
					(value));
				writer.UpdateNumericDocValue(updateTerm, "cf" + field, value * 2);
			}
			writer.Dispose();
			DirectoryReader reader = DirectoryReader.Open(dir);
			BytesRef scratch = new BytesRef();
			foreach (AtomicReaderContext context in reader.Leaves)
			{
				for (int i_2 = 0; i_2 < numBinaryFields; i_2++)
				{
					AtomicReader r = ((AtomicReader)context.Reader);
					BinaryDocValues f = r.GetBinaryDocValues("f" + i_2);
					NumericDocValues cf = r.GetNumericDocValues("cf" + i_2);
					for (int j = 0; j < r.MaxDoc; j++)
					{
						AreEqual("reader=" + r + ", field=f" + i_2 + ", doc=" + j, 
							cf.Get(j), TestBinaryDocValuesUpdates.GetValue(f, j, scratch) * 2);
					}
				}
			}
			reader.Dispose();
			dir.Dispose();
		}
	}
}
