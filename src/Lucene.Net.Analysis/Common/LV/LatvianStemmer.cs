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

namespace Lucene.Net.Analysis.LV
{
	/// <summary>Light stemmer for Latvian.</summary>
	/// <remarks>
	/// Light stemmer for Latvian.
	/// <p>
	/// This is a light version of the algorithm in Karlis Kreslin's PhD thesis
	/// <i>A stemming algorithm for Latvian</i> with the following modifications:
	/// <ul>
	/// <li>Only explicitly stems noun and adjective morphology
	/// <li>Stricter length/vowel checks for the resulting stems (verb etc suffix stripping is removed)
	/// <li>Removes only the primary inflectional suffixes: case and number for nouns ;
	/// case, number, gender, and definitiveness for adjectives.
	/// <li>Palatalization is only handled when a declension II,V,VI noun suffix is removed.
	/// </ul>
	/// </remarks>
	public class LatvianStemmer
	{
		/// <summary>Stem a latvian word.</summary>
		/// <remarks>Stem a latvian word. returns the new adjusted length.</remarks>
		public virtual int Stem(char[] s, int len)
		{
			int numVowels = NumVowels(s, len);
			for (int i = 0; i < affixes.Length; i++)
			{
				LatvianStemmer.Affix affix = affixes[i];
				if (numVowels > affix.vc && len >= affix.AffixArray.Length + 3 && StemmerUtil.EndsWith(s, len, affix.AffixArray))
				{
					len -= affix.AffixArray.Length;
					return affix.palatalizes ? Unpalatalize(s, len) : len;
				}
			}
			return len;
		}

		internal static readonly LatvianStemmer.Affix[] affixes =
		{ new LatvianStemmer.Affix("ajiem", 3, false), new LatvianStemmer.Affix("ajai"
		    , 3, false), new LatvianStemmer.Affix("ajam", 2, false), new LatvianStemmer.Affix
		        ("ajÄ�m", 2, false), new LatvianStemmer.Affix("ajos", 2, false), new LatvianStemmer.Affix
		            ("ajÄ�s", 2, false), new LatvianStemmer.Affix("iem", 2, true), new LatvianStemmer.Affix
		                ("ajÄ�", 2, false), new LatvianStemmer.Affix("ais", 2, false), new LatvianStemmer.Affix
		                    ("ai", 2, false), new LatvianStemmer.Affix("ei", 2, false), new LatvianStemmer.Affix
		                        ("Ä�m", 1, false), new LatvianStemmer.Affix("am", 1, false), new LatvianStemmer.Affix
		                            ("Ä“m", 1, false), new LatvianStemmer.Affix("Ä«m", 1, false), new LatvianStemmer.Affix
		                                ("im", 1, false), new LatvianStemmer.Affix("um", 1, false), new LatvianStemmer.Affix
		                                    ("us", 1, true), new LatvianStemmer.Affix("as", 1, false), new LatvianStemmer.Affix
		                                        ("Ä�s", 1, false), new LatvianStemmer.Affix("es", 1, false), new LatvianStemmer.Affix
		                                            ("os", 1, true), new LatvianStemmer.Affix("ij", 1, false), new LatvianStemmer.Affix
		                                                ("Ä«s", 1, false), new LatvianStemmer.Affix("Ä“s", 1, false), new LatvianStemmer.Affix
		                                                    ("is", 1, false), new LatvianStemmer.Affix("ie", 1, false), new LatvianStemmer.Affix
		                                                        ("u", 1, true), new LatvianStemmer.Affix("a", 1, true), new LatvianStemmer.Affix
		                                                            ("i", 1, true), new LatvianStemmer.Affix("e", 1, false), new LatvianStemmer.Affix
		                                                                ("Ä�", 1, false), new LatvianStemmer.Affix("Ä“", 1, false), new LatvianStemmer.Affix
		                                                                    ("Ä«", 1, false), new LatvianStemmer.Affix("Å«", 1, false), new LatvianStemmer.Affix
		                                                                        ("o", 1, false), new LatvianStemmer.Affix("s", 0, false), new LatvianStemmer.Affix
		                                                                            ("Å¡", 0, false) };

		internal class Affix
		{
			internal char[] AffixArray;

			internal int vc;

			internal bool palatalizes;

			internal Affix(string affix, int vc, bool palatalizes)
			{
				// suffix
				// vowel count of the suffix
				// true if we should fire palatalization rules.
				this.AffixArray = affix.ToCharArray();
				this.vc = vc;
				this.palatalizes = palatalizes;
			}
		}

		/// <summary>
		/// Most cases are handled except for the ambiguous ones:
		/// <ul>
		/// <li> s -&gt; Å¡
		/// <li> t -&gt; Å¡
		/// <li> d -&gt; Å¾
		/// <li> z -&gt; Å¾
		/// </ul>
		/// </summary>
		private int Unpalatalize(char[] s, int len)
		{
			// we check the character removed: if its -u then 
			// its 2,5, or 6 gen pl., and these two can only apply then.
			if (s[len] == 'u')
			{
				// kÅ¡ -> kst
				if (StemmerUtil.EndsWith(s, len, "kÅ¡"))
				{
					len++;
					s[len - 2] = 's';
					s[len - 1] = 't';
					return len;
				}
				// Å†Å† -> nn
				if (StemmerUtil.EndsWith(s, len, "Å†Å†"))
				{
					s[len - 2] = 'n';
					s[len - 1] = 'n';
					return len;
				}
			}
			// otherwise all other rules
			if (StemmerUtil.EndsWith(s, len, "pj") || StemmerUtil.EndsWith(s, len, "bj") || StemmerUtil.EndsWith
				(s, len, "mj") || StemmerUtil.EndsWith(s, len, "vj"))
			{
				// labial consonant
				return len - 1;
			}
			else
			{
				if (StemmerUtil.EndsWith(s, len, "Å¡Å†"))
				{
					s[len - 2] = 's';
					s[len - 1] = 'n';
					return len;
				}
				else
				{
					if (StemmerUtil.EndsWith(s, len, "Å¾Å†"))
					{
						s[len - 2] = 'z';
						s[len - 1] = 'n';
						return len;
					}
					else
					{
						if (StemmerUtil.EndsWith(s, len, "Å¡Ä¼"))
						{
							s[len - 2] = 's';
							s[len - 1] = 'l';
							return len;
						}
						else
						{
							if (StemmerUtil.EndsWith(s, len, "Å¾Ä¼"))
							{
								s[len - 2] = 'z';
								s[len - 1] = 'l';
								return len;
							}
							else
							{
								if (StemmerUtil.EndsWith(s, len, "Ä¼Å†"))
								{
									s[len - 2] = 'l';
									s[len - 1] = 'n';
									return len;
								}
								else
								{
									if (StemmerUtil.EndsWith(s, len, "Ä¼Ä¼"))
									{
										s[len - 2] = 'l';
										s[len - 1] = 'l';
										return len;
									}
								}
							}
						}
					}
				}
			}
			//HM:uncomment
			return len;
		}

		/// <summary>
		/// Count the vowels in the string, we always require at least
		/// one in the remaining stem to accept it.
		/// </summary>
		/// <remarks>
		/// Count the vowels in the string, we always require at least
		/// one in the remaining stem to accept it.
		/// </remarks>
		private int NumVowels(char[] s, int len)
		{
			int n = 0;
			for (int i = 0; i < len; i++)
			{
				switch (s[i])
				{
					case 'a':
					case 'e':
					case 'i':
					case 'o':
					case 'u':
					{
						//HM:uncomment
						n++;
					}
				}
			}
			return n;
		}
	}
}
