/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.PT;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Org.Tartarus.Snowball.Ext;
using Sharpen;

namespace Lucene.Net.Analysis.PT
{
	/// <summary>
	/// <see cref="Lucene.Net.Analysis.Analyzer">Lucene.Net.Analysis.Analyzer
	/// 	</see>
	/// for Portuguese.
	/// <p>
	/// <a name="version"/>
	/// <p>You must specify the required
	/// <see cref="Lucene.Net.Util.Version">Lucene.Net.Util.Version</see>
	/// compatibility when creating PortugueseAnalyzer:
	/// <ul>
	/// <li> As of 3.6, PortugueseLightStemFilter is used for less aggressive stemming.
	/// </ul>
	/// </summary>
	public sealed class PortugueseAnalyzer : StopwordAnalyzerBase
	{
		private readonly CharArraySet stemExclusionSet;

		/// <summary>File containing default Portuguese stopwords.</summary>
		/// <remarks>File containing default Portuguese stopwords.</remarks>
		public static readonly string DEFAULT_STOPWORD_FILE = "portuguese_stop.txt";

		/// <summary>Returns an unmodifiable instance of the default stop words set.</summary>
		/// <remarks>Returns an unmodifiable instance of the default stop words set.</remarks>
		/// <returns>default stop words set.</returns>
		public static CharArraySet GetDefaultStopSet()
		{
			return PortugueseAnalyzer.DefaultSetHolder.DEFAULT_STOP_SET;
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
					DEFAULT_STOP_SET = WordlistLoader.GetSnowballWordSet(IOUtils.GetDecodingReader(typeof(
						SnowballFilter), DEFAULT_STOPWORD_FILE, StandardCharsets.UTF_8), Version.LUCENE_CURRENT
						);
				}
				catch (IOException)
				{
					// default set should always be present as it is part of the
					// distribution (JAR)
					throw new RuntimeException("Unable to load default stopword set");
				}
			}
		}

		/// <summary>
		/// Builds an analyzer with the default stop words:
		/// <see cref="DEFAULT_STOPWORD_FILE">DEFAULT_STOPWORD_FILE</see>
		/// .
		/// </summary>
		protected internal PortugueseAnalyzer(Version matchVersion) : this(matchVersion, 
			PortugueseAnalyzer.DefaultSetHolder.DEFAULT_STOP_SET)
		{
		}

		/// <summary>Builds an analyzer with the given stop words.</summary>
		/// <remarks>Builds an analyzer with the given stop words.</remarks>
		/// <param name="matchVersion">lucene compatibility version</param>
		/// <param name="stopwords">a stopword set</param>
		protected internal PortugueseAnalyzer(Version matchVersion, CharArraySet stopwords
			) : this(matchVersion, stopwords, CharArraySet.EMPTY_SET)
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
		public PortugueseAnalyzer(Version matchVersion, CharArraySet stopwords, CharArraySet
			 stemExclusionSet) : base(matchVersion, stopwords)
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
		/// <see cref="Lucene.Net.Analysis.Core.LowerCaseFilter">Lucene.Net.Analysis.Core.LowerCaseFilter
		/// 	</see>
		/// ,
		/// <see cref="Lucene.Net.Analysis.Core.StopFilter">Lucene.Net.Analysis.Core.StopFilter
		/// 	</see>
		/// ,
		/// <see cref="Lucene.Net.Analysis.Miscellaneous.SetKeywordMarkerFilter">Lucene.Net.Analysis.Miscellaneous.SetKeywordMarkerFilter
		/// 	</see>
		/// if a stem exclusion set is
		/// provided and
		/// <see cref="PortugueseLightStemFilter">PortugueseLightStemFilter</see>
		/// .
		/// </returns>
		public override TokenStreamComponents CreateComponents(string fieldName
			, TextReader reader)
		{
			Tokenizer source = new StandardTokenizer(matchVersion, reader);
			TokenStream result = new StandardFilter(matchVersion, source);
			result = new LowerCaseFilter(matchVersion, result);
			result = new StopFilter(matchVersion, result, stopwords);
			if (stemExclusionSet.Any())
			{
				result = new SetKeywordMarkerFilter(result, stemExclusionSet);
			}
			if (matchVersion.Value.OnOrAfter( Version.LUCENE_36))
			{
				result = new PortugueseLightStemFilter(result);
			}
			else
			{
				result = new SnowballFilter(result, new PortugueseStemmer());
			}
			return new Analyzer.TokenStreamComponents(source, result);
		}
	}
}
