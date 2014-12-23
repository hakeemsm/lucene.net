/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>Test indexes ~82M docs with 52 positions each, so you get &gt; Integer.MAX_VALUE positions
	/// 	</summary>
	/// <lucene.experimental></lucene.experimental>
	public class Test2BPositions : LuceneTestCase
	{
		// uses lots of space and takes a few minutes
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
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
			MergePolicy mp = w.GetConfig().GetMergePolicy();
			if (mp is LogByteSizeMergePolicy)
			{
				// 1 petabyte:
				((LogByteSizeMergePolicy)mp).SetMaxMergeMB(1024 * 1024 * 1024);
			}
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetOmitNorms(true);
			Field field = new Field("field", new Test2BPositions.MyTokenStream(), ft);
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
			w.Close();
			dir.Close();
		}

		public sealed class MyTokenStream : TokenStream
		{
			private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

			private readonly PositionIncrementAttribute posIncAtt = AddAttribute<PositionIncrementAttribute
				>();

			internal int index;

			public override bool IncrementToken()
			{
				if (index < 52)
				{
					ClearAttributes();
					termAtt.SetLength(1);
					termAtt.Buffer()[0] = 'a';
					posIncAtt.SetPositionIncrement(1 + index);
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
