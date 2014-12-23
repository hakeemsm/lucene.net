/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Codecs.Perfield;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Perfield
{
	/// <summary>Basic tests of PerFieldDocValuesFormat</summary>
	public class TestPerFieldDocValuesFormat : BaseDocValuesFormatTestCase
	{
		private Codec codec;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			codec = new RandomCodec(new Random(Random().NextLong()), Collections.EmptySet<string
				>());
			base.SetUp();
		}

		protected override Codec GetCodec()
		{
			return codec;
		}

		protected override bool CodecAcceptsHugeBinaryValues(string field)
		{
			return TestUtil.FieldSupportsHugeBinaryDocValues(field);
		}

		// just a simple trivial test
		// TODO: we should come up with a test that somehow checks that segment suffix
		// is respected by all codec apis (not just docvalues and postings)
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTwoFieldsTwoFormats()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			DocValuesFormat fast = DocValuesFormat.ForName("Lucene45");
			DocValuesFormat slow = DocValuesFormat.ForName("SimpleText");
			iwc.SetCodec(new _Lucene46Codec_84(fast, slow));
			IndexWriter iwriter = new IndexWriter(directory, iwc);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
			string text = "This is the text to be indexed. " + longTerm;
			doc.Add(NewTextField("fieldname", text, Field.Store.YES));
			doc.Add(new NumericDocValuesField("dv1", 5));
			doc.Add(new BinaryDocValuesField("dv2", new BytesRef("hello world")));
			iwriter.AddDocument(doc);
			iwriter.Close();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			IndexSearcher isearcher = NewSearcher(ireader);
			NUnit.Framework.Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname"
				, longTerm)), 1).totalHits);
			Query query = new TermQuery(new Term("fieldname", "text"));
			TopDocs hits = isearcher.Search(query, null, 1);
			NUnit.Framework.Assert.AreEqual(1, hits.totalHits);
			BytesRef scratch = new BytesRef();
			// Iterate through the results:
			for (int i = 0; i < hits.scoreDocs.Length; i++)
			{
				Lucene.Net.Document.Document hitDoc = isearcher.Doc(hits.scoreDocs[i].doc);
				NUnit.Framework.Assert.AreEqual(text, hitDoc.Get("fieldname"));
				//HM:revisit 
				//assert ireader.leaves().size() == 1;
				NumericDocValues dv = ((AtomicReader)ireader.Leaves()[0].Reader()).GetNumericDocValues
					("dv1");
				NUnit.Framework.Assert.AreEqual(5, dv.Get(hits.scoreDocs[i].doc));
				BinaryDocValues dv2 = ((AtomicReader)ireader.Leaves()[0].Reader()).GetBinaryDocValues
					("dv2");
				dv2.Get(hits.scoreDocs[i].doc, scratch);
				NUnit.Framework.Assert.AreEqual(new BytesRef("hello world"), scratch);
			}
			ireader.Close();
			directory.Close();
		}

		private sealed class _Lucene46Codec_84 : Lucene46Codec
		{
			public _Lucene46Codec_84(DocValuesFormat fast, DocValuesFormat slow)
			{
				this.fast = fast;
				this.slow = slow;
			}

			public override DocValuesFormat GetDocValuesFormatForField(string field)
			{
				if ("dv1".Equals(field))
				{
					return fast;
				}
				else
				{
					return slow;
				}
			}

			private readonly DocValuesFormat fast;

			private readonly DocValuesFormat slow;
		}
	}
}
