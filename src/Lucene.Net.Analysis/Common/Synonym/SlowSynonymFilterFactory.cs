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
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Synonym
{
	/// <summary>
	/// Factory for
	/// <see cref="SlowSynonymFilter">SlowSynonymFilter</see>
	/// (only used with luceneMatchVersion &lt; 3.4)
	/// <pre class="prettyprint" >
	/// &lt;fieldType name="text_synonym" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	/// &lt;filter class="solr.SynonymFilterFactory" synonyms="synonyms.txt" ignoreCase="false"
	/// expand="true" tokenizerFactory="solr.WhitespaceTokenizerFactory"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	[System.ObsoleteAttribute(@"(3.4) use SynonymFilterFactory instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0"
		)]
	internal sealed class SlowSynonymFilterFactory : TokenFilterFactory, ResourceLoaderAware
	{
		private readonly string synonyms;

		private readonly bool ignoreCase;

		private readonly bool expand;

		private readonly string tf;

		private readonly IDictionary<string, string> tokArgs = new Dictionary<string, string
			>();

		protected internal SlowSynonymFilterFactory(IDictionary<string, string> args) : base
			(args)
		{
			synonyms = Require(args, "synonyms");
			ignoreCase = GetBoolean(args, "ignoreCase", false);
			expand = GetBoolean(args, "expand", true);
			tf = Get(args, "tokenizerFactory");
			if (tf != null)
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

		/// <exception cref="System.IO.IOException"></exception>
		public void Inform(ResourceLoader loader)
		{
			TokenizerFactory tokFactory = null;
			if (tf != null)
			{
				tokFactory = LoadTokenizerFactory(loader, tf);
			}
			Iterable<string> wlist = LoadRules(synonyms, loader);
			synMap = new SlowSynonymMap(ignoreCase);
			ParseRules(wlist, synMap, "=>", ",", expand, tokFactory);
		}

		/// <returns>a list of all rules</returns>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal Iterable<string> LoadRules(string synonyms, ResourceLoader loader
			)
		{
			IList<string> wlist = null;
			FilePath synonymFile = new FilePath(synonyms);
			if (synonymFile.Exists())
			{
				wlist = GetLines(loader, synonyms);
			}
			else
			{
				IList<string> files = SplitFileNames(synonyms);
				wlist = new AList<string>();
				foreach (string file in files)
				{
					IList<string> lines = GetLines(loader, file.Trim());
					Sharpen.Collections.AddAll(wlist, lines);
				}
			}
			return wlist;
		}

		private SlowSynonymMap synMap;

		/// <exception cref="System.IO.IOException"></exception>
		internal static void ParseRules(Iterable<string> rules, SlowSynonymMap map, string
			 mappingSep, string synSep, bool expansion, TokenizerFactory tokFactory)
		{
			int count = 0;
			foreach (string rule in rules)
			{
				// To use regexes, we need an expression that specifies an odd number of chars.
				// This can't really be done with string.split(), and since we need to
				// do unescaping at some point anyway, we wouldn't be saving any effort
				// by using regexes.
				IList<string> mapping = SplitSmart(rule, mappingSep, false);
				IList<IList<string>> source;
				IList<IList<string>> target;
				if (mapping.Count > 2)
				{
					throw new ArgumentException("Invalid Synonym Rule:" + rule);
				}
				else
				{
					if (mapping.Count == 2)
					{
						source = GetSynList(mapping[0], synSep, tokFactory);
						target = GetSynList(mapping[1], synSep, tokFactory);
					}
					else
					{
						source = GetSynList(mapping[0], synSep, tokFactory);
						if (expansion)
						{
							// expand to all arguments
							target = source;
						}
						else
						{
							// reduce to first argument
							target = new AList<IList<string>>(1);
							target.AddItem(source[0]);
						}
					}
				}
				bool includeOrig = false;
				foreach (IList<string> fromToks in source)
				{
					count++;
					foreach (IList<string> toToks in target)
					{
						map.Add(fromToks, SlowSynonymMap.MakeTokens(toToks), includeOrig, true);
					}
				}
			}
		}

		// a , b c , d e f => [[a],[b,c],[d,e,f]]
		/// <exception cref="System.IO.IOException"></exception>
		private static IList<IList<string>> GetSynList(string str, string separator, TokenizerFactory
			 tokFactory)
		{
			IList<string> strList = SplitSmart(str, separator, false);
			// now split on whitespace to get a list of token strings
			IList<IList<string>> synList = new AList<IList<string>>();
			foreach (string toks in strList)
			{
				IList<string> tokList = tokFactory == null ? SplitWS(toks, true) : SplitByTokenizer
					(toks, tokFactory);
				synList.AddItem(tokList);
			}
			return synList;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static IList<string> SplitByTokenizer(string source, TokenizerFactory tokFactory
			)
		{
			StringReader reader = new StringReader(source);
			TokenStream ts = LoadTokenizer(tokFactory, reader);
			IList<string> tokList = new AList<string>();
			try
			{
				CharTermAttribute termAtt = ts.AddAttribute<CharTermAttribute>();
				ts.Reset();
				while (ts.IncrementToken())
				{
					if (termAtt.Length > 0)
					{
						tokList.AddItem(termAtt.ToString());
					}
				}
			}
			finally
			{
				reader.Close();
			}
			return tokList;
		}

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

		private static TokenStream LoadTokenizer(TokenizerFactory tokFactory, StreamReader
			 reader)
		{
			return tokFactory.Create(reader);
		}

		public SlowSynonymMap GetSynonymMap()
		{
			return synMap;
		}

		public override TokenStream Create(TokenStream input)
		{
			return new SlowSynonymFilter(input, synMap);
		}

		public static IList<string> SplitWS(string s, bool decode)
		{
			AList<string> lst = new AList<string>(2);
			StringBuilder sb = new StringBuilder();
			int pos = 0;
			int end = s.Length;
			while (pos < end)
			{
				char ch = s[pos++];
				if (char.IsWhiteSpace(ch))
				{
					if (sb.Length > 0)
					{
						lst.AddItem(sb.ToString());
						sb = new StringBuilder();
					}
					continue;
				}
				if (ch == '\\')
				{
					if (!decode)
					{
						sb.Append(ch);
					}
					if (pos >= end)
					{
						break;
					}
					// ERROR, or let it go?
					ch = s[pos++];
					if (decode)
					{
						switch (ch)
						{
							case 'n':
							{
								ch = '\n';
								break;
							}

							case 't':
							{
								ch = '\t';
								break;
							}

							case 'r':
							{
								ch = '\r';
								break;
							}

							case 'b':
							{
								ch = '\b';
								break;
							}

							case 'f':
							{
								ch = '\f';
								break;
							}
						}
					}
				}
				sb.Append(ch);
			}
			if (sb.Length > 0)
			{
				lst.AddItem(sb.ToString());
			}
			return lst;
		}

		/// <summary>Splits a backslash escaped string on the separator.</summary>
		/// <remarks>
		/// Splits a backslash escaped string on the separator.
		/// <p>
		/// Current backslash escaping supported:
		/// <br /> \n \t \r \b \f are escaped the same as a Java String
		/// <br /> Other characters following a backslash are produced verbatim (\c =&gt; c)
		/// </remarks>
		/// <param name="s">the string to split</param>
		/// <param name="separator">the separator to split on</param>
		/// <param name="decode">decode backslash escaping</param>
		public static IList<string> SplitSmart(string s, string separator, bool decode)
		{
			AList<string> lst = new AList<string>(2);
			StringBuilder sb = new StringBuilder();
			int pos = 0;
			int end = s.Length;
			while (pos < end)
			{
				if (s.StartsWith(separator, pos))
				{
					if (sb.Length > 0)
					{
						lst.AddItem(sb.ToString());
						sb = new StringBuilder();
					}
					pos += separator.Length;
					continue;
				}
				char ch = s[pos++];
				if (ch == '\\')
				{
					if (!decode)
					{
						sb.Append(ch);
					}
					if (pos >= end)
					{
						break;
					}
					// ERROR, or let it go?
					ch = s[pos++];
					if (decode)
					{
						switch (ch)
						{
							case 'n':
							{
								ch = '\n';
								break;
							}

							case 't':
							{
								ch = '\t';
								break;
							}

							case 'r':
							{
								ch = '\r';
								break;
							}

							case 'b':
							{
								ch = '\b';
								break;
							}

							case 'f':
							{
								ch = '\f';
								break;
							}
						}
					}
				}
				sb.Append(ch);
			}
			if (sb.Length > 0)
			{
				lst.AddItem(sb.ToString());
			}
			return lst;
		}
	}
}
