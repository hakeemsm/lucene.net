using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;

namespace Lucene.Net.TestFramework.Index
{
    public class DocHelper
	{
		public static readonly FieldType customType;

		public static readonly string FIELD_1_TEXT = "field one text";

		public static readonly string TEXT_FIELD_1_KEY = "textField1";

		public static Field textField1;

		

		public static readonly FieldType customType2;

		public static readonly string FIELD_2_TEXT = "field field field two text";

		public static readonly int[] FIELD_2_FREQS = new int[] { 3, 1, 1 };

		public static readonly string TEXT_FIELD_2_KEY = "textField2";

		public static Field textField2;

		

		public static readonly FieldType customType3;

		public static readonly string FIELD_3_TEXT = "aaaNoNorms aaaNoNorms bbbNoNorms";

		public static readonly string TEXT_FIELD_3_KEY = "textField3";

		public static Field textField3;

		

		public static readonly string KEYWORD_TEXT = "Keyword";

		public static readonly string KEYWORD_FIELD_KEY = "keyField";

		public static Field keyField;

		

		public static readonly FieldType customType5;

		public static readonly string NO_NORMS_TEXT = "omitNormsText";

		public static readonly string NO_NORMS_KEY = "omitNorms";

		public static Field noNormsField;

		

		public static readonly FieldType customType6;

		public static readonly string NO_TF_TEXT = "analyzed with no tf and positions";

		public static readonly string NO_TF_KEY = "omitTermFreqAndPositions";

		public static Field noTFField;

		

		public static readonly FieldType customType7;

		public static readonly string UNINDEXED_FIELD_TEXT = "unindexed field text";

		public static readonly string UNINDEXED_FIELD_KEY = "unIndField";

		public static Field unIndField;

		

		public static readonly string UNSTORED_1_FIELD_TEXT = "unstored field text";

		public static readonly string UNSTORED_FIELD_1_KEY = "unStoredField1";

		public static Field unStoredField1 = new TextField(UNSTORED_FIELD_1_KEY, UNSTORED_1_FIELD_TEXT
			, Field.Store.NO);

		public static readonly FieldType customType8;

		public static readonly string UNSTORED_2_FIELD_TEXT = "unstored field text";

		public static readonly string UNSTORED_FIELD_2_KEY = "unStoredField2";

		public static Field unStoredField2;

		

		public static readonly string LAZY_FIELD_BINARY_KEY = "lazyFieldBinary";

		public static byte[] LAZY_FIELD_BINARY_BYTES;

		public static Field lazyFieldBinary;

		public static readonly string LAZY_FIELD_KEY = "lazyField";

		public static readonly string LAZY_FIELD_TEXT = "These are some field bytes";

		public static Field lazyField = new Field(LAZY_FIELD_KEY, LAZY_FIELD_TEXT, customType);

		public static readonly string LARGE_LAZY_FIELD_KEY = "largeLazyField";

		public static string LARGE_LAZY_FIELD_TEXT;

		public static Field largeLazyField;

		public static readonly string FIELD_UTF1_TEXT = "field one \u4e00text";

		public static readonly string TEXT_FIELD_UTF1_KEY = "textField1Utf8";

		public static Field textUtfField1 = new Field(TEXT_FIELD_UTF1_KEY, FIELD_UTF1_TEXT
			, customType);

		public static readonly string FIELD_UTF2_TEXT = "field field field \u4e00two text";

		public static readonly int[] FIELD_UTF2_FREQS = new int[] { 3, 1, 1 };

		public static readonly string TEXT_FIELD_UTF2_KEY = "textField2Utf8";

		public static Field textUtfField2 = new Field(TEXT_FIELD_UTF2_KEY, FIELD_UTF2_TEXT, customType2);

		public static IDictionary<string, object> nameValues = null;

		public static Field[] fields = new Field[] { textField1, textField2, textField3, 
			keyField, noNormsField, noTFField, unIndField, unStoredField1, unStoredField2, textUtfField1
			, textUtfField2, lazyField, lazyFieldBinary, largeLazyField };

		public static IDictionary<string, IIndexableField> all = new Dictionary<string, IIndexableField>();

		public static IDictionary<string, IIndexableField> indexed = new Dictionary<string
			, IIndexableField>();

		public static IDictionary<string, IIndexableField> stored = new Dictionary<string, 
			IIndexableField>();

		public static IDictionary<string, IIndexableField> unstored = new Dictionary<string
			, IIndexableField>();

