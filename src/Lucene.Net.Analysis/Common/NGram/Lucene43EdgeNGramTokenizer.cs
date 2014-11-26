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
using Lucene.Net.Util.Fst;
using Version = System.Version;

namespace Lucene.Net.Analysis.Ngram
{
	/// <summary>
	/// Old version of
	/// <see cref="EdgeNGramTokenizer">EdgeNGramTokenizer</see>
	/// which doesn't handle correctly
	/// supplementary characters.
	/// </summary>
	public sealed class Lucene43EdgeNGramTokenizer : Tokenizer
	{
		public static readonly Lucene43EdgeNGramTokenizer.Side DEFAULT_SIDE = Lucene43EdgeNGramTokenizer.Side
			.FRONT;

		public const int DEFAULT_MAX_GRAM_SIZE = 1;

		public const int DEFAULT_MIN_GRAM_SIZE = 1;

		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly OffsetAttribute offsetAtt = AddAttribute<OffsetAttribute>();

		private readonly PositionIncrementAttribute posIncrAtt = AddAttribute<PositionIncrementAttribute>();

		/// <summary>Specifies which side of the input the n-gram should be generated from</summary>
		public enum Side
		{
			FRONT,
			BACK
		}

		

		private int minGram;

		private int maxGram;

		private int gramSize;

		private Lucene43EdgeNGramTokenizer.Side side;

		private bool started;

		private int inLen;

		private int charsRead;

		private string inStr;

		/// <summary>Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
		/// 	</summary>
		/// <param name="version">the <a href="#version">Lucene match version</a></param>
		/// <param name="input">
		/// 
		/// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
		/// holding the input to be tokenized
		/// </param>
		/// <param name="side">
		/// the
		/// <see cref="Side">Side</see>
		/// from which to chop off an n-gram
		/// </param>
		/// <param name="minGram">the smallest n-gram to generate</param>
		/// <param name="maxGram">the largest n-gram to generate</param>
		[Obsolete]
		public Lucene43EdgeNGramTokenizer(Version version, StreamReader input, Lucene43EdgeNGramTokenizer.Side
			 side, int minGram, int maxGram) : base(input)
		{
			// length of the input AFTER trim()
			// length of the input
			Init(version, side, minGram, maxGram);
		}

		/// <summary>Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
		/// 	</summary>
		/// <param name="version">the <a href="#version">Lucene match version</a></param>
		/// <param name="factory">
		/// 
		/// <see cref="Lucene.NetUtil.AttributeSource.AttributeFactory">Lucene.NetUtil.AttributeSource.AttributeFactory
		/// 	</see>
		/// to use
		/// </param>
		/// <param name="input">
		/// 
		/// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
		/// holding the input to be tokenized
		/// </param>
		/// <param name="side">
		/// the
		/// <see cref="Side">Side</see>
		/// from which to chop off an n-gram
		/// </param>
		/// <param name="minGram">the smallest n-gram to generate</param>
		/// <param name="maxGram">the largest n-gram to generate</param>
		[Obsolete]
		public Lucene43EdgeNGramTokenizer(Version version, AttributeSource.AttributeFactory
			 factory, StreamReader input, Lucene43EdgeNGramTokenizer.Side side, int minGram, 
			int maxGram) : base(factory, input)
		{
			Init(version, side, minGram, maxGram);
		}

		/// <summary>Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
		/// 	</summary>
		/// <param name="version">the <a href="#version">Lucene match version</a></param>
		/// <param name="input">
		/// 
		/// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
		/// holding the input to be tokenized
		/// </param>
		/// <param name="sideLabel">
		/// the name of the
		/// <see cref="Side">Side</see>
		/// from which to chop off an n-gram
		/// </param>
		/// <param name="minGram">the smallest n-gram to generate</param>
		/// <param name="maxGram">the largest n-gram to generate</param>
		[Obsolete]
		public Lucene43EdgeNGramTokenizer(Version version, StreamReader input, string sideLabel
			, int minGram, int maxGram) : this(version, input,  Lucene43EdgeNGramTokenizer.SideHelper
			.GetSide(sideLabel), minGram, maxGram)
		{
		}

