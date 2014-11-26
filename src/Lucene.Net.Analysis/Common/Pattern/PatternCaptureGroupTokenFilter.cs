/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Pattern
{
	/// <summary>
	/// CaptureGroup uses Java regexes to emit multiple tokens - one for each capture
	/// group in one or more patterns.
	/// </summary>
	/// <remarks>
	/// CaptureGroup uses Java regexes to emit multiple tokens - one for each capture
	/// group in one or more patterns.
	/// <p>
	/// For example, a pattern like:
	/// </p>
	/// <p>
	/// <code>"(https?://([a-zA-Z\-_0-9.]+))"</code>
	/// </p>
	/// <p>
	/// when matched against the string "http://www.foo.com/index" would return the
	/// tokens "https://www.foo.com" and "www.foo.com".
	/// </p>
	/// <p>
	/// If none of the patterns match, or if preserveOriginal is true, the original
	/// token will be preserved.
	/// </p>
	/// <p>
	/// Each pattern is matched as often as it can be, so the pattern
	/// <code> "(...)"</code>, when matched against <code>"abcdefghi"</code> would
	/// produce <code>["abc","def","ghi"]</code>
	/// </p>
	/// <p>
	/// A camelCaseFilter could be written as:
	/// </p>
	/// <p>
	/// <code>
	/// "([A-Z]{2,})",                                 <br />
	/// "(?&lt;![A-Z])([A-Z][a-z]+)",                     <br />
	/// "(?:^|\\b|(?&lt;=[0-9_])|(?&lt;=[A-Z]{2}))([a-z]+)", <br />
	/// "([0-9]+)"
	/// </code>
	/// </p>
	/// <p>
	/// plus if
	/// <see cref="preserveOriginal">preserveOriginal</see>
	/// is true, it would also return
	/// <code>"camelCaseFilter</code>
	/// </p>
	/// </remarks>
	public sealed class PatternCaptureGroupTokenFilter : TokenFilter
	{
		private readonly CharTermAttribute charTermAttr = AddAttribute<CharTermAttribute>
			();

		private readonly PositionIncrementAttribute posAttr = AddAttribute<PositionIncrementAttribute
			>();

		private AttributeSource.State state;

		private readonly Matcher[] matchers;

		private readonly CharsRef spare = new CharsRef();

		private readonly int[] groupCounts;

		private readonly bool preserveOriginal;

		private int[] currentGroup;

		private int currentMatcher;

		/// <param name="input">
		/// the input
		/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
		/// 	</see>
		/// </param>
		/// <param name="preserveOriginal">
		/// set to true to return the original token even if one of the
		/// patterns matches
		/// </param>
		/// <param name="patterns">
		/// an array of
		/// <see cref="Sharpen.Pattern">Sharpen.Pattern</see>
		/// objects to match against each token
		/// </param>
		public PatternCaptureGroupTokenFilter(TokenStream input, bool preserveOriginal, params 
			Sharpen.Pattern[] patterns) : base(input)
		{
			this.preserveOriginal = preserveOriginal;
			this.matchers = new Matcher[patterns.Length];
			this.groupCounts = new int[patterns.Length];
			this.currentGroup = new int[patterns.Length];
			for (int i = 0; i < patterns.Length; i++)
			{
				this.matchers[i] = patterns[i].Matcher(string.Empty);
				this.groupCounts[i] = this.matchers[i].GroupCount();
				this.currentGroup[i] = -1;
			}
		}

		private bool NextCapture()
		{
			int min_offset = int.MaxValue;
			currentMatcher = -1;
			Matcher matcher;
			for (int i = 0; i < matchers.Length; i++)
			{
				matcher = matchers[i];
				if (currentGroup[i] == -1)
				{
					currentGroup[i] = matcher.Find() ? 1 : 0;
				}
				if (currentGroup[i] != 0)
				{
					while (currentGroup[i] < groupCounts[i] + 1)
					{
						int start = matcher.Start(currentGroup[i]);
						int end = matcher.End(currentGroup[i]);
						if (start == end || preserveOriginal && start == 0 && spare.length == end)
						{
							currentGroup[i]++;
							continue;
						}
						if (start < min_offset)
						{
							min_offset = start;
							currentMatcher = i;
						}
						break;
					}
					if (currentGroup[i] == groupCounts[i] + 1)
					{
						currentGroup[i] = -1;
						i--;
					}
				}
			}
			return currentMatcher != -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (currentMatcher != -1 && NextCapture())
			{
				//HM:revisit 
				//assert state != null;
				ClearAttributes();
				RestoreState(state);
				int start = matchers[currentMatcher].Start(currentGroup[currentMatcher]);
				int end = matchers[currentMatcher].End(currentGroup[currentMatcher]);
				posAttr.SetPositionIncrement(0);
				charTermAttr.CopyBuffer(spare.chars, start, end - start);
				currentGroup[currentMatcher]++;
				return true;
			}
			if (!input.IncrementToken())
			{
				return false;
			}
			char[] buffer = charTermAttr.Buffer();
			int length = charTermAttr.Length;
			spare.CopyChars(buffer, 0, length);
			state = CaptureState();
			for (int i = 0; i < matchers.Length; i++)
			{
				matchers[i].Reset(spare);
				currentGroup[i] = -1;
			}
			if (preserveOriginal)
			{
				currentMatcher = 0;
			}
			else
			{
				if (NextCapture())
				{
					int start = matchers[currentMatcher].Start(currentGroup[currentMatcher]);
					int end = matchers[currentMatcher].End(currentGroup[currentMatcher]);
					// if we start at 0 we can simply set the length and save the copy
					if (start == 0)
					{
						charTermAttr.SetLength(end);
					}
					else
					{
						charTermAttr.CopyBuffer(spare.chars, start, end - start);
					}
					currentGroup[currentMatcher]++;
				}
			}
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			state = null;
			currentMatcher = -1;
		}
	}
}
