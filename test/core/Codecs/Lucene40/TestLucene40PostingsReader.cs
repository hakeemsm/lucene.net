/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene40
{
	public class TestLucene40PostingsReader : LuceneTestCase
	{
		internal static readonly string terms = new string[100];

		static TestLucene40PostingsReader()
		{
			for (int i = 0; i < terms.Length; i++)
			{
				terms[i] = Sharpen.Extensions.ToString(i + 1);
			}
		}

		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		}

		// explicitly instantiates ancient codec
		/// <summary>tests terms with different probabilities of being in the document.</summary>
		/// <remarks>
		/// tests terms with different probabilities of being in the document.
		/// depends heavily on term vectors cross-check at checkIndex
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestPostings()
		{
			Directory dir = NewFSDirectory(CreateTempDir("postings"));
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetCodec(Codec.ForName("Lucene40"));
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			// id field
			FieldType idType = new FieldType(StringField.TYPE_NOT_STORED);
			idType.SetStoreTermVectors(true);
			Field idField = new Field("id", string.Empty, idType);
			doc.Add(idField);
			// title field: short text field
			FieldType titleType = new FieldType(TextField.TYPE_NOT_STORED);
			titleType.SetStoreTermVectors(true);
			titleType.SetStoreTermVectorPositions(true);
			titleType.SetStoreTermVectorOffsets(true);
			titleType.SetIndexOptions(IndexOptions());
			Field titleField = new Field("title", string.Empty, titleType);
			doc.Add(titleField);
			// body field: long text field
			FieldType bodyType = new FieldType(TextField.TYPE_NOT_STORED);
			bodyType.SetStoreTermVectors(true);
			bodyType.SetStoreTermVectorPositions(true);
			bodyType.SetStoreTermVectorOffsets(true);
			bodyType.SetIndexOptions(IndexOptions());
			Field bodyField = new Field("body", string.Empty, bodyType);
			doc.Add(bodyField);
			int numDocs = AtLeast(1000);
			for (int i = 0; i < numDocs; i++)
			{
				idField.SetStringValue(Sharpen.Extensions.ToString(i));
				titleField.SetStringValue(FieldValue(1));
				bodyField.SetStringValue(FieldValue(3));
				iw.AddDocument(doc);
				if (Random().Next(20) == 0)
				{
					iw.DeleteDocuments(new Term("id", Sharpen.Extensions.ToString(i)));
				}
			}
			if (Random().NextBoolean())
			{
				// delete 1-100% of docs
				iw.DeleteDocuments(new Term("title", terms[Random().Next(terms.Length)]));
			}
			iw.Close();
			dir.Close();
		}

		// checkindex
		internal virtual FieldInfo.IndexOptions IndexOptions()
		{
			switch (Random().Next(4))
			{
				case 0:
				{
					return FieldInfo.IndexOptions.DOCS_ONLY;
				}

				case 1:
				{
					return FieldInfo.IndexOptions.DOCS_AND_FREQS;
				}

				case 2:
				{
					return FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
				}

				default:
				{
					return FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
					break;
				}
			}
		}

		internal virtual string FieldValue(int maxTF)
		{
			AList<string> shuffled = new AList<string>();
			StringBuilder sb = new StringBuilder();
			int i = Random().Next(terms.Length);
			while (i < terms.Length)
			{
				int tf = TestUtil.NextInt(Random(), 1, maxTF);
				for (int j = 0; j < tf; j++)
				{
					shuffled.AddItem(terms[i]);
				}
				i++;
			}
			Collections.Shuffle(shuffled, Random());
			foreach (string term in shuffled)
			{
				sb.Append(term);
				sb.Append(' ');
			}
			return sb.ToString();
		}
	}
}
