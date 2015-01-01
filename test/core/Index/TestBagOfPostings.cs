using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    /// <summary>
    /// Simple test that adds numeric terms, where each term has the
    /// docFreq of its integer value, and checks that the docFreq is correct.
    /// </summary>
    /// <remarks>
    /// Simple test that adds numeric terms, where each term has the
    /// docFreq of its integer value, and checks that the docFreq is correct.
    /// </remarks>
    [TestFixture]
    public class TestBagOfPostings : LuceneTestCase
    {
        // at night this makes like 200k/300k docs and will make Direct's heart beat!
        /// <exception cref="System.Exception"></exception>
        public virtual void TestBagPostings()
        {
            IList<string> postingsList = new List<string>();
            int numTerms = AtLeast(300);
            int maxTermsPerDoc = Random().NextInt(10, 20);
            bool isSimpleText = "SimpleText".Equals(TestUtil.GetPostingsFormat("field"));
            IndexWriterConfig iwc = NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT, new
                MockAnalyzer(Random()));
            if ((isSimpleText || iwc.MergePolicy is MockRandomMergePolicy) && (TEST_NIGHTLY
                                                                               || RANDOM_MULTIPLIER > 1))
            {
                // Otherwise test can take way too long (> 2 hours)
                numTerms /= 2;
            }
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("maxTermsPerDoc=" + maxTermsPerDoc);
                System.Console.Out.WriteLine("numTerms=" + numTerms);
            }
            for (int i = 0; i < numTerms; i++)
            {
                string term = i.ToString();
                for (int j = 0; j < i; j++)
                {
                    postingsList.Add(term);
                }
            }
            postingsList.Shuffle(Random());
            var postings = new ConcurrentQueue<string>(postingsList);
            
            Directory dir = NewFSDirectory(CreateTempDir("bagofpostings"));
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc);
            int threadCount = TestUtil.NextInt(Random(), 1, 5);
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("config: " + iw.w.Config);
                System.Console.Out.WriteLine("threadCount=" + threadCount);
            }
            Thread[] threads = new Thread[threadCount];
            CountdownEvent startingGun = new CountdownEvent(1);
            for (int threadID = 0; threadID < threadCount; threadID++)
            {
                threads[threadID] = new Thread(() =>
                {
                    var document = new Lucene.Net.Documents.Document();
                    Field field = LuceneTestCase.NewTextField("field", string.Empty, Field.Store.NO);
                    document.Add(field);
                    startingGun.Wait();
                    while (postings.Any())
                    {
                        StringBuilder text = new StringBuilder();
                        ICollection<string> visited = new HashSet<string>();
                        for (int i = 0; i < maxTermsPerDoc; i++)
                        {
                            string token;
                            if (!postings.TryDequeue(out token))
                            {
                                break;
                            }
                            if (visited.Contains(token))
                            {
                                postings.Enqueue(token);
                                break;
                            }
                            text.Append(' ');
                            text.Append(token);
                            visited.Add(token);
                        }
                        field.StringValue = text.ToString();
                        iw.AddDocument(document);
                    }
                    // Put it back:
                    threads[threadID].Start();
                });
                startingGun.Signal();
                foreach (Thread t in threads)
                {
                    t.Join();
                }
                iw.ForceMerge(1);
                DirectoryReader ir = iw.Reader;
                AreEqual(1, ir.Leaves.Count);
                AtomicReader air = ((AtomicReader)ir.Leaves[0].Reader);
                Terms terms = air.Terms("field");
                // numTerms-1 because there cannot be a term 0 with 0 postings:
                AreEqual(numTerms - 1, air.Fields.UniqueTermCount);
                if (iwc.Codec is Lucene3xCodec == false)
                {
                    AreEqual(numTerms - 1, terms.Size);
                }
                TermsEnum termsEnum = terms.Iterator(null);
                BytesRef term_1;
                while ((term_1 = termsEnum.Next()) != null)
                {
                    int value = System.Convert.ToInt32(term_1.Utf8ToString());
                    AreEqual(value, termsEnum.DocFreq);
                }
                // don't really need to check more than this, as CheckIndex
                // will verify that docFreq == actual number of documents seen
                // from a docsAndPositionsEnum.
                ir.Dispose();
                iw.Close();
                dir.Dispose();
            }
        }


    }
}
