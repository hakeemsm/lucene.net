/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Pattern;
using Lucene.Net.Analysis.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Pattern
{
	/// <summary>
	/// Factory for
	/// <see cref="PatternReplaceFilter">PatternReplaceFilter</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_ptnreplace" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.KeywordTokenizerFactory"/&gt;
	/// &lt;filter class="solr.PatternReplaceFilterFactory" pattern="([^a-z])" replacement=""
	/// replace="all"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	/// <seealso cref="PatternReplaceFilter">PatternReplaceFilter</seealso>
	public class PatternReplaceFilterFactory : TokenFilterFactory
	{
		internal readonly Sharpen.Pattern pattern;

		internal readonly string replacement;

		internal readonly bool replaceAll;

		/// <summary>Creates a new PatternReplaceFilterFactory</summary>
		protected internal PatternReplaceFilterFactory(IDictionary<string, string> args) : 
			base(args)
		{
			pattern = GetPattern(args, "pattern");
			replacement = Get(args, "replacement");
			replaceAll = "all".Equals(Get(args, "replace", Arrays.AsList("all", "first"), "all"
				));
			if (args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		public override TokenStream Create(TokenStream input)
		{
			return new PatternReplaceFilter(input, pattern, replacement, replaceAll);
		}
	}
}
