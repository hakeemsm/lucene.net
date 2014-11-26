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


using System.Collections.Generic;
using Lucene.Net.Analysis.PT;

namespace Lucene.Net.Analysis.GL
{
	/// <summary>Galician stemmer implementing "Regras do lematizador para o galego".</summary>
	/// <remarks>Galician stemmer implementing "Regras do lematizador para o galego".</remarks>
	/// <seealso cref="Lucene.Net.Analysis.PT.RSLPStemmerBase">Lucene.Net.Analysis.PT.RSLPStemmerBase
	/// 	</seealso>
	/// <seealso><a href="http://bvg.udc.es/recursos_lingua/stemming.jsp">Description of rules</a>
	/// 	</seealso>
	public class GalicianStemmer : RSLPStemmerBase
	{
		private static readonly RSLPStemmerBase.Step plural;

		private static readonly RSLPStemmerBase.Step unification;

		private static readonly RSLPStemmerBase.Step adverb;

		private static readonly RSLPStemmerBase.Step augmentative;

		private static readonly RSLPStemmerBase.Step noun;

		private static readonly RSLPStemmerBase.Step verb;

		private static readonly RSLPStemmerBase.Step vowel;

		static GalicianStemmer()
		{
			IDictionary<string, Step> steps = Parse(typeof(GalicianStemmer), 
				"galician.rslp");
			plural = steps["Plural"];
			unification = steps["Unification"];
			adverb = steps["Adverb"];
			augmentative = steps["Augmentative"];
			noun = steps["Noun"];
			verb = steps["Verb"];
			vowel = steps["Vowel"];
		}

		/// <param name="s">buffer, oversized to at least <code>len+1</code></param>
		/// <param name="len">initial valid length of buffer</param>
		/// <returns>new valid length, stemmed</returns>
		public virtual int Stem(char[] s, int len)
		{
			//HM:revisit 
			//assert s.length >= len + 1 : "this stemmer requires an oversized array of at least 1";
			len = plural.Apply(s, len);
			len = unification.Apply(s, len);
			len = adverb.Apply(s, len);
			int oldlen;
			do
			{
				oldlen = len;
				len = augmentative.Apply(s, len);
			}
			while (len != oldlen);
			oldlen = len;
			len = noun.Apply(s, len);
			if (len == oldlen)
			{
				len = verb.Apply(s, len);
			}
			len = vowel.Apply(s, len);
			// RSLG accent removal
			//HM:uncomment
			return len;
		}
	}
}
