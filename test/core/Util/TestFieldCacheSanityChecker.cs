/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;


namespace Lucene.Net.Util
{
	public class TestFieldCacheSanityChecker : LuceneTestCase
	{
		protected internal AtomicReader readerA;

		protected internal AtomicReader readerB;

		protected internal AtomicReader readerX;

		protected internal AtomicReader readerAclone;

		protected internal Directory dirA;

		protected internal Directory dirB;

		private const int NUM_DOCS = 1000;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dirA = NewDirectory();
			dirB = NewDirectory();
			IndexWriter wA = new IndexWriter(dirA, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			IndexWriter wB = new IndexWriter(dirB, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				new MockAnalyzer(Random())));
			long theLong = long.MaxValue;
			double theDouble = double.MaxValue;
			byte theByte = byte.MaxValue;
			short theShort = short.MaxValue;
			int theInt = int.MaxValue;
			float theFloat = float.MaxValue;
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
				if (0 == i % 3)
				{
					wA.AddDocument(doc);
				}
				else
				{
					wB.AddDocument(doc);
				}
			}
			wA.Dispose();
			wB.Dispose();
			DirectoryReader rA = DirectoryReader.Open(dirA);
			readerA = SlowCompositeReaderWrapper.Wrap(rA);
			readerAclone = SlowCompositeReaderWrapper.Wrap(rA);
			readerA = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dirA));
			readerB = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dirB));
			readerX = SlowCompositeReaderWrapper.Wrap(new MultiReader(readerA, readerB));
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			readerA.Dispose();
			readerAclone.Dispose();
			readerB.Dispose();
			readerX.Dispose();
			dirA.Dispose();
			dirB.Dispose();
			base.TearDown();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSanity()
		{
			FieldCache cache = FieldCache.DEFAULT;
			cache.PurgeAllCaches();
			cache.GetDoubles(readerA, "theDouble", false);
			cache.GetDoubles(readerA, "theDouble", FieldCache.DEFAULT_DOUBLE_PARSER, false);
			cache.GetDoubles(readerAclone, "theDouble", FieldCache.DEFAULT_DOUBLE_PARSER, false
				);
			cache.GetDoubles(readerB, "theDouble", FieldCache.DEFAULT_DOUBLE_PARSER, false);
			cache.GetInts(readerX, "theInt", false);
			cache.GetInts(readerX, "theInt", FieldCache.DEFAULT_INT_PARSER, false);
			// // // 
			FieldCacheSanityChecker.Insanity[] insanity = FieldCacheSanityChecker.CheckSanity
				(cache.GetCacheEntries());
			if (0 < insanity.Length)
			{
				DumpArray(GetTestClass().FullName + "#" + GetTestName() + " INSANITY", insanity, 
					System.Console.Error);
			}
			AreEqual("shouldn't be any cache insanity", 0, insanity.Length
				);
			cache.PurgeAllCaches();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestInsanity1()
		{
			FieldCache cache = FieldCache.DEFAULT;
			cache.PurgeAllCaches();
			cache.GetInts(readerX, "theInt", FieldCache.DEFAULT_INT_PARSER, false);
			cache.GetTerms(readerX, "theInt", false);
			cache.GetBytes(readerX, "theByte", false);
			// // // 
			FieldCacheSanityChecker.Insanity[] insanity = FieldCacheSanityChecker.CheckSanity
				(cache.GetCacheEntries());
			AreEqual("wrong number of cache errors", 1, insanity.Length
				);
			AreEqual("wrong type of cache error", FieldCacheSanityChecker.InsanityType
				.VALUEMISMATCH, insanity[0].GetType());
			AreEqual("wrong number of entries in cache error", 2, insanity
				[0].GetCacheEntries().Length);
			// we expect bad things, don't let tearDown complain about them
			cache.PurgeAllCaches();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestInsanity2()
		{
			FieldCache cache = FieldCache.DEFAULT;
			cache.PurgeAllCaches();
			cache.GetTerms(readerA, "theInt", false);
			cache.GetTerms(readerB, "theInt", false);
			cache.GetTerms(readerX, "theInt", false);
			cache.GetBytes(readerX, "theByte", false);
			// // // 
			FieldCacheSanityChecker.Insanity[] insanity = FieldCacheSanityChecker.CheckSanity
				(cache.GetCacheEntries());
			AreEqual("wrong number of cache errors", 1, insanity.Length
				);
			AreEqual("wrong type of cache error", FieldCacheSanityChecker.InsanityType
				.SUBREADER, insanity[0].GetType());
			AreEqual("wrong number of entries in cache error", 3, insanity
				[0].GetCacheEntries().Length);
			// we expect bad things, don't let tearDown complain about them
			cache.PurgeAllCaches();
		}
	}
}
