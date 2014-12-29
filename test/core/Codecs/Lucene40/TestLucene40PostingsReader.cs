using System.Collections.Generic;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Lucene40
{
    [TestFixture]
	public class TestLucene40PostingsReader : LuceneTestCase
	{
		internal static readonly string[] terms = new string[100];

		static TestLucene40PostingsReader()
		{
			for (int i = 0; i < terms.Length; i++)
			{
				terms[i] = (i + 1).ToString();
			}
		}

		[SetUp]
		public void Setup()
		{
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		}

		// explicitly instantiates ancient codec
		/// <summary>tests terms with different probabilities of being in the document.</summary>
		[Test]
		public virtual void TestPostings()
		{
			Directory dir = NewFSDirectory(CreateTempDir("postings"));
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
			iwc.SetCodec(Codec.ForName("Lucene40"));
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
			var doc = new Lucene.Net.Documents.Document();
			// id field
			FieldType idType = new FieldType(StringField.TYPE_NOT_STORED) {StoreTermVectors = true};
		    Field idField = new Field("id", string.Empty, idType);
			doc.Add(idField);
			// title field: short text field
			var titleType = new FieldType(TextField.TYPE_NOT_STORED)
			{
			    StoreTermVectors = true,
			    StoreTermVectorPositions = true,
			    StoreTermVectorOffsets = true,
			    IndexOptions = IndexOptions()
			};
		    Field titleField = new Field("title", string.Empty, titleType);
			doc.Add(titleField);
			// body field: long text field
			var bodyType = new FieldType(TextField.TYPE_NOT_STORED)
			{
			    StoreTermVectors = true,
			    StoreTermVectorPositions = true,
			    StoreTermVectorOffsets = true,
			    IndexOptions = IndexOptions()
			};
		    Field bodyField = new Field("body", string.Empty, bodyType);
			doc.Add(bodyField);
			int numDocs = AtLeast(1000);
			for (int i = 0; i < numDocs; i++)
			{
				idField.StringValue = i.ToString();
				titleField.StringValue = FieldValue(1);
				bodyField.StringValue = FieldValue(3);
				iw.AddDocument(doc);
				if (Random().Next(20) == 0)
				{
					iw.DeleteDocuments(new Term("id", i.ToString()));
				}
			}
			if (Random().NextBoolean())
			{
				// delete 1-100% of docs
				iw.DeleteDocuments(new Term("title", terms[Random().Next(terms.Length)]));
			}
			iw.Dispose();
			dir.Dispose();
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
			var shuffled = new List<string>();
			StringBuilder sb = new StringBuilder();
			int i = Random().Next(terms.Length);
			while (i < terms.Length)
			{
				int tf = TestUtil.NextInt(Random(), 1, maxTF);
				for (int j = 0; j < tf; j++)
				{
					shuffled.Add(terms[i]);
				}
				i++;
			}
			shuffled.Shuffle(Random());
			foreach (string term in shuffled)
			{
				sb.Append(term);
				sb.Append(' ');
			}
			return sb.ToString();
		}
	}
}