		/// <summary>Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
		/// 	</summary>
		/// <param name="version">the <a href="#version">Lucene match version</a></param>
		/// <param name="factory">
		/// 
		/// <see cref="AttributeSource.AttributeFactory">Lucene.NetUtil.AttributeSource.AttributeFactory
		/// 	</see>
		/// to use
		/// </param>
		/// <param name="input">
		/// 
		/// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
		/// holding the input to be tokenized
		/// </param>
		/// <param name="sideLabel">
		/// the name of the
		/// <see cref="Side">Side</see>
		/// from which to chop off an n-gram
		/// </param>
		/// <param name="minGram">the smallest n-gram to generate</param>
		/// <param name="maxGram">the largest n-gram to generate</param>
		[Obsolete]
		public Lucene43EdgeNGramTokenizer(Version version, AttributeSource.AttributeFactory
			 factory, StreamReader input, string sideLabel, int minGram, int maxGram) : this
			(version, factory, input, Lucene43EdgeNGramTokenizer.SideHelper.GetSide(sideLabel
			), minGram, maxGram)
		{
		}

		/// <summary>Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
		/// 	</summary>
		/// <param name="version">the <a href="#version">Lucene match version</a></param>
		/// <param name="input">
		/// 
		/// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
		/// holding the input to be tokenized
		/// </param>
		/// <param name="minGram">the smallest n-gram to generate</param>
		/// <param name="maxGram">the largest n-gram to generate</param>
		public Lucene43EdgeNGramTokenizer(Version version, StreamReader input, int minGram
			, int maxGram) : this(version, input, Lucene43EdgeNGramTokenizer.Side.FRONT, minGram
			, maxGram)
		{
		}

		/// <summary>Creates EdgeNGramTokenizer that can generate n-grams in the sizes of the given range
		/// 	</summary>
		/// <param name="version">the <a href="#version">Lucene match version</a></param>
		/// <param name="factory">
		/// 
		/// <see cref="Lucene.NetUtil.AttributeSource.AttributeFactory">Lucene.NetUtil.AttributeSource.AttributeFactory
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
		public Lucene43EdgeNGramTokenizer(Version version, AttributeSource.AttributeFactory
			 factory, StreamReader input, int minGram, int maxGram) : this(version, factory, 
			input, Lucene43EdgeNGramTokenizer.Side.FRONT, minGram, maxGram)
		{
		}

		private void Init(Version version, Lucene43EdgeNGramTokenizer.Side side, int minGram
			, int maxGram)
		{
			if (version == null)
			{
				throw new ArgumentException("version must not be null");
			}
			if (side == null)
			{
				throw new ArgumentException("sideLabel must be either front or back");
			}
			if (minGram < 1)
			{
				throw new ArgumentException("minGram must be greater than zero");
			}
			if (minGram > maxGram)
			{
				throw new ArgumentException("minGram must not be greater than maxGram");
			}
			if (VersionHelper.OnOrAfter(version, Version.LUCENE_44))
			{
				if (side == Lucene43EdgeNGramTokenizer.Side.BACK)
				{
					throw new ArgumentException("Side.BACK is not supported anymore as of Lucene 4.4"
						);
				}
			}
			else
			{
				maxGram = Math.Min(maxGram, 1024);
			}
			this.minGram = minGram;
			this.maxGram = maxGram;
			this.side = side;
		}

		/// <summary>Returns the next token in the stream, or null at EOS.</summary>
		/// <remarks>Returns the next token in the stream, or null at EOS.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			ClearAttributes();
			// if we are just starting, read the whole input
			if (!started)
			{
				started = true;
				gramSize = minGram;
				int limit = side == Lucene43EdgeNGramTokenizer.Side.FRONT ? maxGram : 1024;
				char[] chars = new char[Math.Min(1024, limit)];
				charsRead = 0;
				// TODO: refactor to a shared readFully somewhere:
				bool exhausted = false;
				while (charsRead < limit)
				{
					int inc = input.Read(chars, charsRead, chars.Length - charsRead);
					if (inc == -1)
					{
						exhausted = true;
						break;
					}
					charsRead += inc;
					if (charsRead == chars.Length && charsRead < limit)
					{
						chars = ArrayUtil.Grow(chars);
					}
				}
				inStr = new string(chars, 0, charsRead);
				inStr = inStr.Trim();
				if (!exhausted)
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
				posIncrAtt.SetPositionIncrement(1);
			}
			else
			{
				posIncrAtt.SetPositionIncrement(0);
			}
			// if the remaining input is too short, we can't generate any n-grams
			if (gramSize > inLen)
			{
				return false;
			}
			// if we have hit the end of our n-gram size range, quit
			if (gramSize > maxGram || gramSize > inLen)
			{
				return false;
			}
			// grab gramSize chars from front or back
			int start = side == Lucene43EdgeNGramTokenizer.Side.FRONT ? 0 : inLen - gramSize;
			int end = start + gramSize;
			termAtt.SetEmpty().AppendRange(inStr, start, end);
			offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(end));
			gramSize++;
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
		}
	}
}
