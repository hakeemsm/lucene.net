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

namespace Lucene.Net.Analysis.HU
{
	/// <summary>Light Stemmer for Hungarian.</summary>
	/// <remarks>
	/// Light Stemmer for Hungarian.
	/// <p>
	/// This stemmer implements the "UniNE" algorithm in:
	/// <i>Light Stemming Approaches for the French, Portuguese, German and Hungarian Languages</i>
	/// Jacques Savoy
	/// </remarks>
	public class HungarianLightStemmer
	{
		public virtual int Stem(char[] s, int len)
		{
			//HM:uncomment
			len = RemoveCase(s, len);
			len = RemovePossessive(s, len);
			len = RemovePlural(s, len);
			return Normalize(s, len);
		}

		private int RemoveCase(char[] s, int len)
		{
			if (len > 6 && StemmerUtil.EndsWith(s, len, "kent"))
			{
				return len - 4;
			}
			if (len > 5)
			{
				if (StemmerUtil.EndsWith(s, len, "nak") || StemmerUtil.EndsWith(s, len, "nek") ||
					 StemmerUtil.EndsWith(s, len, "val") || StemmerUtil.EndsWith(s, len, "vel") || StemmerUtil.EndsWith
					(s, len, "ert") || StemmerUtil.EndsWith(s, len, "rol") || StemmerUtil.EndsWith(s
					, len, "ban") || StemmerUtil.EndsWith(s, len, "ben") || StemmerUtil.EndsWith(s, 
					len, "bol") || StemmerUtil.EndsWith(s, len, "nal") || StemmerUtil.EndsWith(s, len
					, "nel") || StemmerUtil.EndsWith(s, len, "hoz") || StemmerUtil.EndsWith(s, len, 
					"hez") || StemmerUtil.EndsWith(s, len, "tol"))
				{
					return len - 3;
				}
				if (StemmerUtil.EndsWith(s, len, "al") || StemmerUtil.EndsWith(s, len, "el"))
				{
					if (!IsVowel(s[len - 3]) && s[len - 3] == s[len - 4])
					{
						return len - 3;
					}
				}
			}
			if (len > 4)
			{
				if (StemmerUtil.EndsWith(s, len, "at") || StemmerUtil.EndsWith(s, len, "et") || StemmerUtil.EndsWith
					(s, len, "ot") || StemmerUtil.EndsWith(s, len, "va") || StemmerUtil.EndsWith(s, 
					len, "ve") || StemmerUtil.EndsWith(s, len, "ra") || StemmerUtil.EndsWith(s, len, 
					"re") || StemmerUtil.EndsWith(s, len, "ba") || StemmerUtil.EndsWith(s, len, "be"
					) || StemmerUtil.EndsWith(s, len, "ul") || StemmerUtil.EndsWith(s, len, "ig"))
				{
					return len - 2;
				}
				if ((StemmerUtil.EndsWith(s, len, "on") || StemmerUtil.EndsWith(s, len, "en")) &&
					 !IsVowel(s[len - 3]))
				{
					return len - 2;
				}
				switch (s[len - 1])
				{
					case 't':
					case 'n':
					{
						return len - 1;
					}

					case 'a':
					case 'e':
					{
						if (s[len - 2] == s[len - 3] && !IsVowel(s[len - 2]))
						{
							return len - 2;
						}
					}
                        break;
				}
			}
			return len;
		}

		private int RemovePossessive(char[] s, int len)
		{
			if (len > 6)
			{
				if (!IsVowel(s[len - 5]) && (StemmerUtil.EndsWith(s, len, "atok") || StemmerUtil.EndsWith
					(s, len, "otok") || StemmerUtil.EndsWith(s, len, "etek")))
				{
					return len - 4;
				}
				if (StemmerUtil.EndsWith(s, len, "itek") || StemmerUtil.EndsWith(s, len, "itok"))
				{
					return len - 4;
				}
			}
			if (len > 5)
			{
				if (!IsVowel(s[len - 4]) && (StemmerUtil.EndsWith(s, len, "unk") || StemmerUtil.EndsWith
					(s, len, "tok") || StemmerUtil.EndsWith(s, len, "tek")))
				{
					return len - 3;
				}
				if (IsVowel(s[len - 4]) && StemmerUtil.EndsWith(s, len, "juk"))
				{
					return len - 3;
				}
				if (StemmerUtil.EndsWith(s, len, "ink"))
				{
					return len - 3;
				}
			}
			if (len > 4)
			{
				if (!IsVowel(s[len - 3]) && (StemmerUtil.EndsWith(s, len, "am") || StemmerUtil.EndsWith
					(s, len, "em") || StemmerUtil.EndsWith(s, len, "om") || StemmerUtil.EndsWith(s, 
					len, "ad") || StemmerUtil.EndsWith(s, len, "ed") || StemmerUtil.EndsWith(s, len, 
					"od") || StemmerUtil.EndsWith(s, len, "uk")))
				{
					return len - 2;
				}
				if (IsVowel(s[len - 3]) && (StemmerUtil.EndsWith(s, len, "nk") || StemmerUtil.EndsWith
					(s, len, "ja") || StemmerUtil.EndsWith(s, len, "je")))
				{
					return len - 2;
				}
				if (StemmerUtil.EndsWith(s, len, "im") || StemmerUtil.EndsWith(s, len, "id") || StemmerUtil.EndsWith
					(s, len, "ik"))
				{
					return len - 2;
				}
			}
			if (len > 3)
			{
				switch (s[len - 1])
				{
					case 'a':
					case 'e':
					{
						if (!IsVowel(s[len - 2]))
						{
							return len - 1;
						}
						break;
					}

					case 'm':
					case 'd':
					{
						if (IsVowel(s[len - 2]))
						{
							return len - 1;
						}
						break;
					}

					case 'i':
					{
						return len - 1;
					}
				}
			}
			return len;
		}

		private int RemovePlural(char[] s, int len)
		{
			if (len > 3 && s[len - 1] == 'k')
			{
				switch (s[len - 2])
				{
					case 'a':
					case 'o':
					case 'e':
					{
						if (len > 4)
						{
							return len - 2;
						}
						goto default;
					}

					default:
					{
						return len - 1;
						break;
					}
				}
			}
			return len;
		}

		private int Normalize(char[] s, int len)
		{
			if (len > 3)
			{
				switch (s[len - 1])
				{
					case 'a':
					case 'e':
					case 'i':
					case 'o':
					{
						return len - 1;
					}
				}
			}
			return len;
		}

		private bool IsVowel(char ch)
		{
			switch (ch)
			{
				case 'a':
				case 'e':
				case 'i':
				case 'o':
				case 'u':
				case 'y':
				{
					return true;
				}

				default:
				{
					return false;
					break;
				}
			}
		}
	}
}
