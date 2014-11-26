/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Synonym
{
	/// <summary>
	/// Factory for
	/// <see cref="SynonymFilter">SynonymFilter</see>
	/// .
	/// <pre class="prettyprint" >
	/// &lt;fieldType name="text_synonym" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	/// &lt;filter class="solr.SynonymFilterFactory" synonyms="synonyms.txt"
	/// format="solr" ignoreCase="false" expand="true"
	/// tokenizerFactory="solr.WhitespaceTokenizerFactory"
	/// [optional tokenizer factory parameters]/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// <p>
	/// An optional param name prefix of "tokenizerFactory." may be used for any
	/// init params that the SynonymFilterFactory needs to pass to the specified
	/// TokenizerFactory.  If the TokenizerFactory expects an init parameters with
	/// the same name as an init param used by the SynonymFilterFactory, the prefix
	/// is mandatory.
	/// </p>
	/// <p>
	/// The optional
	/// <code>format</code>
	/// parameter controls how the synonyms will be parsed:
	/// It supports the short names of
	/// <code>solr</code>
	/// for
	/// <see cref="SolrSynonymParser">SolrSynonymParser</see>
	/// 
	/// and
	/// <code>wordnet</code>
	/// for and
	/// <see cref="WordnetSynonymParser">WordnetSynonymParser</see>
	/// , or your own
	/// <code>SynonymMap.Parser</code>
	/// class name. The default is
	/// <code>solr</code>
	/// .
	/// A custom
	/// <see cref="Parser">Parser</see>
	/// is expected to have a constructor taking:
	/// <ul>
	/// <li><code>boolean dedup</code> - true if duplicates should be ignored, false otherwise</li>
	/// <li><code>boolean expand</code> - true if conflation groups should be expanded, false if they are one-directional</li>
	/// <li><code>
	/// <see cref="Lucene.Net.Analysis.Analyzer">Lucene.Net.Analysis.Analyzer
	/// 	</see>
	/// analyzer</code> - an analyzer used for each raw synonym</li>
	/// </ul>
	/// </p>
	/// </summary>
	public class SynonymFilterFactory : TokenFilterFactory, ResourceLoaderAware
	{
		private readonly TokenFilterFactory delegator;

		protected internal SynonymFilterFactory(IDictionary<string, string> args) : base(
			args)
		{
			// javadocs
			AssureMatchVersion();
			if (VersionHelper.OnOrAfter(luceneMatchVersion, Version.LUCENE_34))
			{
				delegator = new FSTSynonymFilterFactory(new Dictionary<string, string>(GetOriginalArgs
					()));
			}
			else
			{
				// check if you use the new optional arg "format". this makes no sense for the old one, 
				// as its wired to solr's synonyms format only.
				if (args.ContainsKey("format") && !args.Get("format").Equals("solr"))
				{
					throw new ArgumentException("You must specify luceneMatchVersion >= 3.4 to use alternate synonyms formats"
						);
				}
				delegator = new SlowSynonymFilterFactory(new Dictionary<string, string>(GetOriginalArgs
					()));
			}
		}

		public override TokenStream Create(TokenStream input)
		{
			return delegator.Create(input);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Inform(ResourceLoader loader)
		{
			((ResourceLoaderAware)delegator).Inform(loader);
		}

		/// <summary>Access to the delegator TokenFilterFactory for test verification</summary>
		/// <lucene.internal></lucene.internal>
		[Obsolete]
		[System.ObsoleteAttribute(@"Method exists only for testing 4x, will be removed in 5.0"
			)]
		internal virtual TokenFilterFactory GetDelegator()
		{
			return delegator;
		}
	}
}
