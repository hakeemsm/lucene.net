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

using Lucene.Analysis.Util;

namespace Lucene.Net.Analysis.FI
{
	/// <summary>Light Stemmer for Finnish.</summary>
	/// <remarks>
	/// Light Stemmer for Finnish.
	/// <p>
	/// This stemmer implements the algorithm described in:
	/// <i>Report on CLEF-2003 Monolingual Tracks</i>
	/// Jacques Savoy
	/// </remarks>
	public class FinnishLightStemmer
	{
		public virtual int Stem(char[] s, int len)
		{
			if (len < 4)
			{
				return len;
			}
			for (int i = 0; i < len; i++)
			{
				//HM:uncomment
				len = Step1(s, len);
			}
			len = Step2(s, len);
			len = Step3(s, len);
			len = Norm1(s, len);
			len = Norm2(s, len);
			return len;
		}

		private int Step1(char[] s, int len)
		{
			if (len > 8)
			{
				if (StemmerUtil.EndsWith(s, len, "kin"))
				{
					return Step1(s, len - 3);
				}
				if (StemmerUtil.EndsWith(s, len, "ko"))
				{
					return Step1(s, len - 2);
				}
			}
			if (len > 11)
			{
				if (StemmerUtil.EndsWith(s, len, "dellinen"))
				{
					return len - 8;
				}
				if (StemmerUtil.EndsWith(s, len, "dellisuus"))
				{
					return len - 9;
				}
			}
			return len;
		}

		private int Step2(char[] s, int len)
		{
			if (len > 5)
			{
				if (StemmerUtil.EndsWith(s, len, "lla") || StemmerUtil.EndsWith(s, len, "tse") ||
					 StemmerUtil.EndsWith(s, len, "sti"))
				{
					return len - 3;
				}
				if (StemmerUtil.EndsWith(s, len, "ni"))
				{
					return len - 2;
				}
				if (StemmerUtil.EndsWith(s, len, "aa"))
				{
					return len - 1;
				}
			}
			// aa -> a
			return len;
		}

		private int Step3(char[] s, int len)
		{
			if (len > 8)
			{
				if (StemmerUtil.EndsWith(s, len, "nnen"))
				{
					s[len - 4] = 's';
					return len - 3;
				}
				if (StemmerUtil.EndsWith(s, len, "ntena"))
				{
					s[len - 5] = 's';
					return len - 4;
				}
				if (StemmerUtil.EndsWith(s, len, "tten"))
				{
					return len - 4;
				}
				if (StemmerUtil.EndsWith(s, len, "eiden"))
				{
					return len - 5;
				}
			}
			if (len > 6)
			{
				if (StemmerUtil.EndsWith(s, len, "neen") || StemmerUtil.EndsWith(s, len, "niin") 
					|| StemmerUtil.EndsWith(s, len, "seen") || StemmerUtil.EndsWith(s, len, "teen") 
					|| StemmerUtil.EndsWith(s, len, "inen"))
				{
					return len - 4;
				}
				if (s[len - 3] == 'h' && IsVowel(s[len - 2]) && s[len - 1] == 'n')
				{
					return len - 3;
				}
				if (StemmerUtil.EndsWith(s, len, "den"))
				{
					s[len - 3] = 's';
					return len - 2;
				}
				if (StemmerUtil.EndsWith(s, len, "ksen"))
				{
					s[len - 4] = 's';
					return len - 3;
				}
				if (StemmerUtil.EndsWith(s, len, "ssa") || StemmerUtil.EndsWith(s, len, "sta") ||
					 StemmerUtil.EndsWith(s, len, "lla") || StemmerUtil.EndsWith(s, len, "lta") || StemmerUtil.EndsWith
					(s, len, "tta") || StemmerUtil.EndsWith(s, len, "ksi") || StemmerUtil.EndsWith(s
					, len, "lle"))
				{
					return len - 3;
				}
			}
			if (len > 5)
			{
				if (StemmerUtil.EndsWith(s, len, "na") || StemmerUtil.EndsWith(s, len, "ne"))
				{
					return len - 2;
				}
				if (StemmerUtil.EndsWith(s, len, "nei"))
				{
					return len - 3;
				}
			}
			if (len > 4)
			{
				if (StemmerUtil.EndsWith(s, len, "ja") || StemmerUtil.EndsWith(s, len, "ta"))
				{
					return len - 2;
				}
				if (s[len - 1] == 'a')
				{
					return len - 1;
				}
				if (s[len - 1] == 'n' && IsVowel(s[len - 2]))
				{
					return len - 2;
				}
				if (s[len - 1] == 'n')
				{
					return len - 1;
				}
			}
			return len;
		}

		private int Norm1(char[] s, int len)
		{
			if (len > 5 && StemmerUtil.EndsWith(s, len, "hde"))
			{
				s[len - 3] = 'k';
				s[len - 2] = 's';
				s[len - 1] = 'i';
			}
			if (len > 4)
			{
				if (StemmerUtil.EndsWith(s, len, "ei") || StemmerUtil.EndsWith(s, len, "at"))
				{
					return len - 2;
				}
			}
			if (len > 3)
			{
				switch (s[len - 1])
				{
					case 't':
					case 's':
					case 'j':
					case 'e':
					case 'a':
					case 'i':
					{
						return len - 1;
					}
				}
			}
			return len;
		}

		private int Norm2(char[] s, int len)
		{
			if (len > 8)
			{
				if (s[len - 1] == 'e' || s[len - 1] == 'o' || s[len - 1] == 'u')
				{
					len--;
				}
			}
			if (len > 4)
			{
				if (s[len - 1] == 'i')
				{
					len--;
				}
				if (len > 4)
				{
					char ch = s[0];
					for (int i = 1; i < len; i++)
					{
						if (s[i] == ch && (ch == 'k' || ch == 'p' || ch == 't'))
						{
							len = StemmerUtil.Delete(s, i--, len);
						}
						else
						{
							ch = s[i];
						}
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
