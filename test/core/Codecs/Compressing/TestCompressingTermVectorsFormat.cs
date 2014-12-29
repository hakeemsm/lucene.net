using System;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework.Codecs.Compressing;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Compressing
{
    [TestFixture]
	public class TestCompressingTermVectorsFormat : BaseTermVectorsFormatTestCase
	{
		// give it a chance to test various compression modes with different chunk sizes
		protected override Codec Codec
		{
		    get { return CompressingCodec.RandomInstance(Random()); }
		}

	    protected override void AddRandomFields(Documents.Document doc)
	    {
	        throw new NotImplementedException();
	    }

	    // https://issues.apache.org/jira/browse/LUCENE-5156
		[Test]
		public virtual void TestNoOrds()
		{
			Directory dir = NewDirectory();
			var iw = new RandomIndexWriter(Random(), dir);
			var doc = new Lucene.Net.Documents.Document();
			var ft = new FieldType(TextField.TYPE_NOT_STORED) {StoreTermVectors = true};
		    doc.Add(new Field("foo", "this is a test", ft));
			iw.AddDocument(doc);
			AtomicReader ir = GetOnlySegmentReader(iw.Reader);
			Terms terms = ir.GetTermVector(0, "foo");
			IsNotNull(terms);
			TermsEnum termsEnum = terms.Iterator(null);
			AreEqual(TermsEnum.SeekStatus.FOUND, termsEnum.SeekCeil(new BytesRef("this")));
			try
			{
			    var ord = termsEnum.Ord;
			    Fail();
			}
			catch (NotSupportedException)
			{
			}
			// expected exception
			try
			{
				termsEnum.SeekExact(0);
				Fail();
			}
			catch (NotSupportedException)
			{
			}
			// expected exception
			ir.Dispose();
			iw.Close();
			dir.Dispose();
		}
	}
}
