/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using System.Text;
using Lucene.Net.Analysis.Charfilter;
using Sharpen;

namespace Lucene.Net.Analysis.Pattern
{
	/// <summary>CharFilter that uses a regular expression for the target of replace string.
	/// 	</summary>
	/// <remarks>
	/// CharFilter that uses a regular expression for the target of replace string.
	/// The pattern match will be done in each "block" in char stream.
	/// <p>
	/// ex1) source="aa&nbsp;&nbsp;bb&nbsp;aa&nbsp;bb", pattern="(aa)\\s+(bb)" replacement="$1#$2"<br/>
	/// output="aa#bb&nbsp;aa#bb"
	/// </p>
	/// NOTE: If you produce a phrase that has different length to source string
	/// and the field is used for highlighting for a term of the phrase, you will
	/// face a trouble.
	/// <p>
	/// ex2) source="aa123bb", pattern="(aa)\\d+(bb)" replacement="$1&nbsp;$2"<br/>
	/// output="aa&nbsp;bb"<br/>
	/// and you want to search bb and highlight it, you will get<br/>
	/// highlight snippet="aa1&lt;em&gt;23bb&lt;/em&gt;"
	/// </p>
	/// </remarks>
	/// <since>Solr 1.5</since>
	public class PatternReplaceCharFilter : BaseCharFilter
	{
		[Obsolete]
		public const int DEFAULT_MAX_BLOCK_CHARS = 10000;

		private readonly Sharpen.Pattern pattern;

		private readonly string replacement;

		private StreamReader transformedInput;

		public PatternReplaceCharFilter(Sharpen.Pattern pattern, string replacement, StreamReader
			 @in) : base(@in)
		{
			this.pattern = pattern;
			this.replacement = replacement;
		}

		[Obsolete]
		public PatternReplaceCharFilter(Sharpen.Pattern pattern, string replacement, int 
			maxBlockChars, string blockDelimiter, StreamReader @in) : this(pattern, replacement
			, @in)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(char[] cbuf, int off, int len)
		{
			// Buffer all input on the first call.
			if (transformedInput == null)
			{
				Fill();
			}
			return transformedInput.Read(cbuf, off, len);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Fill()
		{
			StringBuilder buffered = new StringBuilder();
			char[] temp = new char[1024];
			for (int cnt = input.Read(temp); cnt > 0; cnt = input.Read(temp))
			{
				buffered.Append(temp, 0, cnt);
			}
			transformedInput = new StringReader(ProcessPattern(buffered).ToString());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read()
		{
			if (transformedInput == null)
			{
				Fill();
			}
			return transformedInput.Read();
		}

		protected override int Correct(int currentOff)
		{
			return Math.Max(0, base.Correct(currentOff));
		}

		/// <summary>Replace pattern in input and mark correction offsets.</summary>
		/// <remarks>Replace pattern in input and mark correction offsets.</remarks>
		internal virtual CharSequence ProcessPattern(CharSequence input)
		{
			Matcher m = pattern.Matcher(input);
			StringBuilder cumulativeOutput = new StringBuilder();
			int cumulative = 0;
			int lastMatchEnd = 0;
			while (m.Find())
			{
				int groupSize = m.End() - m.Start();
				int skippedSize = m.Start() - lastMatchEnd;
				lastMatchEnd = m.End();
				int lengthBeforeReplacement = cumulativeOutput.Length + skippedSize;
				m.AppendReplacement(cumulativeOutput, replacement);
				// Matcher doesn't tell us how many characters have been appended before the replacement.
				// So we need to calculate it. Skipped characters have been added as part of appendReplacement.
				int replacementSize = cumulativeOutput.Length - lengthBeforeReplacement;
				if (groupSize != replacementSize)
				{
					if (replacementSize < groupSize)
					{
						// The replacement is smaller. 
						// Add the 'backskip' to the next index after the replacement (this is possibly 
						// after the end of string, but it's fine -- it just means the last character 
						// of the replaced block doesn't reach the end of the original string.
						cumulative += groupSize - replacementSize;
						int atIndex = lengthBeforeReplacement + replacementSize;
						// System.err.println(atIndex + "!" + cumulative);
						AddOffCorrectMap(atIndex, cumulative);
					}
					else
					{
						// The replacement is larger. Every new index needs to point to the last
						// element of the original group (if any).
						for (int i = groupSize; i < replacementSize; i++)
						{
							AddOffCorrectMap(lengthBeforeReplacement + i, --cumulative);
						}
					}
				}
			}
			// System.err.println((lengthBeforeReplacement + i) + " " + cumulative);
			// Append the remaining output, no further changes to indices.
			m.AppendTail(cumulativeOutput);
			return cumulativeOutput;
		}
	}
}
