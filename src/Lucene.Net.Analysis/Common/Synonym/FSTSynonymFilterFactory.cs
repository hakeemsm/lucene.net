/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Synonym
{
	[System.ObsoleteAttribute(@"(3.4) use SynonymFilterFactory instead. this is only a backwards compatibility mechanism that will be removed in Lucene 5.0"
		)]
	internal sealed class FSTSynonymFilterFactory : TokenFilterFactory, ResourceLoaderAware
	{
		private readonly bool ignoreCase;

		private readonly string tokenizerFactory;

		private readonly string synonyms;

		private readonly string format;

		private readonly bool expand;

		private readonly IDictionary<string, string> tokArgs = new Dictionary<string, string
			>();

		private SynonymMap map;

		protected internal FSTSynonymFilterFactory(IDictionary<string, string> args) : base
			(args)
		{
			// NOTE: rename this to "SynonymFilterFactory" and nuke that delegator in Lucene 5.0!
			ignoreCase = GetBoolean(args, "ignoreCase", false);
			synonyms = Require(args, "synonyms");
			format = Get(args, "format");
			expand = GetBoolean(args, "expand", true);
			tokenizerFactory = Get(args, "tokenizerFactory");
			if (tokenizerFactory != null)
			{
				AssureMatchVersion();
				tokArgs.Put("luceneMatchVersion", GetLuceneMatchVersion().ToString());
				for (Iterator<string> itr = args.Keys.Iterator(); itr.HasNext(); )
				{
					string key = itr.Next();
					tokArgs.Put(key.ReplaceAll("^tokenizerFactory\\.", string.Empty), args.Get(key));
					itr.Remove();
				}
			}
			if (args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		public override TokenStream Create(TokenStream input)
		{
			// if the fst is null, it means there's actually no synonyms... just return the original stream
			// as there is nothing to do here.
			return map.fst == null ? input : new SynonymFilter(input, map, ignoreCase);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public void Inform(ResourceLoader loader)
		{
			TokenizerFactory factory = tokenizerFactory == null ? null : LoadTokenizerFactory
				(loader, tokenizerFactory);
			Analyzer analyzer = new _Analyzer_95(this, factory);
			try
			{
				string formatClass = format;
				if (format == null || format.Equals("solr"))
				{
					formatClass = typeof(SolrSynonymParser).FullName;
				}
				else
				{
					if (format.Equals("wordnet"))
					{
						formatClass = typeof(WordnetSynonymParser).FullName;
					}
				}
				// TODO: expose dedup as a parameter?
				map = LoadSynonyms(loader, formatClass, true, analyzer);
			}
			catch (ParseException e)
			{
				throw new IOException("Error parsing synonyms file:", e);
			}
		}

		private sealed class _Analyzer_95 : Analyzer
		{
			public _Analyzer_95(FSTSynonymFilterFactory _enclosing, TokenizerFactory factory)
			{
				this._enclosing = _enclosing;
				this.factory = factory;
			}

			public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer tokenizer = factory == null ? new WhitespaceTokenizer(Lucene.Net.Util.Version.LUCENE_CURRENT
					, reader) : factory.Create(reader);
				TokenStream stream = this._enclosing.ignoreCase ? new LowerCaseFilter(Lucene.Net.Util.Version.LUCENE_CURRENT
					, tokenizer) : tokenizer;
				return new Analyzer.TokenStreamComponents(tokenizer, stream);
			}

			private readonly FSTSynonymFilterFactory _enclosing;

			private readonly TokenizerFactory factory;
		}

		/// <summary>
		/// Load synonyms with the given
		/// <see cref="SynonymMap.Parser">Parser</see>
		/// class.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.ParseException"></exception>
		private SynonymMap LoadSynonyms(IResourceLoader loader, string cname, bool dedup, 
			Analyzer analyzer)
		{
			CharsetDecoder decoder = Sharpen.Extensions.GetEncoding("UTF-8").NewDecoder().OnMalformedInput
				(CodingErrorAction.REPORT).OnUnmappableCharacter(CodingErrorAction.REPORT);
			SynonymMap.Parser parser;
			Type clazz = loader.FindClass<SynonymMap.Parser>(cname);
			try
			{
				parser = clazz.GetConstructor(typeof(bool), typeof(bool), typeof(Analyzer)).NewInstance
					(dedup, expand, analyzer);
			}
			catch (Exception e)
			{
				throw new RuntimeException(e);
			}
			FilePath synonymFile = new FilePath(synonyms);
			if (synonymFile.Exists())
			{
				decoder.Reset();
				parser.Parse(new InputStreamReader(loader.OpenResource(synonyms), decoder));
			}
			else
			{
				IList<string> files = SplitFileNames(synonyms);
				foreach (string file in files)
				{
					decoder.Reset();
					parser.Parse(new InputStreamReader(loader.OpenResource(file), decoder));
				}
			}
			return parser.Build();
		}

		// (there are no tests for this functionality)
		/// <exception cref="System.IO.IOException"></exception>
		private TokenizerFactory LoadTokenizerFactory(ResourceLoader loader, string cname
			)
		{
			Type clazz = loader.FindClass<TokenizerFactory>(cname);
			try
			{
				TokenizerFactory tokFactory = clazz.GetConstructor(typeof(IDictionary)).NewInstance
					(tokArgs);
				if (tokFactory is ResourceLoaderAware)
				{
					((ResourceLoaderAware)tokFactory).Inform(loader);
				}
				return tokFactory;
			}
			catch (Exception e)
			{
				throw new RuntimeException(e);
			}
		}
	}
}
