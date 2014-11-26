/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.PT;
using Lucene.Net.Analysis.Util;
using Sharpen;

namespace Lucene.Net.Analysis.PT
{
	/// <summary>
	/// Factory for
	/// <see cref="PortugueseMinimalStemFilter">PortugueseMinimalStemFilter</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_ptminstem" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.StandardTokenizerFactory"/&gt;
	/// &lt;filter class="solr.LowerCaseFilterFactory"/&gt;
	/// &lt;filter class="solr.PortugueseMinimalStemFilterFactory"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class PortugueseMinimalStemFilterFactory : TokenFilterFactory
	{
		/// <summary>Creates a new PortugueseMinimalStemFilterFactory</summary>
		protected internal PortugueseMinimalStemFilterFactory(IDictionary<string, string>
			 args) : base(args)
		{
			if (args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		public override TokenStream Create(TokenStream input)
		{
			return new PortugueseMinimalStemFilter(input);
		}
	}
}
