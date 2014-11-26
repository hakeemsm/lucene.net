/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Pattern;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Pattern
{
	/// <summary>
	/// Factory for
	/// <see cref="PatternTokenizer">PatternTokenizer</see>
	/// .
	/// This tokenizer uses regex pattern matching to construct distinct tokens
	/// for the input stream.  It takes two arguments:  "pattern" and "group".
	/// <p/>
	/// <ul>
	/// <li>"pattern" is the regular expression.</li>
	/// <li>"group" says which group to extract into tokens.</li>
	/// </ul>
	/// <p>
	/// group=-1 (the default) is equivalent to "split".  In this case, the tokens will
	/// be equivalent to the output from (without empty tokens):
	/// <see cref="string.Split(string)">string.Split(string)</see>
	/// </p>
	/// <p>
	/// Using group &gt;= 0 selects the matching group as the token.  For example, if you have:<br/>
	/// <pre>
	/// pattern = \'([^\']+)\'
	/// group = 0
	/// input = aaa 'bbb' 'ccc'
	/// </pre>
	/// the output will be two tokens: 'bbb' and 'ccc' (including the ' marks).  With the same input
	/// but using group=1, the output would be: bbb and ccc (no ' marks)
	/// </p>
	/// <p>NOTE: This Tokenizer does not output tokens that are of zero length.</p>
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_ptn" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.PatternTokenizerFactory" pattern="\'([^\']+)\'" group="1"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	/// <seealso cref="PatternTokenizer">PatternTokenizer</seealso>
	/// <since>solr1.2</since>
	public class PatternTokenizerFactory : TokenizerFactory
	{
		public static readonly string PATTERN = "pattern";

		public static readonly string GROUP = "group";

		protected internal readonly Sharpen.Pattern pattern;

		protected internal readonly int group;

		/// <summary>Creates a new PatternTokenizerFactory</summary>
		protected internal PatternTokenizerFactory(IDictionary<string, string> args) : base
			(args)
		{
			pattern = GetPattern(args, PATTERN);
			group = GetInt(args, GROUP, -1);
			if (args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		/// <summary>Split the input using configured pattern</summary>
		public override Tokenizer Create(AttributeSource.AttributeFactory factory, StreamReader
			 @in)
		{
			return new PatternTokenizer(factory, @in, pattern, group);
		}
	}
}
