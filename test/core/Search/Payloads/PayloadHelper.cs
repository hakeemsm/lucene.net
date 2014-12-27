/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Test.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Payloads;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Payloads
{
	public class PayloadHelper
	{
		private byte[] payloadField = new byte[] { 1 };

		private byte[] payloadMultiField1 = new byte[] { 2 };

		private byte[] payloadMultiField2 = new byte[] { 4 };

		public static readonly string NO_PAYLOAD_FIELD = "noPayloadField";

		public static readonly string MULTI_FIELD = "multiField";

		public static readonly string FIELD = "field";

		public IndexReader reader;

		public sealed class PayloadAnalyzer : Analyzer
		{
			public PayloadAnalyzer(PayloadHelper _enclosing) : base(Analyzer.PER_FIELD_REUSE_STRATEGY
				)
			{
				this._enclosing = _enclosing;
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
				return new Analyzer.TokenStreamComponents(result, new PayloadHelper.PayloadFilter
					(this, result, fieldName));
			}

			private readonly PayloadHelper _enclosing;
		}

		public sealed class PayloadFilter : TokenFilter
		{
			private readonly string fieldName;

			private int numSeen = 0;

			private readonly PayloadAttribute payloadAtt;

			public PayloadFilter(PayloadHelper _enclosing, TokenStream input, string fieldName
				) : base(input)
			{
				this._enclosing = _enclosing;
				this.fieldName = fieldName;
				this.payloadAtt = this.AddAttribute<PayloadAttribute>();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool IncrementToken()
			{
				if (this.input.IncrementToken())
				{
					if (this.fieldName.Equals(PayloadHelper.FIELD))
					{
						this.payloadAtt.SetPayload(new BytesRef(this._enclosing.payloadField));
					}
					else
					{
						if (this.fieldName.Equals(PayloadHelper.MULTI_FIELD))
						{
							if (this.numSeen % 2 == 0)
							{
								this.payloadAtt.SetPayload(new BytesRef(this._enclosing.payloadMultiField1));
							}
							else
							{
								this.payloadAtt.SetPayload(new BytesRef(this._enclosing.payloadMultiField2));
							}
							this.numSeen++;
						}
					}
					return true;
				}
				return false;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Reset()
			{
				base.Reset();
				this.numSeen = 0;
			}

			private readonly PayloadHelper _enclosing;
		}

		/// <summary>
		/// Sets up a RAMDirectory, and adds documents (using English.intToEnglish()) with two fields: field and multiField
		/// and analyzes them using the PayloadAnalyzer
		/// </summary>
		/// <param name="similarity">The Similarity class to use in the Searcher</param>
		/// <param name="numDocs">The num docs to add</param>
		/// <returns>An IndexSearcher</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual IndexSearcher SetUp(Random random, Similarity similarity, int numDocs
			)
		{
			// TODO: randomize
			Directory directory = new MockDirectoryWrapper(random, new RAMDirectory());
			PayloadHelper.PayloadAnalyzer analyzer = new PayloadHelper.PayloadAnalyzer(this);
			// TODO randomize this
			IndexWriter writer = new IndexWriter(directory, new IndexWriterConfig(LuceneTestCase
				.TEST_VERSION_CURRENT, analyzer).SetSimilarity(similarity));
			// writer.infoStream = System.out;
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new TextField(FIELD, English.IntToEnglish(i), Field.Store.YES));
				doc.Add(new TextField(MULTI_FIELD, English.IntToEnglish(i) + "  " + English.IntToEnglish
					(i), Field.Store.YES));
				doc.Add(new TextField(NO_PAYLOAD_FIELD, English.IntToEnglish(i), Field.Store.YES)
					);
				writer.AddDocument(doc);
			}
			reader = DirectoryReader.Open(writer, true);
			writer.Dispose();
			IndexSearcher searcher = LuceneTestCase.NewSearcher(reader);
			searcher.SetSimilarity(similarity);
			return searcher;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TearDown()
		{
			reader.Dispose();
		}
	}
}
