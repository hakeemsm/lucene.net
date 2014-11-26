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

namespace Lucene.Net.Analysis.HI
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Analysis.TokenFilter">Lucene.Net.Analysis.TokenFilter
	/// 	</see>
	/// that applies
	/// <see cref="HindiNormalizer">HindiNormalizer</see>
	/// to normalize the
	/// orthography.
	/// <p>
	/// In some cases the normalization may cause unrelated terms to conflate, so
	/// to prevent terms from being normalized use an instance of
	/// <see cref="Lucene.Net.Analysis.Miscellaneous.SetKeywordMarkerFilter">Lucene.Net.Analysis.Miscellaneous.SetKeywordMarkerFilter
	/// 	</see>
	/// or a custom
	/// <see cref="Lucene.Net.Analysis.TokenFilter">Lucene.Net.Analysis.TokenFilter
	/// 	</see>
	/// that sets
	/// the
	/// <see cref="Lucene.Net.Analysis.Tokenattributes.KeywordAttribute">Lucene.Net.Analysis.Tokenattributes.KeywordAttribute
	/// 	</see>
	/// before this
	/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
	/// 	</see>
	/// .
	/// </p>
	/// </summary>
	/// <seealso cref="HindiNormalizer">HindiNormalizer</seealso>
	public sealed class HindiNormalizationFilter : TokenFilter
	{
	    private readonly HindiNormalizer normalizer;

	    private readonly ICharTermAttribute termAtt;

	    private readonly IKeywordAttribute keywordAtt;

	    public HindiNormalizationFilter(TokenStream input) : base(input)
		{
            normalizer = new HindiNormalizer();
            termAtt = AddAttribute<ICharTermAttribute>();
            keywordAtt = AddAttribute<IKeywordAttribute>();
		}

		// javadoc @link
		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (input.IncrementToken())
			{
				if (!keywordAtt.IsKeyword)
				{
					termAtt.SetLength(normalizer.Normalize(termAtt.Buffer, termAtt.Length));
				}
				return true;
			}
			return false;
		}
	}
}
