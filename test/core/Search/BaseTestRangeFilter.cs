/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using NUnit.Framework;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search
{
	public class BaseTestRangeFilter : LuceneTestCase
	{
		public const bool F = false;

		public const bool T = true;

		/// <summary>
		/// Collation interacts badly with hyphens -- collation produces different
		/// ordering than Unicode code-point ordering -- so two indexes are created:
		/// one which can't have negative random integers, for testing collated ranges,
		/// and the other which can have negative random integers, for all other tests.
		/// </summary>
		/// <remarks>
		/// Collation interacts badly with hyphens -- collation produces different
		/// ordering than Unicode code-point ordering -- so two indexes are created:
		/// one which can't have negative random integers, for testing collated ranges,
		/// and the other which can have negative random integers, for all other tests.
		/// </remarks>
		internal class TestIndex
		{
			internal int maxR;

			internal int minR;

			internal bool allowNegativeRandomInts;

			internal Directory index;

			internal TestIndex(Random random, int minR, int maxR, bool allowNegativeRandomInts
				)
			{
				this.minR = minR;
				this.maxR = maxR;
				this.allowNegativeRandomInts = allowNegativeRandomInts;
				index = NewDirectory(random);
			}
		}

		internal static IndexReader signedIndexReader;

		internal static IndexReader unsignedIndexReader;

		internal static BaseTestRangeFilter.TestIndex signedIndexDir;

		internal static BaseTestRangeFilter.TestIndex unsignedIndexDir;

		internal static int minId = 0;

		internal static int maxId;

		internal static readonly int intLength = Extensions.ToString(int.MaxValue
			).Length;

		/// <summary>a simple padding function that should work with any int</summary>
		public static string Pad(int n)
		{
			StringBuilder b = new StringBuilder(40);
			string p = "0";
			if (n < 0)
			{
				p = "-";
				n = int.MaxValue + n + 1;
			}
			b.Append(p);
			string s = Extensions.ToString(n);
			for (int i = s.Length; i <= intLength; i++)
			{
				b.Append("0");
			}
			b.Append(s);
			return b.ToString();
		}

		/// <exception cref="System.Exception"></exception>
		[BeforeClass]
		public static void BeforeClassBaseTestRangeFilter()
		{
			maxId = AtLeast(500);
			signedIndexDir = new BaseTestRangeFilter.TestIndex(Random(), int.MaxValue, int.MinValue
				, true);
			unsignedIndexDir = new BaseTestRangeFilter.TestIndex(Random(), int.MaxValue, 0, false
				);
			signedIndexReader = Build(Random(), signedIndexDir);
			unsignedIndexReader = Build(Random(), unsignedIndexDir);
		}

		/// <exception cref="System.Exception"></exception>
		[AfterClass]
		public static void AfterClassBaseTestRangeFilter()
		{
			signedIndexReader.Dispose();
			unsignedIndexReader.Dispose();
			signedIndexDir.index.Dispose();
			unsignedIndexDir.index.Dispose();
			signedIndexReader = null;
			unsignedIndexReader = null;
			signedIndexDir = null;
			unsignedIndexDir = null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static IndexReader Build(Random random, BaseTestRangeFilter.TestIndex index
			)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field idField = NewStringField(random, "id", string.Empty, Field.Store.YES);
			Field randField = NewStringField(random, "rand", string.Empty, Field.Store.YES);
			Field bodyField = NewStringField(random, "body", string.Empty, Field.Store.NO);
			doc.Add(idField);
			doc.Add(randField);
			doc.Add(bodyField);
			RandomIndexWriter writer = new RandomIndexWriter(random, index.index, ((IndexWriterConfig
				)NewIndexWriterConfig(random, TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetOpenMode
				(IndexWriterConfig.OpenMode.CREATE).SetMaxBufferedDocs(TestUtil.NextInt(random, 
				50, 1000))).SetMergePolicy(NewLogMergePolicy()));
			TestUtil.ReduceOpenFiles(writer.w);
			while (true)
			{
				int minCount = 0;
				int maxCount = 0;
				for (int d = minId; d <= maxId; d++)
				{
					idField.StringValue = Pad(d));
					int r = index.allowNegativeRandomInts ? random.Next() : random.Next(int.MaxValue);
					if (index.maxR < r)
					{
						index.maxR = r;
						maxCount = 1;
					}
					else
					{
						if (index.maxR == r)
						{
							maxCount++;
						}
					}
					if (r < index.minR)
					{
						index.minR = r;
						minCount = 1;
					}
					else
					{
						if (r == index.minR)
						{
							minCount++;
						}
					}
					randField.StringValue = Pad(r));
					bodyField.StringValue = "body");
					writer.AddDocument(doc);
				}
				if (minCount == 1 && maxCount == 1)
				{
					// our subclasses rely on only 1 doc having the min or
					// max, so, we loop until we satisfy that.  it should be
					// exceedingly rare (Yonik calculates 1 in ~429,000)
					// times) that this loop requires more than one try:
					IndexReader ir = writer.Reader;
					writer.Dispose();
					return ir;
				}
				// try again
				writer.DeleteAll();
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestPad()
		{
			int[] tests = new int[] { -9999999, -99560, -100, -3, -1, 0, 3, 9, 10, 1000, 999999999
				 };
			for (int i = 0; i < tests.Length - 1; i++)
			{
				int a = tests[i];
				int b = tests[i + 1];
				string aa = Pad(a);
				string bb = Pad(b);
				string label = a + ":" + aa + " vs " + b + ":" + bb;
				AreEqual("length of " + label, aa.Length, bb.Length);
				IsTrue("compare less than " + label, Runtime.CompareOrdinal
					(aa, bb) < 0);
			}
		}
	}
}
