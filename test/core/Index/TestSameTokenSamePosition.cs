using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
	public class TestSameTokenSamePosition : LuceneTestCase
	{
		/// <summary>
		/// Attempt to reproduce an assertion error that happens
		/// only with the trunk version around April 2011.
		/// </summary>
		/// <remarks>
		/// Attempt to reproduce an assertion error that happens
		/// only with the trunk version around April 2011.
		/// </remarks>
		[Test]
		public virtual void TestTextFieldDoc()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter riw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
			{
			    new TextField("eng", new BugReproTokenStream())
			};
		    riw.AddDocument(doc);
			riw.Dispose();
			dir.Dispose();
		}

		/// <summary>Same as the above, but with more docs</summary>
		[Test]
		public virtual void TestMoreDocs()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter riw = new RandomIndexWriter(Random(), dir);
			for (int i = 0; i < 100; i++)
			{
				var doc = new Lucene.Net.Documents.Document
				{
				    new TextField("eng", new BugReproTokenStream())
				};
			    riw.AddDocument(doc);
			}
			riw.Dispose();
			dir.Dispose();
		}
	}

	internal sealed class BugReproTokenStream : TokenStream
	{
	    private readonly CharTermAttribute termAtt;

	    private readonly OffsetAttribute offsetAtt;

	    private readonly PositionIncrementAttribute posIncAtt;

		private readonly int tokenCount = 4;

		private int nextTokenIndex = 0;

		private readonly string[] terms = { "six", "six", "drunken", "drunken"};

		private readonly int[] starts = { 0, 0, 4, 4 };

		private readonly int[] ends = { 3, 3, 11, 11 };

		private readonly int[] incs = { 1, 0, 1, 0 };

	    public BugReproTokenStream()
	    {
            termAtt = AddAttribute<CharTermAttribute>();
            offsetAtt = AddAttribute<OffsetAttribute>();
            posIncAtt = AddAttribute<PositionIncrementAttribute>();
	    }

		public override bool IncrementToken()
		{
		    if (nextTokenIndex < tokenCount)
			{
				termAtt.SetEmpty().Append(terms[nextTokenIndex]);
				offsetAtt.SetOffset(starts[nextTokenIndex], ends[nextTokenIndex]);
				posIncAtt.PositionIncrement = (incs[nextTokenIndex]);
				nextTokenIndex++;
				return true;
			}
		    return false;
		}

	    /// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			this.nextTokenIndex = 0;
		}
	}
}
