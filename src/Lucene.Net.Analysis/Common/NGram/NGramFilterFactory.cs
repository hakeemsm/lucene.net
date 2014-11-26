/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Ngram;
using Lucene.Net.Analysis.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Ngram
{
	/// <summary>
	/// Factory for
	/// <see cref="NGramTokenFilter">NGramTokenFilter</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_ngrm" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	/// &lt;filter class="solr.NGramFilterFactory" minGramSize="1" maxGramSize="2"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class NGramFilterFactory : TokenFilterFactory
	{
		private readonly int maxGramSize;

		private readonly int minGramSize;

		/// <summary>Creates a new NGramFilterFactory</summary>
		protected internal NGramFilterFactory(IDictionary<string, string> args) : base(args
			)
		{
			minGramSize = GetInt(args, "minGramSize", NGramTokenFilter.DEFAULT_MIN_NGRAM_SIZE
				);
			maxGramSize = GetInt(args, "maxGramSize", NGramTokenFilter.DEFAULT_MAX_NGRAM_SIZE
				);
			if (!args.IsEmpty())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		public override TokenStream Create(TokenStream input)
		{
			return new NGramTokenFilter(luceneMatchVersion, input, minGramSize, maxGramSize);
		}
	}
}
