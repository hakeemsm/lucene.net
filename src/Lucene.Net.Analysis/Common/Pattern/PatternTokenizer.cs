/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Pattern
{
	/// <summary>
	/// This tokenizer uses regex pattern matching to construct distinct tokens
	/// for the input stream.
	/// </summary>
	/// <remarks>
	/// This tokenizer uses regex pattern matching to construct distinct tokens
	/// for the input stream.  It takes two arguments:  "pattern" and "group".
	/// <p/>
	/// <ul>
	/// <li>"pattern" is the regular expression.</li>
	/// <li>"group" says which group to extract into tokens.</li>
	/// </ul>
	/// <p>
	/// group=-1 (the default) is equivalent to "split".  In this case, the tokens will
	/// be equivalent to the output from (without empty tokens):
	/// <see cref="string.Split(string)">string.Split(string)</see>
	/// </p>
	/// <p>
	/// Using group &gt;= 0 selects the matching group as the token.  For example, if you have:<br/>
	/// <pre>
	/// pattern = \'([^\']+)\'
	/// group = 0
	/// input = aaa 'bbb' 'ccc'
	/// </pre>
	/// the output will be two tokens: 'bbb' and 'ccc' (including the ' marks).  With the same input
	/// but using group=1, the output would be: bbb and ccc (no ' marks)
	/// </p>
	/// <p>NOTE: This Tokenizer does not output tokens that are of zero length.</p>
	/// </remarks>
	/// <seealso cref="Sharpen.Pattern">Sharpen.Pattern</seealso>
	public sealed class PatternTokenizer : Tokenizer
	{
		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly OffsetAttribute offsetAtt = AddAttribute<OffsetAttribute>();

		private readonly StringBuilder str = new StringBuilder();

		private int index;

		private readonly int group;

		private readonly Matcher matcher;

		/// <summary>creates a new PatternTokenizer returning tokens from group (-1 for split functionality)
		/// 	</summary>
		public PatternTokenizer(StreamReader input, Sharpen.Pattern pattern, int group) : 
			this(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, pattern, 
			group)
		{
		}

		/// <summary>creates a new PatternTokenizer returning tokens from group (-1 for split functionality)
		/// 	</summary>
		public PatternTokenizer(AttributeSource.AttributeFactory factory, StreamReader input
			, Sharpen.Pattern pattern, int group) : base(factory, input)
		{
			this.group = group;
			// Use "" instead of str so don't consume chars
			// (fillBuffer) from the input on throwing IAE below:
			matcher = pattern.Matcher(string.Empty);
			// confusingly group count depends ENTIRELY on the pattern but is only accessible via matcher
			if (group >= 0 && group > matcher.GroupCount())
			{
				throw new ArgumentException("invalid group specified: pattern only has: " + matcher
					.GroupCount() + " capturing groups");
			}
		}

		public override bool IncrementToken()
		{
			if (index >= str.Length)
			{
				return false;
			}
			ClearAttributes();
			if (group >= 0)
			{
				// match a specific group
				while (matcher.Find())
				{
					index = matcher.Start(group);
					int endIndex = matcher.End(group);
					if (index == endIndex)
					{
						continue;
					}
					termAtt.SetEmpty().AppendRange(str, index, endIndex);
					offsetAtt.SetOffset(CorrectOffset(index), CorrectOffset(endIndex));
					return true;
				}
				index = int.MaxValue;
				// mark exhausted
				return false;
			}
			else
			{
				// String.split() functionality
				while (matcher.Find())
				{
					if (matcher.Start() - index > 0)
					{
						// found a non-zero-length token
						termAtt.SetEmpty().AppendRange(str, index, matcher.Start());
						offsetAtt.SetOffset(CorrectOffset(index), CorrectOffset(matcher.Start()));
						index = matcher.End();
						return true;
					}
					index = matcher.End();
				}
				if (str.Length - index == 0)
				{
					index = int.MaxValue;
					// mark exhausted
					return false;
				}
				termAtt.SetEmpty().AppendRange(str, index, str.Length);
				offsetAtt.SetOffset(CorrectOffset(index), CorrectOffset(str.Length));
				index = int.MaxValue;
				// mark exhausted
				return true;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void End()
		{
			base.End();
			int ofs = CorrectOffset(str.Length);
			offsetAtt.SetOffset(ofs, ofs);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			FillBuffer(str, input);
			matcher.Reset(str);
			index = 0;
		}

		internal readonly char[] buffer = new char[8192];

		// TODO: we should see if we can make this tokenizer work without reading
		// the entire document into RAM, perhaps with Matcher.hitEnd/requireEnd ?
		/// <exception cref="System.IO.IOException"></exception>
		private void FillBuffer(StringBuilder sb, StreamReader input)
		{
			int len;
			sb.Length = 0;
			while ((len = input.Read(buffer)) > 0)
			{
				sb.Append(buffer, 0, len);
			}
		}
	}
}
