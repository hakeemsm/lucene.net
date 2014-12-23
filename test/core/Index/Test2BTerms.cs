/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class Test2BTerms : LuceneTestCase
	{
		private const int TOKEN_LEN = 5;

		private static readonly BytesRef bytes = new BytesRef(TOKEN_LEN);

		private sealed class MyTokenStream : TokenStream
		{
			private readonly int tokensPerDoc;

			private int tokenCount;

			public readonly IList<BytesRef> savedTerms = new AList<BytesRef>();

			private int nextSave;

			private long termCounter;

			private readonly Random random;

			public MyTokenStream(Random random, int tokensPerDoc) : base(new Test2BTerms.MyTokenStream.MyAttributeFactory
				(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY))
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
				AddAttribute<TermToBytesRefAttribute>();
				bytes.length = TOKEN_LEN;
				this.random = random;
				nextSave = TestUtil.NextInt(random, 500000, 1000000);
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
					bytes.bytes[i] = unchecked((byte)((termCounter >> shift) & unchecked((int)(0xFF))
						));
					shift -= 8;
				}
				termCounter++;
				tokenCount++;
				if (--nextSave == 0)
				{
					savedTerms.AddItem(BytesRef.DeepCopyOf(bytes));
					System.Console.Out.WriteLine("TEST: save term=" + bytes);
					nextSave = TestUtil.NextInt(random, 500000, 1000000);
				}
				return true;
			}

			public override void Reset()
			{
				tokenCount = 0;
			}

			private sealed class MyTermAttributeImpl : AttributeImpl, TermToBytesRefAttribute
			{
				public void FillBytesRef()
				{
				}

				// no-op: the bytes was already filled by our owner's incrementToken
				public BytesRef GetBytesRef()
				{
					return bytes;
				}

				public override void Clear()
				{
				}

				public override bool Equals(object other)
				{
					return other == this;
				}

				public override int GetHashCode()
				{
					return Runtime.IdentityHashCode(this);
				}

				public override void CopyTo(AttributeImpl target)
				{
				}

				public override AttributeImpl Clone()
				{
					throw new NotSupportedException();
				}
			}

			private sealed class MyAttributeFactory : AttributeSource.AttributeFactory
			{
				private readonly AttributeSource.AttributeFactory delegate_;

				public MyAttributeFactory(AttributeSource.AttributeFactory delegate_)
				{
					this.delegate_ = delegate_;
				}

				public override AttributeImpl CreateAttributeInstance<_T0>(Type<_T0> attClass)
				{
					if (attClass == typeof(TermToBytesRefAttribute))
					{
						return new Test2BTerms.MyTokenStream.MyTermAttributeImpl();
					}
					if (typeof(CharTermAttribute).IsAssignableFrom(attClass))
					{
						throw new ArgumentException("no");
					}
					return delegate_.CreateAttributeInstance(attClass);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Test2BTerms()
		{
			if ("Lucene3x".Equals(Codec.GetDefault().GetName()))
			{
				throw new RuntimeException("this test cannot run with PreFlex codec");
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
				MergePolicy mp = w.GetConfig().GetMergePolicy();
				if (mp is LogByteSizeMergePolicy)
				{
					// 1 petabyte:
					((LogByteSizeMergePolicy)mp).SetMaxMergeMB(1024 * 1024 * 1024);
				}
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				Test2BTerms.MyTokenStream ts = new Test2BTerms.MyTokenStream(Random(), TERMS_PER_DOC
					);
				FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
				customType.SetIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY);
				customType.SetOmitNorms(true);
				Field field = new Field("field", ts, customType);
				doc.Add(field);
				//w.setInfoStream(System.out);
				int numDocs = (int)(TERM_COUNT / TERMS_PER_DOC);
				System.Console.Out.WriteLine("TERMS_PER_DOC=" + TERMS_PER_DOC);
				System.Console.Out.WriteLine("numDocs=" + numDocs);
				for (int i = 0; i < numDocs; i++)
				{
					long t0 = Runtime.CurrentTimeMillis();
					w.AddDocument(doc);
					System.Console.Out.WriteLine(i + " of " + numDocs + " " + (Runtime.CurrentTimeMillis
						() - t0) + " msec");
				}
				savedTerms = ts.savedTerms;
				System.Console.Out.WriteLine("TEST: full merge");
				w.ForceMerge(1);
				System.Console.Out.WriteLine("TEST: close writer");
				w.Close();
			}
			System.Console.Out.WriteLine("TEST: open reader");
			IndexReader r = DirectoryReader.Open(dir);
			if (savedTerms == null)
			{
				savedTerms = FindTerms(r);
			}
			int numSavedTerms = savedTerms.Count;
			IList<BytesRef> bigOrdTerms = new AList<BytesRef>(savedTerms.SubList(numSavedTerms
				 - 10, numSavedTerms));
			System.Console.Out.WriteLine("TEST: test big ord terms...");
			TestSavedTerms(r, bigOrdTerms);
			System.Console.Out.WriteLine("TEST: test all saved terms...");
			TestSavedTerms(r, savedTerms);
			r.Close();
			System.Console.Out.WriteLine("TEST: now CheckIndex...");
			CheckIndex.Status status = TestUtil.CheckIndex(dir);
			long tc = status.segmentInfos[0].termIndexStatus.termCount;
			NUnit.Framework.Assert.IsTrue("count " + tc + " is not > " + int.MaxValue, tc > int.MaxValue
				);
			dir.Close();
			System.Console.Out.WriteLine("TEST: done!");
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IList<BytesRef> FindTerms(IndexReader r)
		{
			System.Console.Out.WriteLine("TEST: findTerms");
			TermsEnum termsEnum = MultiFields.GetTerms(r, "field").Iterator(null);
			IList<BytesRef> savedTerms = new AList<BytesRef>();
			int nextSave = TestUtil.NextInt(Random(), 500000, 1000000);
			BytesRef term;
			while ((term = termsEnum.Next()) != null)
			{
				if (--nextSave == 0)
				{
					savedTerms.AddItem(BytesRef.DeepCopyOf(term));
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
			Sharpen.Collections.Shuffle(terms);
			TermsEnum termsEnum = MultiFields.GetTerms(r, "field").Iterator(null);
			bool failed = false;
			for (int iter = 0; iter < 10 * terms.Count; iter++)
			{
				BytesRef term = terms[Random().Next(terms.Count)];
				System.Console.Out.WriteLine("TEST: search " + term);
				long t0 = Runtime.CurrentTimeMillis();
				int count = s.Search(new TermQuery(new Term("field", term)), 1).totalHits;
				if (count <= 0)
				{
					System.Console.Out.WriteLine("  FAILED: count=" + count);
					failed = true;
				}
				long t1 = Runtime.CurrentTimeMillis();
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
						System.Console.Out.WriteLine("  FAILED: wrong term: got " + termsEnum.Term());
					}
					failed = true;
				}
			}
			NUnit.Framework.Assert.IsFalse(failed);
		}
	}
}
