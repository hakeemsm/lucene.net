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
using Lucene.Net.Analysis.IN;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.HI
{
	/// <summary>Analyzer for Hindi.</summary>
	/// <remarks>
	/// Analyzer for Hindi.
	/// <p>
	/// <a name="version"/>
	/// <p>You must specify the required
	/// <see cref="Lucene.Net.Util.Version">Lucene.Net.Util.Version</see>
	/// compatibility when creating HindiAnalyzer:
	/// <ul>
	/// <li> As of 3.6, StandardTokenizer is used for tokenization
	/// </ul>
	/// </remarks>
	public sealed class HindiAnalyzer : StopwordAnalyzerBase
	{
		private readonly CharArraySet stemExclusionSet;

		/// <summary>File containing default Hindi stopwords.</summary>
		/// <remarks>
		/// File containing default Hindi stopwords.
		/// Default stopword list is from http://members.unine.ch/jacques.savoy/clef/index.html
		/// The stopword list is BSD-Licensed.
		/// </remarks>
		public static readonly string DEFAULT_STOPWORD_FILE = "stopwords.txt";

		private static readonly string STOPWORDS_COMMENT = "#";

		/// <summary>Returns an unmodifiable instance of the default stop-words set.</summary>
		/// <remarks>Returns an unmodifiable instance of the default stop-words set.</remarks>
		/// <returns>an unmodifiable instance of the default stop-words set.</returns>
		public static CharArraySet GetDefaultStopSet()
		{
			return DefaultSetHolder.DEFAULT_STOP_SET;
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
					DEFAULT_STOP_SET = LoadStopwordSet(false, typeof(HindiAnalyzer), DEFAULT_STOPWORD_FILE
						, STOPWORDS_COMMENT);
				}
				catch (IOException)
				{
					// default set should always be present as it is part of the
					// distribution (JAR)
					throw new Exception("Unable to load default stopword set");
				}
			}
		}

		/// <summary>Builds an analyzer with the given stop words</summary>
		/// <param name="version">lucene compatibility version</param>
		/// <param name="stopwords">a stopword set</param>
		/// <param name="stemExclusionSet">a stemming exclusion set</param>
		public HindiAnalyzer(Version version, CharArraySet stopwords, CharArraySet stemExclusionSet
			) : base(version, stopwords)
		{
			this.stemExclusionSet = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion
				, stemExclusionSet));
		}

		/// <summary>Builds an analyzer with the given stop words</summary>
		/// <param name="version">lucene compatibility version</param>
		/// <param name="stopwords">a stopword set</param>
		protected internal HindiAnalyzer(Version version, CharArraySet stopwords) : this(
			version, stopwords, CharArraySet.EMPTY_SET)
		{
		}

		/// <summary>
		/// Builds an analyzer with the default stop words:
		/// <see cref="DEFAULT_STOPWORD_FILE">DEFAULT_STOPWORD_FILE</see>
		/// .
		/// </summary>
		protected internal HindiAnalyzer(Version version) : this(version, HindiAnalyzer.DefaultSetHolder
			.DEFAULT_STOP_SET)
		{
		}

		/// <summary>
		/// Creates
		/// <see cref="Lucene.Net.Analysis.Analyzer.TokenStreamComponents">Lucene.Net.Analysis.Analyzer.TokenStreamComponents
		/// 	</see>
		/// used to tokenize all the text in the provided
		/// <see cref="System.IO.StreamReader">System.IO.StreamReader</see>
		/// .
		/// </summary>
		/// <returns>
		/// 
		/// <see cref="Lucene.Net.Analysis.Analyzer.TokenStreamComponents">Lucene.Net.Analysis.Analyzer.TokenStreamComponents
		/// 	</see>
		/// built from a
		/// <see cref="Lucene.Net.Analysis.Standard.StandardTokenizer">Lucene.Net.Analysis.Standard.StandardTokenizer
		/// 	</see>
		/// filtered with
		/// <see cref="Lucene.Net.Analysis.Core.LowerCaseFilter">Lucene.Net.Analysis.Core.LowerCaseFilter
		/// 	</see>
		/// ,
		/// <see cref="Lucene.Net.Analysis.IN.IndicNormalizationFilter">Lucene.Net.Analysis.IN.IndicNormalizationFilter
		/// 	</see>
		/// ,
		/// <see cref="HindiNormalizationFilter">HindiNormalizationFilter</see>
		/// ,
		/// <see cref="Lucene.Net.Analysis.Miscellaneous.SetKeywordMarkerFilter">Lucene.Net.Analysis.Miscellaneous.SetKeywordMarkerFilter
		/// 	</see>
		/// if a stem exclusion set is provided,
		/// <see cref="HindiStemFilter">HindiStemFilter</see>
		/// , and
		/// Hindi Stop words
		/// </returns>
		public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
		{
			Tokenizer source;
			if (matchVersion.Value.OnOrAfter(Version.LUCENE_36))
			{
				source = new StandardTokenizer(matchVersion, reader);
			}
			else
			{
				source = new IndicTokenizer(matchVersion.Value, reader);
			}
			TokenStream result = new LowerCaseFilter(matchVersion, source);
			if (stemExclusionSet.Any())
			{
				result = new SetKeywordMarkerFilter(result, stemExclusionSet);
			}
			result = new IndicNormalizationFilter(result);
			result = new HindiNormalizationFilter(result);
			result = new StopFilter(matchVersion, result, stopwords);
			result = new HindiStemFilter(result);
			return new Analyzer.TokenStreamComponents(source, result);
		}
	}
}
