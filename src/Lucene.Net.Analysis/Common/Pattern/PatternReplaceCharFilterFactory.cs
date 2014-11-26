/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis.Pattern;
using Lucene.Net.Analysis.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Pattern
{
	/// <summary>
	/// Factory for
	/// <see cref="PatternReplaceCharFilter">PatternReplaceCharFilter</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_ptnreplace" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;charFilter class="solr.PatternReplaceCharFilterFactory"
	/// pattern="([^a-z])" replacement=""/&gt;
	/// &lt;tokenizer class="solr.KeywordTokenizerFactory"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	/// <since>Solr 3.1</since>
	public class PatternReplaceCharFilterFactory : CharFilterFactory
	{
		private readonly Sharpen.Pattern pattern;

		private readonly string replacement;

		private readonly int maxBlockChars;

		private readonly string blockDelimiters;

		/// <summary>Creates a new PatternReplaceCharFilterFactory</summary>
		protected internal PatternReplaceCharFilterFactory(IDictionary<string, string> args
			) : base(args)
		{
			pattern = GetPattern(args, "pattern");
			replacement = Get(args, "replacement", string.Empty);
			// TODO: warn if you set maxBlockChars or blockDelimiters ?
			maxBlockChars = GetInt(args, "maxBlockChars", PatternReplaceCharFilter.DEFAULT_MAX_BLOCK_CHARS
				);
			blockDelimiters = Sharpen.Collections.Remove(args, "blockDelimiters");
			if (args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		public override StreamReader Create(StreamReader input)
		{
			return new PatternReplaceCharFilter(pattern, replacement, maxBlockChars, blockDelimiters
				, input);
		}
	}
}
