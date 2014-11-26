/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Ngram
{
	/// <summary>
	/// Creates new instances of
	/// <see cref="EdgeNGramTokenFilter">EdgeNGramTokenFilter</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_edgngrm" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	/// &lt;filter class="solr.EdgeNGramFilterFactory" minGramSize="1" maxGramSize="1"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class EdgeNGramFilterFactory : TokenFilterFactory
	{
		private readonly int maxGramSize;

		private readonly int minGramSize;

		private readonly string side;

		/// <summary>Creates a new EdgeNGramFilterFactory</summary>
		protected internal EdgeNGramFilterFactory(IDictionary<string, string> args) : base(args)
		{
			minGramSize = GetInt(args, "minGramSize", EdgeNGramTokenFilter.DEFAULT_MIN_GRAM_SIZE);
			maxGramSize = GetInt(args, "maxGramSize", EdgeNGramTokenFilter.DEFAULT_MAX_GRAM_SIZE);
			side = Get(args, "side", Side.FRONT.GetLabel());
			if (args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		public override TokenStream Create(TokenStream input)
		{
			return new EdgeNGramTokenFilter(luceneMatchVersion, input, side, minGramSize, maxGramSize);
		}
	}
}
