/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.EN;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TR;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Snowball
{
	/// <summary>
	/// Filters
	/// <see cref="Lucene.Net.Analysis.Standard.StandardTokenizer">Lucene.Net.Analysis.Standard.StandardTokenizer
	/// 	</see>
	/// with
	/// <see cref="Lucene.Net.Analysis.Standard.StandardFilter">Lucene.Net.Analysis.Standard.StandardFilter
	/// 	</see>
	/// ,
	/// <see cref="Lucene.Net.Analysis.Core.LowerCaseFilter">Lucene.Net.Analysis.Core.LowerCaseFilter
	/// 	</see>
	/// ,
	/// <see cref="Lucene.Net.Analysis.Core.StopFilter">Lucene.Net.Analysis.Core.StopFilter
	/// 	</see>
	/// and
	/// <see cref="SnowballFilter">SnowballFilter</see>
	/// .
	/// Available stemmers are listed in org.tartarus.snowball.ext.  The name of a
	/// stemmer is the part of the class name before "Stemmer", e.g., the stemmer in
	/// <see cref="Org.Tartarus.Snowball.Ext.EnglishStemmer">Org.Tartarus.Snowball.Ext.EnglishStemmer
	/// 	</see>
	/// is named "English".
	/// <p><b>NOTE</b>: This class uses the same
	/// <see cref="Lucene.Net.Util.Version">Lucene.Net.Util.Version</see>
	/// dependent settings as
	/// <see cref="Lucene.Net.Analysis.Standard.StandardAnalyzer">Lucene.Net.Analysis.Standard.StandardAnalyzer
	/// 	</see>
	/// , with the following addition:
	/// <ul>
	/// <li> As of 3.1, uses
	/// <see cref="Lucene.Net.Analysis.TR.TurkishLowerCaseFilter">Lucene.Net.Analysis.TR.TurkishLowerCaseFilter
	/// 	</see>
	/// for Turkish language.
	/// </ul>
	/// </p>
	/// </summary>
	[System.ObsoleteAttribute(@"(3.1) Use the language-specific analyzer in modules/analysis instead. This analyzer will be removed in Lucene 5.0"
		)]
	public sealed class SnowballAnalyzer : Analyzer
	{
		private string name;

		private CharArraySet stopSet;

		private readonly Version matchVersion;

		/// <summary>Builds the named analyzer with no stop words.</summary>
		/// <remarks>Builds the named analyzer with no stop words.</remarks>
		public SnowballAnalyzer(Version matchVersion, string name)
		{
			this.name = name;
			this.matchVersion = matchVersion;
		}

		/// <summary>Builds the named analyzer with the given stop words.</summary>
		/// <remarks>Builds the named analyzer with the given stop words.</remarks>
		public SnowballAnalyzer(Version matchVersion, string name, CharArraySet stopWords
			) : this(matchVersion, name)
		{
			stopSet = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion, stopWords)
				);
		}

		/// <summary>
		/// Constructs a
		/// <see cref="Lucene.Net.Analysis.Standard.StandardTokenizer">Lucene.Net.Analysis.Standard.StandardTokenizer
		/// 	</see>
		/// filtered by a
		/// <see cref="Lucene.Net.Analysis.Standard.StandardFilter">Lucene.Net.Analysis.Standard.StandardFilter
		/// 	</see>
		/// , a
		/// <see cref="Lucene.Net.Analysis.Core.LowerCaseFilter">Lucene.Net.Analysis.Core.LowerCaseFilter
		/// 	</see>
		/// , a
		/// <see cref="Lucene.Net.Analysis.Core.StopFilter">Lucene.Net.Analysis.Core.StopFilter
		/// 	</see>
		/// ,
		/// and a
		/// <see cref="SnowballFilter">SnowballFilter</see>
		/// 
		/// </summary>
		public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
		{
			Tokenizer tokenizer = new StandardTokenizer(matchVersion, reader);
			TokenStream result = new StandardFilter(matchVersion, tokenizer);
			// remove the possessive 's for english stemmers
			if (matchVersion.OnOrAfter(Version.LUCENE_31) && (name.Equals("English"
				) || name.Equals("Porter") || name.Equals("Lovins")))
			{
				result = new EnglishPossessiveFilter(result);
			}
			// Use a special lowercase filter for turkish, the stemmer expects it.
            if (matchVersion.OnOrAfter(Version.LUCENE_31) && name.Equals("Turkish"
				))
			{
				result = new TurkishLowerCaseFilter(result);
			}
			else
			{
				result = new LowerCaseFilter(matchVersion, result);
			}
			if (stopSet != null)
			{
				result = new StopFilter(matchVersion, result, stopSet);
			}
			result = new SnowballFilter(result, name);
			return new Analyzer.TokenStreamComponents(tokenizer, result);
		}
	}
}
