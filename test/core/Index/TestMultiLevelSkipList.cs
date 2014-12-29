/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Test.Analysis.Tokenattributes;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// This testcase tests whether multi-level skipping is being used
	/// to reduce I/O while skipping through posting lists.
	/// </summary>
	/// <remarks>
	/// This testcase tests whether multi-level skipping is being used
	/// to reduce I/O while skipping through posting lists.
	/// Skipping in general is already covered by several other
	/// testcases.
	/// </remarks>
	public class TestMultiLevelSkipList : LuceneTestCase
	{
		internal class CountingRAMDirectory : MockDirectoryWrapper
		{
			protected CountingRAMDirectory(TestMultiLevelSkipList _enclosing, Directory delegate_
				) : base(LuceneTestCase.Random(), delegate_)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IndexInput OpenInput(string fileName, IOContext context)
			{
				IndexInput @in = base.OpenInput(fileName, context);
				if (fileName.EndsWith(".frq"))
				{
					@in = new TestMultiLevelSkipList.CountingStream(this, @in);
				}
				return @in;
			}

			private readonly TestMultiLevelSkipList _enclosing;
		}

		/// <exception cref="System.Exception"></exception>
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			counter = 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSimpleSkip()
		{
			Directory dir = new TestMultiLevelSkipList.CountingRAMDirectory(this, new RAMDirectory
				());
			IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new TestMultiLevelSkipList.PayloadAnalyzer()).SetCodec(TestUtil.AlwaysPostingsFormat
				(new Lucene41PostingsFormat())).SetMergePolicy(NewLogMergePolicy()));
			Term term = new Term("test", "a");
			for (int i = 0; i < 5000; i++)
			{
				Lucene.Net.Documents.Document d1 = new Lucene.Net.Documents.Document(
					);
				d1.Add(NewTextField(term.Field(), term.Text(), Field.Store.NO));
				writer.AddDocument(d1);
			}
			writer.Commit();
			writer.ForceMerge(1);
			writer.Dispose();
			AtomicReader reader = GetOnlySegmentReader(DirectoryReader.Open(dir));
			for (int i_1 = 0; i_1 < 2; i_1++)
			{
				counter = 0;
				DocsAndPositionsEnum tp = reader.TermPositionsEnum(term);
				CheckSkipTo(tp, 14, 185);
				// no skips
				CheckSkipTo(tp, 17, 190);
				// one skip on level 0
				CheckSkipTo(tp, 287, 200);
				// one skip on level 1, two on level 0
				// this test would fail if we had only one skip level,
				// because than more bytes would be read from the freqStream
				CheckSkipTo(tp, 4800, 250);
			}
		}

		// one skip on level 2
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void CheckSkipTo(DocsAndPositionsEnum tp, int target, int maxCounter
			)
		{
			tp.Advance(target);
			if (maxCounter < counter)
			{
				Fail("Too many bytes read: " + counter + " vs " + maxCounter
					);
			}
			AreEqual("Wrong document " + tp.DocID + " after skipTo target "
				 + target, target, tp.DocID);
			AreEqual("Frequency is not 1: " + tp.Freq, 1, tp.Freq);
			tp.NextPosition();
			BytesRef b = tp.Payload;
			AreEqual(1, b.length);
			AreEqual("Wrong payload for the target " + target + ": " +
				 b.bytes[b.offset], unchecked((byte)target), b.bytes[b.offset]);
		}

		private class PayloadAnalyzer : Analyzer
		{
			private readonly AtomicInteger payloadCount = new AtomicInteger(-1);

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
				return new Analyzer.TokenStreamComponents(tokenizer, new TestMultiLevelSkipList.PayloadFilter
					(payloadCount, tokenizer));
			}
		}

		private class PayloadFilter : TokenFilter
		{
			internal PayloadAttribute payloadAtt;

			private AtomicInteger payloadCount;

			protected internal PayloadFilter(AtomicInteger payloadCount, TokenStream input) : 
				base(input)
			{
				this.payloadCount = payloadCount;
				payloadAtt = AddAttribute<PayloadAttribute>();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool IncrementToken()
			{
				bool hasNext = input.IncrementToken();
				if (hasNext)
				{
					payloadAtt.Payload = (new BytesRef(new byte[] { unchecked((byte)payloadCount.IncrementAndGet
						()) }));
				}
				return hasNext;
			}
		}

		private int counter = 0;

		internal class CountingStream : IndexInput
		{
			private IndexInput input;

			internal CountingStream(TestMultiLevelSkipList _enclosing, IndexInput input) : base
				("CountingStream(" + input + ")")
			{
				this._enclosing = _enclosing;
				// Simply extends IndexInput in a way that we are able to count the number
				// of bytes read
				this.input = input;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override byte ReadByte()
			{
				this._enclosing.counter++;
				return this.input.ReadByte();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void ReadBytes(byte[] b, int offset, int len)
			{
				this._enclosing.counter += len;
				this.input.ReadBytes(b, offset, len);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				this.input.Dispose();
			}

			public override long FilePointer
			{
				return this.input.FilePointer;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Seek(long pos)
			{
				this.input.Seek(pos);
			}

			public override long Length()
			{
				return this.input.Length();
			}

			public override DataInput Clone()
			{
				return new TestMultiLevelSkipList.CountingStream(this, ((IndexInput)this.input.Clone
					()));
			}

			private readonly TestMultiLevelSkipList _enclosing;
		}
	}
}
