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

namespace Lucene.Net.Analysis.Ckb
{
	/// <summary>Light stemmer for Sorani</summary>
	public class SoraniStemmer
	{
		/// <summary>Stem an input buffer of Sorani text.</summary>
		/// <remarks>Stem an input buffer of Sorani text.</remarks>
		/// <param name="s">input buffer</param>
		/// <param name="len">length of input buffer</param>
		/// <returns>length of input buffer after normalization</returns>
		public virtual int Stem(char[] s, int len)
		{
			// postposition
			if (len > 5 && StemmerUtil.EndsWith(s, len, "Ø¯Ø§"))
			{
				len -= 2;
			}
			else
			{
				if (len > 4 && StemmerUtil.EndsWith(s, len, "Ù†Ø§"))
				{
					len--;
				}
				else
				{
					if (len > 6 && StemmerUtil.EndsWith(s, len, "Û•ÙˆÛ•"))
					{
						len -= 3;
					}
				}
			}
			// possessive pronoun
			if (len > 6 && (StemmerUtil.EndsWith(s, len, "Ù…Ø§Ù†") || StemmerUtil.EndsWith(s, 
				len, "ÛŒØ§Ù†") || StemmerUtil.EndsWith(s, len, "ØªØ§Ù†")))
			{
				len -= 3;
			}
			// indefinite singular ezafe
			if (len > 6 && StemmerUtil.EndsWith(s, len, "ÛŽÚ©ÛŒ"))
			{
				return len - 3;
			}
			else
			{
				if (len > 7 && StemmerUtil.EndsWith(s, len, "ÛŒÛ•Ú©ÛŒ"))
				{
					return len - 4;
				}
			}
			// indefinite singular
			if (len > 5 && StemmerUtil.EndsWith(s, len, "ÛŽÚ©"))
			{
				return len - 2;
			}
			else
			{
				if (len > 6 && StemmerUtil.EndsWith(s, len, "ÛŒÛ•Ú©"))
				{
					return len - 3;
				}
				else
				{
					// definite singular
					if (len > 6 && StemmerUtil.EndsWith(s, len, "Û•Ú©Û•"))
					{
						return len - 3;
					}
					else
					{
						if (len > 5 && StemmerUtil.EndsWith(s, len, "Ú©Û•"))
						{
							return len - 2;
						}
						else
						{
							// definite plural
							if (len > 7 && StemmerUtil.EndsWith(s, len, "Û•Ú©Ø§Ù†"))
							{
								return len - 4;
							}
							else
							{
								if (len > 6 && StemmerUtil.EndsWith(s, len, "Ú©Ø§Ù†"))
								{
									return len - 3;
								}
								else
								{
									// indefinite plural ezafe
									if (len > 7 && StemmerUtil.EndsWith(s, len, "ÛŒØ§Ù†ÛŒ"))
									{
										return len - 4;
									}
									else
									{
										if (len > 6 && StemmerUtil.EndsWith(s, len, "Ø§Ù†ÛŒ"))
										{
											return len - 3;
										}
										else
										{
											// indefinite plural
											if (len > 6 && StemmerUtil.EndsWith(s, len, "ÛŒØ§Ù†"))
											{
												return len - 3;
											}
											else
											{
												if (len > 5 && StemmerUtil.EndsWith(s, len, "Ø§Ù†"))
												{
													return len - 2;
												}
												else
												{
													// demonstrative plural
													if (len > 7 && StemmerUtil.EndsWith(s, len, "ÛŒØ§Ù†Û•"))
													{
														return len - 4;
													}
													else
													{
														if (len > 6 && StemmerUtil.EndsWith(s, len, "Ø§Ù†Û•"))
														{
															return len - 3;
														}
														else
														{
															// demonstrative singular
															if (len > 5 && (StemmerUtil.EndsWith(s, len, "Ø§ÛŒÛ•") || StemmerUtil.EndsWith(s, 
																len, "Û•ÛŒÛ•")))
															{
																return len - 2;
															}
															else
															{
																if (len > 4 && StemmerUtil.EndsWith(s, len, "Û•"))
																{
																	return len - 1;
																}
																else
																{
																	// absolute singular ezafe
																	if (len > 4 && StemmerUtil.EndsWith(s, len, "ÛŒ"))
																	{
																		return len - 1;
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return len;
		}
	}
}
