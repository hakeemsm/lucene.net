using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.Test.Index
{
	[TestFixture]
    public class Test2BTerms : LuceneTestCase
	{
		private const int TOKEN_LEN = 5;

		private static readonly BytesRef bytes = new BytesRef(TOKEN_LEN);

		private sealed class MyTokenStream : TokenStream
		{
			private readonly int tokensPerDoc;

			private int tokenCount;

			public readonly IList<BytesRef> savedTerms = new List<BytesRef>();

			private int nextSave;

			private long termCounter;

			private readonly Random random;

			public MyTokenStream(Random random, int tokensPerDoc) : base(new MyAttributeFactory(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY))
			{
				// NOTE: this test will fail w/ PreFlexRW codec!  (Because
				// this test uses full binary term space, but PreFlex cannot
				// handle this since it requires the terms are UTF8 bytes).
				//
				// Also, SimpleText codec will consume very large amounts of
				// disk (but, should run successfully).  Best to run w/
				// -Dtests.codec=Standard, and w/ plenty of RAM, eg:
				//
				//   ant test -Dtest.slow=true -Dtests.heapsize=8g
				//
				//   java -server -Xmx8g -d64 -cp .:lib/junit-4.10.jar:./build/classes/test:./build/classes/test-framework:./build/classes/java -Dlucene.version=4.0-dev -Dtests.directory=MMapDirectory -DtempDir=build -ea org.junit.runner.JUnitCore Lucene.Net.index.Test2BTerms
				//
				this.tokensPerDoc = tokensPerDoc;
				AddAttribute<ITermToBytesRefAttribute>();
				bytes.length = TOKEN_LEN;
				this.random = random;
				nextSave = random.NextInt(500000, 1000000);
			}

			public override bool IncrementToken()
			{
				ClearAttributes();
				if (tokenCount >= tokensPerDoc)
				{
					return false;
				}
				int shift = 32;
				for (int i = 0; i < 5; i++)
				{
					bytes.bytes[i] = ((sbyte)((termCounter >> shift) & 0xFF));
					shift -= 8;
				}
				termCounter++;
				tokenCount++;
				if (--nextSave == 0)
				{
					savedTerms.Add(BytesRef.DeepCopyOf(bytes));
					System.Console.Out.WriteLine("TEST: save term=" + bytes);
					nextSave = random.NextInt(500000, 1000000);
				}
				return true;
			}

			public override void Reset()
			{
				tokenCount = 0;
			}

			private sealed class MyTermAttributeImpl : Attribute, ITermToBytesRefAttribute
			{
				public void FillBytesRef()
				{
				}

				// no-op: the bytes was already filled by our owner's incrementToken
				public BytesRef BytesRef
				{
				    get { return bytes; }
				}


			    public override void Clear()
			    {
			        throw new NotImplementedException();
			    }

			    public override void CopyTo(Attribute target)
			    {
			        throw new NotImplementedException();
			    }

			    public override bool Equals(object other)
				{
					return other == this;
				}

				public override int GetHashCode()
				{
				    return base.GetHashCode(); //TODO: return a meaningful hashcode
				}
			}

			private sealed class MyAttributeFactory : AttributeSource.AttributeFactory
			{
				private readonly AttributeSource.AttributeFactory delegate_;

				public MyAttributeFactory(AttributeSource.AttributeFactory delegate_)
				{
					this.delegate_ = delegate_;
				}

				public override Attribute CreateAttributeInstance<T>()
				{
					if (typeof(T) == typeof(ITermToBytesRefAttribute))
					{
						return new Test2BTerms.MyTokenStream.MyTermAttributeImpl();
					}
					if (typeof(CharTermAttribute).IsAssignableFrom(typeof(T)))
					{
						throw new ArgumentException("no");
					}
					return delegate_.CreateAttributeInstance<T>();
				}
			}
		}

		[Test]
		public virtual void TestTerms2B()
		{
			if ("Lucene3x".Equals(Codec.Default.Name))
			{
				throw new SystemException("this test cannot run with PreFlex codec");
			}
			System.Console.Out.WriteLine("Starting Test2B");
			long TERM_COUNT = ((long)int.MaxValue) + 100000000;
			int TERMS_PER_DOC = TestUtil.NextInt(Random(), 100000, 1000000);
			IList<BytesRef> savedTerms = null;
			BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("2BTerms"));
			//MockDirectoryWrapper dir = newFSDirectory(new File("/p/lucene/indices/2bindex"));
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
			}
			dir.SetCheckIndexOnClose(false);
			// don't double-checkindex
			if (true)
			{
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
				var ts = new MyTokenStream(Random(), TERMS_PER_DOC);
				FieldType customType = new FieldType(TextField.TYPE_NOT_STORED)
				{
				    IndexOptions = (FieldInfo.IndexOptions.DOCS_ONLY),
				    OmitNorms = (true)
				};
			    Field field = new Field("field", ts, customType);
				doc.Add(field);
				//w.setInfoStream(System.out);
				int numDocs = (int)(TERM_COUNT / TERMS_PER_DOC);
				System.Console.Out.WriteLine("TERMS_PER_DOC=" + TERMS_PER_DOC);
				System.Console.Out.WriteLine("numDocs=" + numDocs);
				for (int i = 0; i < numDocs; i++)
				{
					long t0 = DateTime.Now.CurrentTimeMillis();
					w.AddDocument(doc);
					System.Console.Out.WriteLine(i + " of " + numDocs + " " + (DateTime.Now.CurrentTimeMillis() - t0) + " msec");
				}
				savedTerms = ts.savedTerms;
				System.Console.Out.WriteLine("TEST: full merge");
				w.ForceMerge(1);
				System.Console.Out.WriteLine("TEST: close writer");
				w.Dispose();
			}
			System.Console.Out.WriteLine("TEST: open reader");
			IndexReader r = DirectoryReader.Open(dir);
			if (savedTerms == null)
			{
				savedTerms = FindTerms(r);
			}
			int numSavedTerms = savedTerms.Count;
			IList<BytesRef> bigOrdTerms = new List<BytesRef>(savedTerms.SubList(numSavedTerms
				 - 10, numSavedTerms));
			System.Console.Out.WriteLine("TEST: test big ord terms...");
			TestSavedTerms(r, bigOrdTerms);
			System.Console.Out.WriteLine("TEST: test all saved terms...");
			TestSavedTerms(r, savedTerms);
			r.Dispose();
			System.Console.Out.WriteLine("TEST: now CheckIndex...");
			CheckIndex.Status status = TestUtil.CheckIndex(dir);
			long tc = status.segmentInfos[0].termIndexStatus.termCount;
			IsTrue(tc > int.MaxValue, "count " + tc + " is not > " + int.MaxValue);
			dir.Dispose();
			System.Console.Out.WriteLine("TEST: done!");
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IList<BytesRef> FindTerms(IndexReader r)
		{
			System.Console.Out.WriteLine("TEST: findTerms");
			TermsEnum termsEnum = MultiFields.GetTerms(r, "field").Iterator(null);
			IList<BytesRef> savedTerms = new List<BytesRef>();
			int nextSave = TestUtil.NextInt(Random(), 500000, 1000000);
			BytesRef term;
			while ((term = termsEnum.Next()) != null)
			{
				if (--nextSave == 0)
				{
					savedTerms.Add(BytesRef.DeepCopyOf(term));
					System.Console.Out.WriteLine("TEST: add " + term);
					nextSave = TestUtil.NextInt(Random(), 500000, 1000000);
				}
			}
			return savedTerms;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void TestSavedTerms(IndexReader r, IList<BytesRef> terms)
		{
			System.Console.Out.WriteLine("TEST: run " + terms.Count + " terms on reader=" + r
				);
			IndexSearcher s = NewSearcher(r);
			terms.Shuffle();
			TermsEnum termsEnum = MultiFields.GetTerms(r, "field").Iterator(null);
			bool failed = false;
			for (int iter = 0; iter < 10 * terms.Count; iter++)
			{
				BytesRef term = terms[Random().Next(terms.Count)];
				System.Console.Out.WriteLine("TEST: search " + term);
				long t0 = DateTime.Now.CurrentTimeMillis();
				int count = s.Search(new TermQuery(new Term("field", term)), 1).TotalHits;
				if (count <= 0)
				{
					System.Console.Out.WriteLine("  FAILED: count=" + count);
					failed = true;
				}
				long t1 = DateTime.Now.CurrentTimeMillis();
				System.Console.Out.WriteLine("  took " + (t1 - t0) + " millis");
				TermsEnum.SeekStatus result = termsEnum.SeekCeil(term);
				if (result != TermsEnum.SeekStatus.FOUND)
				{
					if (result == TermsEnum.SeekStatus.END)
					{
						System.Console.Out.WriteLine("  FAILED: got END");
					}
					else
					{
						System.Console.Out.WriteLine("  FAILED: wrong term: got " + termsEnum.Term);
					}
					failed = true;
				}
			}
			IsFalse(failed);
		}
	}
}
