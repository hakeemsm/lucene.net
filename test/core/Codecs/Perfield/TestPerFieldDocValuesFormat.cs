using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Support;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Perfield
{
	/// <summary>Basic tests of PerFieldDocValuesFormat</summary>
	[TestFixture]
    public class TestPerFieldDocValuesFormat : BaseDocValuesFormatTestCase
	{
		private Codec codec;

		[SetUp]
		public override void SetUp()
		{
			codec = new RandomCodec(new Random(Random().NextInt(0,int.MaxValue)), new List<string>());
			base.SetUp();
		}

	    protected override void AddRandomFields(Documents.Document doc)
	    {
	        throw new NotImplementedException();
	    }

	    protected override Codec Codec
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
		[Test]
		public virtual void TestTwoFieldsTwoFormats()
		{
			Analyzer analyzer = new MockAnalyzer(Random());
			Directory directory = NewDirectory();
			// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			DocValuesFormat fast = DocValuesFormat.ForName("Lucene45");
			DocValuesFormat slow = DocValuesFormat.ForName("SimpleText");
			iwc.SetCodec(new AnonymousLucene46Codec(fast, slow));
			IndexWriter iwriter = new IndexWriter(directory, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
			string text = "This is the text to be indexed. " + longTerm;
			doc.Add(NewTextField("fieldname", text, Field.Store.YES));
			doc.Add(new NumericDocValuesField("dv1", 5));
			doc.Add(new BinaryDocValuesField("dv2", new BytesRef("hello world")));
			iwriter.AddDocument(doc);
			iwriter.Dispose();
			// Now search the index:
			IndexReader ireader = DirectoryReader.Open(directory);
			// read-only=true
			IndexSearcher isearcher = NewSearcher(ireader);
			AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname"
				, longTerm)), 1).TotalHits);
			Query query = new TermQuery(new Term("fieldname", "text"));
			TopDocs hits = isearcher.Search(query, null, 1);
			AreEqual(1, hits.TotalHits);
			BytesRef scratch = new BytesRef();
			// Iterate through the results:
			for (int i = 0; i < hits.ScoreDocs.Length; i++)
			{
				Lucene.Net.Documents.Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
				AreEqual(text, hitDoc.Get("fieldname"));
				//HM:revisit 
				//assert ireader.leaves().size() == 1;
				NumericDocValues dv = ((AtomicReader)ireader.Leaves[0].Reader).GetNumericDocValues
					("dv1");
				AreEqual(5, dv.Get(hits.ScoreDocs[i].Doc));
				BinaryDocValues dv2 = ((AtomicReader)ireader.Leaves[0].Reader).GetBinaryDocValues
					("dv2");
				dv2.Get(hits.ScoreDocs[i].Doc, scratch);
				AreEqual(new BytesRef("hello world"), scratch);
			}
			ireader.Dispose();
			directory.Dispose();
		}

		private sealed class AnonymousLucene46Codec : Lucene46Codec
		{
			public AnonymousLucene46Codec(DocValuesFormat fast, DocValuesFormat slow)
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
