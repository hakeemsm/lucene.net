/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Compressing;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Compressing
{
	public class TestCompressingTermVectorsFormat : BaseTermVectorsFormatTestCase
	{
		// give it a chance to test various compression modes with different chunk sizes
		protected override Codec GetCodec()
		{
			return CompressingCodec.RandomInstance(Random());
		}

		// https://issues.apache.org/jira/browse/LUCENE-5156
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoOrds()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetStoreTermVectors(true);
			doc.Add(new Field("foo", "this is a test", ft));
			iw.AddDocument(doc);
			AtomicReader ir = GetOnlySegmentReader(iw.GetReader());
			Terms terms = ir.GetTermVector(0, "foo");
			NUnit.Framework.Assert.IsNotNull(terms);
			TermsEnum termsEnum = terms.Iterator(null);
			NUnit.Framework.Assert.AreEqual(TermsEnum.SeekStatus.FOUND, termsEnum.SeekCeil(new 
				BytesRef("this")));
			try
			{
				termsEnum.Ord();
				NUnit.Framework.Assert.Fail();
			}
			catch (NotSupportedException)
			{
			}
			// expected exception
			try
			{
				termsEnum.SeekExact(0);
				NUnit.Framework.Assert.Fail();
			}
			catch (NotSupportedException)
			{
			}
			// expected exception
			ir.Close();
			iw.Close();
			dir.Close();
		}
	}
}
