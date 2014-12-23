/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene40
{
	public class TestReuseDocsEnum : LuceneTestCase
	{
		// TODO: really this should be in BaseTestPF or somewhere else? useful test!
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		}

		// explicitly instantiates ancient codec
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestReuseDocsEnumNoReuse()
		{
			Directory dir = NewDirectory();
			Codec cp = TestUtil.AlwaysPostingsFormat(new Lucene40RWPostingsFormat());
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetCodec(cp));
			int numdocs = AtLeast(20);
			CreateRandomIndex(numdocs, writer, Random());
			writer.Commit();
			DirectoryReader open = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext ctx in open.Leaves())
			{
				AtomicReader indexReader = ((AtomicReader)ctx.Reader());
				Terms terms = indexReader.Terms("body");
				TermsEnum iterator = terms.Iterator(null);
				IdentityHashMap<DocsEnum, bool> enums = new IdentityHashMap<DocsEnum, bool>();
				Bits.MatchNoBits bits = new Bits.MatchNoBits(indexReader.MaxDoc());
				while ((iterator.Next()) != null)
				{
					DocsEnum docs = iterator.Docs(Random().NextBoolean() ? bits : new Bits.MatchNoBits
						(indexReader.MaxDoc()), null, Random().NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum
						.FLAG_NONE);
					enums.Put(docs, true);
				}
				NUnit.Framework.Assert.AreEqual(terms.Size(), enums.Count);
			}
			IOUtils.Close(writer, open, dir);
		}

		// tests for reuse only if bits are the same either null or the same instance
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestReuseDocsEnumSameBitsOrNull()
		{
			Directory dir = NewDirectory();
			Codec cp = TestUtil.AlwaysPostingsFormat(new Lucene40RWPostingsFormat());
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetCodec(cp));
			int numdocs = AtLeast(20);
			CreateRandomIndex(numdocs, writer, Random());
			writer.Commit();
			DirectoryReader open = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext ctx in open.Leaves())
			{
				Terms terms = ((AtomicReader)ctx.Reader()).Terms("body");
				TermsEnum iterator = terms.Iterator(null);
				IdentityHashMap<DocsEnum, bool> enums = new IdentityHashMap<DocsEnum, bool>();
				Bits.MatchNoBits bits = new Bits.MatchNoBits(open.MaxDoc());
				DocsEnum docs = null;
				while ((iterator.Next()) != null)
				{
					docs = iterator.Docs(bits, docs, Random().NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum
						.FLAG_NONE);
					enums.Put(docs, true);
				}
				NUnit.Framework.Assert.AreEqual(1, enums.Count);
				enums.Clear();
				iterator = terms.Iterator(null);
				docs = null;
				while ((iterator.Next()) != null)
				{
					docs = iterator.Docs(new Bits.MatchNoBits(open.MaxDoc()), docs, Random().NextBoolean
						() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
					enums.Put(docs, true);
				}
				NUnit.Framework.Assert.AreEqual(terms.Size(), enums.Count);
				enums.Clear();
				iterator = terms.Iterator(null);
				docs = null;
				while ((iterator.Next()) != null)
				{
					docs = iterator.Docs(null, docs, Random().NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum
						.FLAG_NONE);
					enums.Put(docs, true);
				}
				NUnit.Framework.Assert.AreEqual(1, enums.Count);
			}
			IOUtils.Close(writer, open, dir);
		}

		// make sure we never reuse from another reader even if it is the same field & codec etc
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestReuseDocsEnumDifferentReader()
		{
			Directory dir = NewDirectory();
			Codec cp = TestUtil.AlwaysPostingsFormat(new Lucene40RWPostingsFormat());
			MockAnalyzer analyzer = new MockAnalyzer(Random());
			analyzer.SetMaxTokenLength(TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH
				));
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, analyzer).SetCodec(cp));
			int numdocs = AtLeast(20);
			CreateRandomIndex(numdocs, writer, Random());
			writer.Commit();
			DirectoryReader firstReader = DirectoryReader.Open(dir);
			DirectoryReader secondReader = DirectoryReader.Open(dir);
			IList<AtomicReaderContext> leaves = firstReader.Leaves();
			IList<AtomicReaderContext> leaves2 = secondReader.Leaves();
			foreach (AtomicReaderContext ctx in leaves)
			{
				Terms terms = ((AtomicReader)ctx.Reader()).Terms("body");
				TermsEnum iterator = terms.Iterator(null);
				IdentityHashMap<DocsEnum, bool> enums = new IdentityHashMap<DocsEnum, bool>();
				Bits.MatchNoBits bits = new Bits.MatchNoBits(firstReader.MaxDoc());
				iterator = terms.Iterator(null);
				DocsEnum docs = null;
				BytesRef term = null;
				while ((term = iterator.Next()) != null)
				{
					docs = iterator.Docs(null, RandomDocsEnum("body", term, leaves2, bits), Random().
						NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
					enums.Put(docs, true);
				}
				NUnit.Framework.Assert.AreEqual(terms.Size(), enums.Count);
				iterator = terms.Iterator(null);
				enums.Clear();
				docs = null;
				while ((term = iterator.Next()) != null)
				{
					docs = iterator.Docs(bits, RandomDocsEnum("body", term, leaves2, bits), Random().
						NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum.FLAG_NONE);
					enums.Put(docs, true);
				}
				NUnit.Framework.Assert.AreEqual(terms.Size(), enums.Count);
			}
			IOUtils.Close(writer, firstReader, secondReader, dir);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual DocsEnum RandomDocsEnum(string field, BytesRef term, IList<AtomicReaderContext
			> readers, Bits bits)
		{
			if (Random().Next(10) == 0)
			{
				return null;
			}
			AtomicReader indexReader = ((AtomicReader)readers[Random().Next(readers.Count)].Reader
				());
			Terms terms = indexReader.Terms(field);
			if (terms == null)
			{
				return null;
			}
			TermsEnum iterator = terms.Iterator(null);
			if (iterator.SeekExact(term))
			{
				return iterator.Docs(bits, null, Random().NextBoolean() ? DocsEnum.FLAG_FREQS : DocsEnum
					.FLAG_NONE);
			}
			return null;
		}

		/// <summary>populates a writer with random stuff.</summary>
		/// <remarks>
		/// populates a writer with random stuff. this must be fully reproducable with
		/// the seed!
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static void CreateRandomIndex(int numdocs, RandomIndexWriter writer, Random
			 random)
		{
			LineFileDocs lineFileDocs = new LineFileDocs(random);
			for (int i = 0; i < numdocs; i++)
			{
				writer.AddDocument(lineFileDocs.NextDoc());
			}
			lineFileDocs.Close();
		}
	}
}
