/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Test.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestLongPostings : LuceneTestCase
	{
		// Produces a realistic unicode random string that
		// survives MockAnalyzer unchanged:
		/// <exception cref="System.IO.IOException"></exception>
		private string GetRandomTerm(string other)
		{
			Analyzer a = new MockAnalyzer(Random());
			while (true)
			{
				string s = TestUtil.RandomRealisticUnicodeString(Random());
				if (other != null && s.Equals(other))
				{
					continue;
				}
				TermToBytesRefAttribute termAtt = ts.GetAttribute<TermToBytesRefAttribute>();
				BytesRef termBytes = termAtt.GetBytesRef();
				ts.Reset();
				int count = 0;
				bool changed = false;
				while (ts.IncrementToken())
				{
					termAtt.FillBytesRef();
					if (count == 0 && !termBytes.Utf8ToString().Equals(s))
					{
						// The value was changed during analysis.  Keep iterating so the
						// tokenStream is exhausted.
						changed = true;
					}
					count++;
				}
				ts.End();
				// Did we iterate just once and the value was unchanged?
				if (!changed && count == 1)
				{
					return s;
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLongPostings()
		{
			// Don't use TestUtil.getTempDir so that we own the
			// randomness (ie same seed will point to same dir):
			Directory dir = NewFSDirectory(CreateTempDir("longpostings" + "." + Random().NextLong
				()));
			int NUM_DOCS = AtLeast(2000);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS);
			}
			string s1 = GetRandomTerm(null);
			string s2 = GetRandomTerm(s1);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: s1=" + s1 + " s2=" + s2);
			}
			FixedBitSet isS1 = new FixedBitSet(NUM_DOCS);
			for (int idx = 0; idx < NUM_DOCS; idx++)
			{
				if (Random().NextBoolean())
				{
					isS1.Set(idx);
				}
			}
			IndexReader r;
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMergePolicy(NewLogMergePolicy
				());
			iwc.SetRAMBufferSizeMB(16.0 + 16.0 * Random().NextDouble());
			iwc.SetMaxBufferedDocs(-1);
			RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, iwc);
			for (int idx_1 = 0; idx_1 < NUM_DOCS; idx_1++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				string s = isS1.Get(idx_1) ? s1 : s2;
				Field f = NewTextField("field", s, Field.Store.NO);
				int count = TestUtil.NextInt(Random(), 1, 4);
				for (int ct = 0; ct < count; ct++)
				{
					doc.Add(f);
				}
				riw.AddDocument(doc);
			}
			r = riw.Reader;
			riw.Dispose();
			AreEqual(NUM_DOCS, r.NumDocs);
			IsTrue(r.DocFreq(new Term("field", s1)) > 0);
			IsTrue(r.DocFreq(new Term("field", s2)) > 0);
			int num = AtLeast(1000);
			for (int iter = 0; iter < num; iter++)
			{
				string term;
				bool doS1;
				if (Random().NextBoolean())
				{
					term = s1;
					doS1 = true;
				}
				else
				{
					term = s2;
					doS1 = false;
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter + " doS1=" + doS1);
				}
				DocsAndPositionsEnum postings = MultiFields.GetTermPositionsEnum(r, null, "field"
					, new BytesRef(term));
				int docID = -1;
				while (docID < DocIdSetIterator.NO_MORE_DOCS)
				{
					int what = Random().Next(3);
					if (what == 0)
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: docID=" + docID + "; do next()");
						}
						// nextDoc
						int expected = docID + 1;
						while (true)
						{
							if (expected == NUM_DOCS)
							{
								expected = int.MaxValue;
								break;
							}
							else
							{
								if (isS1.Get(expected) == doS1)
								{
									break;
								}
								else
								{
									expected++;
								}
							}
						}
						docID = postings.NextDoc();
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  got docID=" + docID);
						}
						AreEqual(expected, docID);
						if (docID == DocIdSetIterator.NO_MORE_DOCS)
						{
							break;
						}
						if (Random().Next(6) == 3)
						{
							int freq = postings.Freq;
							IsTrue(freq >= 1 && freq <= 4);
							for (int pos = 0; pos < freq; pos++)
							{
								AreEqual(pos, postings.NextPosition());
								if (Random().NextBoolean())
								{
									postings.Payload;
									if (Random().NextBoolean())
									{
										postings.Payload;
									}
								}
							}
						}
					}
					else
					{
						// get it again
						// advance
						int targetDocID;
						if (docID == -1)
						{
							targetDocID = Random().Next(NUM_DOCS + 1);
						}
						else
						{
							targetDocID = docID + TestUtil.NextInt(Random(), 1, NUM_DOCS - docID);
						}
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: docID=" + docID + "; do advance(" + targetDocID
								 + ")");
						}
						int expected = targetDocID;
						while (true)
						{
							if (expected == NUM_DOCS)
							{
								expected = int.MaxValue;
								break;
							}
							else
							{
								if (isS1.Get(expected) == doS1)
								{
									break;
								}
								else
								{
									expected++;
								}
							}
						}
						docID = postings.Advance(targetDocID);
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  got docID=" + docID);
						}
						AreEqual(expected, docID);
						if (docID == DocIdSetIterator.NO_MORE_DOCS)
						{
							break;
						}
						if (Random().Next(6) == 3)
						{
							int freq = postings.Freq;
							IsTrue(freq >= 1 && freq <= 4);
							for (int pos = 0; pos < freq; pos++)
							{
								AreEqual(pos, postings.NextPosition());
								if (Random().NextBoolean())
								{
									postings.Payload;
									if (Random().NextBoolean())
									{
										postings.Payload;
									}
								}
							}
						}
					}
				}
			}
			// get it again
			r.Dispose();
			dir.Dispose();
		}

		// a weaker form of testLongPostings, that doesnt check positions
		/// <exception cref="System.Exception"></exception>
		public virtual void TestLongPostingsNoPositions()
		{
			DoTestLongPostingsNoPositions(FieldInfo.IndexOptions.DOCS_ONLY);
			DoTestLongPostingsNoPositions(FieldInfo.IndexOptions.DOCS_AND_FREQS);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void DoTestLongPostingsNoPositions(FieldInfo.IndexOptions options)
		{
			// Don't use TestUtil.getTempDir so that we own the
			// randomness (ie same seed will point to same dir):
			Directory dir = NewFSDirectory(CreateTempDir("longpostings" + "." + Random().NextLong
				()));
			int NUM_DOCS = AtLeast(2000);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS);
			}
			string s1 = GetRandomTerm(null);
			string s2 = GetRandomTerm(s1);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("\nTEST: s1=" + s1 + " s2=" + s2);
			}
			FixedBitSet isS1 = new FixedBitSet(NUM_DOCS);
			for (int idx = 0; idx < NUM_DOCS; idx++)
			{
				if (Random().NextBoolean())
				{
					isS1.Set(idx);
				}
			}
			IndexReader r;
			if (true)
			{
				IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
					(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMergePolicy(NewLogMergePolicy
					());
				iwc.SetRAMBufferSizeMB(16.0 + 16.0 * Random().NextDouble());
				iwc.SetMaxBufferedDocs(-1);
				RandomIndexWriter riw = new RandomIndexWriter(Random(), dir, iwc);
				FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
				ft.IndexOptions = (options);
				for (int idx_1 = 0; idx_1 < NUM_DOCS; idx_1++)
				{
					Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
						();
					string s = isS1.Get(idx_1) ? s1 : s2;
					Field f = NewField("field", s, ft);
					int count = TestUtil.NextInt(Random(), 1, 4);
					for (int ct = 0; ct < count; ct++)
					{
						doc.Add(f);
					}
					riw.AddDocument(doc);
				}
				r = riw.Reader;
				riw.Dispose();
			}
			else
			{
				r = DirectoryReader.Open(dir);
			}
			AreEqual(NUM_DOCS, r.NumDocs);
			IsTrue(r.DocFreq(new Term("field", s1)) > 0);
			IsTrue(r.DocFreq(new Term("field", s2)) > 0);
			int num = AtLeast(1000);
			for (int iter = 0; iter < num; iter++)
			{
				string term;
				bool doS1;
				if (Random().NextBoolean())
				{
					term = s1;
					doS1 = true;
				}
				else
				{
					term = s2;
					doS1 = false;
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: iter=" + iter + " doS1=" + doS1 + " term=" 
						+ term);
				}
				DocsEnum docs;
				DocsEnum postings;
				if (options == FieldInfo.IndexOptions.DOCS_ONLY)
				{
					docs = TestUtil.Docs(Random(), r, "field", new BytesRef(term), null, null, DocsEnum
						.FLAG_NONE);
					postings = null;
				}
				else
				{
					docs = postings = TestUtil.Docs(Random(), r, "field", new BytesRef(term), null, null
						, DocsEnum.FLAG_FREQS);
				}
				//HM:revisit 
				//assert postings != null;
				//HM:revisit 
				//assert docs != null;
				int docID = -1;
				while (docID < DocIdSetIterator.NO_MORE_DOCS)
				{
					int what = Random().Next(3);
					if (what == 0)
					{
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: docID=" + docID + "; do next()");
						}
						// nextDoc
						int expected = docID + 1;
						while (true)
						{
							if (expected == NUM_DOCS)
							{
								expected = int.MaxValue;
								break;
							}
							else
							{
								if (isS1.Get(expected) == doS1)
								{
									break;
								}
								else
								{
									expected++;
								}
							}
						}
						docID = docs.NextDoc();
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  got docID=" + docID);
						}
						AreEqual(expected, docID);
						if (docID == DocIdSetIterator.NO_MORE_DOCS)
						{
							break;
						}
						if (Random().Next(6) == 3 && postings != null)
						{
							int freq = postings.Freq;
							IsTrue(freq >= 1 && freq <= 4);
						}
					}
					else
					{
						// advance
						int targetDocID;
						if (docID == -1)
						{
							targetDocID = Random().Next(NUM_DOCS + 1);
						}
						else
						{
							targetDocID = docID + TestUtil.NextInt(Random(), 1, NUM_DOCS - docID);
						}
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("TEST: docID=" + docID + "; do advance(" + targetDocID
								 + ")");
						}
						int expected = targetDocID;
						while (true)
						{
							if (expected == NUM_DOCS)
							{
								expected = int.MaxValue;
								break;
							}
							else
							{
								if (isS1.Get(expected) == doS1)
								{
									break;
								}
								else
								{
									expected++;
								}
							}
						}
						docID = docs.Advance(targetDocID);
						if (VERBOSE)
						{
							System.Console.Out.WriteLine("  got docID=" + docID);
						}
						AreEqual(expected, docID);
						if (docID == DocIdSetIterator.NO_MORE_DOCS)
						{
							break;
						}
						if (Random().Next(6) == 3 && postings != null)
						{
							int freq = postings.Freq;
							IsTrue("got invalid freq=" + freq, freq >= 1 && freq <= 4);
						}
					}
				}
			}
			r.Dispose();
			dir.Dispose();
		}
	}
}
