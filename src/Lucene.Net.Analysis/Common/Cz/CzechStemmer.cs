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

using Lucene.Analysis.Util;

namespace Lucene.Net.Analysis.CZ
{
	/// <summary>Light Stemmer for Czech.</summary>
	/// <remarks>
	/// Light Stemmer for Czech.
	/// <p>
	/// Implements the algorithm described in:
	/// <i>
	/// Indexing and stemming approaches for the Czech language
	/// </i>
	/// http://portal.acm.org/citation.cfm?id=1598600
	/// </p>
	/// </remarks>
	public class CzechStemmer
	{
		/// <summary>Stem an input buffer of Czech text.</summary>
		/// <remarks>Stem an input buffer of Czech text.</remarks>
		/// <param name="s">input buffer</param>
		/// <param name="len">length of input buffer</param>
		/// <returns>
		/// length of input buffer after normalization
		/// <p><b>NOTE</b>: Input is expected to be in lowercase,
		/// but with diacritical marks</p>
		/// </returns>
		public virtual int Stem(char[] s, int len)
		{
			len = RemoveCase(s, len);
			len = RemovePossessives(s, len);
			if (len > 0)
			{
				len = Normalize(s, len);
			}
			return len;
		}

		private int RemoveCase(char[] s, int len)
		{
			if (len > 7 && StemmerUtil.EndsWith(s, len, "atech"))
			{
				return len - 5;
			}
			if (len > 6 && (StemmerUtil.EndsWith(s, len, "Ä›tem") || StemmerUtil.EndsWith(s, 
				len, "etem") || StemmerUtil.EndsWith(s, len, "atÅ¯m")))
			{
				return len - 4;
			}
			if (len > 5 && (StemmerUtil.EndsWith(s, len, "ech") || StemmerUtil.EndsWith(s, len
				, "ich") || StemmerUtil.EndsWith(s, len, "Ã­ch") || StemmerUtil.EndsWith(s, len, 
				"Ã©ho") || StemmerUtil.EndsWith(s, len, "Ä›mi") || StemmerUtil.EndsWith(s, len, 
				"emi") || StemmerUtil.EndsWith(s, len, "Ã©mu") || StemmerUtil.EndsWith(s, len, "Ä›te"
				) || StemmerUtil.EndsWith(s, len, "ete") || StemmerUtil.EndsWith(s, len, "Ä›ti")
				 || StemmerUtil.EndsWith(s, len, "eti") || StemmerUtil.EndsWith(s, len, "Ã­ho") 
				|| StemmerUtil.EndsWith(s, len, "iho") || StemmerUtil.EndsWith(s, len, "Ã­mi") ||
				 StemmerUtil.EndsWith(s, len, "Ã­mu") || StemmerUtil.EndsWith(s, len, "imu") || 
				StemmerUtil.EndsWith(s, len, "Ã¡ch") || StemmerUtil.EndsWith(s, len, "ata") || StemmerUtil.EndsWith
				(s, len, "aty") || StemmerUtil.EndsWith(s, len, "Ã½ch") || StemmerUtil.EndsWith(
				s, len, "ama") || StemmerUtil.EndsWith(s, len, "ami") || StemmerUtil.EndsWith(s, 
				len, "ovÃ©") || StemmerUtil.EndsWith(s, len, "ovi") || StemmerUtil.EndsWith(s, len
				, "Ã½mi")))
			{
				return len - 3;
			}
			if (len > 4 && (StemmerUtil.EndsWith(s, len, "em") || StemmerUtil.EndsWith(s, len
				, "es") || StemmerUtil.EndsWith(s, len, "Ã©m") || StemmerUtil.EndsWith(s, len, "Ã­m"
				) || StemmerUtil.EndsWith(s, len, "Å¯m") || StemmerUtil.EndsWith(s, len, "at") ||
				 StemmerUtil.EndsWith(s, len, "Ã¡m") || StemmerUtil.EndsWith(s, len, "os") || StemmerUtil.EndsWith
				(s, len, "us") || StemmerUtil.EndsWith(s, len, "Ã½m") || StemmerUtil.EndsWith(s, 
				len, "mi") || StemmerUtil.EndsWith(s, len, "ou")))
			{
				return len - 2;
			}
			if (len > 3)
			{
				switch (s[len - 1])
				{
					case 'a':
					case 'e':
					case 'i':
					case 'o':
					case 'u':
					{
						//HM:uncomment
						return len - 1;
					}
				}
			}
			return len;
		}

		private int RemovePossessives(char[] s, int len)
		{
			if (len > 5 && (StemmerUtil.EndsWith(s, len, "ov") || StemmerUtil.EndsWith(s, len
				, "in") || StemmerUtil.EndsWith(s, len, "Å¯v")))
			{
				return len - 2;
			}
			return len;
		}

		private int Normalize(char[] s, int len)
		{
			if (StemmerUtil.EndsWith(s, len, "Ä�t"))
			{
				// Ä�t -> ck
				s[len - 2] = 'c';
				s[len - 1] = 'k';
				return len;
			}
			if (StemmerUtil.EndsWith(s, len, "Å¡t"))
			{
				// Å¡t -> sk
				s[len - 2] = 's';
				s[len - 1] = 'k';
				return len;
			}
			switch (s[len - 1])
			{
				case 'c':
				case 'z':
				{
					// [cÄ�] -> k
					//HM:uncomment
					// [zÅ¾] -> h
					//case 'Å¾': //HM:uncomment
					s[len - 1] = 'h';
					return len;
				}
			}
			if (len > 1 && s[len - 2] == 'e')
			{
				s[len - 2] = s[len - 1];
				// e* > *
				return len - 1;
			}
			//HM:uncomment
			return len;
		}
	}
}
