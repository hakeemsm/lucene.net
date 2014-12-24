/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class TestFieldCache : LuceneTestCase
	{
		private static AtomicReader reader;

		private static int NUM_DOCS;

		private static int NUM_ORDS;

		private static string[] unicodeStrings;

		private static BytesRef[][] multiValued;

		private static Directory directory;

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			NUM_DOCS = AtLeast(500);
			NUM_ORDS = AtLeast(2);
			directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				()));
			long theLong = long.MaxValue;
			double theDouble = double.MaxValue;
			byte theByte = byte.MaxValue;
			short theShort = short.MaxValue;
			int theInt = int.MaxValue;
			float theFloat = float.MaxValue;
			unicodeStrings = new string[NUM_DOCS];
			multiValued = new BytesRef[NUM_DOCS][];
			//HM:revisit 2nd subscript removed
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: setUp");
			}
			for (int i = 0; i < NUM_DOCS; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewStringField("theLong", (theLong--).ToString(), Field.Store.NO));
				doc.Add(NewStringField("theDouble", (theDouble--).ToString(), Field.Store.NO));
				doc.Add(NewStringField("theByte", (theByte--).ToString(), Field.Store.NO));
				doc.Add(NewStringField("theShort", (theShort--).ToString(), Field.Store.NO));
				doc.Add(NewStringField("theInt", (theInt--).ToString(), Field.Store.NO));
				doc.Add(NewStringField("theFloat", (theFloat--).ToString(), Field.Store.NO));
				if (i % 2 == 0)
				{
					doc.Add(NewStringField("sparse", i.ToString(), Field.Store.NO));
				}
				if (i % 2 == 0)
				{
					doc.Add(new IntField("numInt", i, Field.Store.NO));
				}
				// sometimes skip the field:
				if (Random().Next(40) != 17)
				{
					unicodeStrings[i] = GenerateString(i);
					doc.Add(NewStringField("theRandomUnicodeString", unicodeStrings[i], Field.Store.YES
						));
				}
				// sometimes skip the field:
				if (Random().Next(10) != 8)
				{
					for (int j = 0; j < NUM_ORDS; j++)
					{
						string newValue = GenerateString(i);
						multiValued[i][j] = new BytesRef(newValue);
						doc.Add(NewStringField("theRandomUnicodeMultiValuedField", newValue, Field.Store.
							YES));
					}
					Arrays.Sort(multiValued[i]);
				}
				writer.AddDocument(doc);
			}
			IndexReader r = writer.GetReader();
			reader = SlowCompositeReaderWrapper.Wrap(r);
			writer.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			reader.Close();
			reader = null;
			directory.Close();
			directory = null;
			unicodeStrings = null;
			multiValued = null;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestInfoStream()
		{
			try
			{
				FieldCache cache = FieldCache.DEFAULT;
				ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
				cache.SetInfoStream(new TextWriter(bos, false, IOUtils.UTF_8));
				cache.GetDoubles(reader, "theDouble", false);
				cache.GetFloats(reader, "theDouble", false);
				IsTrue(bos.ToString(IOUtils.UTF_8).IndexOf("WARNING") != -
					1);
			}
			finally
			{
				FieldCache.DEFAULT.SetInfoStream(null);
				FieldCache.DEFAULT.PurgeAllCaches();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Test()
		{
			FieldCache cache = FieldCache.DEFAULT;
			FieldCache.Doubles doubles = cache.GetDoubles(reader, "theDouble", Random().NextBoolean
				());
			AreSame("Second request to cache return same array", doubles
				, cache.GetDoubles(reader, "theDouble", Random().NextBoolean()));
			AreSame("Second request with explicit parser return same array"
				, doubles, cache.GetDoubles(reader, "theDouble", FieldCache.DEFAULT_DOUBLE_PARSER
				, Random().NextBoolean()));
			for (int i = 0; i < NUM_DOCS; i++)
			{
				IsTrue(doubles.Get(i) + " does not equal: " + (double.MaxValue
					 - i), doubles.Get(i) == (double.MaxValue - i));
			}
			FieldCache.Longs longs = cache.GetLongs(reader, "theLong", Random().NextBoolean()
				);
			AreSame("Second request to cache return same array", longs
				, cache.GetLongs(reader, "theLong", Random().NextBoolean()));
			AreSame("Second request with explicit parser return same array"
				, longs, cache.GetLongs(reader, "theLong", FieldCache.DEFAULT_LONG_PARSER, Random
				().NextBoolean()));
			for (int i_1 = 0; i_1 < NUM_DOCS; i_1++)
			{
				IsTrue(longs.Get(i_1) + " does not equal: " + (long.MaxValue
					 - i_1) + " i=" + i_1, longs.Get(i_1) == (long.MaxValue - i_1));
			}
			FieldCache.Bytes bytes = cache.GetBytes(reader, "theByte", Random().NextBoolean()
				);
			AreSame("Second request to cache return same array", bytes
				, cache.GetBytes(reader, "theByte", Random().NextBoolean()));
			AreSame("Second request with explicit parser return same array"
				, bytes, cache.GetBytes(reader, "theByte", FieldCache.DEFAULT_BYTE_PARSER, Random
				().NextBoolean()));
			for (int i_2 = 0; i_2 < NUM_DOCS; i_2++)
			{
				IsTrue(bytes.Get(i_2) + " does not equal: " + (byte.MaxValue
					 - i_2), bytes.Get(i_2) == unchecked((byte)(byte.MaxValue - i_2)));
			}
			FieldCache.Shorts shorts = cache.GetShorts(reader, "theShort", Random().NextBoolean
				());
			AreSame("Second request to cache return same array", shorts
				, cache.GetShorts(reader, "theShort", Random().NextBoolean()));
			AreSame("Second request with explicit parser return same array"
				, shorts, cache.GetShorts(reader, "theShort", FieldCache.DEFAULT_SHORT_PARSER, Random
				().NextBoolean()));
			for (int i_3 = 0; i_3 < NUM_DOCS; i_3++)
			{
				IsTrue(shorts.Get(i_3) + " does not equal: " + (short.MaxValue
					 - i_3), shorts.Get(i_3) == (short)(short.MaxValue - i_3));
			}
			FieldCache.Ints ints = cache.GetInts(reader, "theInt", Random().NextBoolean());
			AreSame("Second request to cache return same array", ints, 
				cache.GetInts(reader, "theInt", Random().NextBoolean()));
			AreSame("Second request with explicit parser return same array"
				, ints, cache.GetInts(reader, "theInt", FieldCache.DEFAULT_INT_PARSER, Random().
				NextBoolean()));
			for (int i_4 = 0; i_4 < NUM_DOCS; i_4++)
			{
				IsTrue(ints.Get(i_4) + " does not equal: " + (int.MaxValue
					 - i_4), ints.Get(i_4) == (int.MaxValue - i_4));
			}
			FieldCache.Floats floats = cache.GetFloats(reader, "theFloat", Random().NextBoolean
				());
			AreSame("Second request to cache return same array", floats
				, cache.GetFloats(reader, "theFloat", Random().NextBoolean()));
			AreSame("Second request with explicit parser return same array"
				, floats, cache.GetFloats(reader, "theFloat", FieldCache.DEFAULT_FLOAT_PARSER, Random
				().NextBoolean()));
			for (int i_5 = 0; i_5 < NUM_DOCS; i_5++)
			{
				IsTrue(floats.Get(i_5) + " does not equal: " + (float.MaxValue
					 - i_5), floats.Get(i_5) == (float.MaxValue - i_5));
			}
			Bits docsWithField = cache.GetDocsWithField(reader, "theLong");
			AreSame("Second request to cache return same array", docsWithField
				, cache.GetDocsWithField(reader, "theLong"));
			IsTrue("docsWithField(theLong) must be class Bits.MatchAllBits"
				, docsWithField is Bits.MatchAllBits);
			IsTrue("docsWithField(theLong) Size: " + docsWithField.Length
				() + " is not: " + NUM_DOCS, docsWithField.Length() == NUM_DOCS);
			for (int i_6 = 0; i_6 < docsWithField.Length(); i_6++)
			{
				IsTrue(docsWithField.Get(i_6));
			}
			docsWithField = cache.GetDocsWithField(reader, "sparse");
			AreSame("Second request to cache return same array", docsWithField
				, cache.GetDocsWithField(reader, "sparse"));
			IsFalse("docsWithField(sparse) must not be class Bits.MatchAllBits"
				, docsWithField is Bits.MatchAllBits);
			IsTrue("docsWithField(sparse) Size: " + docsWithField.Length
				() + " is not: " + NUM_DOCS, docsWithField.Length() == NUM_DOCS);
			for (int i_7 = 0; i_7 < docsWithField.Length(); i_7++)
			{
				AreEqual(i_7 % 2 == 0, docsWithField.Get(i_7));
			}
			// getTermsIndex
			SortedDocValues termsIndex = cache.GetTermsIndex(reader, "theRandomUnicodeString"
				);
			AreSame("Second request to cache return same array", termsIndex
				, cache.GetTermsIndex(reader, "theRandomUnicodeString"));
			BytesRef br = new BytesRef();
			for (int i_8 = 0; i_8 < NUM_DOCS; i_8++)
			{
				BytesRef term;
				int ord = termsIndex.GetOrd(i_8);
				if (ord == -1)
				{
					term = null;
				}
				else
				{
					termsIndex.LookupOrd(ord, br);
					term = br;
				}
				string s = term == null ? null : term.Utf8ToString();
				IsTrue("for doc " + i_8 + ": " + s + " does not equal: " +
					 unicodeStrings[i_8], unicodeStrings[i_8] == null || unicodeStrings[i_8].Equals(
					s));
			}
			int nTerms = termsIndex.GetValueCount();
			TermsEnum tenum = termsIndex.TermsEnum();
			BytesRef val = new BytesRef();
			for (int i_9 = 0; i_9 < nTerms; i_9++)
			{
				BytesRef val1 = tenum.Next();
				termsIndex.LookupOrd(i_9, val);
				// System.out.println("i="+i);
				AreEqual(val, val1);
			}
			// seek the enum around (note this isn't a great test here)
			int num = AtLeast(100);
			for (int i_10 = 0; i_10 < num; i_10++)
			{
				int k = Random().Next(nTerms);
				termsIndex.LookupOrd(k, val);
				AreEqual(TermsEnum.SeekStatus.FOUND, tenum.SeekCeil(val));
				AreEqual(val, tenum.Term());
			}
			for (int i_11 = 0; i_11 < nTerms; i_11++)
			{
				termsIndex.LookupOrd(i_11, val);
				AreEqual(TermsEnum.SeekStatus.FOUND, tenum.SeekCeil(val));
				AreEqual(val, tenum.Term());
			}
			// test bad field
			termsIndex = cache.GetTermsIndex(reader, "bogusfield");
			// getTerms
			BinaryDocValues terms = cache.GetTerms(reader, "theRandomUnicodeString", true);
			AreSame("Second request to cache return same array", terms
				, cache.GetTerms(reader, "theRandomUnicodeString", true));
			Bits bits = cache.GetDocsWithField(reader, "theRandomUnicodeString");
			for (int i_12 = 0; i_12 < NUM_DOCS; i_12++)
			{
				terms.Get(i_12, br);
				BytesRef term;
				if (!bits.Get(i_12))
				{
					term = null;
				}
				else
				{
					term = br;
				}
				string s = term == null ? null : term.Utf8ToString();
				IsTrue("for doc " + i_12 + ": " + s + " does not equal: " 
					+ unicodeStrings[i_12], unicodeStrings[i_12] == null || unicodeStrings[i_12].Equals
					(s));
			}
			// test bad field
			terms = cache.GetTerms(reader, "bogusfield", false);
			// getDocTermOrds
			SortedSetDocValues termOrds = cache.GetDocTermOrds(reader, "theRandomUnicodeMultiValuedField"
				);
			int numEntries = cache.GetCacheEntries().Length;
			// ask for it again, and check that we didnt create any additional entries:
			termOrds = cache.GetDocTermOrds(reader, "theRandomUnicodeMultiValuedField");
			AreEqual(numEntries, cache.GetCacheEntries().Length);
			for (int i_13 = 0; i_13 < NUM_DOCS; i_13++)
			{
				termOrds.SetDocument(i_13);
				// This will remove identical terms. A DocTermOrds doesn't return duplicate ords for a docId
				IList<BytesRef> values = new AList<BytesRef>(new LinkedHashSet<BytesRef>(Arrays.AsList
					(multiValued[i_13])));
				foreach (BytesRef v in values)
				{
					if (v == null)
					{
						// why does this test use null values... instead of an empty list: confusing
						break;
					}
					long ord = termOrds.NextOrd();
					//HM:revisit 
					//assert ord != SortedSetDocValues.NO_MORE_ORDS;
					BytesRef scratch = new BytesRef();
					termOrds.LookupOrd(ord, scratch);
					AreEqual(v, scratch);
				}
				AreEqual(SortedSetDocValues.NO_MORE_ORDS, termOrds.NextOrd
					());
			}
			// test bad field
			termOrds = cache.GetDocTermOrds(reader, "bogusfield");
			IsTrue(termOrds.GetValueCount() == 0);
			FieldCache.DEFAULT.PurgeByCacheKey(reader.GetCoreCacheKey());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEmptyIndex()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(500)));
			writer.Close();
			IndexReader r = DirectoryReader.Open(dir);
			AtomicReader reader = SlowCompositeReaderWrapper.Wrap(r);
			FieldCache.DEFAULT.GetTerms(reader, "foobar", true);
			FieldCache.DEFAULT.GetTermsIndex(reader, "foobar");
			FieldCache.DEFAULT.PurgeByCacheKey(reader.GetCoreCacheKey());
			r.Close();
			dir.Close();
		}

		private static string GenerateString(int i)
		{
			string s = null;
			if (i > 0 && Random().Next(3) == 1)
			{
				// reuse past string -- try to find one that's not null
				for (int iter = 0; iter < 10 && s == null; iter++)
				{
					s = unicodeStrings[Random().Next(i)];
				}
				if (s == null)
				{
					s = TestUtil.RandomUnicodeString(Random());
				}
			}
			else
			{
				s = TestUtil.RandomUnicodeString(Random());
			}
			return s;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDocsWithField()
		{
			FieldCache cache = FieldCache.DEFAULT;
			cache.PurgeAllCaches();
			AreEqual(0, cache.GetCacheEntries().Length);
			cache.GetDoubles(reader, "theDouble", true);
			// The double[] takes two slots (one w/ null parser, one
			// w/ real parser), and docsWithField should also
			// have been populated:
			AreEqual(3, cache.GetCacheEntries().Length);
			Bits bits = cache.GetDocsWithField(reader, "theDouble");
			// No new entries should appear:
			AreEqual(3, cache.GetCacheEntries().Length);
			IsTrue(bits is Bits.MatchAllBits);
			FieldCache.Ints ints = cache.GetInts(reader, "sparse", true);
			AreEqual(6, cache.GetCacheEntries().Length);
			Bits docsWithField = cache.GetDocsWithField(reader, "sparse");
			AreEqual(6, cache.GetCacheEntries().Length);
			for (int i = 0; i < docsWithField.Length(); i++)
			{
				if (i % 2 == 0)
				{
					IsTrue(docsWithField.Get(i));
					AreEqual(i, ints.Get(i));
				}
				else
				{
					IsFalse(docsWithField.Get(i));
				}
			}
			FieldCache.Ints numInts = cache.GetInts(reader, "numInt", Random().NextBoolean());
			docsWithField = cache.GetDocsWithField(reader, "numInt");
			for (int i_1 = 0; i_1 < docsWithField.Length(); i_1++)
			{
				if (i_1 % 2 == 0)
				{
					IsTrue(docsWithField.Get(i_1));
					AreEqual(i_1, numInts.Get(i_1));
				}
				else
				{
					IsFalse(docsWithField.Get(i_1));
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestGetDocsWithFieldThreadSafety()
		{
			FieldCache cache = FieldCache.DEFAULT;
			cache.PurgeAllCaches();
			int NUM_THREADS = 3;
			Sharpen.Thread[] threads = new Sharpen.Thread[NUM_THREADS];
			AtomicBoolean failed = new AtomicBoolean();
			AtomicInteger iters = new AtomicInteger();
			int NUM_ITER = 200 * RANDOM_MULTIPLIER;
			CyclicBarrier restart = new CyclicBarrier(NUM_THREADS, new _Runnable_394(cache, iters
				));
			for (int threadIDX = 0; threadIDX < NUM_THREADS; threadIDX++)
			{
				threads[threadIDX] = new _Thread_402(failed, restart, iters, NUM_ITER, cache);
				// Purge all caches & resume, once all
				// threads get here:
				threads[threadIDX].Start();
			}
			for (int threadIDX_1 = 0; threadIDX_1 < NUM_THREADS; threadIDX_1++)
			{
				threads[threadIDX_1].Join();
			}
			IsFalse(failed.Get());
		}

		private sealed class _Runnable_394 : Runnable
		{
			public _Runnable_394(FieldCache cache, AtomicInteger iters)
			{
				this.cache = cache;
				this.iters = iters;
			}

			public void Run()
			{
				cache.PurgeAllCaches();
				iters.IncrementAndGet();
			}

			private readonly FieldCache cache;

			private readonly AtomicInteger iters;
		}

		private sealed class _Thread_402 : Sharpen.Thread
		{
			public _Thread_402(AtomicBoolean failed, CyclicBarrier restart, AtomicInteger iters
				, int NUM_ITER, FieldCache cache)
			{
				this.failed = failed;
				this.restart = restart;
				this.iters = iters;
				this.NUM_ITER = NUM_ITER;
				this.cache = cache;
			}

			public override void Run()
			{
				try
				{
					while (!failed.Get())
					{
						int op = LuceneTestCase.Random().Next(3);
						if (op == 0)
						{
							restart.Await();
							if (iters.Get() >= NUM_ITER)
							{
								break;
							}
						}
						else
						{
							if (op == 1)
							{
								Bits docsWithField = cache.GetDocsWithField(TestFieldCache.reader, "sparse");
								for (int i = 0; i < docsWithField.Length(); i++)
								{
									AreEqual(i % 2 == 0, docsWithField.Get(i));
								}
							}
							else
							{
								FieldCache.Ints ints = cache.GetInts(TestFieldCache.reader, "sparse", true);
								Bits docsWithField = cache.GetDocsWithField(TestFieldCache.reader, "sparse");
								for (int i = 0; i < docsWithField.Length(); i++)
								{
									if (i % 2 == 0)
									{
										IsTrue(docsWithField.Get(i));
										AreEqual(i, ints.Get(i));
									}
									else
									{
										IsFalse(docsWithField.Get(i));
									}
								}
							}
						}
					}
				}
				catch (Exception t)
				{
					failed.Set(true);
					restart.Reset();
					throw new RuntimeException(t);
				}
			}

			private readonly AtomicBoolean failed;

			private readonly CyclicBarrier restart;

			private readonly AtomicInteger iters;

			private readonly int NUM_ITER;

			private readonly FieldCache cache;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDocValuesIntegration()
		{
			AssumeTrue("3.x does not support docvalues", DefaultCodecSupportsDocValues());
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new BinaryDocValuesField("binary", new BytesRef("binary value")));
			doc.Add(new SortedDocValuesField("sorted", new BytesRef("sorted value")));
			doc.Add(new NumericDocValuesField("numeric", 42));
			if (DefaultCodecSupportsSortedSet())
			{
				doc.Add(new SortedSetDocValuesField("sortedset", new BytesRef("sortedset value1")
					));
				doc.Add(new SortedSetDocValuesField("sortedset", new BytesRef("sortedset value2")
					));
			}
			iw.AddDocument(doc);
			DirectoryReader ir = iw.GetReader();
			iw.Close();
			AtomicReader ar = GetOnlySegmentReader(ir);
			BytesRef scratch = new BytesRef();
			// Binary type: can be retrieved via getTerms()
			try
			{
				FieldCache.DEFAULT.GetInts(ar, "binary", false);
				Fail();
			}
			catch (InvalidOperationException)
			{
			}
			BinaryDocValues binary = FieldCache.DEFAULT.GetTerms(ar, "binary", true);
			binary.Get(0, scratch);
			AreEqual("binary value", scratch.Utf8ToString());
			try
			{
				FieldCache.DEFAULT.GetTermsIndex(ar, "binary");
				Fail();
			}
			catch (InvalidOperationException)
			{
			}
			try
			{
				FieldCache.DEFAULT.GetDocTermOrds(ar, "binary");
				Fail();
			}
			catch (InvalidOperationException)
			{
			}
			try
			{
				new DocTermOrds(ar, null, "binary");
				Fail();
			}
			catch (InvalidOperationException)
			{
			}
			Bits bits = FieldCache.DEFAULT.GetDocsWithField(ar, "binary");
			IsTrue(bits.Get(0));
			// Sorted type: can be retrieved via getTerms(), getTermsIndex(), getDocTermOrds()
			try
			{
				FieldCache.DEFAULT.GetInts(ar, "sorted", false);
				Fail();
			}
			catch (InvalidOperationException)
			{
			}
			try
			{
				new DocTermOrds(ar, null, "sorted");
				Fail();
			}
			catch (InvalidOperationException)
			{
			}
			binary = FieldCache.DEFAULT.GetTerms(ar, "sorted", true);
			binary.Get(0, scratch);
			AreEqual("sorted value", scratch.Utf8ToString());
			SortedDocValues sorted = FieldCache.DEFAULT.GetTermsIndex(ar, "sorted");
			AreEqual(0, sorted.GetOrd(0));
			AreEqual(1, sorted.GetValueCount());
			sorted.Get(0, scratch);
			AreEqual("sorted value", scratch.Utf8ToString());
			SortedSetDocValues sortedSet = FieldCache.DEFAULT.GetDocTermOrds(ar, "sorted");
			sortedSet.SetDocument(0);
			AreEqual(0, sortedSet.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd
				());
			AreEqual(1, sortedSet.GetValueCount());
			bits = FieldCache.DEFAULT.GetDocsWithField(ar, "sorted");
			IsTrue(bits.Get(0));
			// Numeric type: can be retrieved via getInts() and so on
			FieldCache.Ints numeric = FieldCache.DEFAULT.GetInts(ar, "numeric", false);
			AreEqual(42, numeric.Get(0));
			try
			{
				FieldCache.DEFAULT.GetTerms(ar, "numeric", true);
				Fail();
			}
			catch (InvalidOperationException)
			{
			}
			try
			{
				FieldCache.DEFAULT.GetTermsIndex(ar, "numeric");
				Fail();
			}
			catch (InvalidOperationException)
			{
			}
			try
			{
				FieldCache.DEFAULT.GetDocTermOrds(ar, "numeric");
				Fail();
			}
			catch (InvalidOperationException)
			{
			}
			try
			{
				new DocTermOrds(ar, null, "numeric");
				Fail();
			}
			catch (InvalidOperationException)
			{
			}
			bits = FieldCache.DEFAULT.GetDocsWithField(ar, "numeric");
			IsTrue(bits.Get(0));
			// SortedSet type: can be retrieved via getDocTermOrds() 
			if (DefaultCodecSupportsSortedSet())
			{
				try
				{
					FieldCache.DEFAULT.GetInts(ar, "sortedset", false);
					Fail();
				}
				catch (InvalidOperationException)
				{
				}
				try
				{
					FieldCache.DEFAULT.GetTerms(ar, "sortedset", true);
					Fail();
				}
				catch (InvalidOperationException)
				{
				}
				try
				{
					FieldCache.DEFAULT.GetTermsIndex(ar, "sortedset");
					Fail();
				}
				catch (InvalidOperationException)
				{
				}
				try
				{
					new DocTermOrds(ar, null, "sortedset");
					Fail();
				}
				catch (InvalidOperationException)
				{
				}
				sortedSet = FieldCache.DEFAULT.GetDocTermOrds(ar, "sortedset");
				sortedSet.SetDocument(0);
				AreEqual(0, sortedSet.NextOrd());
				AreEqual(1, sortedSet.NextOrd());
				AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd
					());
				AreEqual(2, sortedSet.GetValueCount());
				bits = FieldCache.DEFAULT.GetDocsWithField(ar, "sortedset");
				IsTrue(bits.Get(0));
			}
			ir.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNonexistantFields()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			iw.AddDocument(doc);
			DirectoryReader ir = iw.GetReader();
			iw.Close();
			AtomicReader ar = GetOnlySegmentReader(ir);
			FieldCache cache = FieldCache.DEFAULT;
			cache.PurgeAllCaches();
			AreEqual(0, cache.GetCacheEntries().Length);
			FieldCache.Bytes bytes = cache.GetBytes(ar, "bogusbytes", true);
			AreEqual(0, bytes.Get(0));
			FieldCache.Shorts shorts = cache.GetShorts(ar, "bogusshorts", true);
			AreEqual(0, shorts.Get(0));
			FieldCache.Ints ints = cache.GetInts(ar, "bogusints", true);
			AreEqual(0, ints.Get(0));
			FieldCache.Longs longs = cache.GetLongs(ar, "boguslongs", true);
			AreEqual(0, longs.Get(0));
			FieldCache.Floats floats = cache.GetFloats(ar, "bogusfloats", true);
			AreEqual(0, floats.Get(0), 0.0f);
			FieldCache.Doubles doubles = cache.GetDoubles(ar, "bogusdoubles", true);
			AreEqual(0, doubles.Get(0), 0.0D);
			BytesRef scratch = new BytesRef();
			BinaryDocValues binaries = cache.GetTerms(ar, "bogusterms", true);
			binaries.Get(0, scratch);
			AreEqual(0, scratch.length);
			SortedDocValues sorted = cache.GetTermsIndex(ar, "bogustermsindex");
			AreEqual(-1, sorted.GetOrd(0));
			sorted.Get(0, scratch);
			AreEqual(0, scratch.length);
			SortedSetDocValues sortedSet = cache.GetDocTermOrds(ar, "bogusmultivalued");
			sortedSet.SetDocument(0);
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd
				());
			Bits bits = cache.GetDocsWithField(ar, "bogusbits");
			IsFalse(bits.Get(0));
			// check that we cached nothing
			AreEqual(0, cache.GetCacheEntries().Length);
			ir.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNonIndexedFields()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StoredField("bogusbytes", "bogus"));
			doc.Add(new StoredField("bogusshorts", "bogus"));
			doc.Add(new StoredField("bogusints", "bogus"));
			doc.Add(new StoredField("boguslongs", "bogus"));
			doc.Add(new StoredField("bogusfloats", "bogus"));
			doc.Add(new StoredField("bogusdoubles", "bogus"));
			doc.Add(new StoredField("bogusterms", "bogus"));
			doc.Add(new StoredField("bogustermsindex", "bogus"));
			doc.Add(new StoredField("bogusmultivalued", "bogus"));
			doc.Add(new StoredField("bogusbits", "bogus"));
			iw.AddDocument(doc);
			DirectoryReader ir = iw.GetReader();
			iw.Close();
			AtomicReader ar = GetOnlySegmentReader(ir);
			FieldCache cache = FieldCache.DEFAULT;
			cache.PurgeAllCaches();
			AreEqual(0, cache.GetCacheEntries().Length);
			FieldCache.Bytes bytes = cache.GetBytes(ar, "bogusbytes", true);
			AreEqual(0, bytes.Get(0));
			FieldCache.Shorts shorts = cache.GetShorts(ar, "bogusshorts", true);
			AreEqual(0, shorts.Get(0));
			FieldCache.Ints ints = cache.GetInts(ar, "bogusints", true);
			AreEqual(0, ints.Get(0));
			FieldCache.Longs longs = cache.GetLongs(ar, "boguslongs", true);
			AreEqual(0, longs.Get(0));
			FieldCache.Floats floats = cache.GetFloats(ar, "bogusfloats", true);
			AreEqual(0, floats.Get(0), 0.0f);
			FieldCache.Doubles doubles = cache.GetDoubles(ar, "bogusdoubles", true);
			AreEqual(0, doubles.Get(0), 0.0D);
			BytesRef scratch = new BytesRef();
			BinaryDocValues binaries = cache.GetTerms(ar, "bogusterms", true);
			binaries.Get(0, scratch);
			AreEqual(0, scratch.length);
			SortedDocValues sorted = cache.GetTermsIndex(ar, "bogustermsindex");
			AreEqual(-1, sorted.GetOrd(0));
			sorted.Get(0, scratch);
			AreEqual(0, scratch.length);
			SortedSetDocValues sortedSet = cache.GetDocTermOrds(ar, "bogusmultivalued");
			sortedSet.SetDocument(0);
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, sortedSet.NextOrd
				());
			Bits bits = cache.GetDocsWithField(ar, "bogusbits");
			IsFalse(bits.Get(0));
			// check that we cached nothing
			AreEqual(0, cache.GetCacheEntries().Length);
			ir.Close();
			dir.Close();
		}

		// Make sure that the use of GrowableWriter doesn't prevent from using the full long range
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLongFieldCache()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig cfg = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			cfg.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, cfg);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			LongField field = new LongField("f", 0L, Field.Store.YES);
			doc.Add(field);
			long[] values = new long[TestUtil.NextInt(Random(), 1, 10)];
			for (int i = 0; i < values.Length; ++i)
			{
				long v;
				switch (Random().Next(10))
				{
					case 0:
					{
						v = long.MinValue;
						break;
					}

					case 1:
					{
						v = 0;
						break;
					}

					case 2:
					{
						v = long.MaxValue;
						break;
					}

					default:
					{
						v = TestUtil.NextLong(Random(), -10, 10);
						break;
						break;
					}
				}
				values[i] = v;
				if (v == 0 && Random().NextBoolean())
				{
					// missing
					iw.AddDocument(new Lucene.Net.Documents.Document());
				}
				else
				{
					field.SetLongValue(v);
					iw.AddDocument(doc);
				}
			}
			iw.ForceMerge(1);
			DirectoryReader reader = iw.GetReader();
			FieldCache.Longs longs = FieldCache.DEFAULT.GetLongs(GetOnlySegmentReader(reader)
				, "f", false);
			for (int i_1 = 0; i_1 < values.Length; ++i_1)
			{
				AreEqual(values[i_1], longs.Get(i_1));
			}
			reader.Close();
			iw.Close();
			dir.Close();
		}

		// Make sure that the use of GrowableWriter doesn't prevent from using the full int range
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestIntFieldCache()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig cfg = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			cfg.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, cfg);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			IntField field = new IntField("f", 0, Field.Store.YES);
			doc.Add(field);
			int[] values = new int[TestUtil.NextInt(Random(), 1, 10)];
			for (int i = 0; i < values.Length; ++i)
			{
				int v;
				switch (Random().Next(10))
				{
					case 0:
					{
						v = int.MinValue;
						break;
					}

					case 1:
					{
						v = 0;
						break;
					}

					case 2:
					{
						v = int.MaxValue;
						break;
					}

					default:
					{
						v = TestUtil.NextInt(Random(), -10, 10);
						break;
						break;
					}
				}
				values[i] = v;
				if (v == 0 && Random().NextBoolean())
				{
					// missing
					iw.AddDocument(new Lucene.Net.Documents.Document());
				}
				else
				{
					field.SetIntValue(v);
					iw.AddDocument(doc);
				}
			}
			iw.ForceMerge(1);
			DirectoryReader reader = iw.GetReader();
			FieldCache.Ints ints = FieldCache.DEFAULT.GetInts(GetOnlySegmentReader(reader), "f"
				, false);
			for (int i_1 = 0; i_1 < values.Length; ++i_1)
			{
				AreEqual(values[i_1], ints.Get(i_1));
			}
			reader.Close();
			iw.Close();
			dir.Close();
		}
	}
}
