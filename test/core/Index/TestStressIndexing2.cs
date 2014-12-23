/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestStressIndexing2 : LuceneTestCase
	{
		internal static int maxFields = 4;

		internal static int bigFieldSize = 10;

		internal static bool sameFieldOrder = false;

		internal static int mergeFactor = 3;

		internal static int maxBufferedDocs = 3;

		internal static int seed = 0;

		public sealed class YieldTestPoint : RandomIndexWriter.TestPoint
		{
			public void Apply(string name)
			{
				//      if (name.equals("startCommit")) {
				if (LuceneTestCase.Random().Next(4) == 2)
				{
					Sharpen.Thread.Yield();
				}
			}

			internal YieldTestPoint(TestStressIndexing2 _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestStressIndexing2 _enclosing;
		}

		//  
		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandomIWReader()
		{
			Directory dir = NewDirectory();
			// TODO: verify equals using IW.getReader
			TestStressIndexing2.DocsAndWriter dw = IndexRandomIWReader(5, 3, 100, dir);
			DirectoryReader reader = dw.writer.GetReader();
			dw.writer.Commit();
			VerifyEquals(Random(), reader, dir, "id");
			reader.Close();
			dw.writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestRandom()
		{
			Directory dir1 = NewDirectory();
			Directory dir2 = NewDirectory();
			// mergeFactor=2; maxBufferedDocs=2; Map docs = indexRandom(1, 3, 2, dir1);
			int maxThreadStates = 1 + Random().Next(10);
			bool doReaderPooling = Random().NextBoolean();
			IDictionary<string, Lucene.Net.Document.Document> docs = IndexRandom(5, 3, 
				100, dir1, maxThreadStates, doReaderPooling);
			IndexSerial(Random(), docs, dir2);
			// verifying verify
			// verifyEquals(dir1, dir1, "id");
			// verifyEquals(dir2, dir2, "id");
			VerifyEquals(dir1, dir2, "id");
			dir1.Close();
			dir2.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMultiConfig()
		{
			// test lots of smaller different params together
			int num = AtLeast(3);
			for (int i = 0; i < num; i++)
			{
				// increase iterations for better testing
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\n\nTEST: top iter=" + i);
				}
				sameFieldOrder = Random().NextBoolean();
				mergeFactor = Random().Next(3) + 2;
				maxBufferedDocs = Random().Next(3) + 2;
				int maxThreadStates = 1 + Random().Next(10);
				bool doReaderPooling = Random().NextBoolean();
				seed++;
				int nThreads = Random().Next(5) + 1;
				int iter = Random().Next(5) + 1;
				int range = Random().Next(20) + 1;
				Directory dir1 = NewDirectory();
				Directory dir2 = NewDirectory();
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("  nThreads=" + nThreads + " iter=" + iter + " range="
						 + range + " doPooling=" + doReaderPooling + " maxThreadStates=" + maxThreadStates
						 + " sameFieldOrder=" + sameFieldOrder + " mergeFactor=" + mergeFactor + " maxBufferedDocs="
						 + maxBufferedDocs);
				}
				IDictionary<string, Lucene.Net.Document.Document> docs = IndexRandom(nThreads
					, iter, range, dir1, maxThreadStates, doReaderPooling);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: index serial");
				}
				IndexSerial(Random(), docs, dir2);
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: verify");
				}
				VerifyEquals(dir1, dir2, "id");
				dir1.Close();
				dir2.Close();
			}
		}

		internal static Term idTerm = new Term("id", string.Empty);

		internal TestStressIndexing2.IndexingThread[] threads;

		private sealed class _IComparer_131 : IComparer<IndexableField>
		{
			public _IComparer_131()
			{
			}

			public int Compare(IndexableField o1, IndexableField o2)
			{
				return Sharpen.Runtime.CompareOrdinal(o1.Name(), o2.Name());
			}
		}

		internal static IComparer<IndexableField> fieldNameComparator = new _IComparer_131
			();

		public class DocsAndWriter
		{
			internal IDictionary<string, Lucene.Net.Document.Document> docs;

			internal IndexWriter writer;
			// This test avoids using any extra synchronization in the multiple
			// indexing threads to test that IndexWriter does correctly synchronize
			// everything.
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		public virtual TestStressIndexing2.DocsAndWriter IndexRandomIWReader(int nThreads
			, int iterations, int range, Directory dir)
		{
			IDictionary<string, Lucene.Net.Document.Document> docs = new Dictionary<string
				, Lucene.Net.Document.Document>();
			IndexWriter w = RandomIndexWriter.MockIndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode
				(IndexWriterConfig.OpenMode.CREATE).SetRAMBufferSizeMB(0.1)).SetMaxBufferedDocs(
				maxBufferedDocs)).SetMergePolicy(NewLogMergePolicy()), new TestStressIndexing2.YieldTestPoint
				(this));
			w.Commit();
			LogMergePolicy lmp = (LogMergePolicy)w.GetConfig().GetMergePolicy();
			lmp.SetNoCFSRatio(0.0);
			lmp.SetMergeFactor(mergeFactor);
			threads = new TestStressIndexing2.IndexingThread[nThreads];
			for (int i = 0; i < threads.Length; i++)
			{
				TestStressIndexing2.IndexingThread th = new TestStressIndexing2.IndexingThread(this
					);
				th.w = w;
				th.@base = 1000000 * i;
				th.range = range;
				th.iterations = iterations;
				threads[i] = th;
			}
			for (int i_1 = 0; i_1 < threads.Length; i_1++)
			{
				threads[i_1].Start();
			}
			for (int i_2 = 0; i_2 < threads.Length; i_2++)
			{
				threads[i_2].Join();
			}
			// w.forceMerge(1);
			//w.close();    
			for (int i_3 = 0; i_3 < threads.Length; i_3++)
			{
				TestStressIndexing2.IndexingThread th = threads[i_3];
				lock (th)
				{
					docs.PutAll(th.docs);
				}
			}
			TestUtil.CheckIndex(dir);
			TestStressIndexing2.DocsAndWriter dw = new TestStressIndexing2.DocsAndWriter();
			dw.docs = docs;
			dw.writer = w;
			return dw;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.Exception"></exception>
		public virtual IDictionary<string, Lucene.Net.Document.Document> IndexRandom
			(int nThreads, int iterations, int range, Directory dir, int maxThreadStates, bool
			 doReaderPooling)
		{
			IDictionary<string, Lucene.Net.Document.Document> docs = new Dictionary<string
				, Lucene.Net.Document.Document>();
			IndexWriter w = RandomIndexWriter.MockIndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode
				(IndexWriterConfig.OpenMode.CREATE).SetRAMBufferSizeMB(0.1)).SetMaxBufferedDocs(
				maxBufferedDocs)).SetIndexerThreadPool(new DocumentsWriterPerThreadPool(maxThreadStates
				)).SetReaderPooling(doReaderPooling).SetMergePolicy(NewLogMergePolicy()), new TestStressIndexing2.YieldTestPoint
				(this));
			LogMergePolicy lmp = (LogMergePolicy)w.GetConfig().GetMergePolicy();
			lmp.SetNoCFSRatio(0.0);
			lmp.SetMergeFactor(mergeFactor);
			threads = new TestStressIndexing2.IndexingThread[nThreads];
			for (int i = 0; i < threads.Length; i++)
			{
				TestStressIndexing2.IndexingThread th = new TestStressIndexing2.IndexingThread(this
					);
				th.w = w;
				th.@base = 1000000 * i;
				th.range = range;
				th.iterations = iterations;
				threads[i] = th;
			}
			for (int i_1 = 0; i_1 < threads.Length; i_1++)
			{
				threads[i_1].Start();
			}
			for (int i_2 = 0; i_2 < threads.Length; i_2++)
			{
				threads[i_2].Join();
			}
			//w.forceMerge(1);
			w.Close();
			for (int i_3 = 0; i_3 < threads.Length; i_3++)
			{
				TestStressIndexing2.IndexingThread th = threads[i_3];
				lock (th)
				{
					docs.PutAll(th.docs);
				}
			}
			//System.out.println("TEST: checkindex");
			TestUtil.CheckIndex(dir);
			return docs;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void IndexSerial(Random random, IDictionary<string, Lucene.Net.Document.Document
			> docs, Directory dir)
		{
			IndexWriter w = new IndexWriter(dir, LuceneTestCase.NewIndexWriterConfig(random, 
				TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetMergePolicy(NewLogMergePolicy
				()));
			// index all docs in a single thread
			Iterator<Lucene.Net.Document.Document> iter = docs.Values.Iterator();
			while (iter.HasNext())
			{
				Lucene.Net.Document.Document d = iter.Next();
				AList<IndexableField> fields = new AList<IndexableField>();
				Sharpen.Collections.AddAll(fields, d.GetFields());
				// put fields in same order each time
				fields.Sort(fieldNameComparator);
				Lucene.Net.Document.Document d1 = new Lucene.Net.Document.Document(
					);
				for (int i = 0; i < fields.Count; i++)
				{
					d1.Add(fields[i]);
				}
				w.AddDocument(d1);
			}
			// System.out.println("indexing "+d1);
			w.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void VerifyEquals(Random r, DirectoryReader r1, Directory dir2, string
			 idField)
		{
			DirectoryReader r2 = DirectoryReader.Open(dir2);
			VerifyEquals(r1, r2, idField);
			r2.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void VerifyEquals(Directory dir1, Directory dir2, string idField)
		{
			DirectoryReader r1 = DirectoryReader.Open(dir1);
			DirectoryReader r2 = DirectoryReader.Open(dir2);
			VerifyEquals(r1, r2, idField);
			r1.Close();
			r2.Close();
		}

		/// <exception cref="System.Exception"></exception>
		private static void PrintDocs(DirectoryReader r)
		{
			foreach (AtomicReaderContext ctx in r.Leaves())
			{
				// TODO: improve this
				AtomicReader sub = ((AtomicReader)ctx.Reader());
				Bits liveDocs = sub.GetLiveDocs();
				System.Console.Out.WriteLine("  " + ((SegmentReader)sub).GetSegmentInfo());
				for (int docID = 0; docID < sub.MaxDoc(); docID++)
				{
					Lucene.Net.Document.Document doc = sub.Document(docID);
					if (liveDocs == null || liveDocs.Get(docID))
					{
						System.Console.Out.WriteLine("    docID=" + docID + " id:" + doc.Get("id"));
					}
					else
					{
						System.Console.Out.WriteLine("    DEL docID=" + docID + " id:" + doc.Get("id"));
					}
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void VerifyEquals(DirectoryReader r1, DirectoryReader r2, string idField
			)
		{
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nr1 docs:");
				PrintDocs(r1);
				System.Console.Out.WriteLine("\nr2 docs:");
				PrintDocs(r2);
			}
			if (r1.NumDocs() != r2.NumDocs())
			{
			}
			//HM:revisit 
			//assert false: "r1.numDocs()=" + r1.numDocs() + " vs r2.numDocs()=" + r2.numDocs();
			bool hasDeletes = !(r1.MaxDoc() == r2.MaxDoc() && r1.NumDocs() == r1.MaxDoc());
			int[] r2r1 = new int[r2.MaxDoc()];
			// r2 id to r1 id mapping
			// create mapping from id2 space to id2 based on idField
			Fields f1 = MultiFields.GetFields(r1);
			if (f1 == null)
			{
				// make sure r2 is empty
				NUnit.Framework.Assert.IsNull(MultiFields.GetFields(r2));
				return;
			}
			Terms terms1 = f1.Terms(idField);
			if (terms1 == null)
			{
				NUnit.Framework.Assert.IsTrue(MultiFields.GetFields(r2) == null || MultiFields.GetFields
					(r2).Terms(idField) == null);
				return;
			}
			TermsEnum termsEnum = terms1.Iterator(null);
			Bits liveDocs1 = MultiFields.GetLiveDocs(r1);
			Bits liveDocs2 = MultiFields.GetLiveDocs(r2);
			Fields fields = MultiFields.GetFields(r2);
			if (fields == null)
			{
				// make sure r1 is in fact empty (eg has only all
				// deleted docs):
				Bits liveDocs = MultiFields.GetLiveDocs(r1);
				DocsEnum docs = null;
				while (termsEnum.Next() != null)
				{
					docs = TestUtil.Docs(Random(), termsEnum, liveDocs, docs, DocsEnum.FLAG_NONE);
					while (docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
					{
						NUnit.Framework.Assert.Fail("r1 is not empty but r2 is");
					}
				}
				return;
			}
			Terms terms2 = fields.Terms(idField);
			TermsEnum termsEnum2 = terms2.Iterator(null);
			DocsEnum termDocs1 = null;
			DocsEnum termDocs2 = null;
			while (true)
			{
				BytesRef term = termsEnum.Next();
				//System.out.println("TEST: match id term=" + term);
				if (term == null)
				{
					break;
				}
				termDocs1 = TestUtil.Docs(Random(), termsEnum, liveDocs1, termDocs1, DocsEnum.FLAG_NONE
					);
				if (termsEnum2.SeekExact(term))
				{
					termDocs2 = TestUtil.Docs(Random(), termsEnum2, liveDocs2, termDocs2, DocsEnum.FLAG_NONE
						);
				}
				else
				{
					termDocs2 = null;
				}
				if (termDocs1.NextDoc() == DocIdSetIterator.NO_MORE_DOCS)
				{
					// This doc is deleted and wasn't replaced
					NUnit.Framework.Assert.IsTrue(termDocs2 == null || termDocs2.NextDoc() == DocIdSetIterator
						.NO_MORE_DOCS);
					continue;
				}
				int id1 = termDocs1.DocID();
				NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, termDocs1.NextDoc(
					));
				NUnit.Framework.Assert.IsTrue(termDocs2.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
					);
				int id2 = termDocs2.DocID();
				NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, termDocs2.NextDoc(
					));
				r2r1[id2] = id1;
				// verify stored fields are equivalent
				try
				{
					VerifyEquals(r1.Document(id1), r2.Document(id2));
				}
				catch (Exception t)
				{
					System.Console.Out.WriteLine("FAILED id=" + term + " id1=" + id1 + " id2=" + id2 
						+ " term=" + term);
					System.Console.Out.WriteLine("  d1=" + r1.Document(id1));
					System.Console.Out.WriteLine("  d2=" + r2.Document(id2));
					throw;
				}
				try
				{
					// verify term vectors are equivalent        
					VerifyEquals(r1.GetTermVectors(id1), r2.GetTermVectors(id2));
				}
				catch (Exception e)
				{
					System.Console.Out.WriteLine("FAILED id=" + term + " id1=" + id1 + " id2=" + id2);
					Fields tv1 = r1.GetTermVectors(id1);
					System.Console.Out.WriteLine("  d1=" + tv1);
					if (tv1 != null)
					{
						DocsAndPositionsEnum dpEnum = null;
						DocsEnum dEnum = null;
						foreach (string field in tv1)
						{
							System.Console.Out.WriteLine("    " + field + ":");
							Terms terms3 = tv1.Terms(field);
							NUnit.Framework.Assert.IsNotNull(terms3);
							TermsEnum termsEnum3 = terms3.Iterator(null);
							BytesRef term2;
							while ((term2 = termsEnum3.Next()) != null)
							{
								System.Console.Out.WriteLine("      " + term2.Utf8ToString() + ": freq=" + termsEnum3
									.TotalTermFreq());
								dpEnum = termsEnum3.DocsAndPositions(null, dpEnum);
								if (dpEnum != null)
								{
									NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
									int freq = dpEnum.Freq();
									System.Console.Out.WriteLine("        doc=" + dpEnum.DocID() + " freq=" + freq);
									for (int posUpto = 0; posUpto < freq; posUpto++)
									{
										System.Console.Out.WriteLine("          pos=" + dpEnum.NextPosition());
									}
								}
								else
								{
									dEnum = TestUtil.Docs(Random(), termsEnum3, null, dEnum, DocsEnum.FLAG_FREQS);
									NUnit.Framework.Assert.IsNotNull(dEnum);
									NUnit.Framework.Assert.IsTrue(dEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
									int freq = dEnum.Freq();
									System.Console.Out.WriteLine("        doc=" + dEnum.DocID() + " freq=" + freq);
								}
							}
						}
					}
					Fields tv2 = r2.GetTermVectors(id2);
					System.Console.Out.WriteLine("  d2=" + tv2);
					if (tv2 != null)
					{
						DocsAndPositionsEnum dpEnum = null;
						DocsEnum dEnum = null;
						foreach (string field in tv2)
						{
							System.Console.Out.WriteLine("    " + field + ":");
							Terms terms3 = tv2.Terms(field);
							NUnit.Framework.Assert.IsNotNull(terms3);
							TermsEnum termsEnum3 = terms3.Iterator(null);
							BytesRef term2;
							while ((term2 = termsEnum3.Next()) != null)
							{
								System.Console.Out.WriteLine("      " + term2.Utf8ToString() + ": freq=" + termsEnum3
									.TotalTermFreq());
								dpEnum = termsEnum3.DocsAndPositions(null, dpEnum);
								if (dpEnum != null)
								{
									NUnit.Framework.Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
									int freq = dpEnum.Freq();
									System.Console.Out.WriteLine("        doc=" + dpEnum.DocID() + " freq=" + freq);
									for (int posUpto = 0; posUpto < freq; posUpto++)
									{
										System.Console.Out.WriteLine("          pos=" + dpEnum.NextPosition());
									}
								}
								else
								{
									dEnum = TestUtil.Docs(Random(), termsEnum3, null, dEnum, DocsEnum.FLAG_FREQS);
									NUnit.Framework.Assert.IsNotNull(dEnum);
									NUnit.Framework.Assert.IsTrue(dEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
									int freq = dEnum.Freq();
									System.Console.Out.WriteLine("        doc=" + dEnum.DocID() + " freq=" + freq);
								}
							}
						}
					}
					throw;
				}
			}
			//System.out.println("TEST: done match id");
			// Verify postings
			//System.out.println("TEST: create te1");
			Fields fields1 = MultiFields.GetFields(r1);
			Iterator<string> fields1Enum = fields1.Iterator();
			Fields fields2 = MultiFields.GetFields(r2);
			Iterator<string> fields2Enum = fields2.Iterator();
			string field1 = null;
			string field2 = null;
			TermsEnum termsEnum1 = null;
			termsEnum2 = null;
			DocsEnum docs1 = null;
			DocsEnum docs2 = null;
			// pack both doc and freq into single element for easy sorting
			long[] info1 = new long[r1.NumDocs()];
			long[] info2 = new long[r2.NumDocs()];
			for (; ; )
			{
				BytesRef term1 = null;
				BytesRef term2 = null;
				// iterate until we get some docs
				int len1;
				for (; ; )
				{
					len1 = 0;
					if (termsEnum1 == null)
					{
						if (!fields1Enum.HasNext())
						{
							break;
						}
						field1 = fields1Enum.Next();
						Terms terms = fields1.Terms(field1);
						if (terms == null)
						{
							continue;
						}
						termsEnum1 = terms.Iterator(null);
					}
					term1 = termsEnum1.Next();
					if (term1 == null)
					{
						// no more terms in this field
						termsEnum1 = null;
						continue;
					}
					//System.out.println("TEST: term1=" + term1);
					docs1 = TestUtil.Docs(Random(), termsEnum1, liveDocs1, docs1, DocsEnum.FLAG_FREQS
						);
					while (docs1.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
					{
						int d = docs1.DocID();
						int f = docs1.Freq();
						info1[len1] = (((long)d) << 32) | f;
						len1++;
					}
					if (len1 > 0)
					{
						break;
					}
				}
				// iterate until we get some docs
				int len2;
				for (; ; )
				{
					len2 = 0;
					if (termsEnum2 == null)
					{
						if (!fields2Enum.HasNext())
						{
							break;
						}
						field2 = fields2Enum.Next();
						Terms terms = fields2.Terms(field2);
						if (terms == null)
						{
							continue;
						}
						termsEnum2 = terms.Iterator(null);
					}
					term2 = termsEnum2.Next();
					if (term2 == null)
					{
						// no more terms in this field
						termsEnum2 = null;
						continue;
					}
					//System.out.println("TEST: term1=" + term1);
					docs2 = TestUtil.Docs(Random(), termsEnum2, liveDocs2, docs2, DocsEnum.FLAG_FREQS
						);
					while (docs2.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
					{
						int d = r2r1[docs2.DocID()];
						int f = docs2.Freq();
						info2[len2] = (((long)d) << 32) | f;
						len2++;
					}
					if (len2 > 0)
					{
						break;
					}
				}
				NUnit.Framework.Assert.AreEqual(len1, len2);
				if (len1 == 0)
				{
					break;
				}
				// no more terms
				NUnit.Framework.Assert.AreEqual(field1, field2);
				NUnit.Framework.Assert.IsTrue(term1.BytesEquals(term2));
				if (!hasDeletes)
				{
					NUnit.Framework.Assert.AreEqual(termsEnum1.DocFreq(), termsEnum2.DocFreq());
				}
				NUnit.Framework.Assert.AreEqual("len1=" + len1 + " len2=" + len2 + " deletes?=" +
					 hasDeletes, term1, term2);
				// sort info2 to get it into ascending docid
				Arrays.Sort(info2, 0, len2);
				// now compare
				for (int i = 0; i < len1; i++)
				{
					NUnit.Framework.Assert.AreEqual("i=" + i + " len=" + len1 + " d1=" + ((long)(((ulong
						)info1[i]) >> 32)) + " f1=" + (info1[i] & int.MaxValue) + " d2=" + ((long)(((ulong
						)info2[i]) >> 32)) + " f2=" + (info2[i] & int.MaxValue) + " field=" + field1 + " term="
						 + term1.Utf8ToString(), info1[i], info2[i]);
				}
			}
		}

		public static void VerifyEquals(Lucene.Net.Document.Document d1, Lucene.Net.Document.Document
			 d2)
		{
			IList<IndexableField> ff1 = d1.GetFields();
			IList<IndexableField> ff2 = d2.GetFields();
			ff1.Sort(fieldNameComparator);
			ff2.Sort(fieldNameComparator);
			NUnit.Framework.Assert.AreEqual(ff1 + " : " + ff2, ff1.Count, ff2.Count);
			for (int i = 0; i < ff1.Count; i++)
			{
				IndexableField f1 = ff1[i];
				IndexableField f2 = ff2[i];
				if (f1.BinaryValue() != null)
				{
				}
				else
				{
					//HM:revisit 
					//assert(f2.binaryValue() != null);
					string s1 = f1.StringValue();
					string s2 = f2.StringValue();
					NUnit.Framework.Assert.AreEqual(ff1 + " : " + ff2, s1, s2);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void VerifyEquals(Fields d1, Fields d2)
		{
			if (d1 == null)
			{
				NUnit.Framework.Assert.IsTrue(d2 == null || d2.Size() == 0);
				return;
			}
			NUnit.Framework.Assert.IsTrue(d2 != null);
			Iterator<string> fieldsEnum2 = d2.Iterator();
			foreach (string field1 in d1)
			{
				string field2 = fieldsEnum2.Next();
				NUnit.Framework.Assert.AreEqual(field1, field2);
				Terms terms1 = d1.Terms(field1);
				NUnit.Framework.Assert.IsNotNull(terms1);
				TermsEnum termsEnum1 = terms1.Iterator(null);
				Terms terms2 = d2.Terms(field2);
				NUnit.Framework.Assert.IsNotNull(terms2);
				TermsEnum termsEnum2 = terms2.Iterator(null);
				DocsAndPositionsEnum dpEnum1 = null;
				DocsAndPositionsEnum dpEnum2 = null;
				DocsEnum dEnum1 = null;
				DocsEnum dEnum2 = null;
				BytesRef term1;
				while ((term1 = termsEnum1.Next()) != null)
				{
					BytesRef term2 = termsEnum2.Next();
					NUnit.Framework.Assert.AreEqual(term1, term2);
					NUnit.Framework.Assert.AreEqual(termsEnum1.TotalTermFreq(), termsEnum2.TotalTermFreq
						());
					dpEnum1 = termsEnum1.DocsAndPositions(null, dpEnum1);
					dpEnum2 = termsEnum2.DocsAndPositions(null, dpEnum2);
					if (dpEnum1 != null)
					{
						NUnit.Framework.Assert.IsNotNull(dpEnum2);
						int docID1 = dpEnum1.NextDoc();
						dpEnum2.NextDoc();
						// docIDs are not supposed to be equal
						//int docID2 = dpEnum2.nextDoc();
						//assertEquals(docID1, docID2);
						NUnit.Framework.Assert.IsTrue(docID1 != DocIdSetIterator.NO_MORE_DOCS);
						int freq1 = dpEnum1.Freq();
						int freq2 = dpEnum2.Freq();
						NUnit.Framework.Assert.AreEqual(freq1, freq2);
						OffsetAttribute offsetAtt1 = dpEnum1.Attributes().HasAttribute(typeof(OffsetAttribute
							)) ? dpEnum1.Attributes().GetAttribute<OffsetAttribute>() : null;
						OffsetAttribute offsetAtt2 = dpEnum2.Attributes().HasAttribute(typeof(OffsetAttribute
							)) ? dpEnum2.Attributes().GetAttribute<OffsetAttribute>() : null;
						if (offsetAtt1 != null)
						{
							NUnit.Framework.Assert.IsNotNull(offsetAtt2);
						}
						else
						{
							NUnit.Framework.Assert.IsNull(offsetAtt2);
						}
						for (int posUpto = 0; posUpto < freq1; posUpto++)
						{
							int pos1 = dpEnum1.NextPosition();
							int pos2 = dpEnum2.NextPosition();
							NUnit.Framework.Assert.AreEqual(pos1, pos2);
							if (offsetAtt1 != null)
							{
								NUnit.Framework.Assert.AreEqual(offsetAtt1.StartOffset(), offsetAtt2.StartOffset(
									));
								NUnit.Framework.Assert.AreEqual(offsetAtt1.EndOffset(), offsetAtt2.EndOffset());
							}
						}
						NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum1.NextDoc());
						NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum2.NextDoc());
					}
					else
					{
						dEnum1 = TestUtil.Docs(Random(), termsEnum1, null, dEnum1, DocsEnum.FLAG_FREQS);
						dEnum2 = TestUtil.Docs(Random(), termsEnum2, null, dEnum2, DocsEnum.FLAG_FREQS);
						NUnit.Framework.Assert.IsNotNull(dEnum1);
						NUnit.Framework.Assert.IsNotNull(dEnum2);
						int docID1 = dEnum1.NextDoc();
						dEnum2.NextDoc();
						// docIDs are not supposed to be equal
						//int docID2 = dEnum2.nextDoc();
						//assertEquals(docID1, docID2);
						NUnit.Framework.Assert.IsTrue(docID1 != DocIdSetIterator.NO_MORE_DOCS);
						int freq1 = dEnum1.Freq();
						int freq2 = dEnum2.Freq();
						NUnit.Framework.Assert.AreEqual(freq1, freq2);
						NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dEnum1.NextDoc());
						NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dEnum2.NextDoc());
					}
				}
				NUnit.Framework.Assert.IsNull(termsEnum2.Next());
			}
			NUnit.Framework.Assert.IsFalse(fieldsEnum2.HasNext());
		}

		private class IndexingThread : Sharpen.Thread
		{
			internal IndexWriter w;

			internal int @base;

			internal int range;

			internal int iterations;

			internal IDictionary<string, Lucene.Net.Document.Document> docs = new Dictionary
				<string, Lucene.Net.Document.Document>();

			internal Random r;

			public virtual int NextInt(int lim)
			{
				return this.r.Next(lim);
			}

			// start is inclusive and end is exclusive
			public virtual int NextInt(int start, int end)
			{
				return start + this.r.Next(end - start);
			}

			internal char[] buffer = new char[100];

			private int AddUTF8Token(int start)
			{
				int end = start + this.NextInt(20);
				if (this.buffer.Length < 1 + end)
				{
					char[] newBuffer = new char[(int)((1 + end) * 1.25)];
					System.Array.Copy(this.buffer, 0, newBuffer, 0, this.buffer.Length);
					this.buffer = newBuffer;
				}
				for (int i = start; i < end; i++)
				{
					int t = this.NextInt(5);
					if (0 == t && i < end - 1)
					{
						// Make a surrogate pair
						// High surrogate
						this.buffer[i++] = (char)this.NextInt(unchecked((int)(0xd800)), unchecked((int)(0xdc00
							)));
						// Low surrogate
						this.buffer[i] = (char)this.NextInt(unchecked((int)(0xdc00)), unchecked((int)(0xe000
							)));
					}
					else
					{
						if (t <= 1)
						{
							this.buffer[i] = (char)this.NextInt(unchecked((int)(0x80)));
						}
						else
						{
							if (2 == t)
							{
								this.buffer[i] = (char)this.NextInt(unchecked((int)(0x80)), unchecked((int)(0x800
									)));
							}
							else
							{
								if (3 == t)
								{
									this.buffer[i] = (char)this.NextInt(unchecked((int)(0x800)), unchecked((int)(0xd800
										)));
								}
								else
								{
									if (4 == t)
									{
										this.buffer[i] = (char)this.NextInt(unchecked((int)(0xe000)), unchecked((int)(0xffff
											)));
									}
								}
							}
						}
					}
				}
				this.buffer[end] = ' ';
				return 1 + end;
			}

			public virtual string GetString(int nTokens)
			{
				nTokens = nTokens != 0 ? nTokens : this.r.Next(4) + 1;
				// Half the time make a random UTF8 string
				if (this.r.NextBoolean())
				{
					return this.GetUTF8String(nTokens);
				}
				// avoid StringBuffer because it adds extra synchronization.
				char[] arr = new char[nTokens * 2];
				for (int i = 0; i < nTokens; i++)
				{
					arr[i * 2] = (char)('A' + this.r.Next(10));
					arr[i * 2 + 1] = ' ';
				}
				return new string(arr);
			}

			public virtual string GetUTF8String(int nTokens)
			{
				int upto = 0;
				Arrays.Fill(this.buffer, (char)0);
				for (int i = 0; i < nTokens; i++)
				{
					upto = this.AddUTF8Token(upto);
				}
				return new string(this.buffer, 0, upto);
			}

			public virtual string GetIdString()
			{
				return Sharpen.Extensions.ToString(this.@base + this.NextInt(this.range));
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void IndexDoc()
			{
				Lucene.Net.Document.Document d = new Lucene.Net.Document.Document();
				FieldType customType1 = new FieldType(TextField.TYPE_STORED);
				customType1.SetTokenized(false);
				customType1.SetOmitNorms(true);
				AList<Field> fields = new AList<Field>();
				string idString = this.GetIdString();
				Field idField = LuceneTestCase.NewField("id", idString, customType1);
				fields.AddItem(idField);
				int nFields = this.NextInt(TestStressIndexing2.maxFields);
				for (int i = 0; i < nFields; i++)
				{
					FieldType customType = new FieldType();
					switch (this.NextInt(4))
					{
						case 0:
						{
							break;
						}

						case 1:
						{
							customType.SetStoreTermVectors(true);
							break;
						}

						case 2:
						{
							customType.SetStoreTermVectors(true);
							customType.SetStoreTermVectorPositions(true);
							break;
						}

						case 3:
						{
							customType.SetStoreTermVectors(true);
							customType.SetStoreTermVectorOffsets(true);
							break;
						}
					}
					switch (this.NextInt(4))
					{
						case 0:
						{
							customType.SetStored(true);
							customType.SetOmitNorms(true);
							customType.SetIndexed(true);
							fields.AddItem(LuceneTestCase.NewField("f" + this.NextInt(100), this.GetString(1)
								, customType));
							break;
						}

						case 1:
						{
							customType.SetIndexed(true);
							customType.SetTokenized(true);
							fields.AddItem(LuceneTestCase.NewField("f" + this.NextInt(100), this.GetString(0)
								, customType));
							break;
						}

						case 2:
						{
							customType.SetStored(true);
							customType.SetStoreTermVectors(false);
							customType.SetStoreTermVectorOffsets(false);
							customType.SetStoreTermVectorPositions(false);
							fields.AddItem(LuceneTestCase.NewField("f" + this.NextInt(100), this.GetString(0)
								, customType));
							break;
						}

						case 3:
						{
							customType.SetStored(true);
							customType.SetIndexed(true);
							customType.SetTokenized(true);
							fields.AddItem(LuceneTestCase.NewField("f" + this.NextInt(100), this.GetString(TestStressIndexing2
								.bigFieldSize), customType));
							break;
						}
					}
				}
				if (TestStressIndexing2.sameFieldOrder)
				{
					fields.Sort(TestStressIndexing2.fieldNameComparator);
				}
				else
				{
					// random placement of id field also
					Sharpen.Collections.Swap(fields, this.NextInt(fields.Count), 0);
				}
				for (int i_1 = 0; i_1 < fields.Count; i_1++)
				{
					d.Add(fields[i_1]);
				}
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": indexing id:"
						 + idString);
				}
				this.w.UpdateDocument(new Term("id", idString), d);
				//System.out.println(Thread.currentThread().getName() + ": indexing "+d);
				this.docs.Put(idString, d);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void DeleteDoc()
			{
				string idString = this.GetIdString();
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": del id:"
						 + idString);
				}
				this.w.DeleteDocuments(new Term("id", idString));
				Sharpen.Collections.Remove(this.docs, idString);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void DeleteByQuery()
			{
				string idString = this.GetIdString();
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": del query id:"
						 + idString);
				}
				this.w.DeleteDocuments(new TermQuery(new Term("id", idString)));
				Sharpen.Collections.Remove(this.docs, idString);
			}

			public override void Run()
			{
				try
				{
					this.r = new Random(this.@base + this.range + TestStressIndexing2.seed);
					for (int i = 0; i < this.iterations; i++)
					{
						int what = this.NextInt(100);
						if (what < 5)
						{
							this.DeleteDoc();
						}
						else
						{
							if (what < 10)
							{
								this.DeleteByQuery();
							}
							else
							{
								this.IndexDoc();
							}
						}
					}
				}
				catch (Exception e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
				}
				//HM:revisit 
				//assert.fail(e.toString());
				lock (this)
				{
					this.docs.Count;
				}
			}

			internal IndexingThread(TestStressIndexing2 _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TestStressIndexing2 _enclosing;
		}
	}
}
