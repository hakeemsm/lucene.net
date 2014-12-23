/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>Test adding to the info stream when there's an exception thrown during field analysis.
	/// 	</summary>
	/// <remarks>Test adding to the info stream when there's an exception thrown during field analysis.
	/// 	</remarks>
	public class TestDocInverterPerFieldErrorInfo : LuceneTestCase
	{
		private static readonly FieldType storedTextType = new FieldType(TextField.TYPE_NOT_STORED
			);

		[System.Serializable]
		private class BadNews : RuntimeException
		{
			public BadNews(string message) : base(message)
			{
			}
		}

		private class ThrowingAnalyzer : Analyzer
		{
			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader input)
			{
				Tokenizer tokenizer = new MockTokenizer(input);
				if (fieldName.Equals("distinctiveFieldName"))
				{
					TokenFilter tosser = new _TokenFilter_55(tokenizer);
					return new Analyzer.TokenStreamComponents(tokenizer, tosser);
				}
				else
				{
					return new Analyzer.TokenStreamComponents(tokenizer);
				}
			}

			private sealed class _TokenFilter_55 : TokenFilter
			{
				public _TokenFilter_55(TokenStream baseArg1) : base(baseArg1)
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
		[NUnit.Framework.Test]
		public virtual void TestInfoStreamGetsFieldName()
		{
			Directory dir = NewDirectory();
			IndexWriter writer;
			IndexWriterConfig c = new IndexWriterConfig(TEST_VERSION_CURRENT, new TestDocInverterPerFieldErrorInfo.ThrowingAnalyzer
				());
			ByteArrayOutputStream infoBytes = new ByteArrayOutputStream();
			TextWriter infoPrintStream = new TextWriter(infoBytes, true, IOUtils.UTF_8);
			PrintStreamInfoStream printStreamInfoStream = new PrintStreamInfoStream(infoPrintStream
				);
			c.SetInfoStream(printStreamInfoStream);
			writer = new IndexWriter(dir, c);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewField("distinctiveFieldName", "aaa ", storedTextType));
			try
			{
				writer.AddDocument(doc);
				NUnit.Framework.Assert.Fail("Failed to fail.");
			}
			catch (TestDocInverterPerFieldErrorInfo.BadNews)
			{
				infoPrintStream.Flush();
				string infoStream = Sharpen.Runtime.GetStringForBytes(infoBytes.ToByteArray(), IOUtils
					.UTF_8);
				NUnit.Framework.Assert.IsTrue(infoStream.Contains("distinctiveFieldName"));
			}
			writer.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestNoExtraNoise()
		{
			Directory dir = NewDirectory();
			IndexWriter writer;
			IndexWriterConfig c = new IndexWriterConfig(TEST_VERSION_CURRENT, new TestDocInverterPerFieldErrorInfo.ThrowingAnalyzer
				());
			ByteArrayOutputStream infoBytes = new ByteArrayOutputStream();
			TextWriter infoPrintStream = new TextWriter(infoBytes, true, IOUtils.UTF_8);
			PrintStreamInfoStream printStreamInfoStream = new PrintStreamInfoStream(infoPrintStream
				);
			c.SetInfoStream(printStreamInfoStream);
			writer = new IndexWriter(dir, c);
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			doc.Add(NewField("boringFieldName", "aaa ", storedTextType));
			try
			{
				writer.AddDocument(doc);
			}
			catch (TestDocInverterPerFieldErrorInfo.BadNews)
			{
				NUnit.Framework.Assert.Fail("Unwanted exception");
			}
			infoPrintStream.Flush();
			string infoStream = Sharpen.Runtime.GetStringForBytes(infoBytes.ToByteArray(), IOUtils
				.UTF_8);
			NUnit.Framework.Assert.IsFalse(infoStream.Contains("boringFieldName"));
			writer.Close();
			dir.Close();
		}
	}
}