		public static IDictionary<string, IIndexableField> unindexed = new Dictionary<string
			, IIndexableField>();

		public static IDictionary<string, IIndexableField> termvector = new Dictionary<string
			, IIndexableField>();

		public static IDictionary<string, IIndexableField> notermvector = new Dictionary<string
			, IIndexableField>();

		public static IDictionary<string, IIndexableField> lazy = new Dictionary<string, IIndexableField
			>();

		public static IDictionary<string, IIndexableField> noNorms = new Dictionary<string
			, IIndexableField>();

		public static IDictionary<string, IIndexableField> noTf = new Dictionary<string, IIndexableField
			>();

		static DocHelper()
		{

            customType = new FieldType(TextField.TYPE_STORED);
            textField1 = new Field(TEXT_FIELD_1_KEY, FIELD_1_TEXT, customType);
            //Fields will be lexicographically sorted.  So, the order is: field, text, two
            customType2 = new FieldType(TextField.TYPE_STORED);
            customType2.StoreTermVectors = (true);
            customType2.StoreTermVectorPositions = (true);
            customType2.StoreTermVectorOffsets = (true);
            textField2 = new Field(TEXT_FIELD_2_KEY, FIELD_2_TEXT, customType2);
            customType3 = new FieldType(TextField.TYPE_STORED);
            customType3.OmitNorms = (true);
            textField3 = new Field(TEXT_FIELD_3_KEY, FIELD_3_TEXT, customType3);
            keyField = new StringField(KEYWORD_FIELD_KEY, KEYWORD_TEXT, Field.Store.YES);
            customType5 = new FieldType(TextField.TYPE_STORED);
            customType5.OmitNorms = (true);
            customType5.Tokenized = (false);
            noNormsField = new Field(NO_NORMS_KEY, NO_NORMS_TEXT, customType5);
            customType6 = new FieldType(TextField.TYPE_STORED);
            customType6.IndexOptions = (FieldInfo.IndexOptions.DOCS_ONLY);
            noTFField = new Field(NO_TF_KEY, NO_TF_TEXT, customType6);
            customType7 = new FieldType();
            customType7.Stored =(true);
            unIndField = new Field(UNINDEXED_FIELD_KEY, UNINDEXED_FIELD_TEXT, customType7);
            customType8 = new FieldType(TextField.TYPE_NOT_STORED);
            customType8.StoreTermVectors = (true);
            unStoredField2 = new Field(UNSTORED_FIELD_2_KEY, UNSTORED_2_FIELD_TEXT, customType8);

			//From Issue 509
			//Fields will be lexicographically sorted.  So, the order is: field, text, two
			// ordered list of all the fields...
			// could use LinkedHashMap for this purpose if Java1.4 is OK
			//placeholder for binary field, since this is null.  It must be second to last.
			//placeholder for large field, since this is null.  It must always be last
			//Initialize the large Lazy Field
			StringBuilder buffer = new StringBuilder();
			for (int i = 0; i < 10000; i++)
			{
				buffer.Append("Lazily loading lengths of language in lieu of laughing ");
			}
			try
			{
				LAZY_FIELD_BINARY_BYTES = Sharpen.Runtime.GetBytesForString("These are some binary field bytes"
					, "UTF8");
			}
			catch (UnsupportedEncodingException)
			{
			}
			lazyFieldBinary = new StoredField(LAZY_FIELD_BINARY_KEY, LAZY_FIELD_BINARY_BYTES);
			fields[fields.Length - 2] = lazyFieldBinary;
			LARGE_LAZY_FIELD_TEXT = buffer.ToString();
			largeLazyField = new Field(LARGE_LAZY_FIELD_KEY, LARGE_LAZY_FIELD_TEXT, customType
				);
			fields[fields.Length - 1] = largeLazyField;
			for (int i_1 = 0; i_1 < fields.Length; i_1++)
			{
				IIndexableField f = fields[i_1];
				Add(all, f);
				if (f.FieldType().Indexed())
				{
					Add(indexed, f);
				}
				else
				{
					Add(unindexed, f);
				}
				if (f.FieldType().StoreTermVectors())
				{
					Add(termvector, f);
				}
				if (f.FieldType().Indexed() && !f.FieldType().StoreTermVectors())
				{
					Add(notermvector, f);
				}
				if (f.FieldType().Stored())
				{
					Add(stored, f);
				}
				else
				{
					Add(unstored, f);
				}
				if (f.FieldType().IndexOptions() == FieldInfo.IndexOptions.DOCS_ONLY)
				{
					Add(noTf, f);
				}
				if (f.FieldType().OmitNorms())
				{
					Add(noNorms, f);
				}
				if (f.FieldType().IndexOptions() == FieldInfo.IndexOptions.DOCS_ONLY)
				{
					Add(noTf, f);
				}
			}
		}

