using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestDocTermOrds : LuceneTestCase
	{
		// TODO:
		//   - test w/ del docs
		//   - test prefix
		//   - test w/ cutoff
		//   - crank docs way up so we get some merging sometimes
		[Test]
		public virtual void TestSimple()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field field = NewTextField("field", string.Empty, Field.Store.NO);
			doc.Add(field);
			field.StringValue = "a b c");
			w.AddDocument(doc);
			field.StringValue = "d e f");
			w.AddDocument(doc);
			field.StringValue = "a f");
			w.AddDocument(doc);
			IndexReader r = w.Reader;
			w.Close();
			AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r);
			DocTermOrds dto = new DocTermOrds(ar, ar.LiveDocs, "field");
			SortedSetDocValues iter = dto.IEnumerator(ar);
			iter.SetDocument(0);
			AreEqual(0, iter.NextOrd());
			AreEqual(1, iter.NextOrd());
			AreEqual(2, iter.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, iter.NextOrd());
			iter.SetDocument(1);
			AreEqual(3, iter.NextOrd());
			AreEqual(4, iter.NextOrd());
			AreEqual(5, iter.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, iter.NextOrd());
			iter.SetDocument(2);
			AreEqual(0, iter.NextOrd());
			AreEqual(5, iter.NextOrd());
			AreEqual(SortedSetDocValues.NO_MORE_ORDS, iter.NextOrd());
			r.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestRandom()
		{
			Directory dir = NewDirectory();
			int NUM_TERMS = AtLeast(20);
			var terms = new HashSet<BytesRef>();
			while (terms.Count < NUM_TERMS)
			{
				string s = TestUtil.RandomRealisticUnicodeString(Random());
				//final String s = TestUtil.randomSimpleString(random);
				if (s.Length > 0)
				{
					terms.Add(new BytesRef(s));
				}
			}
			BytesRef[] termsArray = terms.ToArray();
			Arrays.Sort(termsArray);
			int NUM_DOCS = AtLeast(100);
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			// Sometimes swap in codec that impls ord():
			if (Random().Next(10) == 7)
			{
				// Make sure terms index has ords:
				Codec codec = TestUtil.AlwaysPostingsFormat(PostingsFormat.ForName("Lucene41WithOrds"
					));
				conf.SetCodec(codec);
			}
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, conf);
			int[][] idToOrds = new int[NUM_DOCS][];
			ICollection<int> ordsForDocSet = new HashSet<int>();
			for (int id = 0; id < NUM_DOCS; id++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new IntField("id", id, Field.Store.NO));
				int termCount = TestUtil.NextInt(Random(), 0, 20 * RANDOM_MULTIPLIER);
				while (ordsForDocSet.Count < termCount)
				{
					ordsForDocSet.Add(Random().Next(termsArray.Length));
				}
				int[] ordsForDoc = new int[termCount];
				int upto = 0;
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: doc id=" + id);
				}
				foreach (int ord in ordsForDocSet)
				{
					ordsForDoc[upto++] = ord;
					Field field = NewStringField("field", termsArray[ord].Utf8ToString(), Field.Store
						.NO);
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  f=" + termsArray[ord].Utf8ToString());
					}
					doc.Add(field);
				}
				ordsForDocSet.Clear();
				Arrays.Sort(ordsForDoc);
				idToOrds[id] = ordsForDoc;
				w.AddDocument(doc);
			}
			DirectoryReader r = w.Reader;
			w.Close();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: reader=" + r);
			}
			foreach (AtomicReaderContext ctx in r.Leaves)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("\nTEST: sub=" + ((AtomicReader)ctx.Reader));
				}
				Verify(((AtomicReader)ctx.Reader), idToOrds, termsArray, null);
			}
			// Also test top-level reader: its enum does not support
			// ord, so this forces the OrdWrapper to run:
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: top reader");
			}
			AtomicReader slowR = SlowCompositeReaderWrapper.Wrap(r);
			Verify(slowR, idToOrds, termsArray, null);
			FieldCache.DEFAULT.PurgeByCacheKey(slowR.CoreCacheKey);
			r.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestRandomWithPrefix()
		{
			Directory dir = NewDirectory();
			ICollection<string> prefixes = new HashSet<string>();
			int numPrefix = Random().NextInt(2, 7);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: use " + numPrefix + " prefixes");
			}
			while (prefixes.Count < numPrefix)
			{
				prefixes.Add(TestUtil.RandomRealisticUnicodeString(Random()));
			}
			//prefixes.add(TestUtil.randomSimpleString(random));
			string[] prefixesArray = prefixes.ToArray();
			int NUM_TERMS = AtLeast(20);
			ICollection<BytesRef> terms = new HashSet<BytesRef>();
			while (terms.Count < NUM_TERMS)
			{
				string s = prefixesArray[Random().Next(prefixesArray.Length)] + TestUtil.RandomRealisticUnicodeString
					(Random());
				//final String s = prefixesArray[random.nextInt(prefixesArray.length)] + TestUtil.randomSimpleString(random);
				if (s.Length > 0)
				{
					terms.Add(new BytesRef(s));
				}
			}
			BytesRef[] termsArray = terms.ToArray();
			Arrays.Sort(termsArray);
			int NUM_DOCS = AtLeast(100);
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			// Sometimes swap in codec that impls ord():
			if (Random().Next(10) == 7)
			{
				Codec codec = TestUtil.AlwaysPostingsFormat(PostingsFormat.ForName("Lucene41WithOrds"
					));
				conf.SetCodec(codec);
			}
			RandomIndexWriter w = new RandomIndexWriter(Random(), dir, conf);
			int[][] idToOrds = new int[NUM_DOCS][];
			ICollection<int> ordsForDocSet = new HashSet<int>();
			for (int id = 0; id < NUM_DOCS; id++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new IntField("id", id, Field.Store.NO));
				int termCount = TestUtil.NextInt(Random(), 0, 20 * RANDOM_MULTIPLIER);
				while (ordsForDocSet.Count < termCount)
				{
					ordsForDocSet.Add(Random().Next(termsArray.Length));
				}
				int[] ordsForDoc = new int[termCount];
				int upto = 0;
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: doc id=" + id);
				}
				foreach (int ord in ordsForDocSet)
				{
					ordsForDoc[upto++] = ord;
					Field field = NewStringField("field", termsArray[ord].Utf8ToString(), Field.Store
						.NO);
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  f=" + termsArray[ord].Utf8ToString());
					}
					doc.Add(field);
				}
				ordsForDocSet.Clear();
				Arrays.Sort(ordsForDoc);
				idToOrds[id] = ordsForDoc;
				w.AddDocument(doc);
			}
			DirectoryReader r = w.Reader;
			w.Close();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: reader=" + r);
			}
			AtomicReader slowR = SlowCompositeReaderWrapper.Wrap(r);
			foreach (string prefix in prefixesArray)
			{
				BytesRef prefixRef = prefix == null ? null : new BytesRef(prefix);
				int[][] idToOrdsPrefix = new int[NUM_DOCS][];
				for (int id_1 = 0; id_1 < NUM_DOCS; id_1++)
				{
					int[] docOrds = idToOrds[id_1];
					IList<int> newOrds = new List<int>();
					foreach (int ord in idToOrds[id_1])
					{
						if (StringHelper.StartsWith(termsArray[ord], prefixRef))
						{
							newOrds.Add(ord);
						}
					}
					int[] newOrdsArray = new int[newOrds.Count];
					int upto = 0;
					foreach (int ord_1 in newOrds)
					{
						newOrdsArray[upto++] = ord_1;
					}
					idToOrdsPrefix[id_1] = newOrdsArray;
				}
				foreach (AtomicReaderContext ctx in r.Leaves)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("\nTEST: sub=" + ((AtomicReader)ctx.Reader));
					}
					Verify(((AtomicReader)ctx.Reader), idToOrdsPrefix, termsArray, prefixRef);
				}
				// Also test top-level reader: its enum does not support
				// ord, so this forces the OrdWrapper to run:
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: top reader");
				}
				Verify(slowR, idToOrdsPrefix, termsArray, prefixRef);
			}
			FieldCache.DEFAULT.PurgeByCacheKey(slowR.CoreCacheKey);
			r.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		private void Verify(AtomicReader r, int[][] idToOrds, BytesRef[] termsArray, BytesRef
			 prefixRef)
		{
			DocTermOrds dto = new DocTermOrds(r, r.LiveDocs, "field", prefixRef, int.MaxValue
				, TestUtil.NextInt(Random(), 2, 10));
			FieldCache.Ints docIDToID = FieldCache.DEFAULT.GetInts(r, "id", false);
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: verify prefix=" + (prefixRef == null ? "null"
					 : prefixRef.Utf8ToString()));
				System.Console.Out.WriteLine("TEST: all TERMS:");
				TermsEnum allTE = MultiFields.GetTerms(r, "field").IEnumerator(null);
				int ord = 0;
				while (allTE.Next() != null)
				{
					System.Console.Out.WriteLine("  ord=" + (ord++) + " term=" + allTE.Term.Utf8ToString
						());
				}
			}
			//final TermsEnum te = subR.fields().terms("field").iterator();
			TermsEnum te = dto.GetOrdTermsEnum(r);
			if (dto.NumTerms == 0)
			{
				if (prefixRef == null)
				{
					IsNull(MultiFields.GetTerms(r, "field"));
				}
				else
				{
					Terms terms = MultiFields.GetTerms(r, "field");
					if (terms != null)
					{
						TermsEnum termsEnum = terms.IEnumerator(null);
						TermsEnum.SeekStatus result = termsEnum.SeekCeil(prefixRef);
						if (result != TermsEnum.SeekStatus.END)
						{
							AssertFalse("term=" + termsEnum.Term.Utf8ToString() + " matches prefix="
								 + prefixRef.Utf8ToString(), StringHelper.StartsWith(termsEnum.Term, prefixRef
								));
						}
					}
				}
				// ok
				// ok
				return;
			}
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: TERMS:");
				te.SeekExact(0);
				while (true)
				{
					System.Console.Out.WriteLine("  ord=" + te.Ord + " term=" + te.Term.Utf8ToString
						());
					if (te.Next() == null)
					{
						break;
					}
				}
			}
			SortedSetDocValues iter = dto.IEnumerator(r);
			for (int docID = 0; docID < r.MaxDoc; docID++)
			{
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("TEST: docID=" + docID + " of " + r.MaxDoc + " (id="
						 + docIDToID.Get(docID) + ")");
				}
				iter.SetDocument(docID);
				int[] answers = idToOrds[docIDToID.Get(docID)];
				int upto = 0;
				long ord;
				while ((ord = iter.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
				{
					te.SeekExact(ord);
					BytesRef expected = termsArray[answers[upto++]];
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("  exp=" + expected.Utf8ToString() + " actual=" + te
							.Term.Utf8ToString());
					}
					AssertEquals("expected=" + expected.Utf8ToString() + " actual="
						 + te.Term.Utf8ToString() + " ord=" + ord, expected, te.Term);
				}
				AreEqual(answers.Length, upto);
			}
		}

		[Test]
		public virtual void TestBackToTheFuture()
		{
			Directory dir = NewDirectory();
			IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				null));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("foo", "bar", Field.Store.NO));
			iw.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(NewStringField("foo", "baz", Field.Store.NO));
			iw.AddDocument(doc);
			DirectoryReader r1 = DirectoryReader.Open(iw, true);
			iw.DeleteDocuments(new Term("foo", "baz"));
			DirectoryReader r2 = DirectoryReader.Open(iw, true);
			FieldCache.DEFAULT.GetDocTermOrds(GetOnlySegmentReader(r2), "foo");
			SortedSetDocValues v = FieldCache.DEFAULT.GetDocTermOrds(GetOnlySegmentReader(r1)
				, "foo");
			AreEqual(2, v.ValueCount);
			v.SetDocument(1);
			AreEqual(1, v.NextOrd());
			iw.Dispose();
			r1.Dispose();
			r2.Dispose();
			dir.Dispose();
		}

		[Test]
		public virtual void TestSortedTermsEnum()
		{
			Directory directory = NewDirectory();
			Analyzer analyzer = new MockAnalyzer(Random());
			IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwconfig.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new StringField("field", "hello", Field.Store.NO));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("field", "world", Field.Store.NO));
			iwriter.AddDocument(doc);
			doc = new Lucene.Net.Documents.Document();
			doc.Add(new StringField("field", "beer", Field.Store.NO));
			iwriter.AddDocument(doc);
			iwriter.ForceMerge(1);
			DirectoryReader ireader = iwriter.Reader;
			iwriter.Close();
			AtomicReader ar = GetOnlySegmentReader(ireader);
			SortedSetDocValues dv = FieldCache.DEFAULT.GetDocTermOrds(ar, "field");
			AreEqual(3, dv.ValueCount);
			TermsEnum termsEnum = dv.TermsEnum;
			// next()
			AreEqual("beer", termsEnum.Next().Utf8ToString());
			AreEqual(0, termsEnum.Ord);
			AreEqual("hello", termsEnum.Next().Utf8ToString());
			AreEqual(1, termsEnum.Ord);
			AreEqual("world", termsEnum.Next().Utf8ToString());
			AreEqual(2, termsEnum.Ord);
			// seekCeil()
			AreEqual(TermsEnum.SeekStatus.NOT_FOUND, termsEnum.SeekCeil
				(new BytesRef("ha!")));
			AreEqual("hello", termsEnum.Term.Utf8ToString());
			AreEqual(1, termsEnum.Ord);
			AreEqual(TermsEnum.SeekStatus.FOUND, termsEnum.SeekCeil(new 
				BytesRef("beer")));
			AreEqual("beer", termsEnum.Term.Utf8ToString());
			AreEqual(0, termsEnum.Ord);
			AreEqual(TermsEnum.SeekStatus.END, termsEnum.SeekCeil(new 
				BytesRef("zzz")));
			// seekExact()
			IsTrue(termsEnum.SeekExact(new BytesRef("beer")));
			AreEqual("beer", termsEnum.Term.Utf8ToString());
			AreEqual(0, termsEnum.Ord);
			IsTrue(termsEnum.SeekExact(new BytesRef("hello")));
			AreEqual("hello", termsEnum.Term.Utf8ToString());
			AreEqual(1, termsEnum.Ord);
			IsTrue(termsEnum.SeekExact(new BytesRef("world")));
			AreEqual("world", termsEnum.Term.Utf8ToString());
			AreEqual(2, termsEnum.Ord);
			IsFalse(termsEnum.SeekExact(new BytesRef("bogus")));
			// seek(ord)
			termsEnum.SeekExact(0);
			AreEqual("beer", termsEnum.Term.Utf8ToString());
			AreEqual(0, termsEnum.Ord);
			termsEnum.SeekExact(1);
			AreEqual("hello", termsEnum.Term.Utf8ToString());
			AreEqual(1, termsEnum.Ord);
			termsEnum.SeekExact(2);
			AreEqual("world", termsEnum.Term.Utf8ToString());
			AreEqual(2, termsEnum.Ord);
			ireader.Dispose();
			directory.Dispose();
		}
	}
}
