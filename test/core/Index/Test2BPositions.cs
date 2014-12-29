using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>Test indexes ~82M docs with 52 positions each, so you get &gt; Integer.MAX_VALUE positions
	/// 	</summary>
	/// <lucene.experimental></lucene.experimental>
	[TestFixture]
    public class Test2BPositions : LuceneTestCase
	{
		// uses lots of space and takes a few minutes
		[Test]
		public virtual void TestPositions()
		{
			BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BPositions"));
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig)new 
				IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(IndexWriterConfig.DISABLE_AUTO_FLUSH)).SetRAMBufferSizeMB(256.0)).SetMergeScheduler
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
			ft.OmitsNorms = true;
			Field field = new Field("field", new MyTokenStream(), ft);
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

		    private readonly PositionIncrementAttribute posIncAtt;

			internal int index;

		    public MyTokenStream()
		    {
                termAtt = AddAttribute<CharTermAttribute>();
                posIncAtt = AddAttribute<PositionIncrementAttribute>();
		    }

			public override bool IncrementToken()
			{
				if (index < 52)
				{
					ClearAttributes();
					termAtt.SetLength(1);
					termAtt.Buffer[0] = 'a';
					posIncAtt.PositionIncrement = (1 + index);
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
