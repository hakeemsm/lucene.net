/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>
	/// Create an index with random unicode terms
	/// Generates random prefix queries, and validates against a simple impl.
	/// </summary>
	/// <remarks>
	/// Create an index with random unicode terms
	/// Generates random prefix queries, and validates against a simple impl.
	/// </remarks>
	public class TestPrefixRandom : LuceneTestCase
	{
		private IndexSearcher searcher;

		private IndexReader reader;

		private Directory dir;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, ((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer
				.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000))));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field field = NewStringField("field", string.Empty, Field.Store.NO);
			doc.Add(field);
			// we generate aweful prefixes: good for testing.
			// but for preflex codec, the test can be very slow, so use less iterations.
			string codec = Codec.GetDefault().GetName();
			int num = codec.Equals("Lucene3x") ? 200 * RANDOM_MULTIPLIER : AtLeast(1000);
			for (int i = 0; i < num; i++)
			{
				field.StringValue = TestUtil.RandomUnicodeString(Random(), 10));
				writer.AddDocument(doc);
			}
			reader = writer.Reader;
			searcher = NewSearcher(reader);
			writer.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Dispose();
			dir.Dispose();
			base.TearDown();
		}

		/// <summary>a stupid prefix query that just blasts thru the terms</summary>
		private class DumbPrefixQuery : MultiTermQuery
		{
			private readonly BytesRef prefix;

			internal DumbPrefixQuery(TestPrefixRandom _enclosing, Term term) : base(term.Field
				())
			{
				this._enclosing = _enclosing;
				this.prefix = term.Bytes();
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
			{
				return new TestPrefixRandom.DumbPrefixQuery.SimplePrefixTermsEnum(this, terms.IEnumerator
					(null), this.prefix);
			}

			private class SimplePrefixTermsEnum : FilteredTermsEnum
			{
				private readonly BytesRef prefix;

				private SimplePrefixTermsEnum(DumbPrefixQuery _enclosing, TermsEnum tenum, BytesRef
					 prefix) : base(tenum)
				{
					this._enclosing = _enclosing;
					this.prefix = prefix;
					this.SetInitialSeekTerm(new BytesRef(string.Empty));
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected override FilteredTermsEnum.AcceptStatus Accept(BytesRef term)
				{
					return StringHelper.StartsWith(term, this.prefix) ? FilteredTermsEnum.AcceptStatus
						.YES : FilteredTermsEnum.AcceptStatus.NO;
				}

				private readonly DumbPrefixQuery _enclosing;
			}

			public override string ToString(string field)
			{
				return field.ToString() + ":" + this.prefix.ToString();
			}

			private readonly TestPrefixRandom _enclosing;
		}

		/// <summary>test a bunch of random prefixes</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPrefixes()
		{
			int num = AtLeast(100);
			for (int i = 0; i < num; i++)
			{
				AssertSame(TestUtil.RandomUnicodeString(Random(), 5));
			}
		}

		/// <summary>
		/// check that the # of hits is the same as from a very
		/// simple prefixquery implementation.
		/// </summary>
		/// <remarks>
		/// check that the # of hits is the same as from a very
		/// simple prefixquery implementation.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void AssertSame(string prefix)
		{
			PrefixQuery smart = new PrefixQuery(new Term("field", prefix));
			TestPrefixRandom.DumbPrefixQuery dumb = new TestPrefixRandom.DumbPrefixQuery(this
				, new Term("field", prefix));
			TopDocs smartDocs = searcher.Search(smart, 25);
			TopDocs dumbDocs = searcher.Search(dumb, 25);
			CheckHits.CheckEqual(smart, smartDocs.ScoreDocs, dumbDocs.ScoreDocs);
		}
	}
}
