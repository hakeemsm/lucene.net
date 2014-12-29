using System;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// Tests that a useful exception is thrown when attempting to index a term that is
	/// too large
	/// </summary>
	/// <seealso cref="IndexWriter.MAX_TERM_LENGTH">IndexWriter.MAX_TERM_LENGTH</seealso>
	[TestFixture]
    public class TestExceedMaxTermLength : LuceneTestCase
	{
		private const int minTestTermLength = IndexWriter.MAX_TERM_LENGTH + 1;

		private const int maxTestTermLegnth = IndexWriter.MAX_TERM_LENGTH * 2;

		internal Directory dir = null;

		[SetUp]
		public virtual void CreateDir()
		{
			dir = NewDirectory();
		}

		/// <exception cref="System.IO.IOException"></exception>
		[TearDown]
		public virtual void DestroyDir()
		{
			dir.Dispose();
			dir = null;
		}

		[Test]
		public virtual void TestArgumentsLength()
		{
			IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(Random(), TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			try
			{
				FieldType ft = new FieldType();
				ft.Indexed = (true);
				ft.Stored = (Random().NextBoolean());
				ft.Freeze();
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				if (Random().NextBoolean())
				{
					// totally ok short field value
					doc.Add(new Field(TestUtil.RandomSimpleString(Random(), 1, 10), TestUtil.RandomSimpleString
						(Random(), 1, 10), ft));
				}
				// problematic field
				string name = TestUtil.RandomSimpleString(Random(), 1, 50);
				string value = TestUtil.RandomSimpleString(Random(), minTestTermLength, maxTestTermLegnth
					);
				Field f = new Field(name, value, ft);
				if (Random().NextBoolean())
				{
					// totally ok short field value
					doc.Add(new Field(TestUtil.RandomSimpleString(Random(), 1, 10), TestUtil.RandomSimpleString
						(Random(), 1, 10), ft));
				}
				doc.Add(f);
				try
				{
					w.AddDocument(doc);
					Fail("Did not get an exception from adding a monster term"
						);
				}
				catch (ArgumentException e)
				{
					string maxLengthMsg = IndexWriter.MAX_TERM_LENGTH.ToString();
					string msg = e.Message;
					AssertTrue("IllegalArgumentException didn't mention 'immense term': "
						 + msg, msg.Contains("immense term"));
					AssertTrue("IllegalArgumentException didn't mention max length ("
						 + maxLengthMsg + "): " + msg, msg.Contains(maxLengthMsg));
					AssertTrue("IllegalArgumentException didn't mention field name ("
						 + name + "): " + msg, msg.Contains(name));
				}
			}
			finally
			{
				w.Dispose();
			}
		}
	}
}
