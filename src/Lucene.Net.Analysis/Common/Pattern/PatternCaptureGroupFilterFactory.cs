/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Pattern;
using Lucene.Net.Analysis.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Pattern
{
	/// <summary>
	/// Factory for
	/// <see cref="PatternCaptureGroupTokenFilter">PatternCaptureGroupTokenFilter</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_ptncapturegroup" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.KeywordTokenizerFactory"/&gt;
	/// &lt;filter class="solr.PatternCaptureGroupFilterFactory" pattern="([^a-z])" preserve_original="true"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	/// <seealso cref="PatternCaptureGroupTokenFilter">PatternCaptureGroupTokenFilter</seealso>
	public class PatternCaptureGroupFilterFactory : TokenFilterFactory
	{
		private Sharpen.Pattern pattern;

		private bool preserveOriginal = true;

		protected internal PatternCaptureGroupFilterFactory(IDictionary<string, string> args
			) : base(args)
		{
			pattern = GetPattern(args, "pattern");
			preserveOriginal = args.ContainsKey("preserve_original") ? System.Boolean.Parse(args
				.Get("preserve_original")) : true;
		}

		public override TokenStream Create(TokenStream input)
		{
			return new PatternCaptureGroupTokenFilter(input, preserveOriginal, pattern);
		}
	}
}
