using System;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.TestFramework;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	/// <summary>Test adding to the info stream when there's an exception thrown during field analysis.
	/// 	</summary>
	/// <remarks>Test adding to the info stream when there's an exception thrown during field analysis.
	/// 	</remarks>
	[TestFixture]
    public class TestDocInverterPerFieldErrorInfo : LuceneTestCase
	{
		private static readonly FieldType storedTextType = new FieldType(TextField.TYPE_NOT_STORED
			);

		[System.Serializable]
		private class BadNews : SystemException
		{
			public BadNews(string message) : base(message)
			{
			}
		}

		private class ThrowingAnalyzer : Analyzer
		{
		    public override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, TextReader input)
			{
				Tokenizer tokenizer = new MockTokenizer(input);
				if (fieldName.Equals("distinctiveFieldName"))
				{
					TokenFilter tosser = new AnonymousTokenFilter(tokenizer);
					return new Analyzer.TokenStreamComponents(tokenizer, tosser);
				}
				else
				{
					return new Analyzer.TokenStreamComponents(tokenizer);
				}
			}

			private sealed class AnonymousTokenFilter : TokenFilter
			{
				public AnonymousTokenFilter(TokenStream baseArg1) : base(baseArg1)
				{
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override bool IncrementToken()
				{
					throw new TestDocInverterPerFieldErrorInfo.BadNews("Something is icky.");
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		[Test]
		public virtual void TestInfoStreamGetsFieldName()
		{
			Lucene.Net.Store.Directory dir = NewDirectory();
			IndexWriter writer;
			var c = new IndexWriterConfig(TEST_VERSION_CURRENT, new ThrowingAnalyzer());
			var infoBytes = new MemoryStream();
			TextWriter infoPrintStream = new StreamWriter(infoBytes, Encoding.UTF8);
			var printStreamInfoStream = new PrintStreamInfoStream(infoPrintStream);
			c.SetInfoStream(printStreamInfoStream);
			writer = new IndexWriter(dir, c);
			var doc = new Lucene.Net.Documents.Document
			{
			    NewField("distinctiveFieldName", "aaa ", storedTextType)
			};
		    try
			{
				writer.AddDocument(doc);
				Fail("Failed to fail.");
			}
			catch (BadNews)
			{
				infoPrintStream.Flush();
			    string infoStream = Encoding.UTF8.GetString(infoBytes.GetBuffer());
				IsTrue(infoStream.Contains("distinctiveFieldName"));
			}
			writer.Dispose();
			dir.Dispose();
		}

		
		[Test]
		public virtual void TestNoExtraNoise()
		{
			Lucene.Net.Store.Directory dir = NewDirectory();
			IndexWriter writer;
			var c = new IndexWriterConfig(TEST_VERSION_CURRENT, new ThrowingAnalyzer());
			var infoBytes = new MemoryStream();
			TextWriter infoPrintStream = new StreamWriter(infoBytes, new UTF8Encoding());
			var printStreamInfoStream = new PrintStreamInfoStream(infoPrintStream);
			c.SetInfoStream(printStreamInfoStream);
			writer = new IndexWriter(dir, c);
			var doc = new Lucene.Net.Documents.Document
			{
			    NewField("boringFieldName", "aaa ", storedTextType)
			};
		    try
			{
				writer.AddDocument(doc);
			}
			catch (TestDocInverterPerFieldErrorInfo.BadNews)
			{
				Fail("Unwanted exception");
			}
			infoPrintStream.Flush();
		    string infoStream = new UTF8Encoding().GetString(infoBytes.GetBuffer());
			IsFalse(infoStream.Contains("boringFieldName"));
			writer.Dispose();
			dir.Dispose();
		}
	}
}
