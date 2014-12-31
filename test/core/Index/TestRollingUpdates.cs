using System;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;

namespace Lucene.Net.Test.Index
{
	public class TestRollingUpdates : LuceneTestCase
	{
		// Just updates the same set of N docs over and over, to
		// stress out deletions
		/// <exception cref="System.Exception"></exception>
        //[NUnit.Framework.Test]
        //public virtual void TestForRollingUpdates()
        //{
        //    Random random = new Random(Random().Next());
        //    BaseDirectoryWrapper dir = NewDirectory();
        //    LineFileDocs docs = new LineFileDocs(random, DefaultCodecSupportsDocValues());
        //    //provider.register(new MemoryCodec());
        //    if ((!"Lucene3x".Equals(Codec.Default.Name)) && Random().NextBoolean())
        //    {
        //        Codec.Default = (TestUtil.AlwaysPostingsFormat(new MemoryPostingsFormat(Random().
        //            NextBoolean(), random.NextDouble())));
        //    }
        //    MockAnalyzer analyzer = new MockAnalyzer(Random());
        //    analyzer.SetMaxTokenLength(Random().NextInt(1, IndexWriter.MAX_TERM_LENGTH));
        //    IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer
        //        ));
        //    int SIZE = AtLeast(20);
        //    int id = 0;
        //    IndexReader r = null;
        //    IndexSearcher s = null;
        //    int numUpdates = (int)(SIZE * (2 + (TEST_NIGHTLY ? 200 * Random().NextDouble() : 
        //        5 * Random().NextDouble())));
        //    if (VERBOSE)
        //    {
        //        System.Console.Out.WriteLine("TEST: numUpdates=" + numUpdates);
        //    }
        //    int updateCount = 0;
        //    // TODO: sometimes update ids not in order...
        //    for (int docIter = 0; docIter < numUpdates; docIter++)
        //    {
        //        Lucene.Net.Documents.Document doc = docs.NextDoc();
        //        string myID = string.Empty + id;
        //        if (id == SIZE - 1)
        //        {
        //            id = 0;
        //        }
        //        else
        //        {
        //            id++;
        //        }
        //        if (VERBOSE)
        //        {
        //            System.Console.Out.WriteLine("  docIter=" + docIter + " id=" + id);
        //        }
        //        ((Field)doc.GetField("docid")).StringValue = myID;
        //        Term idTerm = new Term("docid", myID);
        //        bool doUpdate;
        //        if (s != null && updateCount < SIZE)
        //        {
        //            TopDocs hits = s.Search(new TermQuery(idTerm), 1);
        //            AreEqual(1, hits.TotalHits);
        //            doUpdate = !w.TryDeleteDocument(r, hits.ScoreDocs[0].Doc);
        //            if (VERBOSE)
        //            {
        //                if (doUpdate)
        //                {
        //                    System.Console.Out.WriteLine("  tryDeleteDocument failed");
        //                }
        //                else
        //                {
        //                    System.Console.Out.WriteLine("  tryDeleteDocument succeeded");
        //                }
        //            }
        //        }
        //        else
        //        {
        //            doUpdate = true;
        //            if (VERBOSE)
        //            {
        //                System.Console.Out.WriteLine("  no searcher: doUpdate=true");
        //            }
        //        }
        //        updateCount++;
        //        if (doUpdate)
        //        {
        //            w.UpdateDocument(idTerm, doc);
        //        }
        //        else
        //        {
        //            w.AddDocument(doc);
        //        }
        //        if (docIter >= SIZE && Random().Next(50) == 17)
        //        {
        //            if (r != null)
        //            {
        //                r.Dispose();
        //            }
        //            bool applyDeletions = Random().NextBoolean();
        //            if (VERBOSE)
        //            {
        //                System.Console.Out.WriteLine("TEST: reopen applyDeletions=" + applyDeletions);
        //            }
        //            r = w.GetReader(applyDeletions);
        //            if (applyDeletions)
        //            {
        //                s = NewSearcher(r);
        //            }
        //            else
        //            {
        //                s = null;
        //            }
        //            AssertTrue("applyDeletions=" + applyDeletions + " r.numDocs()="
        //                 + r.NumDocs + " vs SIZE=" + SIZE, !applyDeletions || r.NumDocs == SIZE);
        //            updateCount = 0;
        //        }
        //    }
        //    if (r != null)
        //    {
        //        r.Dispose();
        //    }
        //    w.Commit();
        //    AreEqual(SIZE, w.NumDocs);
        //    w.Dispose();
        //    TestIndexWriter.AssertNoUnreferencedFiles(dir, "leftover files after rolling updates"
        //        );
        //    docs.Close();
        //    // LUCENE-4455:
        //    SegmentInfos infos = new SegmentInfos();
        //    infos.Read(dir);
        //    long totalBytes = 0;
        //    foreach (SegmentCommitInfo sipc in infos)
        //    {
        //        totalBytes += sipc.SizeInBytes();
        //    }
        //    long totalBytes2 = 0;
        //    foreach (string fileName in dir.ListAll())
        //    {
        //        if (!fileName.StartsWith(IndexFileNames.SEGMENTS))
        //        {
        //            totalBytes2 += dir.FileLength(fileName);
        //        }
        //    }
        //    AreEqual(totalBytes2, totalBytes);
        //    dir.Dispose();
        //}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestUpdateSameDoc()
		{
			Directory dir = NewDirectory();
			LineFileDocs docs = new LineFileDocs(Random());
			for (int r = 0; r < 3; r++)
			{
				IndexWriter w = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
					, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
				int numUpdates = AtLeast(20);
				int numThreads = Random().NextInt(2, 6);
				var threads = new Thread[numThreads];
				for (int i = 0; i < numThreads; i++)
				{
				    threads[i] = new Thread(new IndexingThread(docs, w, numUpdates).Run);
					threads[i].Start();
				}
				for (int i_1 = 0; i_1 < numThreads; i_1++)
				{
					threads[i_1].Join();
				}
				w.Dispose();
			}
			IndexReader open = DirectoryReader.Open(dir);
			AreEqual(1, open.NumDocs);
			open.Dispose();
			docs.Close();
			dir.Dispose();
		}

		internal class IndexingThread
		{
			internal readonly LineFileDocs docs;

			internal readonly IndexWriter writer;

			internal readonly int num;

			public IndexingThread(LineFileDocs docs, IndexWriter writer, int num) 
			{
				this.docs = docs;
				this.writer = writer;
				this.num = num;
			}

			public void Run()
			{
				try
				{
					DirectoryReader open = null;
					for (int i = 0; i < num; i++)
					{
						Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
							();
						// docs.nextDoc();
						doc.Add(NewStringField("id", "test", Field.Store.NO));
						writer.UpdateDocument(new Term("id", "test"), doc);
						if (Random().Next(3) == 0)
						{
							if (open == null)
							{
								open = DirectoryReader.Open(writer, true);
							}
							DirectoryReader reader = DirectoryReader.OpenIfChanged(open);
							if (reader != null)
							{
								open.Dispose();
								open = reader;
							}
							AssertEquals("iter: " + i + " numDocs: " + open.NumDocs + " del: "
								 + open.NumDeletedDocs + " max: " + open.MaxDoc, 1, open.NumDocs);
						}
					}
					if (open != null)
					{
						open.Dispose();
					}
				}
				catch (Exception e)
				{
					throw new SystemException(e.Message,e);
				}
			}
		}
	}
}
