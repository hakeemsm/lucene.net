/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Test.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>Test indexes ~82M docs with 26 terms each, so you get &gt; Integer.MAX_VALUE terms/docs pairs
	/// 	</summary>
	/// <lucene.experimental></lucene.experimental>
	public class Test2BPostings : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		[LuceneTestCase.Nightly]
		public virtual void Test()
		{
			BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BPostings"));
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			IndexWriterConfig iwc = ((IndexWriterConfig)((IndexWriterConfig)new IndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(IndexWriterConfig
				.DISABLE_AUTO_FLUSH)).SetRAMBufferSizeMB(256.0)).SetMergeScheduler(new ConcurrentMergeScheduler
				()).SetMergePolicy(NewLogMergePolicy(false, 10)).SetOpenMode(IndexWriterConfig.OpenMode
				.CREATE);
			IndexWriter w = new IndexWriter(dir, iwc);
			MergePolicy mp = w.GetConfig().GetMergePolicy();
			if (mp is LogByteSizeMergePolicy)
			{
				// 1 petabyte:
				((LogByteSizeMergePolicy)mp).SetMaxMergeMB(1024 * 1024 * 1024);
			}
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.SetOmitNorms(true);
			ft.SetIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY);
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
			w.Close();
			dir.Close();
		}

		public sealed class MyTokenStream : TokenStream
		{
			private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

			internal int index;

			public override bool IncrementToken()
			{
				if (index <= 'z')
				{
					ClearAttributes();
					termAtt.SetLength(1);
					termAtt.Buffer()[0] = (char)index++;
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
