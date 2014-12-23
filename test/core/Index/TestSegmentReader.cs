/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestSegmentReader : LuceneTestCase
	{
		private Directory dir;

		private Lucene.Net.Document.Document testDoc = new Lucene.Net.Document.Document
			();

		private SegmentReader reader = null;

		//TODO: Setup the reader w/ multiple documents
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			dir = NewDirectory();
			DocHelper.SetupDoc(testDoc);
			SegmentCommitInfo info = DocHelper.WriteDoc(Random(), dir, testDoc);
			reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, IOContext
				.READ);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader.Close();
			dir.Close();
			base.TearDown();
		}

		public virtual void Test()
		{
			NUnit.Framework.Assert.IsTrue(dir != null);
			NUnit.Framework.Assert.IsTrue(reader != null);
			NUnit.Framework.Assert.IsTrue(DocHelper.nameValues.Count > 0);
			NUnit.Framework.Assert.IsTrue(DocHelper.NumFields(testDoc) == DocHelper.all.Count
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDocument()
		{
			NUnit.Framework.Assert.IsTrue(reader.NumDocs() == 1);
			NUnit.Framework.Assert.IsTrue(reader.MaxDoc() >= 1);
			Lucene.Net.Document.Document result = reader.Document(0);
			NUnit.Framework.Assert.IsTrue(result != null);
			//There are 2 unstored fields on the document that are not preserved across writing
			NUnit.Framework.Assert.IsTrue(DocHelper.NumFields(result) == DocHelper.NumFields(
				testDoc) - DocHelper.unstored.Count);
			IList<IndexableField> fields = result.GetFields();
			foreach (IndexableField field in fields)
			{
				NUnit.Framework.Assert.IsTrue(field != null);
				NUnit.Framework.Assert.IsTrue(DocHelper.nameValues.ContainsKey(field.Name()));
			}
		}

		public virtual void TestGetFieldNameVariations()
		{
			ICollection<string> allFieldNames = new HashSet<string>();
			ICollection<string> indexedFieldNames = new HashSet<string>();
			ICollection<string> notIndexedFieldNames = new HashSet<string>();
			ICollection<string> tvFieldNames = new HashSet<string>();
			ICollection<string> noTVFieldNames = new HashSet<string>();
			foreach (FieldInfo fieldInfo in reader.GetFieldInfos())
			{
				string name = fieldInfo.name;
				allFieldNames.AddItem(name);
				if (fieldInfo.IsIndexed())
				{
					indexedFieldNames.AddItem(name);
				}
				else
				{
					notIndexedFieldNames.AddItem(name);
				}
				if (fieldInfo.HasVectors())
				{
					tvFieldNames.AddItem(name);
				}
				else
				{
					if (fieldInfo.IsIndexed())
					{
						noTVFieldNames.AddItem(name);
					}
				}
			}
			NUnit.Framework.Assert.IsTrue(allFieldNames.Count == DocHelper.all.Count);
			foreach (string s in allFieldNames)
			{
				NUnit.Framework.Assert.IsTrue(DocHelper.nameValues.ContainsKey(s) == true || s.Equals
					(string.Empty));
			}
			NUnit.Framework.Assert.IsTrue(indexedFieldNames.Count == DocHelper.indexed.Count);
			foreach (string s_1 in indexedFieldNames)
			{
				NUnit.Framework.Assert.IsTrue(DocHelper.indexed.ContainsKey(s_1) == true || s_1.Equals
					(string.Empty));
			}
			NUnit.Framework.Assert.IsTrue(notIndexedFieldNames.Count == DocHelper.unindexed.Count
				);
			//Get all indexed fields that are storing term vectors
			NUnit.Framework.Assert.IsTrue(tvFieldNames.Count == DocHelper.termvector.Count);
			NUnit.Framework.Assert.IsTrue(noTVFieldNames.Count == DocHelper.notermvector.Count
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTerms()
		{
			Fields fields = MultiFields.GetFields(reader);
			foreach (string field in fields)
			{
				Terms terms = fields.Terms(field);
				NUnit.Framework.Assert.IsNotNull(terms);
				TermsEnum termsEnum = terms.Iterator(null);
				while (termsEnum.Next() != null)
				{
					BytesRef term = termsEnum.Term();
					NUnit.Framework.Assert.IsTrue(term != null);
					string fieldValue = (string)DocHelper.nameValues.Get(field);
					NUnit.Framework.Assert.IsTrue(fieldValue.IndexOf(term.Utf8ToString()) != -1);
				}
			}
			DocsEnum termDocs = TestUtil.Docs(Random(), reader, DocHelper.TEXT_FIELD_1_KEY, new 
				BytesRef("field"), MultiFields.GetLiveDocs(reader), null, 0);
			NUnit.Framework.Assert.IsTrue(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			termDocs = TestUtil.Docs(Random(), reader, DocHelper.NO_NORMS_KEY, new BytesRef(DocHelper
				.NO_NORMS_TEXT), MultiFields.GetLiveDocs(reader), null, 0);
			NUnit.Framework.Assert.IsTrue(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			DocsAndPositionsEnum positions = MultiFields.GetTermPositionsEnum(reader, MultiFields
				.GetLiveDocs(reader), DocHelper.TEXT_FIELD_1_KEY, new BytesRef("field"));
			// NOTE: prior rev of this test was failing to first
			// call next here:
			NUnit.Framework.Assert.IsTrue(positions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			NUnit.Framework.Assert.IsTrue(positions.DocID() == 0);
			NUnit.Framework.Assert.IsTrue(positions.NextPosition() >= 0);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestNorms()
		{
			//TODO: Not sure how these work/should be tested
			CheckNorms(reader);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void CheckNorms(AtomicReader reader)
		{
			// test omit norms
			for (int i = 0; i < DocHelper.fields.Length; i++)
			{
				IndexableField f = DocHelper.fields[i];
				if (f.FieldType().Indexed())
				{
					NUnit.Framework.Assert.AreEqual(reader.GetNormValues(f.Name()) != null, !f.FieldType
						().OmitNorms());
					NUnit.Framework.Assert.AreEqual(reader.GetNormValues(f.Name()) != null, !DocHelper
						.noNorms.ContainsKey(f.Name()));
					if (reader.GetNormValues(f.Name()) == null)
					{
						// test for norms of null
						NumericDocValues norms = MultiDocValues.GetNormValues(reader, f.Name());
						NUnit.Framework.Assert.IsNull(norms);
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTermVectors()
		{
			Terms result = reader.GetTermVectors(0).Terms(DocHelper.TEXT_FIELD_2_KEY);
			NUnit.Framework.Assert.IsNotNull(result);
			NUnit.Framework.Assert.AreEqual(3, result.Size());
			TermsEnum termsEnum = result.Iterator(null);
			while (termsEnum.Next() != null)
			{
				string term = termsEnum.Term().Utf8ToString();
				int freq = (int)termsEnum.TotalTermFreq();
				NUnit.Framework.Assert.IsTrue(DocHelper.FIELD_2_TEXT.IndexOf(term) != -1);
				NUnit.Framework.Assert.IsTrue(freq > 0);
			}
			Fields results = reader.GetTermVectors(0);
			NUnit.Framework.Assert.IsTrue(results != null);
			NUnit.Framework.Assert.AreEqual("We do not have 3 term freq vectors", 3, results.
				Size());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestOutOfBoundsAccess()
		{
			int numDocs = reader.MaxDoc();
			try
			{
				reader.Document(-1);
				NUnit.Framework.Assert.Fail();
			}
			catch (IndexOutOfRangeException)
			{
			}
			try
			{
				reader.GetTermVectors(-1);
				NUnit.Framework.Assert.Fail();
			}
			catch (IndexOutOfRangeException)
			{
			}
			try
			{
				reader.Document(numDocs);
				NUnit.Framework.Assert.Fail();
			}
			catch (IndexOutOfRangeException)
			{
			}
			try
			{
				reader.GetTermVectors(numDocs);
				NUnit.Framework.Assert.Fail();
			}
			catch (IndexOutOfRangeException)
			{
			}
		}
	}
}
