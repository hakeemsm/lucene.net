using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using Lucene.Net.Store;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
    public class TestDocValuesWithThreads : LuceneTestCase
    {
        [Test]
        public virtual void TestNumberThreads()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new
                MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy()));
            IList<long> numbers = new List<long>();
            IList<BytesRef> binary = new List<BytesRef>();
            IList<BytesRef> sorted = new List<BytesRef>();
            int numDocs = AtLeast(100);
            for (int i = 0; i < numDocs; i++)
            {
                Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
                long number = Random().NextLong();
                d.Add(new NumericDocValuesField("number", number));
                BytesRef bytes = new BytesRef(TestUtil.RandomRealisticUnicodeString(Random()));
                d.Add(new BinaryDocValuesField("bytes", bytes));
                binary.Add(bytes);
                bytes = new BytesRef(TestUtil.RandomRealisticUnicodeString(Random()));
                d.Add(new SortedDocValuesField("sorted", bytes));
                sorted.Add(bytes);
                w.AddDocument(d);
                numbers.Add(number);
            }
            w.ForceMerge(1);
            IndexReader r = w.Reader;
            w.Dispose();
            AreEqual(1, r.Leaves.Count);
            AtomicReader ar = ((AtomicReader)r.Leaves[0].Reader);
            int numThreads = TestUtil.NextInt(Random(), 2, 5);
            IList<Thread> threads = new List<Thread>();
            CountdownEvent startingGun = new CountdownEvent(1);
            for (int t = 0; t < numThreads; t++)
            {
                Random threadRandom = new Random(Random().NextInt(0, int.MaxValue));
                Thread thread = new Thread(new DocThread(ar, startingGun, threadRandom, numDocs, numbers
                    , binary, sorted).Run);
                //NumericDocValues ndv = ar.getNumericDocValues("number");
                //BinaryDocValues bdv = ar.getBinaryDocValues("bytes");
                // Cannot share a single scratch against two "sources":
                thread.Start();
                threads.Add(thread);
            }
            startingGun.Signal();
            foreach (Thread thread_1 in threads)
            {
                thread_1.Join();
            }
            r.Dispose();
            dir.Dispose();
        }

        private sealed class DocThread
        {
            public DocThread(AtomicReader ar, CountdownEvent startingGun, Random threadRandom
                , int numDocs, IList<long> numbers, IList<BytesRef> binary, IList<BytesRef> sorted
                )
            {
                this.ar = ar;
                this.startingGun = startingGun;
                this.threadRandom = threadRandom;
                this.numDocs = numDocs;
                this.numbers = numbers;
                this.binary = binary;
                this.sorted = sorted;
            }

            public void Run()
            {
                try
                {
                    FieldCache.Longs ndv = FieldCache.DEFAULT.GetLongs(ar, "number", false);
                    BinaryDocValues bdv = FieldCache.DEFAULT.GetTerms(ar, "bytes", false);
                    SortedDocValues sdv = FieldCache.DEFAULT.GetTermsIndex(ar, "sorted");
                    startingGun.Wait();
                    int iters = LuceneTestCase.AtLeast(1000);
                    BytesRef scratch = new BytesRef();
                    BytesRef scratch2 = new BytesRef();
                    for (int iter = 0; iter < iters; iter++)
                    {
                        int docID = threadRandom.Next(numDocs);
                        switch (threadRandom.Next(6))
                        {
                            case 0:
                                {
                                    AreEqual(unchecked((byte)numbers[docID]), FieldCache.DEFAULT
                                        .GetBytes(ar, "number", false).Get(docID));
                                    break;
                                }

                            case 1:
                                {
                                    AreEqual((short)numbers[docID], FieldCache.DEFAULT.GetShorts
                                        (ar, "number", false).Get(docID));
                                    break;
                                }

                            case 2:
                                {
                                    AreEqual((int)numbers[docID], FieldCache.DEFAULT.GetInts(ar
                                        , "number", false).Get(docID));
                                    break;
                                }

                            case 3:
                                {
                                    AreEqual(numbers[docID], FieldCache.DEFAULT.GetLongs(ar, "number"
                                        , false).Get(docID));
                                    break;
                                }

                            case 4:
                                {
                                    AreEqual(((int)numbers[docID
                                        ]).IntBitsToFloat(), FieldCache.DEFAULT.GetFloats(ar, "number", false).Get(docID), 0.0f);
                                    break;
                                }

                            case 5:
                                {
                                    AreEqual((numbers[docID]).LongBitsToDouble(), FieldCache
                                        .DEFAULT.GetDoubles(ar, "number", false).Get(docID), 0.0);
                                    break;
                                }
                        }
                        bdv.Get(docID, scratch);
                        AreEqual(binary[docID], scratch);
                        sdv.Get(docID, scratch2);
                        AreEqual(sorted[docID], scratch2);
                    }
                }
                catch (Exception e)
                {
                    throw new SystemException();
                }
            }

            private readonly AtomicReader ar;

            private readonly CountdownEvent startingGun;

            private readonly Random threadRandom;

            private readonly int numDocs;

            private readonly IList<long> numbers;

            private readonly IList<BytesRef> binary;

            private readonly IList<BytesRef> sorted;
        }

        [Test]
        public virtual void TestDocThreads2()
        {
            Random random = Random();
            int NUM_DOCS = AtLeast(100);
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(random, dir);
            bool allowDups = random.NextBoolean();
            ICollection<string> seen = new HashSet<string>();
            if (VERBOSE)
            {
                System.Console.Out.WriteLine("TEST: NUM_DOCS=" + NUM_DOCS + " allowDups=" + allowDups
                    );
            }
            int numDocs = 0;
            IList<BytesRef> docValues = new List<BytesRef>();
            // TODO: deletions
            while (numDocs < NUM_DOCS)
            {
                string s;
                if (random.NextBoolean())
                {
                    s = TestUtil.RandomSimpleString(random);
                }
                else
                {
                    s = TestUtil.RandomUnicodeString(random);
                }
                BytesRef br = new BytesRef(s);
                if (!allowDups)
                {
                    if (seen.Contains(s))
                    {
                        continue;
                    }
                    seen.Add(s);
                }
                if (VERBOSE)
                {
                    System.Console.Out.WriteLine("  " + numDocs + ": s=" + s);
                }
                var doc = new Lucene.Net.Documents.Document
				{
				    new SortedDocValuesField("stringdv", br),
				    new NumericDocValuesField("id", numDocs)
				};
                docValues.Add(br);
                writer.AddDocument(doc);
                numDocs++;
                if (random.Next(40) == 17)
                {
                    // force flush
                    writer.GetReader().Dispose();
                }
            }
            writer.ForceMerge(1);
            DirectoryReader r = writer.GetReader();
            writer.Close();
            AtomicReader sr = GetOnlySegmentReader(r);
            long END_TIME = DateTime.Now.CurrentTimeMillis() + (TEST_NIGHTLY ? 30 : 1);
            int NUM_THREADS = TestUtil.NextInt(Random(), 1, 10);
            Thread[] threads = new Thread[NUM_THREADS];
            for (int thread = 0; thread < NUM_THREADS; thread++)
            {
                threads[thread] = new Thread(new DocThread2(sr, END_TIME, docValues).Run);
                threads[thread].Start();
            }
            foreach (Thread thread_1 in threads)
            {
                thread_1.Join();
            }
            r.Dispose();
            dir.Dispose();
        }

        private sealed class DocThread2
        {
            public DocThread2(AtomicReader sr, long END_TIME, IList<BytesRef> docValues)
            {
                this.sr = sr;
                this.END_TIME = END_TIME;
                this.docValues = docValues;
            }

            public void Run()
            {
                Random random = LuceneTestCase.Random();
                SortedDocValues stringDVDirect;
                NumericDocValues docIDToID;

                stringDVDirect = sr.GetSortedDocValues("stringdv");
                docIDToID = sr.GetNumericDocValues("id");
                IsNotNull(stringDVDirect);
                while (DateTime.Now.CurrentTimeMillis() < END_TIME)
                {
                    SortedDocValues source;
                    source = stringDVDirect;
                    BytesRef scratch = new BytesRef();
                    for (int iter = 0; iter < 100; iter++)
                    {
                        int docID = random.Next(sr.MaxDoc);
                        source.Get(docID, scratch);
                        AreEqual(docValues[(int)docIDToID.Get(docID)], scratch);
                    }
                }
            }

            private readonly AtomicReader sr;

            private readonly long END_TIME;

            private readonly IList<BytesRef> docValues;
        }
    }
}
