/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Lucene.Net.Analysis.Ngram;
using Version = System.Version;

namespace Lucene.Net.Ngram
{
	/// <summary>
	/// Creates new instances of
	/// <see cref="EdgeNGramTokenizer">EdgeNGramTokenizer</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_edgngrm" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.EdgeNGramTokenizerFactory" minGramSize="1" maxGramSize="1"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class EdgeNGramTokenizerFactory : TokenizerFactory
	{
		private readonly int maxGramSize;

		private readonly int minGramSize;

		private readonly string side;

		/// <summary>Creates a new EdgeNGramTokenizerFactory</summary>
		protected internal EdgeNGramTokenizerFactory(IDictionary<string, string> args) : 
			base(args)
		{
			minGramSize = GetInt(args, "minGramSize", EdgeNGramTokenizer.DEFAULT_MIN_GRAM_SIZE
				);
			maxGramSize = GetInt(args, "maxGramSize", EdgeNGramTokenizer.DEFAULT_MAX_GRAM_SIZE
				);
		    side = Get(args, "side", Side.FRONT.GetLabel());
			if (args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input)
		{
		    if (luceneMatchVersion.Value.OnOrAfter(Lucene.Net.Util.Version.LUCENE_44))
			{
				if (!Side.FRONT.GetLabel().Equals(side))
				{
					throw new ArgumentException(typeof(EdgeNGramTokenizer).Name + " does not support backward n-grams as of Lucene 4.4"
						);
				}
				return new EdgeNGramTokenizer(luceneMatchVersion.Value, input, minGramSize, maxGramSize
					);
			}
		    return new Lucene43EdgeNGramTokenizer(luceneMatchVersion, input, side, minGramSize, maxGramSize);
		}
	}
}
