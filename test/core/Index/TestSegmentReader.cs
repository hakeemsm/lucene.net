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

namespace Lucene.Net.Test.Index
{
	public class TestSegmentReader : LuceneTestCase
	{
		private Directory dir;

		private Lucene.Net.Documents.Document testDoc = new Lucene.Net.Documents.Document
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
			reader.Dispose();
			dir.Dispose();
			base.TearDown();
		}

		public virtual void Test()
		{
			IsTrue(dir != null);
			IsTrue(reader != null);
			IsTrue(DocHelper.nameValues.Count > 0);
			IsTrue(DocHelper.NumFields(testDoc) == DocHelper.all.Count
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDocument()
		{
			IsTrue(reader.NumDocs == 1);
			IsTrue(reader.MaxDoc >= 1);
			Lucene.Net.Documents.Document result = reader.Document(0);
			IsTrue(result != null);
			//There are 2 unstored fields on the document that are not preserved across writing
			IsTrue(DocHelper.NumFields(result) == DocHelper.NumFields(
				testDoc) - DocHelper.unstored.Count);
			IList<IIndexableField> fields = result.GetFields();
			foreach (IIndexableField field in fields)
			{
				IsTrue(field != null);
				IsTrue(DocHelper.nameValues.ContainsKey(field.Name()));
			}
		}

		public virtual void TestGetFieldNameVariations()
		{
			ICollection<string> allFieldNames = new HashSet<string>();
			ICollection<string> indexedFieldNames = new HashSet<string>();
			ICollection<string> notIndexedFieldNames = new HashSet<string>();
			ICollection<string> tvFieldNames = new HashSet<string>();
			ICollection<string> noTVFieldNames = new HashSet<string>();
			foreach (FieldInfo fieldInfo in reader.FieldInfos)
			{
				string name = fieldInfo.name;
				allFieldNames.Add(name);
				if (fieldInfo.IsIndexed)
				{
					indexedFieldNames.Add(name);
				}
				else
				{
					notIndexedFieldNames.Add(name);
				}
				if (fieldInfo.HasVectors)
				{
					tvFieldNames.Add(name);
				}
				else
				{
					if (fieldInfo.IsIndexed)
					{
						noTVFieldNames.Add(name);
					}
				}
			}
			IsTrue(allFieldNames.Count == DocHelper.all.Count);
			foreach (string s in allFieldNames)
			{
				IsTrue(DocHelper.nameValues.ContainsKey(s) == true || s.Equals
					(string.Empty));
			}
			IsTrue(indexedFieldNames.Count == DocHelper.indexed.Count);
			foreach (string s_1 in indexedFieldNames)
			{
				IsTrue(DocHelper.indexed.ContainsKey(s_1) == true || s_1.Equals
					(string.Empty));
			}
			IsTrue(notIndexedFieldNames.Count == DocHelper.unindexed.Count
				);
			//Get all indexed fields that are storing term vectors
			IsTrue(tvFieldNames.Count == DocHelper.termvector.Count);
			IsTrue(noTVFieldNames.Count == DocHelper.notermvector.Count
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTerms()
		{
			Fields fields = MultiFields.GetFields(reader);
			foreach (string field in fields)
			{
				Terms terms = fields.Terms(field);
				IsNotNull(terms);
				TermsEnum termsEnum = terms.Iterator(null);
				while (termsEnum.Next() != null)
				{
					BytesRef term = termsEnum.Term();
					IsTrue(term != null);
					string fieldValue = (string)DocHelper.nameValues.Get(field);
					IsTrue(fieldValue.IndexOf(term.Utf8ToString()) != -1);
				}
			}
			DocsEnum termDocs = TestUtil.Docs(Random(), reader, DocHelper.TEXT_FIELD_1_KEY, new 
				BytesRef("field"), MultiFields.GetLiveDocs(reader), null, 0);
			IsTrue(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			termDocs = TestUtil.Docs(Random(), reader, DocHelper.NO_NORMS_KEY, new BytesRef(DocHelper
				.NO_NORMS_TEXT), MultiFields.GetLiveDocs(reader), null, 0);
			IsTrue(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			DocsAndPositionsEnum positions = MultiFields.GetTermPositionsEnum(reader, MultiFields
				.GetLiveDocs(reader), DocHelper.TEXT_FIELD_1_KEY, new BytesRef("field"));
			// NOTE: prior rev of this test was failing to first
			// call next here:
			IsTrue(positions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			IsTrue(positions.DocID == 0);
			IsTrue(positions.NextPosition() >= 0);
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
				IIndexableField f = DocHelper.fields[i];
				if (f.FieldType().Indexed())
				{
					AreEqual(reader.GetNormValues(f.Name()) != null, !f.FieldType
						().OmitsNorms());
					AreEqual(reader.GetNormValues(f.Name()) != null, !DocHelper
						.noNorms.ContainsKey(f.Name()));
					if (reader.GetNormValues(f.Name()) == null)
					{
						// test for norms of null
						NumericDocValues norms = MultiDocValues.GetNormValues(reader, f.Name());
						IsNull(norms);
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTermVectors()
		{
			Terms result = reader.GetTermVectors(0).Terms(DocHelper.TEXT_FIELD_2_KEY);
			IsNotNull(result);
			AreEqual(3, result.Size());
			TermsEnum termsEnum = result.Iterator(null);
			while (termsEnum.Next() != null)
			{
				string term = termsEnum.Term().Utf8ToString();
				int freq = (int)termsEnum.TotalTermFreq;
				IsTrue(DocHelper.FIELD_2_TEXT.IndexOf(term) != -1);
				IsTrue(freq > 0);
			}
			Fields results = reader.GetTermVectors(0);
			IsTrue(results != null);
			AreEqual("We do not have 3 term freq vectors", 3, results.
				Size());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestOutOfBoundsAccess()
		{
			int numDocs = reader.MaxDoc;
			try
			{
				reader.Document(-1);
				Fail();
			}
			catch (IndexOutOfRangeException)
			{
			}
			try
			{
				reader.GetTermVectors(-1);
				Fail();
			}
			catch (IndexOutOfRangeException)
			{
			}
			try
			{
				reader.Document(numDocs);
				Fail();
			}
			catch (IndexOutOfRangeException)
			{
			}
			try
			{
				reader.GetTermVectors(numDocs);
				Fail();
			}
			catch (IndexOutOfRangeException)
			{
			}
		}
	}
}
