/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TR;
using Lucene.Net.Analysis.Util;
using Sharpen;

namespace Lucene.Net.Analysis.TR
{
	/// <summary>
	/// Factory for
	/// <see cref="TurkishLowerCaseFilter">TurkishLowerCaseFilter</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_trlwr" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.StandardTokenizerFactory"/&gt;
	/// &lt;filter class="solr.TurkishLowerCaseFilterFactory"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class TurkishLowerCaseFilterFactory : TokenFilterFactory, MultiTermAwareComponent
	{
		/// <summary>Creates a new TurkishLowerCaseFilterFactory</summary>
		protected internal TurkishLowerCaseFilterFactory(IDictionary<string, string> args
			) : base(args)
		{
			if (args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		public override TokenStream Create(TokenStream input)
		{
			return new TurkishLowerCaseFilter(input);
		}

		public virtual AbstractAnalysisFactory GetMultiTermComponent()
		{
			return this;
		}
	}
}
