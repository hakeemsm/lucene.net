/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Ngram;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Ngram
{
	/// <summary>
	/// Factory for
	/// <see cref="NGramTokenizer">NGramTokenizer</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_ngrm" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.NGramTokenizerFactory" minGramSize="1" maxGramSize="2"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class NGramTokenizerFactory : TokenizerFactory
	{
		private readonly int maxGramSize;

		private readonly int minGramSize;

		/// <summary>Creates a new NGramTokenizerFactory</summary>
		protected internal NGramTokenizerFactory(IDictionary<string, string> args) : base
			(args)
		{
			minGramSize = GetInt(args, "minGramSize", NGramTokenizer.DEFAULT_MIN_NGRAM_SIZE);
			maxGramSize = GetInt(args, "maxGramSize", NGramTokenizer.DEFAULT_MAX_NGRAM_SIZE);
			if (!args.IsEmpty())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		/// <summary>
		/// Creates the
		/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
		/// 	</see>
		/// of n-grams from the given
		/// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
		/// and
		/// <see cref="Lucene.Net.Util.AttributeSource.AttributeFactory">Lucene.Net.Util.AttributeSource.AttributeFactory
		/// 	</see>
		/// .
		/// </summary>
		public override Tokenizer Create(AttributeSource.AttributeFactory factory, StreamReader
			 input)
		{
			if (VersionHelper.OnOrAfter(luceneMatchVersion, Version.LUCENE_44))
			{
				return new NGramTokenizer(luceneMatchVersion, factory, input, minGramSize, maxGramSize
					);
			}
			else
			{
				return new Lucene43NGramTokenizer(factory, input, minGramSize, maxGramSize);
			}
		}
	}
}
