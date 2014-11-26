/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.NO;
using Lucene.Net.Analysis.Util;
using Sharpen;

namespace Lucene.Net.Analysis.NO
{
	/// <summary>
	/// Factory for
	/// <see cref="NorwegianMinimalStemFilter">NorwegianMinimalStemFilter</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_svlgtstem" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.StandardTokenizerFactory"/&gt;
	/// &lt;filter class="solr.LowerCaseFilterFactory"/&gt;
	/// &lt;filter class="solr.NorwegianMinimalStemFilterFactory" variant="nb"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class NorwegianMinimalStemFilterFactory : TokenFilterFactory
	{
		private readonly int flags;

		/// <summary>Creates a new NorwegianMinimalStemFilterFactory</summary>
		protected internal NorwegianMinimalStemFilterFactory(IDictionary<string, string> 
			args) : base(args)
		{
			string variant = Get(args, "variant");
			if (variant == null || "nb".Equals(variant))
			{
				flags = NorwegianLightStemmer.BOKMAAL;
			}
			else
			{
				if ("nn".Equals(variant))
				{
					flags = NorwegianLightStemmer.NYNORSK;
				}
				else
				{
					if ("no".Equals(variant))
					{
						flags = NorwegianLightStemmer.BOKMAAL | NorwegianLightStemmer.NYNORSK;
					}
					else
					{
						throw new ArgumentException("invalid variant: " + variant);
					}
				}
			}
			if (args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		public override TokenStream Create(TokenStream input)
		{
			return new NorwegianMinimalStemFilter(input, flags);
		}
	}
}
