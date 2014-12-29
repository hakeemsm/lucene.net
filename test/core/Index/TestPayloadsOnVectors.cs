/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Test.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestPayloadsOnVectors : LuceneTestCase
	{
		/// <summary>some docs have payload att, some not</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixupDocs()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetMergePolicy(NewLogMergePolicy());
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, iwc);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorPayloads = true;
			customType.SetStoreTermVectorOffsets(Random().NextBoolean());
			Field field = new Field("field", string.Empty, customType);
			TokenStream ts = new MockTokenizer(new StringReader("here we go"), MockTokenizer.
				WHITESPACE, true);
			IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
			field.SetTokenStream(ts);
			doc.Add(field);
			writer.AddDocument(doc);
			Token withPayload = new Token("withPayload", 0, 11);
			withPayload.Payload = (new BytesRef("test"));
			ts = new CannedTokenStream(withPayload);
			IsTrue(ts.HasAttribute(typeof(PayloadAttribute)));
			field.SetTokenStream(ts);
			writer.AddDocument(doc);
			ts = new MockTokenizer(new StringReader("another"), MockTokenizer.WHITESPACE, true
				);
			IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
			field.SetTokenStream(ts);
			writer.AddDocument(doc);
			DirectoryReader reader = writer.Reader;
			Terms terms = reader.GetTermVector(1, "field");
			//HM:revisit 
			//assert terms != null;
			TermsEnum termsEnum = terms.Iterator(null);
			IsTrue(termsEnum.SeekExact(new BytesRef("withPayload")));
			DocsAndPositionsEnum de = termsEnum.DocsAndPositions(null, null);
			AreEqual(0, de.NextDoc());
			AreEqual(0, de.NextPosition());
			AreEqual(new BytesRef("test"), de.Payload);
			writer.Dispose();
			reader.Dispose();
			dir.Dispose();
		}

		/// <summary>some field instances have payload att, some not</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMixupMultiValued()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorPayloads = true;
			customType.SetStoreTermVectorOffsets(Random().NextBoolean());
			Field field = new Field("field", string.Empty, customType);
			TokenStream ts = new MockTokenizer(new StringReader("here we go"), MockTokenizer.
				WHITESPACE, true);
			IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
			field.SetTokenStream(ts);
			doc.Add(field);
			Field field2 = new Field("field", string.Empty, customType);
			Token withPayload = new Token("withPayload", 0, 11);
			withPayload.Payload = (new BytesRef("test"));
			ts = new CannedTokenStream(withPayload);
			IsTrue(ts.HasAttribute(typeof(PayloadAttribute)));
			field2.SetTokenStream(ts);
			doc.Add(field2);
			Field field3 = new Field("field", string.Empty, customType);
			ts = new MockTokenizer(new StringReader("nopayload"), MockTokenizer.WHITESPACE, true
				);
			IsFalse(ts.HasAttribute(typeof(PayloadAttribute)));
			field3.SetTokenStream(ts);
			doc.Add(field3);
			writer.AddDocument(doc);
			DirectoryReader reader = writer.Reader;
			Terms terms = reader.GetTermVector(0, "field");
			//HM:revisit 
			//assert terms != null;
			TermsEnum termsEnum = terms.Iterator(null);
			IsTrue(termsEnum.SeekExact(new BytesRef("withPayload")));
			DocsAndPositionsEnum de = termsEnum.DocsAndPositions(null, null);
			AreEqual(0, de.NextDoc());
			AreEqual(3, de.NextPosition());
			AreEqual(new BytesRef("test"), de.Payload);
			writer.Dispose();
			reader.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPayloadsWithoutPositions()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = (false);
			customType.StoreTermVectorPayloads = true;
			customType.SetStoreTermVectorOffsets(Random().NextBoolean());
			doc.Add(new Field("field", "foo", customType));
			try
			{
				writer.AddDocument(doc);
				Fail();
			}
			catch (ArgumentException)
			{
			}
			// expected
			writer.Dispose();
			dir.Dispose();
		}
	}
}
