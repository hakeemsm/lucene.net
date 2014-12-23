/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Lucene.Net.Util.Packed;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestIndexWriter : LuceneTestCase
	{
		private static readonly FieldType storedTextType = new FieldType(TextField.TYPE_NOT_STORED
			);

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDocCount()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = null;
			IndexReader reader = null;
			int i;
			long savedWriteLockTimeout = IndexWriterConfig.GetDefaultWriteLockTimeout();
			try
			{
				IndexWriterConfig.SetDefaultWriteLockTimeout(2000);
				NUnit.Framework.Assert.AreEqual(2000, IndexWriterConfig.GetDefaultWriteLockTimeout
					());
				writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random())));
			}
			finally
			{
				IndexWriterConfig.SetDefaultWriteLockTimeout(savedWriteLockTimeout);
			}
			// add 100 documents
			for (i = 0; i < 100; i++)
			{
				AddDocWithIndex(writer, i);
			}
			NUnit.Framework.Assert.AreEqual(100, writer.MaxDoc());
			writer.Close();
			// delete 40 documents
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
			for (i = 0; i < 40; i++)
			{
				writer.DeleteDocuments(new Term("id", string.Empty + i));
			}
			writer.Close();
			reader = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual(60, reader.NumDocs());
			reader.Close();
			// merge the index down and check that the new doc count is correct
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			NUnit.Framework.Assert.AreEqual(60, writer.NumDocs());
			writer.ForceMerge(1);
			NUnit.Framework.Assert.AreEqual(60, writer.MaxDoc());
			NUnit.Framework.Assert.AreEqual(60, writer.NumDocs());
			writer.Close();
			// check that the index reader gives the same numbers.
			reader = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual(60, reader.MaxDoc());
			NUnit.Framework.Assert.AreEqual(60, reader.NumDocs());
			reader.Close();
			// make sure opening a new index for create over
			// this existing one works correctly:
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			NUnit.Framework.Assert.AreEqual(0, writer.MaxDoc());
			NUnit.Framework.Assert.AreEqual(0, writer.NumDocs());
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void AddDoc(IndexWriter writer)
		{
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewTextField("content", "aaa", Field.Store.NO));
			writer.AddDocument(doc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static void AddDocWithIndex(IndexWriter writer, int index)
		{
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewField("content", "aaa " + index, storedTextType));
			doc.Add(NewField("id", string.Empty + index, storedTextType));
			writer.AddDocument(doc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void AssertNoUnreferencedFiles(Directory dir, string message)
		{
			string[] startFiles = dir.ListAll();
			new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()))).Rollback();
			string[] endFiles = dir.ListAll();
			Arrays.Sort(startFiles);
			Arrays.Sort(endFiles);
			if (!Arrays.Equals(startFiles, endFiles))
			{
				NUnit.Framework.Assert.Fail(message + ": before delete:\n    " + ArrayToString(startFiles
					) + "\n  after delete:\n    " + ArrayToString(endFiles));
			}
		}

		internal static string ArrayToString(string[] l)
		{
			string s = string.Empty;
			for (int i = 0; i < l.Length; i++)
			{
				if (i > 0)
				{
					s += "\n    ";
				}
				s += l[i];
			}
			return s;
		}

		// Make sure we can open an index for create even when a
		// reader holds it open (this fails pre lock-less
		// commits on windows):
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCreateWithReader()
		{
			Directory dir = NewDirectory();
			// add one document & close writer
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			AddDoc(writer);
			writer.Close();
			// now open reader:
			IndexReader reader = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual("should be one document", reader.NumDocs(), 1);
			// now open index for create:
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			NUnit.Framework.Assert.AreEqual("should be zero documents", writer.MaxDoc(), 0);
			AddDoc(writer);
			writer.Close();
			NUnit.Framework.Assert.AreEqual("should be one document", reader.NumDocs(), 1);
			IndexReader reader2 = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual("should be one document", reader2.NumDocs(), 1);
			reader.Close();
			reader2.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestChangesAfterClose()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = null;
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			AddDoc(writer);
			// close
			writer.Close();
			try
			{
				AddDoc(writer);
				NUnit.Framework.Assert.Fail("did not hit AlreadyClosedException");
			}
			catch (AlreadyClosedException)
			{
			}
			// expected
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIndexNoDocuments()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.Commit();
			writer.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual(0, reader.MaxDoc());
			NUnit.Framework.Assert.AreEqual(0, reader.NumDocs());
			reader.Close();
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			writer.Commit();
			writer.Close();
			reader = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual(0, reader.MaxDoc());
			NUnit.Framework.Assert.AreEqual(0, reader.NumDocs());
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestManyFields()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)));
			for (int j = 0; j < 100; j++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(NewField("a" + j, "aaa" + j, storedTextType));
				doc.Add(NewField("b" + j, "aaa" + j, storedTextType));
				doc.Add(NewField("c" + j, "aaa" + j, storedTextType));
				doc.Add(NewField("d" + j, "aaa", storedTextType));
				doc.Add(NewField("e" + j, "aaa", storedTextType));
				doc.Add(NewField("f" + j, "aaa", storedTextType));
				writer.AddDocument(doc);
			}
			writer.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual(100, reader.MaxDoc());
			NUnit.Framework.Assert.AreEqual(100, reader.NumDocs());
			for (int j_1 = 0; j_1 < 100; j_1++)
			{
				NUnit.Framework.Assert.AreEqual(1, reader.DocFreq(new Term("a" + j_1, "aaa" + j_1
					)));
				NUnit.Framework.Assert.AreEqual(1, reader.DocFreq(new Term("b" + j_1, "aaa" + j_1
					)));
				NUnit.Framework.Assert.AreEqual(1, reader.DocFreq(new Term("c" + j_1, "aaa" + j_1
					)));
				NUnit.Framework.Assert.AreEqual(1, reader.DocFreq(new Term("d" + j_1, "aaa")));
				NUnit.Framework.Assert.AreEqual(1, reader.DocFreq(new Term("e" + j_1, "aaa")));
				NUnit.Framework.Assert.AreEqual(1, reader.DocFreq(new Term("f" + j_1, "aaa")));
			}
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSmallRAMBuffer()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.000001))
				.SetMergePolicy(NewLogMergePolicy(10)));
			int lastNumFile = dir.ListAll().Length;
			for (int j = 0; j < 9; j++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(NewField("field", "aaa" + j, storedTextType));
				writer.AddDocument(doc);
				int numFile = dir.ListAll().Length;
				// Verify that with a tiny RAM buffer we see new
				// segment after every doc
				NUnit.Framework.Assert.IsTrue(numFile > lastNumFile);
				lastNumFile = numFile;
			}
			writer.Close();
			dir.Close();
		}

		// Make sure it's OK to change RAM buffer size and
		// maxBufferedDocs in a write session
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestChangingRAMBuffer()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.GetConfig().SetMaxBufferedDocs(10);
			writer.GetConfig().SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
			int lastFlushCount = -1;
			for (int j = 1; j < 52; j++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(new Field("field", "aaa" + j, storedTextType));
				writer.AddDocument(doc);
				TestUtil.SyncConcurrentMerges(writer);
				int flushCount = writer.GetFlushCount();
				if (j == 1)
				{
					lastFlushCount = flushCount;
				}
				else
				{
					if (j < 10)
					{
						// No new files should be created
						NUnit.Framework.Assert.AreEqual(flushCount, lastFlushCount);
					}
					else
					{
						if (10 == j)
						{
							NUnit.Framework.Assert.IsTrue(flushCount > lastFlushCount);
							lastFlushCount = flushCount;
							writer.GetConfig().SetRAMBufferSizeMB(0.000001);
							writer.GetConfig().SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
						}
						else
						{
							if (j < 20)
							{
								NUnit.Framework.Assert.IsTrue(flushCount > lastFlushCount);
								lastFlushCount = flushCount;
							}
							else
							{
								if (20 == j)
								{
									writer.GetConfig().SetRAMBufferSizeMB(16);
									writer.GetConfig().SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
									lastFlushCount = flushCount;
								}
								else
								{
									if (j < 30)
									{
										NUnit.Framework.Assert.AreEqual(flushCount, lastFlushCount);
									}
									else
									{
										if (30 == j)
										{
											writer.GetConfig().SetRAMBufferSizeMB(0.000001);
											writer.GetConfig().SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
										}
										else
										{
											if (j < 40)
											{
												NUnit.Framework.Assert.IsTrue(flushCount > lastFlushCount);
												lastFlushCount = flushCount;
											}
											else
											{
												if (40 == j)
												{
													writer.GetConfig().SetMaxBufferedDocs(10);
													writer.GetConfig().SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
													lastFlushCount = flushCount;
												}
												else
												{
													if (j < 50)
													{
														NUnit.Framework.Assert.AreEqual(flushCount, lastFlushCount);
														writer.GetConfig().SetMaxBufferedDocs(10);
														writer.GetConfig().SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
													}
													else
													{
														if (50 == j)
														{
															NUnit.Framework.Assert.IsTrue(flushCount > lastFlushCount);
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestChangingRAMBuffer2()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.GetConfig().SetMaxBufferedDocs(10);
			writer.GetConfig().SetMaxBufferedDeleteTerms(10);
			writer.GetConfig().SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
			for (int j = 1; j < 52; j++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(new Field("field", "aaa" + j, storedTextType));
				writer.AddDocument(doc);
			}
			int lastFlushCount = -1;
			for (int j_1 = 1; j_1 < 52; j_1++)
			{
				writer.DeleteDocuments(new Term("field", "aaa" + j_1));
				TestUtil.SyncConcurrentMerges(writer);
				int flushCount = writer.GetFlushCount();
				if (j_1 == 1)
				{
					lastFlushCount = flushCount;
				}
				else
				{
					if (j_1 < 10)
					{
						// No new files should be created
						NUnit.Framework.Assert.AreEqual(flushCount, lastFlushCount);
					}
					else
					{
						if (10 == j_1)
						{
							NUnit.Framework.Assert.IsTrue(string.Empty + j_1, flushCount > lastFlushCount);
							lastFlushCount = flushCount;
							writer.GetConfig().SetRAMBufferSizeMB(0.000001);
							writer.GetConfig().SetMaxBufferedDeleteTerms(1);
						}
						else
						{
							if (j_1 < 20)
							{
								NUnit.Framework.Assert.IsTrue(flushCount > lastFlushCount);
								lastFlushCount = flushCount;
							}
							else
							{
								if (20 == j_1)
								{
									writer.GetConfig().SetRAMBufferSizeMB(16);
									writer.GetConfig().SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH
										);
									lastFlushCount = flushCount;
								}
								else
								{
									if (j_1 < 30)
									{
										NUnit.Framework.Assert.AreEqual(flushCount, lastFlushCount);
									}
									else
									{
										if (30 == j_1)
										{
											writer.GetConfig().SetRAMBufferSizeMB(0.000001);
											writer.GetConfig().SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH
												);
											writer.GetConfig().SetMaxBufferedDeleteTerms(1);
										}
										else
										{
											if (j_1 < 40)
											{
												NUnit.Framework.Assert.IsTrue(flushCount > lastFlushCount);
												lastFlushCount = flushCount;
											}
											else
											{
												if (40 == j_1)
												{
													writer.GetConfig().SetMaxBufferedDeleteTerms(10);
													writer.GetConfig().SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
													lastFlushCount = flushCount;
												}
												else
												{
													if (j_1 < 50)
													{
														NUnit.Framework.Assert.AreEqual(flushCount, lastFlushCount);
														writer.GetConfig().SetMaxBufferedDeleteTerms(10);
														writer.GetConfig().SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
													}
													else
													{
														if (50 == j_1)
														{
															NUnit.Framework.Assert.IsTrue(flushCount > lastFlushCount);
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDiverseDocs()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.5)));
			int n = AtLeast(1);
			for (int i = 0; i < n; i++)
			{
				// First, docs where every term is unique (heavy on
				// Posting instances)
				for (int j = 0; j < 100; j++)
				{
					Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
						();
					for (int k = 0; k < 100; k++)
					{
						doc.Add(NewField("field", Sharpen.Extensions.ToString(Random().Next()), storedTextType
							));
					}
					writer.AddDocument(doc);
				}
				// Next, many single term docs where only one term
				// occurs (heavy on byte blocks)
				for (int j_1 = 0; j_1 < 100; j_1++)
				{
					Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
						();
					doc.Add(NewField("field", "aaa aaa aaa aaa aaa aaa aaa aaa aaa aaa", storedTextType
						));
					writer.AddDocument(doc);
				}
				// Next, many single term docs where only one term
				// occurs but the terms are very long (heavy on
				// char[] arrays)
				for (int j_2 = 0; j_2 < 100; j_2++)
				{
					StringBuilder b = new StringBuilder();
					string x = Sharpen.Extensions.ToString(j_2) + ".";
					for (int k = 0; k < 1000; k++)
					{
						b.Append(x);
					}
					string longTerm = b.ToString();
					Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
						();
					doc.Add(NewField("field", longTerm, storedTextType));
					writer.AddDocument(doc);
				}
			}
			writer.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			int totalHits = searcher.Search(new TermQuery(new Term("field", "aaa")), null, 1)
				.totalHits;
			NUnit.Framework.Assert.AreEqual(n * 100, totalHits);
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEnablingNorms()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(10)));
			// Enable norms for only 1 doc, pre flush
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetOmitNorms(true);
			for (int j = 0; j < 10; j++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				Field f = null;
				if (j != 8)
				{
					f = NewField("field", "aaa", customType);
				}
				else
				{
					f = NewField("field", "aaa", storedTextType);
				}
				doc.Add(f);
				writer.AddDocument(doc);
			}
			writer.Close();
			Term searchTerm = new Term("field", "aaa");
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = searcher.Search(new TermQuery(searchTerm), null, 1000).scoreDocs;
			NUnit.Framework.Assert.AreEqual(10, hits.Length);
			reader.Close();
			writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMaxBufferedDocs
				(10)));
			// Enable norms for only 1 doc, post flush
			for (int j_1 = 0; j_1 < 27; j_1++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				Field f = null;
				if (j_1 != 26)
				{
					f = NewField("field", "aaa", customType);
				}
				else
				{
					f = NewField("field", "aaa", storedTextType);
				}
				doc.Add(f);
				writer.AddDocument(doc);
			}
			writer.Close();
			reader = DirectoryReader.Open(dir);
			searcher = NewSearcher(reader);
			hits = searcher.Search(new TermQuery(searchTerm), null, 1000).scoreDocs;
			NUnit.Framework.Assert.AreEqual(27, hits.Length);
			reader.Close();
			reader = DirectoryReader.Open(dir);
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestHighFreqTerm()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.01)));
			// Massive doc that has 128 K a's
			StringBuilder b = new StringBuilder(1024 * 1024);
			for (int i = 0; i < 4096; i++)
			{
				b.Append(" a a a a a a a a");
				b.Append(" a a a a a a a a");
				b.Append(" a a a a a a a a");
				b.Append(" a a a a a a a a");
			}
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			doc.Add(NewField("field", b.ToString(), customType));
			writer.AddDocument(doc);
			writer.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual(1, reader.MaxDoc());
			NUnit.Framework.Assert.AreEqual(1, reader.NumDocs());
			Term t = new Term("field", "a");
			NUnit.Framework.Assert.AreEqual(1, reader.DocFreq(t));
			DocsEnum td = TestUtil.Docs(Random(), reader, "field", new BytesRef("a"), MultiFields
				.GetLiveDocs(reader), null, DocsEnum.FLAG_FREQS);
			td.NextDoc();
			NUnit.Framework.Assert.AreEqual(128 * 1024, td.Freq());
			reader.Close();
			dir.Close();
		}

		// Make sure that a Directory implementation that does
		// not use LockFactory at all (ie overrides makeLock and
		// implements its own private locking) works OK.  This
		// was raised on java-dev as loss of backwards
		// compatibility.
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNullLockFactory()
		{
			Directory dir = new _T1455748355(this, new RAMDirectory());
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			for (int i = 0; i < 100; i++)
			{
				AddDoc(writer);
			}
			writer.Close();
			Term searchTerm = new Term("content", "aaa");
			IndexReader reader = DirectoryReader.Open(dir);
			IndexSearcher searcher = NewSearcher(reader);
			ScoreDoc[] hits = searcher.Search(new TermQuery(searchTerm), null, 1000).scoreDocs;
			NUnit.Framework.Assert.AreEqual("did not get right number of hits", 100, hits.Length
				);
			reader.Close();
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE));
			writer.Close();
			dir.Close();
		}

		internal sealed class _T1455748355 : MockDirectoryWrapper
		{
			private LockFactory myLockFactory;

			protected _T1455748355(TestIndexWriter _enclosing, Directory delegate_) : base(LuceneTestCase
				.Random(), delegate_)
			{
				this._enclosing = _enclosing;
				this.lockFactory = null;
				this.myLockFactory = new SingleInstanceLockFactory();
			}

			public override Lock MakeLock(string name)
			{
				return this.myLockFactory.MakeLock(name);
			}

			private readonly TestIndexWriter _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestFlushWithNoMerging()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy
				(NewLogMergePolicy(10)));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			doc.Add(NewField("field", "aaa", customType));
			for (int i = 0; i < 19; i++)
			{
				writer.AddDocument(doc);
			}
			writer.Flush(false, true);
			writer.Close();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			// Since we flushed w/o allowing merging we should now
			// have 10 segments
			NUnit.Framework.Assert.AreEqual(10, sis.Size());
			dir.Close();
		}

		// Make sure we can flush segment w/ norms, then add
		// empty doc (no norms) and flush
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEmptyDocAfterFlushingRealDoc()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			doc.Add(NewField("field", "aaa", customType));
			writer.AddDocument(doc);
			writer.Commit();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: now add empty doc");
			}
			writer.AddDocument(new Lucene.Net.Document.Document());
			writer.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual(2, reader.NumDocs());
			reader.Close();
			dir.Close();
		}

		/// <summary>
		/// Test that no NullPointerException will be raised,
		/// when adding one document with a single, empty field
		/// and term vectors enabled.
		/// </summary>
		/// <remarks>
		/// Test that no NullPointerException will be raised,
		/// when adding one document with a single, empty field
		/// and term vectors enabled.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBadSegment()
		{
			Directory dir = NewDirectory();
			IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			Lucene.Net.Document.Document document = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			document.Add(NewField("tvtest", string.Empty, customType));
			iw.AddDocument(document);
			iw.Close();
			dir.Close();
		}

		// LUCENE-1036
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMaxThreadPriority()
		{
			int pri = Sharpen.Thread.CurrentThread().GetPriority();
			try
			{
				Directory dir = NewDirectory();
				IndexWriterConfig conf = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy(NewLogMergePolicy
					());
				((LogMergePolicy)conf.GetMergePolicy()).SetMergeFactor(2);
				IndexWriter iw = new IndexWriter(dir, conf);
				Lucene.Net.Document.Document document = new Lucene.Net.Document.Document
					();
				FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
				customType.SetStoreTermVectors(true);
				document.Add(NewField("tvtest", "a b c", customType));
				Sharpen.Thread.CurrentThread().SetPriority(Sharpen.Thread.MAX_PRIORITY);
				for (int i = 0; i < 4; i++)
				{
					iw.AddDocument(document);
				}
				iw.Close();
				dir.Close();
			}
			finally
			{
				Sharpen.Thread.CurrentThread().SetPriority(pri);
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestVariableSchema()
		{
			Directory dir = NewDirectory();
			for (int i = 0; i < 20; i++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: iter=" + i);
				}
				IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
					(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetMergePolicy
					(NewLogMergePolicy()));
				//LogMergePolicy lmp = (LogMergePolicy) writer.getConfig().getMergePolicy();
				//lmp.setMergeFactor(2);
				//lmp.setNoCFSRatio(0.0);
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				string contents = "aa bb cc dd ee ff gg hh ii jj kk";
				FieldType customType = new FieldType(TextField.TYPE_STORED);
				FieldType type = null;
				if (i == 7)
				{
					// Add empty docs here
					doc.Add(NewTextField("content3", string.Empty, Field.Store.NO));
				}
				else
				{
					if (i % 2 == 0)
					{
						doc.Add(NewField("content4", contents, customType));
						type = customType;
					}
					else
					{
						type = TextField.TYPE_NOT_STORED;
					}
					doc.Add(NewTextField("content1", contents, Field.Store.NO));
					doc.Add(NewField("content3", string.Empty, customType));
					doc.Add(NewField("content5", string.Empty, type));
				}
				for (int j = 0; j < 4; j++)
				{
					writer.AddDocument(doc);
				}
				writer.Close();
				if (0 == i % 4)
				{
					writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
						(Random())));
					//LogMergePolicy lmp2 = (LogMergePolicy) writer.getConfig().getMergePolicy();
					//lmp2.setNoCFSRatio(0.0);
					writer.ForceMerge(1);
					writer.Close();
				}
			}
			dir.Close();
		}

		// LUCENE-1084: test unlimited field length
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestUnlimitedMaxFieldLength()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			StringBuilder b = new StringBuilder();
			for (int i = 0; i < 10000; i++)
			{
				b.Append(" a");
			}
			b.Append(" x");
			doc.Add(NewTextField("field", b.ToString(), Field.Store.NO));
			writer.AddDocument(doc);
			writer.Close();
			IndexReader reader = DirectoryReader.Open(dir);
			Term t = new Term("field", "x");
			NUnit.Framework.Assert.AreEqual(1, reader.DocFreq(t));
			reader.Close();
			dir.Close();
		}

		// LUCENE-1179
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEmptyFieldName()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewTextField(string.Empty, "a b c", Field.Store.NO));
			writer.AddDocument(doc);
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEmptyFieldNameTerms()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewTextField(string.Empty, "a b c", Field.Store.NO));
			writer.AddDocument(doc);
			writer.Close();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AtomicReader subreader = GetOnlySegmentReader(reader);
			TermsEnum te = subreader.Fields().Terms(string.Empty).Iterator(null);
			NUnit.Framework.Assert.AreEqual(new BytesRef("a"), te.Next());
			NUnit.Framework.Assert.AreEqual(new BytesRef("b"), te.Next());
			NUnit.Framework.Assert.AreEqual(new BytesRef("c"), te.Next());
			NUnit.Framework.Assert.IsNull(te.Next());
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEmptyFieldNameWithEmptyTerm()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewStringField(string.Empty, string.Empty, Field.Store.NO));
			doc.Add(NewStringField(string.Empty, "a", Field.Store.NO));
			doc.Add(NewStringField(string.Empty, "b", Field.Store.NO));
			doc.Add(NewStringField(string.Empty, "c", Field.Store.NO));
			writer.AddDocument(doc);
			writer.Close();
			DirectoryReader reader = DirectoryReader.Open(dir);
			AtomicReader subreader = GetOnlySegmentReader(reader);
			TermsEnum te = subreader.Fields().Terms(string.Empty).Iterator(null);
			NUnit.Framework.Assert.AreEqual(new BytesRef(string.Empty), te.Next());
			NUnit.Framework.Assert.AreEqual(new BytesRef("a"), te.Next());
			NUnit.Framework.Assert.AreEqual(new BytesRef("b"), te.Next());
			NUnit.Framework.Assert.AreEqual(new BytesRef("c"), te.Next());
			NUnit.Framework.Assert.IsNull(te.Next());
			reader.Close();
			dir.Close();
		}

		private sealed class MockIndexWriter : IndexWriter
		{
			/// <exception cref="System.IO.IOException"></exception>
			public MockIndexWriter(Directory dir, IndexWriterConfig conf) : base(dir, conf)
			{
			}

			internal bool afterWasCalled;

			internal bool beforeWasCalled;

			protected override void DoAfterFlush()
			{
				afterWasCalled = true;
			}

			protected override void DoBeforeFlush()
			{
				beforeWasCalled = true;
			}
		}

		// LUCENE-1222
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDoBeforeAfterFlush()
		{
			Directory dir = NewDirectory();
			TestIndexWriter.MockIndexWriter w = new TestIndexWriter.MockIndexWriter(dir, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			doc.Add(NewField("field", "a field", customType));
			w.AddDocument(doc);
			w.Commit();
			NUnit.Framework.Assert.IsTrue(w.beforeWasCalled);
			NUnit.Framework.Assert.IsTrue(w.afterWasCalled);
			w.beforeWasCalled = false;
			w.afterWasCalled = false;
			w.DeleteDocuments(new Term("field", "field"));
			w.Commit();
			NUnit.Framework.Assert.IsTrue(w.beforeWasCalled);
			NUnit.Framework.Assert.IsTrue(w.afterWasCalled);
			w.Close();
			IndexReader ir = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual(0, ir.NumDocs());
			ir.Close();
			dir.Close();
		}

		// LUCENE-1255
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNegativePositions()
		{
			TokenStream tokens = new _TokenStream_880();
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(new TextField("field", tokens));
			try
			{
				w.AddDocument(doc);
				NUnit.Framework.Assert.Fail("did not hit expected exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			w.Close();
			dir.Close();
		}

		private sealed class _TokenStream_880 : TokenStream
		{
			public _TokenStream_880()
			{
				this.termAtt = this.AddAttribute<CharTermAttribute>();
				this.posIncrAtt = this.AddAttribute<PositionIncrementAttribute>();
				this.terms = Arrays.AsList("a", "b", "c").Iterator();
				this.first = true;
			}

			internal readonly CharTermAttribute termAtt;

			internal readonly PositionIncrementAttribute posIncrAtt;

			internal readonly Iterator<string> terms;

			internal bool first;

			public override bool IncrementToken()
			{
				if (!this.terms.HasNext())
				{
					return false;
				}
				this.ClearAttributes();
				this.termAtt.Append(this.terms.Next());
				this.posIncrAtt.SetPositionIncrement(this.first ? 0 : 1);
				this.first = false;
				return true;
			}
		}

		// LUCENE-2529
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPositionIncrementGapEmptyField()
		{
			Directory dir = NewDirectory();
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetPositionIncrementGap(100);
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer
				));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			Field f = NewField("field", string.Empty, customType);
			Field f2 = NewField("field", "crunch man", customType);
			doc.Add(f);
			doc.Add(f2);
			w.AddDocument(doc);
			w.Close();
			IndexReader r = DirectoryReader.Open(dir);
			Terms tpv = r.GetTermVectors(0).Terms("field");
			TermsEnum termsEnum = tpv.Iterator(null);
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
			NUnit.Framework.Assert.IsNotNull(dpEnum);
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			NUnit.Framework.Assert.AreEqual(1, dpEnum.Freq());
			NUnit.Framework.Assert.AreEqual(100, dpEnum.NextPosition());
			NUnit.Framework.Assert.IsNotNull(termsEnum.Next());
			dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
			NUnit.Framework.Assert.IsNotNull(dpEnum);
			NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			NUnit.Framework.Assert.AreEqual(1, dpEnum.Freq());
			NUnit.Framework.Assert.AreEqual(101, dpEnum.NextPosition());
			NUnit.Framework.Assert.IsNull(termsEnum.Next());
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDeadlock()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			doc.Add(NewField("content", "aaa bbb ccc ddd eee fff ggg hhh iii", customType));
			writer.AddDocument(doc);
			writer.AddDocument(doc);
			writer.AddDocument(doc);
			writer.Commit();
			// index has 2 segments
			Directory dir2 = NewDirectory();
			IndexWriter writer2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer2.AddDocument(doc);
			writer2.Close();
			IndexReader r1 = DirectoryReader.Open(dir2);
			writer.AddIndexes(r1, r1);
			writer.Close();
			IndexReader r3 = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual(5, r3.NumDocs());
			r3.Close();
			r1.Close();
			dir2.Close();
			dir.Close();
		}

		private class IndexerThreadInterrupt : Sharpen.Thread
		{
			internal volatile bool failed;

			internal volatile bool finish;

			internal volatile bool allowInterrupt = false;

			internal readonly Random random;

			internal readonly Directory adder;

			/// <exception cref="System.IO.IOException"></exception>
			public IndexerThreadInterrupt(TestIndexWriter _enclosing)
			{
				this._enclosing = _enclosing;
				this.random = new Random(LuceneTestCase.Random().NextLong());
				// make a little directory for addIndexes
				// LUCENE-2239: won't work with NIOFS/MMAP
				this.adder = new MockDirectoryWrapper(this.random, new RAMDirectory());
				IndexWriterConfig conf = LuceneTestCase.NewIndexWriterConfig(this.random, LuceneTestCase
					.TEST_VERSION_CURRENT, new MockAnalyzer(this.random));
				IndexWriter w = new IndexWriter(this.adder, conf);
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(LuceneTestCase.NewStringField(this.random, "id", "500", Field.Store.NO));
				doc.Add(LuceneTestCase.NewField(this.random, "field", "some prepackaged text contents"
					, TestIndexWriter.storedTextType));
				if (LuceneTestCase.DefaultCodecSupportsDocValues())
				{
					doc.Add(new BinaryDocValuesField("binarydv", new BytesRef("500")));
					doc.Add(new NumericDocValuesField("numericdv", 500));
					doc.Add(new SortedDocValuesField("sorteddv", new BytesRef("500")));
				}
				if (LuceneTestCase.DefaultCodecSupportsSortedSet())
				{
					doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("one")));
					doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("two")));
				}
				w.AddDocument(doc);
				doc = new Lucene.Net.Document.Document();
				doc.Add(LuceneTestCase.NewStringField(this.random, "id", "501", Field.Store.NO));
				doc.Add(LuceneTestCase.NewField(this.random, "field", "some more contents", TestIndexWriter
					.storedTextType));
				if (LuceneTestCase.DefaultCodecSupportsDocValues())
				{
					doc.Add(new BinaryDocValuesField("binarydv", new BytesRef("501")));
					doc.Add(new NumericDocValuesField("numericdv", 501));
					doc.Add(new SortedDocValuesField("sorteddv", new BytesRef("501")));
				}
				if (LuceneTestCase.DefaultCodecSupportsSortedSet())
				{
					doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("two")));
					doc.Add(new SortedSetDocValuesField("sortedsetdv", new BytesRef("three")));
				}
				w.AddDocument(doc);
				w.DeleteDocuments(new Term("id", "500"));
				w.Close();
			}

			public override void Run()
			{
				// LUCENE-2239: won't work with NIOFS/MMAP
				MockDirectoryWrapper dir = new MockDirectoryWrapper(this.random, new RAMDirectory
					());
				// When interrupt arrives in w.close(), when it's
				// writing liveDocs, this can lead to double-write of
				// _X_N.del:
				//dir.setPreventDoubleWrite(false);
				IndexWriter w = null;
				while (!this.finish)
				{
					try
					{
						while (!this.finish)
						{
							if (w != null)
							{
								// If interrupt arrives inside here, it's
								// fine: we will cycle back and the first
								// thing we do is try to close again,
								// i.e. we'll never try to open a new writer
								// until this one successfully closes:
								w.Close();
								w = null;
							}
							IndexWriterConfig conf = ((IndexWriterConfig)LuceneTestCase.NewIndexWriterConfig(
								this.random, LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(this.random))
								.SetMaxBufferedDocs(2));
							w = new IndexWriter(dir, conf);
							Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
								();
							Field idField = LuceneTestCase.NewStringField(this.random, "id", string.Empty, Field.Store
								.NO);
							Field binaryDVField = null;
							Field numericDVField = null;
							Field sortedDVField = null;
							Field sortedSetDVField = new SortedSetDocValuesField("sortedsetdv", new BytesRef(
								));
							doc.Add(idField);
							doc.Add(LuceneTestCase.NewField(this.random, "field", "some text contents", TestIndexWriter
								.storedTextType));
							if (LuceneTestCase.DefaultCodecSupportsDocValues())
							{
								binaryDVField = new BinaryDocValuesField("binarydv", new BytesRef());
								numericDVField = new NumericDocValuesField("numericdv", 0);
								sortedDVField = new SortedDocValuesField("sorteddv", new BytesRef());
								doc.Add(binaryDVField);
								doc.Add(numericDVField);
								doc.Add(sortedDVField);
							}
							if (LuceneTestCase.DefaultCodecSupportsSortedSet())
							{
								doc.Add(sortedSetDVField);
							}
							for (int i = 0; i < 100; i++)
							{
								idField.SetStringValue(Sharpen.Extensions.ToString(i));
								if (LuceneTestCase.DefaultCodecSupportsDocValues())
								{
									binaryDVField.SetBytesValue(new BytesRef(idField.StringValue()));
									numericDVField.SetLongValue(i);
									sortedDVField.SetBytesValue(new BytesRef(idField.StringValue()));
								}
								sortedSetDVField.SetBytesValue(new BytesRef(idField.StringValue()));
								int action = this.random.Next(100);
								if (action == 17)
								{
									w.AddIndexes(this.adder);
								}
								else
								{
									if (action % 30 == 0)
									{
										w.DeleteAll();
									}
									else
									{
										if (action % 2 == 0)
										{
											w.UpdateDocument(new Term("id", idField.StringValue()), doc);
										}
										else
										{
											w.AddDocument(doc);
										}
									}
								}
								if (this.random.Next(3) == 0)
								{
									IndexReader r = null;
									try
									{
										r = DirectoryReader.Open(w, this.random.NextBoolean());
										if (this.random.NextBoolean() && r.MaxDoc() > 0)
										{
											int docid = this.random.Next(r.MaxDoc());
											w.TryDeleteDocument(r, docid);
										}
									}
									finally
									{
										IOUtils.CloseWhileHandlingException(r);
									}
								}
								if (i % 10 == 0)
								{
									w.Commit();
								}
								if (this.random.Next(50) == 0)
								{
									w.ForceMerge(1);
								}
							}
							w.Close();
							w = null;
							DirectoryReader.Open(dir).Close();
							// Strangely, if we interrupt a thread before
							// all classes are loaded, the class loader
							// seems to do scary things with the interrupt
							// status.  In java 1.5, it'll throw an
							// incorrect ClassNotFoundException.  In java
							// 1.6, it'll silently clear the interrupt.
							// So, on first iteration through here we
							// don't open ourselves up for interrupts
							// until we've done the above loop.
							this.allowInterrupt = true;
						}
					}
					catch (ThreadInterruptedException re)
					{
						// NOTE: important to leave this verbosity/noise
						// on!!  This test doesn't repro easily so when
						// Jenkins hits a fail we need to study where the
						// interrupts struck!
						System.Console.Out.WriteLine("TEST: got interrupt");
						Sharpen.Runtime.PrintStackTrace(re, System.Console.Out);
						Exception e = re.InnerException;
						NUnit.Framework.Assert.IsTrue(e is Exception);
						if (this.finish)
						{
							break;
						}
					}
					catch (Exception t)
					{
						System.Console.Out.WriteLine("FAILED; unexpected exception");
						Sharpen.Runtime.PrintStackTrace(t, System.Console.Out);
						this.failed = true;
						break;
					}
				}
				if (!this.failed)
				{
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("TEST: now rollback");
					}
					// clear interrupt state:
					Sharpen.Thread.Interrupted();
					if (w != null)
					{
						try
						{
							w.Rollback();
						}
						catch (IOException ioe)
						{
							throw new RuntimeException(ioe);
						}
					}
					try
					{
						TestUtil.CheckIndex(dir);
					}
					catch (Exception e)
					{
						this.failed = true;
						System.Console.Out.WriteLine("CheckIndex FAILED: unexpected exception");
						Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
					}
					try
					{
						IndexReader r = DirectoryReader.Open(dir);
						//System.out.println("doc count=" + r.numDocs());
						r.Close();
					}
					catch (Exception e)
					{
						this.failed = true;
						System.Console.Out.WriteLine("DirectoryReader.open FAILED: unexpected exception");
						Sharpen.Runtime.PrintStackTrace(e, System.Console.Out);
					}
				}
				try
				{
					IOUtils.Close(dir);
				}
				catch (IOException e)
				{
					throw new RuntimeException(e);
				}
				try
				{
					IOUtils.Close(this.adder);
				}
				catch (IOException e)
				{
					throw new RuntimeException(e);
				}
			}

			private readonly TestIndexWriter _enclosing;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestThreadInterruptDeadlock()
		{
			TestIndexWriter.IndexerThreadInterrupt t = new TestIndexWriter.IndexerThreadInterrupt
				(this);
			t.SetDaemon(true);
			t.Start();
			// Force class loader to load ThreadInterruptedException
			// up front... else we can see a false failure if 2nd
			// interrupt arrives while class loader is trying to
			// init this class (in servicing a first interrupt):
			NUnit.Framework.Assert.IsTrue(new ThreadInterruptedException(new Exception()).InnerException
				 is Exception);
			// issue 300 interrupts to child thread
			int numInterrupts = AtLeast(300);
			int i = 0;
			while (i < numInterrupts)
			{
				// TODO: would be nice to also sometimes interrupt the
				// CMS merge threads too ...
				Sharpen.Thread.Sleep(10);
				if (t.allowInterrupt)
				{
					i++;
					t.Interrupt();
				}
				if (!t.IsAlive())
				{
					break;
				}
			}
			t.finish = true;
			t.Join();
			NUnit.Framework.Assert.IsFalse(t.failed);
		}

		/// <summary>testThreadInterruptDeadlock but with 2 indexer threads</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestTwoThreadsInterruptDeadlock()
		{
			TestIndexWriter.IndexerThreadInterrupt t1 = new TestIndexWriter.IndexerThreadInterrupt
				(this);
			t1.SetDaemon(true);
			t1.Start();
			TestIndexWriter.IndexerThreadInterrupt t2 = new TestIndexWriter.IndexerThreadInterrupt
				(this);
			t2.SetDaemon(true);
			t2.Start();
			// Force class loader to load ThreadInterruptedException
			// up front... else we can see a false failure if 2nd
			// interrupt arrives while class loader is trying to
			// init this class (in servicing a first interrupt):
			NUnit.Framework.Assert.IsTrue(new ThreadInterruptedException(new Exception()).InnerException
				 is Exception);
			// issue 300 interrupts to child thread
			int numInterrupts = AtLeast(300);
			int i = 0;
			while (i < numInterrupts)
			{
				// TODO: would be nice to also sometimes interrupt the
				// CMS merge threads too ...
				Sharpen.Thread.Sleep(10);
				TestIndexWriter.IndexerThreadInterrupt t = Random().NextBoolean() ? t1 : t2;
				if (t.allowInterrupt)
				{
					i++;
					t.Interrupt();
				}
				if (!t1.IsAlive() && !t2.IsAlive())
				{
					break;
				}
			}
			t1.finish = true;
			t2.finish = true;
			t1.Join();
			t2.Join();
			NUnit.Framework.Assert.IsFalse(t1.failed);
			NUnit.Framework.Assert.IsFalse(t2.failed);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIndexStoreCombos()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			byte[] b = new byte[50];
			for (int i = 0; i < 50; i++)
			{
				b[i] = unchecked((byte)(i + 77));
			}
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(StoredField.TYPE);
			customType.SetTokenized(true);
			Field f = new Field("binary", b, 10, 17, customType);
			customType.SetIndexed(true);
			f.SetTokenStream(new MockTokenizer(new StringReader("doc1field1"), MockTokenizer.
				WHITESPACE, false));
			FieldType customType2 = new FieldType(TextField.TYPE_STORED);
			Field f2 = NewField("string", "value", customType2);
			f2.SetTokenStream(new MockTokenizer(new StringReader("doc1field2"), MockTokenizer
				.WHITESPACE, false));
			doc.Add(f);
			doc.Add(f2);
			w.AddDocument(doc);
			// add 2 docs to test in-memory merging
			f.SetTokenStream(new MockTokenizer(new StringReader("doc2field1"), MockTokenizer.
				WHITESPACE, false));
			f2.SetTokenStream(new MockTokenizer(new StringReader("doc2field2"), MockTokenizer
				.WHITESPACE, false));
			w.AddDocument(doc);
			// force segment flush so we can force a segment merge with doc3 later.
			w.Commit();
			f.SetTokenStream(new MockTokenizer(new StringReader("doc3field1"), MockTokenizer.
				WHITESPACE, false));
			f2.SetTokenStream(new MockTokenizer(new StringReader("doc3field2"), MockTokenizer
				.WHITESPACE, false));
			w.AddDocument(doc);
			w.Commit();
			w.ForceMerge(1);
			// force segment merge.
			w.Close();
			IndexReader ir = DirectoryReader.Open(dir);
			Lucene.Net.Document.Document doc2 = ir.Document(0);
			IndexableField f3 = doc2.GetField("binary");
			b = f3.BinaryValue().bytes;
			NUnit.Framework.Assert.IsTrue(b != null);
			NUnit.Framework.Assert.AreEqual(17, b.Length, 17);
			NUnit.Framework.Assert.AreEqual(87, b[0]);
			NUnit.Framework.Assert.IsTrue(ir.Document(0).GetField("binary").BinaryValue() != 
				null);
			NUnit.Framework.Assert.IsTrue(ir.Document(1).GetField("binary").BinaryValue() != 
				null);
			NUnit.Framework.Assert.IsTrue(ir.Document(2).GetField("binary").BinaryValue() != 
				null);
			NUnit.Framework.Assert.AreEqual("value", ir.Document(0).Get("string"));
			NUnit.Framework.Assert.AreEqual("value", ir.Document(1).Get("string"));
			NUnit.Framework.Assert.AreEqual("value", ir.Document(2).Get("string"));
			// test that the terms were indexed.
			NUnit.Framework.Assert.IsTrue(TestUtil.Docs(Random(), ir, "binary", new BytesRef(
				"doc1field1"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			NUnit.Framework.Assert.IsTrue(TestUtil.Docs(Random(), ir, "binary", new BytesRef(
				"doc2field1"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			NUnit.Framework.Assert.IsTrue(TestUtil.Docs(Random(), ir, "binary", new BytesRef(
				"doc3field1"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			NUnit.Framework.Assert.IsTrue(TestUtil.Docs(Random(), ir, "string", new BytesRef(
				"doc1field2"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			NUnit.Framework.Assert.IsTrue(TestUtil.Docs(Random(), ir, "string", new BytesRef(
				"doc2field2"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			NUnit.Framework.Assert.IsTrue(TestUtil.Docs(Random(), ir, "string", new BytesRef(
				"doc3field2"), null, null, DocsEnum.FLAG_NONE).NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			ir.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoDocsIndex()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			writer.AddDocument(new Lucene.Net.Document.Document());
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestIndexDivisor()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig config = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			config.SetTermIndexInterval(2);
			IndexWriter w = new IndexWriter(dir, config);
			StringBuilder s = new StringBuilder();
			// must be > 256
			for (int i = 0; i < 300; i++)
			{
				s.Append(' ').Append(i);
			}
			Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
			Field f = NewTextField("field", s.ToString(), Field.Store.NO);
			d.Add(f);
			w.AddDocument(d);
			AtomicReader r = GetOnlySegmentReader(w.GetReader());
			TermsEnum t = r.Fields().Terms("field").Iterator(null);
			int count = 0;
			while (t.Next() != null)
			{
				DocsEnum docs = TestUtil.Docs(Random(), t, null, null, DocsEnum.FLAG_NONE);
				NUnit.Framework.Assert.AreEqual(0, docs.NextDoc());
				NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docs.NextDoc());
				count++;
			}
			NUnit.Framework.Assert.AreEqual(300, count);
			r.Close();
			w.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDeleteUnusedFiles()
		{
			for (int iter = 0; iter < 2; iter++)
			{
				Directory dir = NewMockDirectory();
				// relies on windows semantics
				MergePolicy mergePolicy = NewLogMergePolicy(true);
				// This test expects all of its segments to be in CFS
				mergePolicy.SetNoCFSRatio(1.0);
				mergePolicy.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
				IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetMergePolicy(mergePolicy).SetUseCompoundFile(true
					)));
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(NewTextField("field", "go", Field.Store.NO));
				w.AddDocument(doc);
				DirectoryReader r;
				if (iter == 0)
				{
					// use NRT
					r = w.GetReader();
				}
				else
				{
					// don't use NRT
					w.Commit();
					r = DirectoryReader.Open(dir);
				}
				IList<string> files = new AList<string>(Arrays.AsList(dir.ListAll()));
				// RAMDir won't have a write.lock, but fs dirs will:
				files.Remove("write.lock");
				NUnit.Framework.Assert.IsTrue(files.Contains("_0.cfs"));
				NUnit.Framework.Assert.IsTrue(files.Contains("_0.cfe"));
				NUnit.Framework.Assert.IsTrue(files.Contains("_0.si"));
				if (iter == 1)
				{
					// we run a full commit so there should be a segments file etc.
					NUnit.Framework.Assert.IsTrue(files.Contains("segments_1"));
					NUnit.Framework.Assert.IsTrue(files.Contains("segments.gen"));
					NUnit.Framework.Assert.AreEqual(files.ToString(), files.Count, 5);
				}
				else
				{
					// this is an NRT reopen - no segments files yet
					NUnit.Framework.Assert.AreEqual(files.ToString(), files.Count, 3);
				}
				w.AddDocument(doc);
				w.ForceMerge(1);
				if (iter == 1)
				{
					w.Commit();
				}
				IndexReader r2 = DirectoryReader.OpenIfChanged(r);
				NUnit.Framework.Assert.IsNotNull(r2);
				NUnit.Framework.Assert.IsTrue(r != r2);
				files = Arrays.AsList(dir.ListAll());
				// NOTE: here we rely on "Windows" behavior, ie, even
				// though IW wanted to delete _0.cfs since it was
				// merged away, because we have a reader open
				// against this file, it should still be here:
				NUnit.Framework.Assert.IsTrue(files.Contains("_0.cfs"));
				// forceMerge created this
				//assertTrue(files.contains("_2.cfs"));
				w.DeleteUnusedFiles();
				files = Arrays.AsList(dir.ListAll());
				// r still holds this file open
				NUnit.Framework.Assert.IsTrue(files.Contains("_0.cfs"));
				//assertTrue(files.contains("_2.cfs"));
				r.Close();
				if (iter == 0)
				{
					// on closing NRT reader, it calls writer.deleteUnusedFiles
					files = Arrays.AsList(dir.ListAll());
					NUnit.Framework.Assert.IsFalse(files.Contains("_0.cfs"));
				}
				else
				{
					// now writer can remove it
					w.DeleteUnusedFiles();
					files = Arrays.AsList(dir.ListAll());
					NUnit.Framework.Assert.IsFalse(files.Contains("_0.cfs"));
				}
				//assertTrue(files.contains("_2.cfs"));
				w.Close();
				r2.Close();
				dir.Close();
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDeleteUnsedFiles2()
		{
			// Validates that iw.deleteUnusedFiles() also deletes unused index commits
			// in case a deletion policy which holds onto commits is used.
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetIndexDeletionPolicy(new SnapshotDeletionPolicy(
				new KeepOnlyLastCommitDeletionPolicy())));
			SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy)writer.GetConfig().GetIndexDeletionPolicy
				();
			// First commit
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			doc.Add(NewField("c", "val", customType));
			writer.AddDocument(doc);
			writer.Commit();
			NUnit.Framework.Assert.AreEqual(1, DirectoryReader.ListCommits(dir).Count);
			// Keep that commit
			IndexCommit id = sdp.Snapshot();
			// Second commit - now KeepOnlyLastCommit cannot delete the prev commit.
			doc = new Lucene.Net.Document.Document();
			doc.Add(NewField("c", "val", customType));
			writer.AddDocument(doc);
			writer.Commit();
			NUnit.Framework.Assert.AreEqual(2, DirectoryReader.ListCommits(dir).Count);
			// Should delete the unreferenced commit
			sdp.Release(id);
			writer.DeleteUnusedFiles();
			NUnit.Framework.Assert.AreEqual(1, DirectoryReader.ListCommits(dir).Count);
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmptyFSDirWithNoLock()
		{
			// Tests that if FSDir is opened w/ a NoLockFactory (or SingleInstanceLF),
			// then IndexWriter ctor succeeds. Previously (LUCENE-2386) it failed
			// when listAll() was called in IndexFileDeleter.
			Directory dir = NewFSDirectory(CreateTempDir("emptyFSDirNoLock"), NoLockFactory.GetNoLockFactory
				());
			new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(
				Random()))).Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmptyDirRollback()
		{
			// TODO: generalize this test
			AssumeFalse("test makes assumptions about file counts", Codec.GetDefault() is SimpleTextCodec
				);
			// Tests that if IW is created over an empty Directory, some documents are
			// indexed, flushed (but not committed) and then IW rolls back, then no
			// files are left in the Directory.
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(2)).SetMergePolicy(NewLogMergePolicy()).SetUseCompoundFile(false)));
			string[] files = dir.ListAll();
			// Creating over empty dir should not create any files,
			// or, at most the write.lock file
			int extraFileCount;
			if (files.Length == 1)
			{
				NUnit.Framework.Assert.IsTrue(files[0].EndsWith("write.lock"));
				extraFileCount = 1;
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(0, files.Length);
				extraFileCount = 0;
			}
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			// create as many files as possible
			doc.Add(NewField("c", "val", customType));
			writer.AddDocument(doc);
			// Adding just one document does not call flush yet.
			int computedExtraFileCount = 0;
			foreach (string file in dir.ListAll())
			{
				if (file.LastIndexOf('.') < 0 || !Arrays.AsList("fdx", "fdt", "tvx", "tvd", "tvf"
					).Contains(Sharpen.Runtime.Substring(file, file.LastIndexOf('.') + 1)))
				{
					// don't count stored fields and term vectors in
					++computedExtraFileCount;
				}
			}
			NUnit.Framework.Assert.AreEqual("only the stored and term vector files should exist in the directory"
				, extraFileCount, computedExtraFileCount);
			doc = new Lucene.Net.Document.Document();
			doc.Add(NewField("c", "val", customType));
			writer.AddDocument(doc);
			// The second document should cause a flush.
			NUnit.Framework.Assert.IsTrue("flush should have occurred and files should have been created"
				, dir.ListAll().Length > 5 + extraFileCount);
			// After rollback, IW should remove all files
			writer.Rollback();
			string[] allFiles = dir.ListAll();
			NUnit.Framework.Assert.IsTrue("no files should exist in the directory after rollback"
				, allFiles.Length == 0 || Arrays.Equals(allFiles, new string[] { IndexWriter.WRITE_LOCK_NAME
				 }));
			// Since we rolled-back above, that close should be a no-op
			writer.Close();
			allFiles = dir.ListAll();
			NUnit.Framework.Assert.IsTrue("expected a no-op close after IW.rollback()", allFiles
				.Length == 0 || Arrays.Equals(allFiles, new string[] { IndexWriter.WRITE_LOCK_NAME
				 }));
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNoSegmentFile()
		{
			BaseDirectoryWrapper dir = NewDirectory();
			dir.SetLockFactory(NoLockFactory.GetNoLockFactory());
			IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			doc.Add(NewField("c", "val", customType));
			w.AddDocument(doc);
			w.AddDocument(doc);
			IndexWriter w2 = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)).SetOpenMode(IndexWriterConfig.OpenMode
				.CREATE));
			w2.Close();
			// If we don't do that, the test fails on Windows
			w.Rollback();
			// This test leaves only segments.gen, which causes
			// DirectoryReader.indexExists to return true:
			dir.SetCheckIndexOnClose(false);
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoUnwantedTVFiles()
		{
			Directory dir = NewDirectory();
			IndexWriter indexWriter = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetRAMBufferSizeMB(0.01)).SetMergePolicy
				(NewLogMergePolicy()));
			indexWriter.GetConfig().GetMergePolicy().SetNoCFSRatio(0.0);
			string BIG = "alskjhlaksjghlaksjfhalksvjepgjioefgjnsdfjgefgjhelkgjhqewlrkhgwlekgrhwelkgjhwelkgrhwlkejg";
			BIG = BIG + BIG + BIG + BIG;
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.SetOmitNorms(true);
			FieldType customType2 = new FieldType(TextField.TYPE_STORED);
			customType2.SetTokenized(false);
			FieldType customType3 = new FieldType(TextField.TYPE_STORED);
			customType3.SetTokenized(false);
			customType3.SetOmitNorms(true);
			for (int i = 0; i < 2; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(new Field("id", Sharpen.Extensions.ToString(i) + BIG, customType3));
				doc.Add(new Field("str", Sharpen.Extensions.ToString(i) + BIG, customType2));
				doc.Add(new Field("str2", Sharpen.Extensions.ToString(i) + BIG, storedTextType));
				doc.Add(new Field("str3", Sharpen.Extensions.ToString(i) + BIG, customType));
				indexWriter.AddDocument(doc);
			}
			indexWriter.Close();
			TestUtil.CheckIndex(dir);
			AssertNoUnreferencedFiles(dir, "no tv files");
			DirectoryReader r0 = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext ctx in r0.Leaves())
			{
				SegmentReader sr = (SegmentReader)((AtomicReader)ctx.Reader());
				NUnit.Framework.Assert.IsFalse(sr.GetFieldInfos().HasVectors());
			}
			r0.Close();
			dir.Close();
		}

		internal sealed class StringSplitAnalyzer : Analyzer
		{
			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				return new Analyzer.TokenStreamComponents(new TestIndexWriter.StringSplitTokenizer
					(reader));
			}
		}

		private class StringSplitTokenizer : Tokenizer
		{
			private string[] tokens;

			private int upto;

			private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

			protected StringSplitTokenizer(StreamReader r) : base(r)
			{
				try
				{
					SetReader(r);
				}
				catch (IOException e)
				{
					throw new RuntimeException(e);
				}
			}

			public sealed override bool IncrementToken()
			{
				ClearAttributes();
				if (upto < tokens.Length)
				{
					termAtt.SetEmpty();
					termAtt.Append(tokens[upto]);
					upto++;
					return true;
				}
				else
				{
					return false;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Reset()
			{
				base.Reset();
				this.upto = 0;
				StringBuilder b = new StringBuilder();
				char[] buffer = new char[1024];
				int n;
				while ((n = input.Read(buffer)) != -1)
				{
					b.Append(buffer, 0, n);
				}
				this.tokens = b.ToString().Split(" ");
			}
		}

		/// <summary>Make sure we skip wicked long terms.</summary>
		/// <remarks>Make sure we skip wicked long terms.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestWickedLongTerm()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, new TestIndexWriter.StringSplitAnalyzer
				());
			char[] chars = new char[DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8];
			Arrays.Fill(chars, 'x');
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			string bigTerm = new string(chars);
			BytesRef bigTermBytesRef = new BytesRef(bigTerm);
			// This contents produces a too-long term:
			string contents = "abc xyz x" + bigTerm + " another term";
			doc.Add(new TextField("content", contents, Field.Store.NO));
			try
			{
				w.AddDocument(doc);
				NUnit.Framework.Assert.Fail("should have hit exception");
			}
			catch (ArgumentException)
			{
			}
			// expected
			// Make sure we can add another normal document
			doc = new Lucene.Net.Document.Document();
			doc.Add(new TextField("content", "abc bbb ccc", Field.Store.NO));
			w.AddDocument(doc);
			// So we remove the deleted doc:
			w.ForceMerge(1);
			IndexReader reader = w.GetReader();
			w.Close();
			// Make sure all terms < max size were indexed
			NUnit.Framework.Assert.AreEqual(1, reader.DocFreq(new Term("content", "abc")));
			NUnit.Framework.Assert.AreEqual(1, reader.DocFreq(new Term("content", "bbb")));
			NUnit.Framework.Assert.AreEqual(0, reader.DocFreq(new Term("content", "term")));
			// Make sure the doc that has the massive term is NOT in
			// the index:
			NUnit.Framework.Assert.AreEqual("document with wicked long term is in the index!"
				, 1, reader.NumDocs());
			reader.Close();
			dir.Close();
			dir = NewDirectory();
			// Make sure we can add a document with exactly the
			// maximum length term, and search on that term:
			doc = new Lucene.Net.Document.Document();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetTokenized(false);
			Field contentField = new Field("content", string.Empty, customType);
			doc.Add(contentField);
			w = new RandomIndexWriter(Random(), dir);
			contentField.SetStringValue("other");
			w.AddDocument(doc);
			contentField.SetStringValue("term");
			w.AddDocument(doc);
			contentField.SetStringValue(bigTerm);
			w.AddDocument(doc);
			contentField.SetStringValue("zzz");
			w.AddDocument(doc);
			reader = w.GetReader();
			w.Close();
			NUnit.Framework.Assert.AreEqual(1, reader.DocFreq(new Term("content", bigTerm)));
			SortedDocValues dti = FieldCache.DEFAULT.GetTermsIndex(SlowCompositeReaderWrapper
				.Wrap(reader), "content", Random().NextFloat() * PackedInts.FAST);
			NUnit.Framework.Assert.AreEqual(4, dti.GetValueCount());
			BytesRef br = new BytesRef();
			dti.LookupOrd(2, br);
			NUnit.Framework.Assert.AreEqual(bigTermBytesRef, br);
			reader.Close();
			dir.Close();
		}

		// LUCENE-3183
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEmptyFieldNameTIIOne()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetTermIndexInterval(1);
			iwc.SetReaderTermsIndexDivisor(1);
			IndexWriter writer = new IndexWriter(dir, iwc);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewTextField(string.Empty, "a b c", Field.Store.NO));
			writer.AddDocument(doc);
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDeleteAllNRTLeftoverFiles()
		{
			Directory d = new MockDirectoryWrapper(Random(), new RAMDirectory());
			IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			for (int i = 0; i < 20; i++)
			{
				for (int j = 0; j < 100; ++j)
				{
					w.AddDocument(doc);
				}
				w.Commit();
				DirectoryReader.Open(w, true).Close();
				w.DeleteAll();
				w.Commit();
				// Make sure we accumulate no files except for empty
				// segments_N and segments.gen:
				NUnit.Framework.Assert.IsTrue(d.ListAll().Length <= 2);
			}
			w.Close();
			d.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNRTReaderVersion()
		{
			Directory d = new MockDirectoryWrapper(Random(), new RAMDirectory());
			IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewStringField("id", "0", Field.Store.YES));
			w.AddDocument(doc);
			DirectoryReader r = w.GetReader();
			long version = r.GetVersion();
			r.Close();
			w.AddDocument(doc);
			r = w.GetReader();
			long version2 = r.GetVersion();
			r.Close();
			//HM:revisit 
			//assert(version2 > version);
			w.DeleteDocuments(new Term("id", "0"));
			r = w.GetReader();
			w.Close();
			long version3 = r.GetVersion();
			r.Close();
			//HM:revisit 
			//assert(version3 > version2);
			d.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestWhetherDeleteAllDeletesWriteLock()
		{
			Directory d = NewFSDirectory(CreateTempDir("TestIndexWriter.testWhetherDeleteAllDeletesWriteLock"
				));
			// Must use SimpleFSLockFactory... NativeFSLockFactory
			// somehow "knows" a lock is held against write.lock
			// even if you remove that file:
			d.SetLockFactory(new SimpleFSLockFactory());
			RandomIndexWriter w1 = new RandomIndexWriter(Random(), d);
			w1.DeleteAll();
			try
			{
				new RandomIndexWriter(Random(), d, NewIndexWriterConfig(TEST_VERSION_CURRENT, null
					).SetWriteLockTimeout(100));
				NUnit.Framework.Assert.Fail("should not be able to create another writer");
			}
			catch (LockObtainFailedException)
			{
			}
			// expected
			w1.Close();
			d.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestChangeIndexOptions()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			FieldType docsAndFreqs = new FieldType(TextField.TYPE_NOT_STORED);
			docsAndFreqs.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS);
			FieldType docsOnly = new FieldType(TextField.TYPE_NOT_STORED);
			docsOnly.SetIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(new Field("field", "a b c", docsAndFreqs));
			w.AddDocument(doc);
			w.AddDocument(doc);
			doc = new Lucene.Net.Document.Document();
			doc.Add(new Field("field", "a b c", docsOnly));
			w.AddDocument(doc);
			w.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOnlyUpdateDocuments()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			IList<Lucene.Net.Document.Document> docs = new AList<Lucene.Net.Document.Document
				>();
			docs.AddItem(new Lucene.Net.Document.Document());
			w.UpdateDocuments(new Term("foo", "bar"), docs.AsIterable());
			w.Close();
			dir.Close();
		}

		// LUCENE-3872
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPrepareCommitThenClose()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			w.PrepareCommit();
			try
			{
				w.Close();
				NUnit.Framework.Assert.Fail("should have hit exception");
			}
			catch (InvalidOperationException)
			{
			}
			// expected
			w.Commit();
			w.Close();
			IndexReader r = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual(0, r.MaxDoc());
			r.Close();
			dir.Close();
		}

		// LUCENE-3872
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPrepareCommitThenRollback()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			w.PrepareCommit();
			w.Rollback();
			NUnit.Framework.Assert.IsFalse(DirectoryReader.IndexExists(dir));
			dir.Close();
		}

		// LUCENE-3872
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPrepareCommitThenRollback2()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			w.Commit();
			w.AddDocument(new Lucene.Net.Document.Document());
			w.PrepareCommit();
			w.Rollback();
			NUnit.Framework.Assert.IsTrue(DirectoryReader.IndexExists(dir));
			IndexReader r = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual(0, r.MaxDoc());
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDontInvokeAnalyzerForUnAnalyzedFields()
		{
			Analyzer analyzer = new _Analyzer_1949();
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer
				));
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
			customType.SetStoreTermVectors(true);
			customType.SetStoreTermVectorPositions(true);
			customType.SetStoreTermVectorOffsets(true);
			Field f = NewField("field", "abcd", customType);
			doc.Add(f);
			doc.Add(f);
			Field f2 = NewField("field", string.Empty, customType);
			doc.Add(f2);
			doc.Add(f);
			w.AddDocument(doc);
			w.Close();
			dir.Close();
		}

		private sealed class _Analyzer_1949 : Analyzer
		{
			public _Analyzer_1949()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				throw new InvalidOperationException("don't invoke me!");
			}

			public override int GetPositionIncrementGap(string fieldName)
			{
				throw new InvalidOperationException("don't invoke me!");
			}

			public override int GetOffsetGap(string fieldName)
			{
				throw new InvalidOperationException("don't invoke me!");
			}
		}

		//LUCENE-1468 -- make sure opening an IndexWriter with
		// create=true does not remove non-index files
		/// <exception cref="System.Exception"></exception>
		public virtual void TestOtherFiles()
		{
			Directory dir = NewDirectory();
			IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			iw.AddDocument(new Lucene.Net.Document.Document());
			iw.Close();
			try
			{
				// Create my own random file:
				IndexOutput @out = dir.CreateOutput("myrandomfile", NewIOContext(Random()));
				@out.WriteByte(unchecked((byte)42));
				@out.Close();
				new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(
					Random()))).Close();
				NUnit.Framework.Assert.IsTrue(SlowFileExists(dir, "myrandomfile"));
			}
			finally
			{
				dir.Close();
			}
		}

		// LUCENE-3849
		/// <exception cref="System.Exception"></exception>
		public virtual void TestStopwordsPosIncHole()
		{
			Directory dir = NewDirectory();
			Analyzer a = new _Analyzer_2010();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, a);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(new TextField("body", "just a", Field.Store.NO));
			doc.Add(new TextField("body", "test of gaps", Field.Store.NO));
			iw.AddDocument(doc);
			IndexReader ir = iw.GetReader();
			iw.Close();
			IndexSearcher @is = NewSearcher(ir);
			PhraseQuery pq = new PhraseQuery();
			pq.Add(new Term("body", "just"), 0);
			pq.Add(new Term("body", "test"), 2);
			// body:"just ? test"
			NUnit.Framework.Assert.AreEqual(1, @is.Search(pq, 5).totalHits);
			ir.Close();
			dir.Close();
		}

		private sealed class _Analyzer_2010 : Analyzer
		{
			public _Analyzer_2010()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader);
				TokenStream stream = new MockTokenFilter(tokenizer, MockTokenFilter.ENGLISH_STOPSET
					);
				return new Analyzer.TokenStreamComponents(tokenizer, stream);
			}
		}

		// LUCENE-3849
		/// <exception cref="System.Exception"></exception>
		public virtual void TestStopwordsPosIncHole2()
		{
			// use two stopfilters for testing here
			Directory dir = NewDirectory();
			Lucene.Net.Util.Automaton.Automaton secondSet = BasicAutomata.MakeString("foobar"
				);
			Analyzer a = new _Analyzer_2040(secondSet);
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, a);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(new TextField("body", "just a foobar", Field.Store.NO));
			doc.Add(new TextField("body", "test of gaps", Field.Store.NO));
			iw.AddDocument(doc);
			IndexReader ir = iw.GetReader();
			iw.Close();
			IndexSearcher @is = NewSearcher(ir);
			PhraseQuery pq = new PhraseQuery();
			pq.Add(new Term("body", "just"), 0);
			pq.Add(new Term("body", "test"), 3);
			// body:"just ? ? test"
			NUnit.Framework.Assert.AreEqual(1, @is.Search(pq, 5).totalHits);
			ir.Close();
			dir.Close();
		}

		private sealed class _Analyzer_2040 : Analyzer
		{
			public _Analyzer_2040(Lucene.Net.Util.Automaton.Automaton secondSet)
			{
				this.secondSet = secondSet;
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader);
				TokenStream stream = new MockTokenFilter(tokenizer, MockTokenFilter.ENGLISH_STOPSET
					);
				stream = new MockTokenFilter(stream, new CharacterRunAutomaton(secondSet));
				return new Analyzer.TokenStreamComponents(tokenizer, stream);
			}

			private readonly Lucene.Net.Util.Automaton.Automaton secondSet;
		}

		// here we do better, there is no current segments file, so we don't delete anything.
		// however, if you actually go and make a commit, the next time you run indexwriter
		// this file will be gone.
		/// <exception cref="System.Exception"></exception>
		public virtual void TestOtherFiles2()
		{
			Directory dir = NewDirectory();
			try
			{
				// Create my own random file:
				IndexOutput @out = dir.CreateOutput("_a.frq", NewIOContext(Random()));
				@out.WriteByte(unchecked((byte)42));
				@out.Close();
				new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(
					Random()))).Close();
				NUnit.Framework.Assert.IsTrue(SlowFileExists(dir, "_a.frq"));
				IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
					new MockAnalyzer(Random())));
				iw.AddDocument(new Lucene.Net.Document.Document());
				iw.Close();
				NUnit.Framework.Assert.IsFalse(SlowFileExists(dir, "_a.frq"));
			}
			finally
			{
				dir.Close();
			}
		}

		// LUCENE-4398
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRotatingFieldNames()
		{
			Directory dir = NewFSDirectory(CreateTempDir("TestIndexWriter.testChangingFields"
				));
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetRAMBufferSizeMB(0.2);
			iwc.SetMaxBufferedDocs(-1);
			IndexWriter w = new IndexWriter(dir, iwc);
			int upto = 0;
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetOmitNorms(true);
			int firstDocCount = -1;
			for (int iter = 0; iter < 10; iter++)
			{
				int startFlushCount = w.GetFlushCount();
				int docCount = 0;
				while (w.GetFlushCount() == startFlushCount)
				{
					Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
						();
					for (int i = 0; i < 10; i++)
					{
						doc.Add(new Field("field" + (upto++), "content", ft));
					}
					w.AddDocument(doc);
					docCount++;
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: iter=" + iter + " flushed after docCount=" + 
						docCount);
				}
				if (iter == 0)
				{
					firstDocCount = docCount;
				}
				NUnit.Framework.Assert.IsTrue("flushed after too few docs: first segment flushed at docCount="
					 + firstDocCount + ", but current segment flushed after docCount=" + docCount + 
					"; iter=" + iter, ((float)docCount) / firstDocCount > 0.9);
				if (upto > 5000)
				{
					// Start re-using field names after a while
					// ... important because otherwise we can OOME due
					// to too many FieldInfo instances.
					upto = 0;
				}
			}
			w.Close();
			dir.Close();
		}

		// LUCENE-4575
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCommitWithUserDataOnly()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, null));
			writer.Commit();
			// first commit to complete IW create transaction.
			// this should store the commit data, even though no other changes were made
			writer.SetCommitData(new _Dictionary_2145());
			writer.Commit();
			DirectoryReader r = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual("value", r.GetIndexCommit().GetUserData().Get("key"
				));
			r.Close();
			// now check setCommitData and prepareCommit/commit sequence
			writer.SetCommitData(new _Dictionary_2155());
			writer.PrepareCommit();
			writer.SetCommitData(new _Dictionary_2159());
			writer.Commit();
			// should commit the first commitData only, per protocol
			r = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual("value1", r.GetIndexCommit().GetUserData().Get("key"
				));
			r.Close();
			// now should commit the second commitData - there was a bug where 
			// IndexWriter.finishCommit overrode the second commitData
			writer.Commit();
			r = DirectoryReader.Open(dir);
			NUnit.Framework.Assert.AreEqual("IndexWriter.finishCommit may have overridden the second commitData"
				, "value2", r.GetIndexCommit().GetUserData().Get("key"));
			r.Close();
			writer.Close();
			dir.Close();
		}

		private sealed class _Dictionary_2145 : Dictionary<string, string>
		{
			public _Dictionary_2145()
			{
				{
					this.Put("key", "value");
				}
			}
		}

		private sealed class _Dictionary_2155 : Dictionary<string, string>
		{
			public _Dictionary_2155()
			{
				{
					this.Put("key", "value1");
				}
			}
		}

		private sealed class _Dictionary_2159 : Dictionary<string, string>
		{
			public _Dictionary_2159()
			{
				{
					this.Put("key", "value2");
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestGetCommitData()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, null));
			writer.SetCommitData(new _Dictionary_2184());
			NUnit.Framework.Assert.AreEqual("value", writer.GetCommitData().Get("key"));
			writer.Close();
			// validate that it's also visible when opening a new IndexWriter
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null).SetOpenMode
				(IndexWriterConfig.OpenMode.APPEND));
			NUnit.Framework.Assert.AreEqual("value", writer.GetCommitData().Get("key"));
			writer.Close();
			dir.Close();
		}

		private sealed class _Dictionary_2184 : Dictionary<string, string>
		{
			public _Dictionary_2184()
			{
				{
					this.Put("key", "value");
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNullAnalyzer()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConf);
			// add 3 good docs
			for (int i = 0; i < 3; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(new StringField("id", Sharpen.Extensions.ToString(i), Field.Store.NO));
				iw.AddDocument(doc);
			}
			// add broken doc
			try
			{
				Lucene.Net.Document.Document broke = new Lucene.Net.Document.Document
					();
				broke.Add(NewTextField("test", "broken", Field.Store.NO));
				iw.AddDocument(broke);
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentNullException)
			{
			}
			// ensure good docs are still ok
			IndexReader ir = iw.GetReader();
			NUnit.Framework.Assert.AreEqual(3, ir.NumDocs());
			ir.Close();
			iw.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNullDocument()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			// add 3 good docs
			for (int i = 0; i < 3; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(new StringField("id", Sharpen.Extensions.ToString(i), Field.Store.NO));
				iw.AddDocument(doc);
			}
			// add broken doc
			try
			{
				iw.AddDocument(null);
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentNullException)
			{
			}
			// ensure good docs are still ok
			IndexReader ir = iw.GetReader();
			NUnit.Framework.Assert.AreEqual(3, ir.NumDocs());
			ir.Close();
			iw.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNullDocuments()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			// add 3 good docs
			for (int i = 0; i < 3; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				doc.Add(new StringField("id", Sharpen.Extensions.ToString(i), Field.Store.NO));
				iw.AddDocument(doc);
			}
			// add broken doc block
			try
			{
				iw.AddDocuments(null);
				NUnit.Framework.Assert.Fail();
			}
			catch (ArgumentNullException)
			{
			}
			// ensure good docs are still ok
			IndexReader ir = iw.GetReader();
			NUnit.Framework.Assert.AreEqual(3, ir.NumDocs());
			ir.Close();
			iw.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIterableFieldThrowsException()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			int iters = AtLeast(100);
			int docCount = 0;
			int docId = 0;
			ICollection<string> liveIds = new HashSet<string>();
			for (int i = 0; i < iters; i++)
			{
				int numDocs = AtLeast(4);
				for (int j = 0; j < numDocs; j++)
				{
					string id = Sharpen.Extensions.ToString(docId++);
					IList<IndexableField> fields = new AList<IndexableField>();
					fields.AddItem(new StringField("id", id, Field.Store.YES));
					fields.AddItem(new StringField("foo", TestUtil.RandomSimpleString(Random()), Field.Store
						.NO));
					docId++;
					bool success = false;
					try
					{
						w.AddDocument(new TestIndexWriter.RandomFailingIterable<IndexableField>(fields, Random
							()));
						success = true;
					}
					catch (RuntimeException e)
					{
						NUnit.Framework.Assert.AreEqual("boom", e.Message);
					}
					finally
					{
						if (success)
						{
							docCount++;
							liveIds.AddItem(id);
						}
					}
				}
			}
			DirectoryReader reader = w.GetReader();
			NUnit.Framework.Assert.AreEqual(docCount, reader.NumDocs());
			IList<AtomicReaderContext> leaves = reader.Leaves();
			foreach (AtomicReaderContext atomicReaderContext in leaves)
			{
				AtomicReader ar = ((AtomicReader)atomicReaderContext.Reader());
				Bits liveDocs = ar.GetLiveDocs();
				int maxDoc = ar.MaxDoc();
				for (int i_1 = 0; i_1 < maxDoc; i_1++)
				{
					if (liveDocs == null || liveDocs.Get(i_1))
					{
						NUnit.Framework.Assert.IsTrue(liveIds.Remove(ar.Document(i_1).Get("id")));
					}
				}
			}
			NUnit.Framework.Assert.IsTrue(liveIds.IsEmpty());
			w.Close();
			IOUtils.Close(reader, dir);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIterableThrowsException()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			int iters = AtLeast(100);
			int docCount = 0;
			int docId = 0;
			ICollection<string> liveIds = new HashSet<string>();
			for (int i = 0; i < iters; i++)
			{
				IList<Iterable<IndexableField>> docs = new AList<Iterable<IndexableField>>();
				FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
				FieldType idFt = new FieldType(TextField.TYPE_STORED);
				int numDocs = AtLeast(4);
				for (int j = 0; j < numDocs; j++)
				{
					Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
						();
					doc.Add(NewField("id", string.Empty + (docId++), idFt));
					doc.Add(NewField("foo", TestUtil.RandomSimpleString(Random()), ft));
					docs.AddItem(doc);
				}
				bool success = false;
				try
				{
					w.AddDocuments(new TestIndexWriter.RandomFailingIterable<Iterable<IndexableField>
						>(docs, Random()));
					success = true;
				}
				catch (RuntimeException e)
				{
					NUnit.Framework.Assert.AreEqual("boom", e.Message);
				}
				finally
				{
					if (success)
					{
						docCount += docs.Count;
						foreach (Iterable<IndexableField> indexDocument in docs)
						{
							liveIds.AddItem(((Lucene.Net.Document.Document)indexDocument).Get("id"));
						}
					}
				}
			}
			DirectoryReader reader = w.GetReader();
			NUnit.Framework.Assert.AreEqual(docCount, reader.NumDocs());
			IList<AtomicReaderContext> leaves = reader.Leaves();
			foreach (AtomicReaderContext atomicReaderContext in leaves)
			{
				AtomicReader ar = ((AtomicReader)atomicReaderContext.Reader());
				Bits liveDocs = ar.GetLiveDocs();
				int maxDoc = ar.MaxDoc();
				for (int i_1 = 0; i_1 < maxDoc; i_1++)
				{
					if (liveDocs == null || liveDocs.Get(i_1))
					{
						NUnit.Framework.Assert.IsTrue(liveIds.Remove(ar.Document(i_1).Get("id")));
					}
				}
			}
			NUnit.Framework.Assert.IsTrue(liveIds.IsEmpty());
			IOUtils.Close(reader, w, dir);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIterableThrowsException2()
		{
			Directory dir = NewDirectory();
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new 
				MockAnalyzer(Random())));
			try
			{
				w.AddDocuments(new _Iterable_2373());
			}
			catch (Exception e)
			{
				//HM:revisit 
				NUnit.Framework.Assert.IsNotNull(e.Message);
				NUnit.Framework.Assert.AreEqual("boom", e.Message);
			}
			w.Close();
			IOUtils.Close(dir);
		}

		private sealed class _Iterable_2373 : Iterable<Lucene.Net.Document.Document
			>
		{
			public _Iterable_2373()
			{
			}

			public override Iterator<Lucene.Net.Document.Document> Iterator()
			{
				return new _Iterator_2376();
			}

			private sealed class _Iterator_2376 : Iterator<Lucene.Net.Document.Document
				>
			{
				public _Iterator_2376()
				{
				}

				public override bool HasNext()
				{
					return true;
				}

				public override Lucene.Net.Document.Document Next()
				{
					throw new RuntimeException("boom");
				}

				public override void Remove()
				{
				}
			}
		}

		private class RandomFailingIterable<T> : Iterable<T>
		{
			private readonly Iterable<T> list;

			private readonly int failOn;

			public RandomFailingIterable(Iterable<T> list, Random random)
			{
				this.list = list;
				this.failOn = random.Next(5);
			}

			public override Sharpen.Iterator<T> Iterator()
			{
				Sharpen.Iterator<T> docIter = list.Iterator();
				return new _Iterator_2415(this, docIter);
			}

			private sealed class _Iterator_2415 : Sharpen.Iterator<T>
			{
				public _Iterator_2415(RandomFailingIterable<T> _enclosing, Sharpen.Iterator<T> docIter
					)
				{
					this._enclosing = _enclosing;
					this.docIter = docIter;
					this.count = 0;
				}

				internal int count;

				public override bool HasNext()
				{
					return docIter.HasNext();
				}

				public override T Next()
				{
					if (this.count == this._enclosing.failOn)
					{
						throw new RuntimeException("boom");
					}
					this.count++;
					return docIter.Next();
				}

				public override void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly RandomFailingIterable<T> _enclosing;

				private readonly Sharpen.Iterator<T> docIter;
			}
		}

		// LUCENE-2727/LUCENE-2812/LUCENE-4738:
		/// <exception cref="System.Exception"></exception>
		public virtual void TestCorruptFirstCommit()
		{
			for (int i = 0; i < 6; i++)
			{
				BaseDirectoryWrapper dir = NewDirectory();
				dir.CreateOutput("segments_0", IOContext.DEFAULT).Close();
				IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random()));
				int mode = i / 2;
				if (mode == 0)
				{
					iwc.SetOpenMode(IndexWriterConfig.OpenMode.CREATE);
				}
				else
				{
					if (mode == 1)
					{
						iwc.SetOpenMode(IndexWriterConfig.OpenMode.APPEND);
					}
					else
					{
						if (mode == 2)
						{
							iwc.SetOpenMode(IndexWriterConfig.OpenMode.CREATE_OR_APPEND);
						}
					}
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: i=" + i);
				}
				try
				{
					if ((i & 1) == 0)
					{
						new IndexWriter(dir, iwc).Close();
					}
					else
					{
						new IndexWriter(dir, iwc).Rollback();
					}
					if (mode != 0)
					{
						NUnit.Framework.Assert.Fail("expected exception");
					}
				}
				catch (IOException ioe)
				{
					// OpenMode.APPEND should throw an exception since no
					// index exists:
					if (mode == 0)
					{
						// Unexpected
						throw;
					}
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  at close: " + Arrays.ToString(dir.ListAll()));
				}
				if (mode != 0)
				{
					dir.SetCheckIndexOnClose(false);
				}
				dir.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestHasUncommittedChanges()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			NUnit.Framework.Assert.IsTrue(writer.HasUncommittedChanges());
			// this will be true because a commit will create an empty index
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewTextField("myfield", "a b c", Field.Store.NO));
			writer.AddDocument(doc);
			NUnit.Framework.Assert.IsTrue(writer.HasUncommittedChanges());
			// Must commit, waitForMerges, commit again, to be
			// certain that hasUncommittedChanges returns false:
			writer.Commit();
			writer.WaitForMerges();
			writer.Commit();
			NUnit.Framework.Assert.IsFalse(writer.HasUncommittedChanges());
			writer.AddDocument(doc);
			NUnit.Framework.Assert.IsTrue(writer.HasUncommittedChanges());
			writer.Commit();
			doc = new Lucene.Net.Document.Document();
			doc.Add(NewStringField("id", "xyz", Field.Store.YES));
			writer.AddDocument(doc);
			NUnit.Framework.Assert.IsTrue(writer.HasUncommittedChanges());
			// Must commit, waitForMerges, commit again, to be
			// certain that hasUncommittedChanges returns false:
			writer.Commit();
			writer.WaitForMerges();
			writer.Commit();
			NUnit.Framework.Assert.IsFalse(writer.HasUncommittedChanges());
			writer.DeleteDocuments(new Term("id", "xyz"));
			NUnit.Framework.Assert.IsTrue(writer.HasUncommittedChanges());
			// Must commit, waitForMerges, commit again, to be
			// certain that hasUncommittedChanges returns false:
			writer.Commit();
			writer.WaitForMerges();
			writer.Commit();
			NUnit.Framework.Assert.IsFalse(writer.HasUncommittedChanges());
			writer.Close();
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())));
			NUnit.Framework.Assert.IsFalse(writer.HasUncommittedChanges());
			writer.AddDocument(doc);
			NUnit.Framework.Assert.IsTrue(writer.HasUncommittedChanges());
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMergeAllDeleted()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			SetOnce<IndexWriter> iwRef = new SetOnce<IndexWriter>();
			iwc.SetInfoStream(new RandomIndexWriter.TestPointInfoStream(iwc.GetInfoStream(), 
				new _TestPoint_2539(iwRef)));
			IndexWriter evilWriter = new IndexWriter(dir, iwc);
			iwRef.Set(evilWriter);
			for (int i = 0; i < 1000; i++)
			{
				AddDoc(evilWriter);
				if (Random().Next(17) == 0)
				{
					evilWriter.Commit();
				}
			}
			evilWriter.DeleteDocuments(new MatchAllDocsQuery());
			evilWriter.ForceMerge(1);
			evilWriter.Close();
			dir.Close();
		}

		private sealed class _TestPoint_2539 : RandomIndexWriter.TestPoint
		{
			public _TestPoint_2539(SetOnce<IndexWriter> iwRef)
			{
				this.iwRef = iwRef;
			}

			public void Apply(string message)
			{
				if ("startCommitMerge".Equals(message))
				{
					iwRef.Get().SetKeepFullyDeletedSegments(false);
				}
				else
				{
					if ("startMergeInit".Equals(message))
					{
						iwRef.Get().SetKeepFullyDeletedSegments(true);
					}
				}
			}

			private readonly SetOnce<IndexWriter> iwRef;
		}

		// LUCENE-5239
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDeleteSameTermAcrossFields()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriter w = new IndexWriter(dir, iwc);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(new TextField("a", "foo", Field.Store.NO));
			w.AddDocument(doc);
			// Should not delete the document; with LUCENE-5239 the
			// "foo" from the 2nd delete term would incorrectly
			// match field a's "foo":
			w.DeleteDocuments(new Term("a", "xxx"));
			w.DeleteDocuments(new Term("b", "foo"));
			IndexReader r = w.GetReader();
			w.Close();
			// Make sure document was not (incorrectly) deleted:
			NUnit.Framework.Assert.AreEqual(1, r.NumDocs());
			r.Close();
			dir.Close();
		}

		// LUCENE-5574
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestClosingNRTReaderDoesNotCorruptYourIndex()
		{
			// Windows disallows deleting & overwriting files still
			// open for reading:
			AssumeFalse("this test can't run on Windows", Constants.WINDOWS);
			MockDirectoryWrapper dir = NewMockDirectory();
			// Allow deletion of still open files:
			dir.SetNoDeleteOpenFile(false);
			// Allow writing to same file more than once:
			dir.SetPreventDoubleWrite(false);
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			LogMergePolicy lmp = new LogDocMergePolicy();
			lmp.SetMergeFactor(2);
			iwc.SetMergePolicy(lmp);
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, iwc);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(new TextField("a", "foo", Field.Store.NO));
			w.AddDocument(doc);
			w.Commit();
			w.AddDocument(doc);
			// Get a new reader, but this also sets off a merge:
			IndexReader r = w.GetReader();
			w.Close();
			// Blow away index and make a new writer:
			foreach (string fileName in dir.ListAll())
			{
				dir.DeleteFile(fileName);
			}
			w = new RandomIndexWriter(Random(), dir);
			w.AddDocument(doc);
			w.Close();
			r.Close();
			dir.Close();
		}
	}
}
