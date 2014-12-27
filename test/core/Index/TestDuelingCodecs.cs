/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	/// <summary>Compares one codec against another</summary>
	public class TestDuelingCodecs : LuceneTestCase
	{
		private Directory leftDir;

		private IndexReader leftReader;

		private Codec leftCodec;

		private Directory rightDir;

		private IndexReader rightReader;

		private Codec rightCodec;

		private string info;

		// for debugging
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			// for now its SimpleText vs Lucene46(random postings format)
			// as this gives the best overall coverage. when we have more
			// codecs we should probably pick 2 from Codec.availableCodecs()
			leftCodec = Codec.ForName("SimpleText");
			rightCodec = new RandomCodec(Random());
			leftDir = NewDirectory();
			rightDir = NewDirectory();
			long seed = Random().NextLong();
			// must use same seed because of random payloads, etc
			int maxTermLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);
			MockAnalyzer leftAnalyzer = new MockAnalyzer(new Random(seed));
			leftAnalyzer.SetMaxTokenLength(maxTermLength);
			MockAnalyzer rightAnalyzer = new MockAnalyzer(new Random(seed));
			rightAnalyzer.SetMaxTokenLength(maxTermLength);
			// but these can be different
			// TODO: this turns this into a really big test of Multi*, is that what we want?
			IndexWriterConfig leftConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, leftAnalyzer
				);
			leftConfig.SetCodec(leftCodec);
			// preserve docids
			leftConfig.SetMergePolicy(NewLogMergePolicy());
			IndexWriterConfig rightConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, rightAnalyzer
				);
			rightConfig.SetCodec(rightCodec);
			// preserve docids
			rightConfig.SetMergePolicy(NewLogMergePolicy());
			// must use same seed because of random docvalues fields, etc
			RandomIndexWriter leftWriter = new RandomIndexWriter(new Random(seed), leftDir, leftConfig
				);
			RandomIndexWriter rightWriter = new RandomIndexWriter(new Random(seed), rightDir, 
				rightConfig);
			int numdocs = AtLeast(100);
			CreateRandomIndex(numdocs, leftWriter, seed);
			CreateRandomIndex(numdocs, rightWriter, seed);
			leftReader = MaybeWrapReader(leftWriter.GetReader());
			leftWriter.Dispose();
			rightReader = MaybeWrapReader(rightWriter.GetReader());
			rightWriter.Dispose();
			// check that our readers are valid
			TestUtil.CheckReader(leftReader);
			TestUtil.CheckReader(rightReader);
			info = "left: " + leftCodec.ToString() + " / right: " + rightCodec.ToString();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			if (leftReader != null)
			{
				leftReader.Dispose();
			}
			if (rightReader != null)
			{
				rightReader.Dispose();
			}
			if (leftDir != null)
			{
				leftDir.Dispose();
			}
			if (rightDir != null)
			{
				rightDir.Dispose();
			}
			base.TearDown();
		}

		/// <summary>populates a writer with random stuff.</summary>
		/// <remarks>populates a writer with random stuff. this must be fully reproducable with the seed!
		/// 	</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static void CreateRandomIndex(int numdocs, RandomIndexWriter writer, long 
			seed)
		{
			Random random = new Random(seed);
			// primary source for our data is from linefiledocs, its realistic.
			LineFileDocs lineFileDocs = new LineFileDocs(random);
			// TODO: we should add other fields that use things like docs&freqs but omit positions,
			// because linefiledocs doesn't cover all the possibilities.
			for (int i = 0; i < numdocs; i++)
			{
				Lucene.Net.Documents.Document document = lineFileDocs.NextDoc();
				// grab the title and add some SortedSet instances for fun
				string title = document.Get("titleTokenized");
				string[] split = title.Split("\\s+");
				foreach (string trash in split)
				{
					document.Add(new SortedSetDocValuesField("sortedset", new BytesRef(trash)));
				}
				// add a numeric dv field sometimes
				document.RemoveFields("sparsenumeric");
				if (random.Next(4) == 2)
				{
					document.Add(new NumericDocValuesField("sparsenumeric", random.Next()));
				}
				writer.AddDocument(document);
			}
			lineFileDocs.Dispose();
		}

		/// <summary>checks the two indexes are equivalent</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestEquals()
		{
			AssertReaderEquals(info, leftReader, rightReader);
		}
	}
}
