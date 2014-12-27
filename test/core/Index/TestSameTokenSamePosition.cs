/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Test.Analysis.Tokenattributes;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

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
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter riw = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(new TextField("eng", new BugReproTokenStream()));
			riw.AddDocument(doc);
			riw.Dispose();
			dir.Dispose();
		}

		/// <summary>Same as the above, but with more docs</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMoreDocs()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter riw = new RandomIndexWriter(Random(), dir);
			for (int i = 0; i < 100; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(new TextField("eng", new BugReproTokenStream()));
				riw.AddDocument(doc);
			}
			riw.Dispose();
			dir.Dispose();
		}
	}

	internal sealed class BugReproTokenStream : TokenStream
	{
		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly OffsetAttribute offsetAtt = AddAttribute<OffsetAttribute>();

		private readonly PositionIncrementAttribute posIncAtt = AddAttribute<PositionIncrementAttribute
			>();

		private readonly int tokenCount = 4;

		private int nextTokenIndex = 0;

		private readonly string terms = new string[] { "six", "six", "drunken", "drunken"
			 };

		private readonly int starts = new int[] { 0, 0, 4, 4 };

		private readonly int ends = new int[] { 3, 3, 11, 11 };

		private readonly int incs = new int[] { 1, 0, 1, 0 };

		public override bool IncrementToken()
		{
			if (nextTokenIndex < tokenCount)
			{
				termAtt.SetEmpty().Append(terms[nextTokenIndex]);
				offsetAtt.SetOffset(starts[nextTokenIndex], ends[nextTokenIndex]);
				posIncAtt.SetPositionIncrement(incs[nextTokenIndex]);
				nextTokenIndex++;
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			this.nextTokenIndex = 0;
		}
	}
}
