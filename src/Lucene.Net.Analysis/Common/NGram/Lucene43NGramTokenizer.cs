/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Ngram
{
	/// <summary>
	/// Old broken version of
	/// <see cref="NGramTokenizer">NGramTokenizer</see>
	/// .
	/// </summary>
	public sealed class Lucene43NGramTokenizer : Tokenizer
	{
		public const int DEFAULT_MIN_NGRAM_SIZE = 1;

		public const int DEFAULT_MAX_NGRAM_SIZE = 2;

		private int minGram;

		private int maxGram;

		private int gramSize;

		private int pos;

		private int inLen;

		private int charsRead;

		private string inStr;

		private bool started;

		private CharTermAttribute termAtt;
	    private OffsetAttribute offsetAtt;

		/// <summary>Creates NGramTokenizer with given min and max n-grams.</summary>
		/// <remarks>Creates NGramTokenizer with given min and max n-grams.</remarks>
		/// <param name="input">
		/// 
		/// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
		/// holding the input to be tokenized
		/// </param>
		/// <param name="minGram">the smallest n-gram to generate</param>
		/// <param name="maxGram">the largest n-gram to generate</param>
		public Lucene43NGramTokenizer(StreamReader input, int minGram, int maxGram) : base(input)
		{
			// length of the input AFTER trim()
			// length of the input
			Init(minGram, maxGram);
		}

		/// <summary>Creates NGramTokenizer with given min and max n-grams.</summary>
		/// <remarks>Creates NGramTokenizer with given min and max n-grams.</remarks>
		/// <param name="factory">
		/// 
		/// <see cref="Lucene.Net.Util.AttributeSource.AttributeFactory">Lucene.Net.Util.AttributeSource.AttributeFactory
		/// 	</see>
		/// to use
		/// </param>
		/// <param name="input">
		/// 
		/// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
		/// holding the input to be tokenized
		/// </param>
		/// <param name="minGram">the smallest n-gram to generate</param>
		/// <param name="maxGram">the largest n-gram to generate</param>
		public Lucene43NGramTokenizer(AttributeFactory factory, StreamReader
			 input, int minGram, int maxGram) : base(factory, input)
		{
			Init(minGram, maxGram);
		}

		/// <summary>Creates NGramTokenizer with default min and max n-grams.</summary>
		/// <remarks>Creates NGramTokenizer with default min and max n-grams.</remarks>
		/// <param name="input">
		/// 
		/// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
		/// holding the input to be tokenized
		/// </param>
		protected Lucene43NGramTokenizer(StreamReader input) : this(input, DEFAULT_MIN_NGRAM_SIZE
			, DEFAULT_MAX_NGRAM_SIZE)
		{
		}

		private void Init(int minGram, int maxGram)
		{
			if (minGram < 1)
			{
				throw new ArgumentException("minGram must be greater than zero");
			}
			if (minGram > maxGram)
			{
				throw new ArgumentException("minGram must not be greater than maxGram");
			}
			this.minGram = minGram;
			this.maxGram = maxGram;
            termAtt = AddAttribute<CharTermAttribute>();
            offsetAtt = AddAttribute<OffsetAttribute>();
		}

		/// <summary>Returns the next token in the stream, or null at EOS.</summary>
		/// <remarks>Returns the next token in the stream, or null at EOS.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			ClearAttributes();
			if (!started)
			{
				started = true;
				gramSize = minGram;
				char[] chars = new char[1024];
				charsRead = 0;
				// TODO: refactor to a shared readFully somewhere:
				while (charsRead < chars.Length)
				{
					int inc = input.Read(chars, charsRead, chars.Length - charsRead);
					if (inc == -1)
					{
						break;
					}
					charsRead += inc;
				}
				inStr = new string(chars, 0, charsRead).Trim();
				// remove any trailing empty strings 
				if (charsRead == chars.Length)
				{
					// Read extra throwaway chars so that on end() we
					// report the correct offset:
					char[] throwaway = new char[1024];
					while (true)
					{
						int inc = input.Read(throwaway, 0, throwaway.Length);
						if (inc == -1)
						{
							break;
						}
						charsRead += inc;
					}
				}
				inLen = inStr.Length;
				if (inLen == 0)
				{
					return false;
				}
			}
			if (pos + gramSize > inLen)
			{
				// if we hit the end of the string
				pos = 0;
				// reset to beginning of string
				gramSize++;
				// increase n-gram size
				if (gramSize > maxGram)
				{
					// we are done
					return false;
				}
				if (pos + gramSize > inLen)
				{
					return false;
				}
			}
			int oldPos = pos;
			pos++;
			termAtt.SetEmpty().Append(inStr, oldPos, oldPos + gramSize);
			offsetAtt.SetOffset(CorrectOffset(oldPos), CorrectOffset(oldPos + gramSize));
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void End()
		{
			base.End();
			// set final offset
			int finalOffset = CorrectOffset(charsRead);
			this.offsetAtt.SetOffset(finalOffset, finalOffset);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			started = false;
			pos = 0;
		}
	}
}
