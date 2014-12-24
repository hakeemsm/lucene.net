/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>Tests lazy skipping on the proximity file.</summary>
	/// <remarks>Tests lazy skipping on the proximity file.</remarks>
	public class TestLazyProxSkipping : LuceneTestCase
	{
		private IndexSearcher searcher;

		private int seeksCounter = 0;

		private string field = "tokens";

		private string term1 = "xx";

		private string term2 = "yy";

		private string term3 = "zz";

		private class SeekCountingDirectory : MockDirectoryWrapper
		{
			protected SeekCountingDirectory(TestLazyProxSkipping _enclosing, Directory delegate_
				) : base(LuceneTestCase.Random(), delegate_)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IndexInput OpenInput(string name, IOContext context)
			{
				IndexInput ii = base.OpenInput(name, context);
				if (name.EndsWith(".prx") || name.EndsWith(".pos"))
				{
					// we decorate the proxStream with a wrapper class that allows to count the number of calls of seek()
					ii = new TestLazyProxSkipping.SeeksCountingStream(this, ii);
				}
				return ii;
			}

			private readonly TestLazyProxSkipping _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CreateIndex(int numHits)
		{
			int numDocs = 500;
			Analyzer analyzer = new _Analyzer_71();
			Directory directory = new TestLazyProxSkipping.SeekCountingDirectory(this, new RAMDirectory
				());
			// note: test explicitly disables payloads
			IndexWriter writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(10)).SetMergePolicy(NewLogMergePolicy
				(false)));
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				string content;
				if (i % (numDocs / numHits) == 0)
				{
					// add a document that matches the query "term1 term2"
					content = this.term1 + " " + this.term2;
				}
				else
				{
					if (i % 15 == 0)
					{
						// add a document that only contains term1
						content = this.term1 + " " + this.term1;
					}
					else
					{
						// add a document that contains term2 but not term 1
						content = this.term3 + " " + this.term2;
					}
				}
				doc.Add(NewTextField(this.field, content, Field.Store.YES));
				writer.AddDocument(doc);
			}
			// make sure the index has only a single segment
			writer.ForceMerge(1);
			writer.Close();
			SegmentReader reader = GetOnlySegmentReader(DirectoryReader.Open(directory));
			this.searcher = NewSearcher(reader);
		}

		private sealed class _Analyzer_71 : Analyzer
		{
			public _Analyzer_71()
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				return new Analyzer.TokenStreamComponents(new MockTokenizer(reader, MockTokenizer
					.WHITESPACE, true));
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ScoreDoc[] Search()
		{
			// create PhraseQuery "term1 term2" and search
			PhraseQuery pq = new PhraseQuery();
			pq.Add(new Term(this.field, this.term1));
			pq.Add(new Term(this.field, this.term2));
			return this.searcher.Search(pq, null, 1000).scoreDocs;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void PerformTest(int numHits)
		{
			CreateIndex(numHits);
			this.seeksCounter = 0;
			ScoreDoc[] hits = Search();
			// verify that the right number of docs was found
			AreEqual(numHits, hits.Length);
			// check if the number of calls of seek() does not exceed the number of hits
			IsTrue(this.seeksCounter > 0);
			IsTrue("seeksCounter=" + this.seeksCounter + " numHits=" +
				 numHits, this.seeksCounter <= numHits + 1);
			searcher.GetIndexReader().Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLazySkipping()
		{
			string fieldFormat = TestUtil.GetPostingsFormat(this.field);
			AssumeFalse("This test cannot run with Memory postings format", fieldFormat.Equals
				("Memory"));
			AssumeFalse("This test cannot run with Direct postings format", fieldFormat.Equals
				("Direct"));
			AssumeFalse("This test cannot run with SimpleText postings format", fieldFormat.Equals
				("SimpleText"));
			// test whether only the minimum amount of seeks()
			// are performed
			PerformTest(5);
			PerformTest(10);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSeek()
		{
			Directory directory = NewDirectory();
			IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			for (int i = 0; i < 10; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewTextField(this.field, "a b", Field.Store.YES));
				writer.AddDocument(doc);
			}
			writer.Close();
			IndexReader reader = DirectoryReader.Open(directory);
			DocsAndPositionsEnum tp = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs
				(reader), this.field, new BytesRef("b"));
			for (int i_1 = 0; i_1 < 10; i_1++)
			{
				tp.NextDoc();
				AreEqual(tp.DocID, i_1);
				AreEqual(tp.NextPosition(), 1);
			}
			tp = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), this
				.field, new BytesRef("a"));
			for (int i_2 = 0; i_2 < 10; i_2++)
			{
				tp.NextDoc();
				AreEqual(tp.DocID, i_2);
				AreEqual(tp.NextPosition(), 0);
			}
			reader.Close();
			directory.Close();
		}

		internal class SeeksCountingStream : IndexInput
		{
			private IndexInput input;

			internal SeeksCountingStream(TestLazyProxSkipping _enclosing, IndexInput input) : 
				base("SeekCountingStream(" + input + ")")
			{
				this._enclosing = _enclosing;
				// Simply extends IndexInput in a way that we are able to count the number
				// of invocations of seek()
				this.input = input;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override byte ReadByte()
			{
				return this.input.ReadByte();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void ReadBytes(byte[] b, int offset, int len)
			{
				this.input.ReadBytes(b, offset, len);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				this.input.Close();
			}

			public override long FilePointer
			{
				return this.input.FilePointer;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Seek(long pos)
			{
				this._enclosing.seeksCounter++;
				this.input.Seek(pos);
			}

			public override long Length()
			{
				return this.input.Length();
			}

			public override DataInput Clone()
			{
				return new TestLazyProxSkipping.SeeksCountingStream(this, ((IndexInput)this.input
					.Clone()));
			}

			private readonly TestLazyProxSkipping _enclosing;
		}
	}
}
