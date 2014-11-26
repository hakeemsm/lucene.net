/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.ID
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Analysis.TokenFilter">Lucene.Net.Analysis.TokenFilter
	/// 	</see>
	/// that applies
	/// <see cref="IndonesianStemmer">IndonesianStemmer</see>
	/// to stem Indonesian words.
	/// </summary>
	public sealed class IndonesianStemFilter : TokenFilter
	{
		private readonly ICharTermAttribute termAtt;
	    private readonly IKeywordAttribute keywordAtt;
	    private readonly IndonesianStemmer stemmer;

		private readonly bool stemDerivational;

		/// <summary>
		/// Calls
		/// <see cref="IndonesianStemFilter(Lucene.Net.Analysis.TokenStream, bool)">IndonesianStemFilter(input, true)
		/// 	</see>
		/// </summary>
		public IndonesianStemFilter(TokenStream input) : this(input, true)
		{
            termAtt = AddAttribute<ICharTermAttribute>();
            keywordAtt = AddAttribute<IKeywordAttribute>();
            stemmer = new IndonesianStemmer();
		}

		/// <summary>Create a new IndonesianStemFilter.</summary>
		/// <remarks>
		/// Create a new IndonesianStemFilter.
		/// <p>
		/// If <code>stemDerivational</code> is false,
		/// only inflectional suffixes (particles and possessive pronouns) are stemmed.
		/// </remarks>
		public IndonesianStemFilter(TokenStream input, bool stemDerivational) : base(input
			)
		{
			this.stemDerivational = stemDerivational;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (input.IncrementToken())
			{
				if (!keywordAtt.IsKeyword)
				{
					int newlen = stemmer.Stem(termAtt.Buffer, termAtt.Length, stemDerivational);
					termAtt.SetLength(newlen);
				}
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
