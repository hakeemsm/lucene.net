/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Fr;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Org.Tartarus.Snowball.Ext;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.GA
{
	/// <summary>
	/// <see cref="Lucene.Net.Analysis.Analyzer">Lucene.Net.Analysis.Analyzer
	/// 	</see>
	/// for Irish.
	/// </summary>
	public sealed class IrishAnalyzer : StopwordAnalyzerBase
	{
		private readonly CharArraySet stemExclusionSet;

		/// <summary>File containing default Irish stopwords.</summary>
		/// <remarks>File containing default Irish stopwords.</remarks>
		public static readonly string DEFAULT_STOPWORD_FILE = "stopwords.txt";

		private static readonly CharArraySet DEFAULT_ARTICLES = CharArraySet.UnmodifiableSet
			(new CharArraySet(Version.LUCENE_CURRENT, new []{"d", "m", "b"}, true));

		/// <summary>
		/// When StandardTokenizer splits tâ€‘athair into {t, athair}, we don't
		/// want to cause a position increment, otherwise there will be problems
		/// with phrase queries versus tAthair (which would not have a gap).
		/// </summary>
		/// <remarks>
		/// When StandardTokenizer splits tâ€‘athair into {t, athair}, we don't
		/// want to cause a position increment, otherwise there will be problems
		/// with phrase queries versus tAthair (which would not have a gap).
		/// </remarks>
		private static readonly CharArraySet HYPHENATIONS = CharArraySet.UnmodifiableSet(
			new CharArraySet(Version.LUCENE_CURRENT, new[]{"h", "n", "t"}, true));

		/// <summary>Returns an unmodifiable instance of the default stop words set.</summary>
		/// <remarks>Returns an unmodifiable instance of the default stop words set.</remarks>
		/// <returns>default stop words set.</returns>
		public static CharArraySet GetDefaultStopSet()
		{
			return IrishAnalyzer.DefaultSetHolder.DEFAULT_STOP_SET;
		}

		/// <summary>
		/// Atomically loads the DEFAULT_STOP_SET in a lazy fashion once the outer class
		/// accesses the static final set the first time.;
		/// </summary>
		private class DefaultSetHolder
		{
			internal static readonly CharArraySet DEFAULT_STOP_SET;

			static DefaultSetHolder()
			{
				try
				{
					DEFAULT_STOP_SET = LoadStopwordSet(false, typeof(IrishAnalyzer), DEFAULT_STOPWORD_FILE
						, "#");
				}
				catch (IOException)
				{
					// default set should always be present as it is part of the
					// distribution (JAR)
					throw new Exception("Unable to load default stopword set");
				}
			}
		}

		/// <summary>
		/// Builds an analyzer with the default stop words:
		/// <see cref="DEFAULT_STOPWORD_FILE">DEFAULT_STOPWORD_FILE</see>
		/// .
		/// </summary>
		protected internal IrishAnalyzer(Version matchVersion) : this(matchVersion, IrishAnalyzer.DefaultSetHolder
			.DEFAULT_STOP_SET)
		{
		}

		/// <summary>Builds an analyzer with the given stop words.</summary>
		/// <remarks>Builds an analyzer with the given stop words.</remarks>
		/// <param name="matchVersion">lucene compatibility version</param>
		/// <param name="stopwords">a stopword set</param>
		protected internal IrishAnalyzer(Version matchVersion, CharArraySet stopwords) : 
			this(matchVersion, stopwords, CharArraySet.EMPTY_SET)
		{
		}

		/// <summary>Builds an analyzer with the given stop words.</summary>
		/// <remarks>
		/// Builds an analyzer with the given stop words. If a non-empty stem exclusion set is
		/// provided this analyzer will add a
		/// <see cref="Lucene.Net.Analysis.Miscellaneous.SetKeywordMarkerFilter">Lucene.Net.Analysis.Miscellaneous.SetKeywordMarkerFilter
		/// 	</see>
		/// before
		/// stemming.
		/// </remarks>
		/// <param name="matchVersion">lucene compatibility version</param>
		/// <param name="stopwords">a stopword set</param>
		/// <param name="stemExclusionSet">a set of terms not to be stemmed</param>
		public IrishAnalyzer(Version matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet
			) : base(matchVersion, stopwords)
		{
			this.stemExclusionSet = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion
				, stemExclusionSet));
		}

		/// <summary>
		/// Creates a
		/// <see cref="Lucene.Net.Analysis.Analyzer.TokenStreamComponents">Lucene.Net.Analysis.Analyzer.TokenStreamComponents
		/// 	</see>
		/// which tokenizes all the text in the provided
		/// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
		/// .
		/// </summary>
		/// <returns>
		/// A
		/// <see cref="Lucene.Net.Analysis.Analyzer.TokenStreamComponents">Lucene.Net.Analysis.Analyzer.TokenStreamComponents
		/// 	</see>
		/// built from an
		/// <see cref="Lucene.Net.Analysis.Standard.StandardTokenizer">Lucene.Net.Analysis.Standard.StandardTokenizer
		/// 	</see>
		/// filtered with
		/// <see cref="Lucene.Net.Analysis.Standard.StandardFilter">Lucene.Net.Analysis.Standard.StandardFilter
		/// 	</see>
		/// ,
		/// <see cref="IrishLowerCaseFilter">IrishLowerCaseFilter</see>
		/// ,
		/// <see cref="Lucene.Net.Analysis.Core.StopFilter">Lucene.Net.Analysis.Core.StopFilter
		/// 	</see>
		/// ,
		/// <see cref="Lucene.Net.Analysis.Miscellaneous.SetKeywordMarkerFilter">Lucene.Net.Analysis.Miscellaneous.SetKeywordMarkerFilter
		/// 	</see>
		/// if a stem exclusion set is
		/// provided and
		/// <see cref="Lucene.Net.Analysis.Snowball.SnowballFilter">Lucene.Net.Analysis.Snowball.SnowballFilter
		/// 	</see>
		/// .
		/// </returns>
		public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
		{
			Tokenizer source = new StandardTokenizer(matchVersion, reader);
			TokenStream result = new StandardFilter(matchVersion, source);
			StopFilter s = new StopFilter(matchVersion, result, HYPHENATIONS);
			if (!matchVersion.Value.OnOrAfter(Version.LUCENE_44))
			{
				s.EnablePositionIncrements = false;
			}
			result = s;
			result = new ElisionFilter(result, DEFAULT_ARTICLES);
			result = new IrishLowerCaseFilter(result);
			result = new StopFilter(matchVersion, result, stopwords);
			if (stemExclusionSet.Any()())
			{
				result = new SetKeywordMarkerFilter(result, stemExclusionSet);
			}
			result = new SnowballFilter(result, new IrishStemmer());
			return new Analyzer.TokenStreamComponents(source, result);
		}
	}
}
