/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Analysis.Wikipedia;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Wikipedia
{
	/// <summary>
	/// Factory for
	/// <see cref="WikipediaTokenizer">WikipediaTokenizer</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_wiki" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.WikipediaTokenizerFactory"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class WikipediaTokenizerFactory : TokenizerFactory
	{
		/// <summary>Creates a new WikipediaTokenizerFactory</summary>
		protected internal WikipediaTokenizerFactory(IDictionary<string, string> args) : 
			base(args)
		{
			if (args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		// TODO: add support for WikipediaTokenizer's advanced options.
		public override Tokenizer Create(AttributeSource.AttributeFactory factory, StreamReader
			 input)
		{
			return new WikipediaTokenizer(factory, input, WikipediaTokenizer.TOKENS_ONLY, Sharpen.Collections
				.EmptySet<string>());
		}
	}
}
