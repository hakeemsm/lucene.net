using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace Lucene.Net.Test.Index
{
	/// <summary>Test indexes ~82M docs with 26 terms each, so you get &gt; Integer.MAX_VALUE terms/docs pairs
	/// 	</summary>
	/// <lucene.experimental></lucene.experimental>
	public class Test2BPostings : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		//[LuceneTestCase.Nightly]
		public virtual void TestPostings()
		{
			BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BPostings"));
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			IndexWriterConfig iwc = ((IndexWriterConfig)new IndexWriterConfig
			    (TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(IndexWriterConfig
			        .DISABLE_AUTO_FLUSH).SetRAMBufferSizeMB(256.0)).SetMergeScheduler(new ConcurrentMergeScheduler
				()).SetMergePolicy(NewLogMergePolicy(false, 10)).SetOpenMode(IndexWriterConfig.OpenMode
				.CREATE);
			IndexWriter w = new IndexWriter(dir, iwc);
			MergePolicy mp = w.Config.MergePolicy;
			if (mp is LogByteSizeMergePolicy)
			{
				// 1 petabyte:
				((LogByteSizeMergePolicy)mp).MaxMergeMB = (1024 * 1024 * 1024);
			}
			var doc = new Lucene.Net.Documents.Document();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.OmitNorms = (true);
			ft.IndexOptions = (FieldInfo.IndexOptions.DOCS_ONLY);
			Field field = new Field("field", new Test2BPostings.MyTokenStream(), ft);
			doc.Add(field);
			int numDocs = (int.MaxValue / 26) + 1;
			for (int i = 0; i < numDocs; i++)
			{
				w.AddDocument(doc);
				if (VERBOSE && i % 100000 == 0)
				{
					System.Console.Out.WriteLine(i + " of " + numDocs + "...");
				}
			}
			w.ForceMerge(1);
			w.Dispose();
			dir.Dispose();
		}

		public sealed class MyTokenStream : TokenStream
		{
		    private readonly CharTermAttribute termAtt;

			internal int index;

		    public MyTokenStream()
		    {
                termAtt = AddAttribute<CharTermAttribute>();
		    }

			public override bool IncrementToken()
			{
				if (index <= 'z')
				{
					ClearAttributes();
					termAtt.SetLength(1);
					termAtt.Buffer[0] = (char)index++;
					return true;
				}
				return false;
			}

			public override void Reset()
			{
				index = 'a';
			}
		}
	}
}
