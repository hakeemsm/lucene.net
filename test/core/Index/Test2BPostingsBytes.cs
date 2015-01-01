using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// Test indexes 2B docs with 65k freqs each,
	/// so you get &gt; Integer.MAX_VALUE postings data for the term
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	[TestFixture]
    public class Test2BPostingsBytes : LuceneTestCase
	{
		// disable Lucene3x: older lucene formats always had this issue.
		// @Absurd @Ignore takes ~20GB-30GB of space and 10 minutes.
		// with some codecs needs more heap space as well.
		/// <exception cref="System.Exception"></exception>
		[Test]
        public virtual void TestBytePostings()
		{
			BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BPostingsBytes1"));
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)new 
			    IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
			    (IndexWriterConfig.DISABLE_AUTO_FLUSH).SetRAMBufferSizeMB(256.0)).SetMergeScheduler
				(new ConcurrentMergeScheduler()).SetMergePolicy(NewLogMergePolicy(false, 10)).SetOpenMode
				(IndexWriterConfig.OpenMode.CREATE));
			MergePolicy mp = w.Config.MergePolicy;
			if (mp is LogByteSizeMergePolicy)
			{
				// 1 petabyte:
				((LogByteSizeMergePolicy)mp).MaxMergeMB = (1024 * 1024 * 1024);
			}
			var doc = new Lucene.Net.Documents.Document();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.IndexOptions = (FieldInfo.IndexOptions.DOCS_AND_FREQS);
			ft.OmitNorms = (true);
			Test2BPostingsBytes.MyTokenStream tokenStream = new Test2BPostingsBytes.MyTokenStream
				();
			Field field = new Field("field", tokenStream, ft);
			doc.Add(field);
			int numDocs = 1000;
			for (int i = 0; i < numDocs; i++)
			{
				if (i % 2 == 1)
				{
					// trick blockPF's little optimization
					tokenStream.n = 65536;
				}
				else
				{
					tokenStream.n = 65537;
				}
				w.AddDocument(doc);
			}
			w.ForceMerge(1);
			w.Dispose();
			DirectoryReader oneThousand = DirectoryReader.Open(dir);
			IndexReader[] subReaders = new IndexReader[1000];
			Arrays.Fill(subReaders, oneThousand);
			MultiReader mr = new MultiReader(subReaders);
			BaseDirectoryWrapper dir2 = NewFSDirectory(CreateTempDir("2BPostingsBytes2"));
			if (dir2 is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir2).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			IndexWriter w2 = new IndexWriter(dir2, new IndexWriterConfig(TEST_VERSION_CURRENT
				, null));
			w2.AddIndexes(mr);
			w2.ForceMerge(1);
			w2.Dispose();
			oneThousand.Dispose();
			DirectoryReader oneMillion = DirectoryReader.Open(dir2);
			subReaders = new IndexReader[2000];
			Arrays.Fill(subReaders, oneMillion);
			mr = new MultiReader(subReaders);
			BaseDirectoryWrapper dir3 = NewFSDirectory(CreateTempDir("2BPostingsBytes3"));
			if (dir3 is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir3).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			IndexWriter w3 = new IndexWriter(dir3, new IndexWriterConfig(TEST_VERSION_CURRENT
				, null));
			w3.AddIndexes(mr);
			w3.ForceMerge(1);
			w3.Dispose();
			oneMillion.Dispose();
			dir.Dispose();
			dir2.Dispose();
			dir3.Dispose();
		}

		public sealed class MyTokenStream : TokenStream
		{
		    private readonly CharTermAttribute termAtt;

			internal int index;

			internal int n;

		    public MyTokenStream()
		    {
                termAtt = AddAttribute<CharTermAttribute>();
		    }

			public override bool IncrementToken()
			{
				if (index < n)
				{
					ClearAttributes();
					termAtt.Buffer[0] = 'a';
					termAtt.SetLength(1);
					index++;
					return true;
				}
				return false;
			}

			public override void Reset()
			{
				index = 0;
			}
		}
	}
}
