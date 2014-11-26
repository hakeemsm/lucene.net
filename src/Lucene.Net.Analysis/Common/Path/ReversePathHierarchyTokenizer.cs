/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Path
{
	/// <summary>Tokenizer for domain-like hierarchies.</summary>
	/// <remarks>
	/// Tokenizer for domain-like hierarchies.
	/// <p>
	/// Take something like:
	/// <pre>
	/// www.site.co.uk
	/// </pre>
	/// and make:
	/// <pre>
	/// www.site.co.uk
	/// site.co.uk
	/// co.uk
	/// uk
	/// </pre>
	/// </remarks>
	public class ReversePathHierarchyTokenizer : Tokenizer
	{
		protected ReversePathHierarchyTokenizer(StreamReader input) : this(input, DEFAULT_BUFFER_SIZE
			, DEFAULT_DELIMITER, DEFAULT_DELIMITER, DEFAULT_SKIP)
		{
		}

		public ReversePathHierarchyTokenizer(StreamReader input, int skip) : this(input, 
			DEFAULT_BUFFER_SIZE, DEFAULT_DELIMITER, DEFAULT_DELIMITER, skip)
		{
		}

		public ReversePathHierarchyTokenizer(StreamReader input, int bufferSize, char delimiter
			) : this(input, bufferSize, delimiter, delimiter, DEFAULT_SKIP)
		{
		}

		public ReversePathHierarchyTokenizer(StreamReader input, char delimiter, char replacement
			) : this(input, DEFAULT_BUFFER_SIZE, delimiter, replacement, DEFAULT_SKIP)
		{
		}

		public ReversePathHierarchyTokenizer(StreamReader input, int bufferSize, char delimiter
			, char replacement) : this(input, bufferSize, delimiter, replacement, DEFAULT_SKIP
			)
		{
		}

		public ReversePathHierarchyTokenizer(StreamReader input, char delimiter, int skip
			) : this(input, DEFAULT_BUFFER_SIZE, delimiter, delimiter, skip)
		{
		}

		public ReversePathHierarchyTokenizer(StreamReader input, char delimiter, char replacement
			, int skip) : this(input, DEFAULT_BUFFER_SIZE, delimiter, replacement, skip)
		{
		}

		public ReversePathHierarchyTokenizer(AttributeSource.AttributeFactory factory, StreamReader
			 input, char delimiter, char replacement, int skip) : this(factory, input, DEFAULT_BUFFER_SIZE
			, delimiter, replacement, skip)
		{
		}

		public ReversePathHierarchyTokenizer(StreamReader input, int bufferSize, char delimiter
			, char replacement, int skip) : this(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY
			, input, bufferSize, delimiter, replacement, skip)
		{
		}

		public ReversePathHierarchyTokenizer(AttributeSource.AttributeFactory factory, StreamReader
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
			resultTokenBuffer = new char[bufferSize];
			delimiterPositions = new AList<int>(bufferSize / 10);
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

		private int endPosition = 0;

		private int finalOffset = 0;

		private int skipped = 0;

		private StringBuilder resultToken;

		private IList<int> delimiterPositions;

		private int delimitersCount = -1;

		private char[] resultTokenBuffer;

		/// <exception cref="System.IO.IOException"></exception>
		public sealed override bool IncrementToken()
		{
			ClearAttributes();
			if (delimitersCount == -1)
			{
				int length = 0;
				delimiterPositions.AddItem(0);
				while (true)
				{
					int c = input.Read();
					if (c < 0)
					{
						break;
					}
					length++;
					if (c == delimiter)
					{
						delimiterPositions.AddItem(length);
						resultToken.Append(replacement);
					}
					else
					{
						resultToken.Append((char)c);
					}
				}
				delimitersCount = delimiterPositions.Count;
				if (delimiterPositions[delimitersCount - 1] < length)
				{
					delimiterPositions.AddItem(length);
					delimitersCount++;
				}
				if (resultTokenBuffer.Length < resultToken.Length)
				{
					resultTokenBuffer = new char[resultToken.Length];
				}
				resultToken.GetChars(0, resultToken.Length, resultTokenBuffer, 0);
				resultToken.Length = 0;
				int idx = delimitersCount - 1 - skip;
				if (idx >= 0)
				{
					// otherwise its ok, because we will skip and return false
					endPosition = delimiterPositions[idx];
				}
				finalOffset = CorrectOffset(length);
				posAtt.SetPositionIncrement(1);
			}
			else
			{
				posAtt.SetPositionIncrement(0);
			}
			while (skipped < delimitersCount - skip - 1)
			{
				int start = delimiterPositions[skipped];
				termAtt.CopyBuffer(resultTokenBuffer, start, endPosition - start);
				offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(endPosition));
				skipped++;
				return true;
			}
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public sealed override void End()
		{
			base.End();
			// set final offset
			offsetAtt.SetOffset(finalOffset, finalOffset);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			resultToken.Length = 0;
			finalOffset = 0;
			endPosition = 0;
			skipped = 0;
			delimitersCount = -1;
			delimiterPositions.Clear();
		}
	}
}