		//if (f.isLazy()) add(lazy, f);
		private static void Add(IDictionary<string, IIndexableField> map, IIndexableField field
			)
		{
			map.Put(field.Name(), field);
		}

		
		{
			nameValues = new Dictionary<string, object>();
			nameValues.Put(TEXT_FIELD_1_KEY, FIELD_1_TEXT);
			nameValues.Put(TEXT_FIELD_2_KEY, FIELD_2_TEXT);
			nameValues.Put(TEXT_FIELD_3_KEY, FIELD_3_TEXT);
			nameValues.Put(KEYWORD_FIELD_KEY, KEYWORD_TEXT);
			nameValues.Put(NO_NORMS_KEY, NO_NORMS_TEXT);
			nameValues.Put(NO_TF_KEY, NO_TF_TEXT);
			nameValues.Put(UNINDEXED_FIELD_KEY, UNINDEXED_FIELD_TEXT);
			nameValues.Put(UNSTORED_FIELD_1_KEY, UNSTORED_1_FIELD_TEXT);
			nameValues.Put(UNSTORED_FIELD_2_KEY, UNSTORED_2_FIELD_TEXT);
			nameValues.Put(LAZY_FIELD_KEY, LAZY_FIELD_TEXT);
			nameValues.Put(LAZY_FIELD_BINARY_KEY, LAZY_FIELD_BINARY_BYTES);
			nameValues.Put(LARGE_LAZY_FIELD_KEY, LARGE_LAZY_FIELD_TEXT);
			nameValues.Put(TEXT_FIELD_UTF1_KEY, FIELD_UTF1_TEXT);
			nameValues.Put(TEXT_FIELD_UTF2_KEY, FIELD_UTF2_TEXT);
		}

		/// <summary>Adds the fields above to a document</summary>
		/// <param name="doc">The document to write</param>
		public static void SetupDoc(Lucene.Net.Documents.Document doc)
		{
			for (int i = 0; i < fields.Length; i++)
			{
				doc.Add(fields[i]);
			}
		}

		/// <summary>
		/// Writes the document to the directory using a segment
		/// named "test"; returns the SegmentInfo describing the new
		/// segment
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static SegmentCommitInfo WriteDoc(Random random, Lucene.Net.Store.Directory dir, Lucene.Net.Documents.Document doc)
		{
			return WriteDoc(random, dir, new MockAnalyzer(random, MockTokenizer.WHITESPACE, false
				), null, doc);
		}

		/// <summary>
		/// Writes the document to the directory using the analyzer
		/// and the similarity score; returns the SegmentInfo
		/// describing the new segment
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static SegmentCommitInfo WriteDoc(Random random, Lucene.Net.Store.Directory dir, Analyzer analyzer
			, Similarity similarity, Lucene.Net.Documents.Document doc)
		{
			IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT
				, analyzer).SetSimilarity(similarity ?? IndexSearcher.DefaultSimilarity));
			//writer.setNoCFSRatio(0.0);
			writer.AddDocument(doc);
			writer.Commit();
			SegmentCommitInfo info = writer.NewestSegment;
			writer.Dispose();
			return info;
		}

		public static int NumFields(Lucene.Net.Documents.Document doc)
		{
			return doc.GetFields().Count;
		}

		public static Lucene.Net.Documents.Document CreateDocument(int n, string indexName
			, int numFields)
		{
			StringBuilder sb = new StringBuilder();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.StoreTermVectors = (true);
			customType.StoreTermVectorPositions = (true);
			customType.StoreTermVectorOffsets = (true);
			FieldType customType1 = new FieldType(StringField.TYPE_STORED);
			customType1.StoreTermVectors = (true);
			customType1.StoreTermVectorPositions = (true);
			customType1.StoreTermVectorOffsets = (true);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new Field("id", Sharpen.Extensions.ToString(n), customType1));
			doc.Add(new Field("indexname", indexName, customType1));
			sb.Append("a");
			sb.Append(n);
			doc.Add(new Field("field1", sb.ToString(), customType));
			sb.Append(" b");
			sb.Append(n);
			for (int i = 1; i < numFields; i++)
			{
				doc.Add(new Field("field" + (i + 1), sb.ToString(), customType));
			}
			return doc;
		}
	}
}
