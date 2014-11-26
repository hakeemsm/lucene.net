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

namespace Lucene.Net.Analysis.Path
{
	/// <summary>Tokenizer for path-like hierarchies.</summary>
	/// <remarks>
	/// Tokenizer for path-like hierarchies.
	/// <p>
	/// Take something like:
	/// <pre>
	/// /something/something/else
	/// </pre>
	/// and make:
	/// <pre>
	/// /something
	/// /something/something
	/// /something/something/else
	/// </pre>
	/// </remarks>
	public class PathHierarchyTokenizer : Tokenizer
	{
		protected PathHierarchyTokenizer(StreamReader input) : this(input, DEFAULT_BUFFER_SIZE
			, DEFAULT_DELIMITER, DEFAULT_DELIMITER, DEFAULT_SKIP)
		{
		}

		public PathHierarchyTokenizer(StreamReader input, int skip) : this(input, DEFAULT_BUFFER_SIZE
			, DEFAULT_DELIMITER, DEFAULT_DELIMITER, skip)
		{
		}

		public PathHierarchyTokenizer(StreamReader input, int bufferSize, char delimiter)
			 : this(input, bufferSize, delimiter, delimiter, DEFAULT_SKIP)
		{
		}

		public PathHierarchyTokenizer(StreamReader input, char delimiter, char replacement
			) : this(input, DEFAULT_BUFFER_SIZE, delimiter, replacement, DEFAULT_SKIP)
		{
		}

		public PathHierarchyTokenizer(StreamReader input, char delimiter, char replacement
			, int skip) : this(input, DEFAULT_BUFFER_SIZE, delimiter, replacement, skip)
		{
		}

		public PathHierarchyTokenizer(AttributeSource.AttributeFactory factory, StreamReader
			 input, char delimiter, char replacement, int skip) : this(factory, input, DEFAULT_BUFFER_SIZE
			, delimiter, replacement, skip)
		{
		}

		public PathHierarchyTokenizer(StreamReader input, int bufferSize, char delimiter, 
			char replacement, int skip) : this(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY
			, input, bufferSize, delimiter, replacement, skip)
		{
		}

		public PathHierarchyTokenizer(AttributeSource.AttributeFactory factory, StreamReader
			 input, int bufferSize, char delimiter, char replacement, int skip) : base(factory
			, input)
		{
			if (bufferSize < 0)
			{
				throw new ArgumentException("bufferSize cannot be negative");
			}
			if (skip < 0)
			{
				throw new ArgumentException("skip cannot be negative");
			}
			termAtt.ResizeBuffer(bufferSize);
			this.delimiter = delimiter;
			this.replacement = replacement;
			this.skip = skip;
			resultToken = new StringBuilder(bufferSize);
		}

		private const int DEFAULT_BUFFER_SIZE = 1024;

		public const char DEFAULT_DELIMITER = '/';

		public const int DEFAULT_SKIP = 0;

		private readonly char delimiter;

		private readonly char replacement;

		private readonly int skip;

		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly OffsetAttribute offsetAtt = AddAttribute<OffsetAttribute>();

		private readonly PositionIncrementAttribute posAtt = AddAttribute<PositionIncrementAttribute
			>();

		private int startPosition = 0;

		private int skipped = 0;

		private bool endDelimiter = false;

		private StringBuilder resultToken;

		private int charsRead = 0;

		/// <exception cref="System.IO.IOException"></exception>
		public sealed override bool IncrementToken()
		{
			ClearAttributes();
			termAtt.Append(resultToken);
			if (resultToken.Length == 0)
			{
				posAtt.SetPositionIncrement(1);
			}
			else
			{
				posAtt.SetPositionIncrement(0);
			}
			int length = 0;
			bool added = false;
			if (endDelimiter)
			{
				termAtt.Append(replacement);
				length++;
				endDelimiter = false;
				added = true;
			}
			while (true)
			{
				int c = input.Read();
				if (c >= 0)
				{
					charsRead++;
				}
				else
				{
					if (skipped > skip)
					{
						length += resultToken.Length;
						termAtt.SetLength(length);
						offsetAtt.SetOffset(CorrectOffset(startPosition), CorrectOffset(startPosition + length
							));
						if (added)
						{
							resultToken.Length = 0;
							resultToken.Append(termAtt.Buffer, 0, length);
						}
						return added;
					}
					else
					{
						return false;
					}
				}
				if (!added)
				{
					added = true;
					skipped++;
					if (skipped > skip)
					{
						termAtt.Append(c == delimiter ? replacement : (char)c);
						length++;
					}
					else
					{
						startPosition++;
					}
				}
				else
				{
					if (c == delimiter)
					{
						if (skipped > skip)
						{
							endDelimiter = true;
							break;
						}
						skipped++;
						if (skipped > skip)
						{
							termAtt.Append(replacement);
							length++;
						}
						else
						{
							startPosition++;
						}
					}
					else
					{
						if (skipped > skip)
						{
							termAtt.Append((char)c);
							length++;
						}
						else
						{
							startPosition++;
						}
					}
				}
			}
			length += resultToken.Length;
			termAtt.SetLength(length);
			offsetAtt.SetOffset(CorrectOffset(startPosition), CorrectOffset(startPosition + length
				));
			resultToken.Length = 0;
			resultToken.Append(termAtt.Buffer, 0, length);
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public sealed override void End()
		{
			base.End();
			// set final offset
			int finalOffset = CorrectOffset(charsRead);
			offsetAtt.SetOffset(finalOffset, finalOffset);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			resultToken.Length = 0;
			charsRead = 0;
			endDelimiter = false;
			skipped = 0;
			startPosition = 0;
		}
	}
}
