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

using System;
using Lucene.Net.Analysis.Tokenattributes;
using SF.Snowball;

namespace Lucene.Net.Analysis.Snowball
{
	/// <summary>A filter that stems words using a Snowball-generated stemmer.</summary>
	/// <remarks>
	/// A filter that stems words using a Snowball-generated stemmer.
	/// Available stemmers are listed in
	/// <see cref="Org.Tartarus.Snowball.Ext">Org.Tartarus.Snowball.Ext</see>
	/// .
	/// <p><b>NOTE</b>: SnowballFilter expects lowercased text.
	/// <ul>
	/// <li>For the Turkish language, see
	/// <see cref="Lucene.Net.Analysis.TR.TurkishLowerCaseFilter">Lucene.Net.Analysis.TR.TurkishLowerCaseFilter
	/// 	</see>
	/// .
	/// <li>For other languages, see
	/// <see cref="Lucene.Net.Analysis.Core.LowerCaseFilter">Lucene.Net.Analysis.Core.LowerCaseFilter
	/// 	</see>
	/// .
	/// </ul>
	/// </p>
	/// <p>
	/// Note: This filter is aware of the
	/// <see cref="Lucene.Net.Analysis.Tokenattributes.KeywordAttribute">Lucene.Net.Analysis.Tokenattributes.KeywordAttribute
	/// 	</see>
	/// . To prevent
	/// certain terms from being passed to the stemmer
	/// <see cref="Lucene.Net.Analysis.Tokenattributes.KeywordAttribute.IsKeyword()
	/// 	">Lucene.Net.Analysis.Tokenattributes.KeywordAttribute.IsKeyword()</see>
	/// should be set to <code>true</code>
	/// in a previous
	/// <see cref="Lucene.Net.Analysis.TokenStream">Lucene.Net.Analysis.TokenStream
	/// 	</see>
	/// .
	/// Note: For including the original term as well as the stemmed version, see
	/// <see cref="Lucene.Net.Analysis.Miscellaneous.KeywordRepeatFilterFactory">Lucene.Net.Analysis.Miscellaneous.KeywordRepeatFilterFactory
	/// 	</see>
	/// </p>
	/// </remarks>
	public sealed class SnowballFilter : TokenFilter
	{
		private readonly SnowballProgram stemmer;

		private CharTermAttribute termAtt;

	    private KeywordAttribute keywordAttr;

		public SnowballFilter(TokenStream input, SnowballProgram stemmer) : base(input)
		{
			// javadoc @link
			this.stemmer = stemmer;
		    Init();
		}

	    private void Init()
	    {
            termAtt = AddAttribute<CharTermAttribute>();
            keywordAttr = AddAttribute<KeywordAttribute>();
	    }

	    /// <summary>Construct the named stemming filter.</summary>
		/// <remarks>
		/// Construct the named stemming filter.
		/// Available stemmers are listed in
		/// <see cref="Org.Tartarus.Snowball.Ext">Org.Tartarus.Snowball.Ext</see>
		/// .
		/// The name of a stemmer is the part of the class name before "Stemmer",
		/// e.g., the stemmer in
		/// <see cref="Org.Tartarus.Snowball.Ext.EnglishStemmer">Org.Tartarus.Snowball.Ext.EnglishStemmer
		/// 	</see>
		/// is named "English".
		/// </remarks>
		/// <param name="in">the input tokens to stem</param>
		/// <param name="name">the name of a stemmer</param>
		public SnowballFilter(TokenStream @in, string name) : base(@in)
		{
			//Class.forName is frowned upon in place of the ResourceLoader but in this case,
			// the factory will use the other constructor so that the program is already loaded.
			Init();
            try
			{
                System.Type stemClass = Type.GetType("SF.Snowball.Ext." + name + "Stemmer");
                stemmer = (SnowballProgram)Activator.CreateInstance(stemClass);
			}
			catch (Exception e)
			{
				throw new ArgumentException("Invalid stemmer class specified: " + name, e);
			}
		}

		/// <summary>Returns the next input Token, after being stemmed</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public sealed override bool IncrementToken()
		{
			if (input.IncrementToken())
			{
				if (!keywordAttr.IsKeyword)
				{
					char[] termBuffer = termAtt.Buffer;
					int length = termAtt.Length;
					stemmer.SetCurrent(termBuffer, length);
					stemmer.Stem();
					char[] finalTerm = stemmer.GetCurrentBuffer();
					int newLength = stemmer.GetCurrentBufferLength();
					if (finalTerm != termBuffer)
					{
						termAtt.CopyBuffer(finalTerm, 0, newLength);
					}
					else
					{
						termAtt.SetLength(newLength);
					}
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
