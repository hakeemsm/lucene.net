using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using Lucene.Net.Store;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;


namespace Lucene.Net.Test.Index
{
	internal class RepeatingTokenizer : Tokenizer
	{
		private readonly Random random;

		private readonly float percentDocs;

		private readonly int maxTF;

		private int num;

		internal CharTermAttribute termAtt;

		internal string value;

		public RepeatingTokenizer(TextReader reader, string val, Random random, float percentDocs
			, int maxTF) : base(reader)
		{
			this.value = val;
			this.random = random;
			this.percentDocs = percentDocs;
			this.maxTF = maxTF;
			this.termAtt = AddAttribute<CharTermAttribute>();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			num--;
			if (num >= 0)
			{
				ClearAttributes();
				termAtt.Append(value);
				return true;
			}
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			if (random.NextDouble() < percentDocs)
			{
				num = random.Next(maxTF) + 1;
			}
			else
			{
				num = 0;
			}
		}
	}

	public class TestTermdocPerf : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void AddDocs(Random random, Directory dir, int ndocs, string field
			, string val, int maxTF, float percentDocs)
		{
			Analyzer analyzer = new AnonymousAnalyzer(val, random, percentDocs, maxTF);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField(field, val, Field.Store.NO));
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, analyzer).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).
				SetMaxBufferedDocs(100)).SetMergePolicy(NewLogMergePolicy(100)));
			for (int i = 0; i < ndocs; i++)
			{
				writer.AddDocument(doc);
			}
			writer.ForceMerge(1);
			writer.Dispose();
		}

		private sealed class AnonymousAnalyzer : Analyzer
		{
			public AnonymousAnalyzer(string val, Random random, float percentDocs, int maxTF)
			{
				this.val = val;
				this.random = random;
				this.percentDocs = percentDocs;
				this.maxTF = maxTF;
			}

		    public override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, TextReader reader)
			{
				return new Analyzer.TokenStreamComponents(new RepeatingTokenizer(reader, val, random
					, percentDocs, maxTF));
			}

			private readonly string val;

			private readonly Random random;

			private readonly float percentDocs;

			private readonly int maxTF;
		}

		[Test]
		public virtual int DoTest(int iter, int ndocs, int maxTF, float percentDocs)
		{
			Directory dir = NewDirectory();
			long start = DateTime.Now.CurrentTimeMillis();
			AddDocs(Random(), dir, ndocs, "foo", "val", maxTF, percentDocs);
			long end = DateTime.Now.CurrentTimeMillis();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("milliseconds for creation of " + ndocs + " docs = "
					 + (end - start));
			}
			IndexReader reader = DirectoryReader.Open(dir);
			TermsEnum tenum = MultiFields.GetTerms(reader, "foo").Iterator(null);
			start = DateTime.Now.CurrentTimeMillis();
			int ret = 0;
			DocsEnum tdocs = null;
			Random random = new Random(Random().Next());
			for (int i = 0; i < iter; i++)
			{
				tenum.SeekCeil(new BytesRef("val"));
				tdocs = TestUtil.Docs(random, tenum, MultiFields.GetLiveDocs(reader), tdocs, DocsEnum
					.FLAG_NONE);
				while (tdocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
				{
					ret += tdocs.DocID;
				}
			}
			end = DateTime.Now.CurrentTimeMillis();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("milliseconds for " + iter + " TermDocs iteration: "
					 + (end - start));
			}
			return ret;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestTermDocPerf()
		{
		}
		// performance test for 10% of documents containing a term
		// doTest(100000, 10000,3,.1f);
	}
}
