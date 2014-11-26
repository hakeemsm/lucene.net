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
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.Compound;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Compound
{
	/// <summary>
	/// Factory for
	/// <see cref="DictionaryCompoundWordTokenFilter">DictionaryCompoundWordTokenFilter</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_dictcomp" class="TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="WhitespaceTokenizerFactory"/&gt;
	/// &lt;filter class="DictionaryCompoundWordTokenFilterFactory" dictionary="dictionary.txt"
	/// minWordSize="5" minSubwordSize="2" maxSubwordSize="15" onlyLongestMatch="true"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class DictionaryCompoundWordTokenFilterFactory : TokenFilterFactory, IResourceLoaderAware
	{
		private CharArraySet dictionary;

		private readonly string dictFile;

		private readonly int minWordSize;

		private readonly int minSubwordSize;

		private readonly int maxSubwordSize;

		private readonly bool onlyLongestMatch;

		/// <summary>Creates a new DictionaryCompoundWordTokenFilterFactory</summary>
		protected internal DictionaryCompoundWordTokenFilterFactory(IDictionary<string, string
			> args) : base(args)
		{
			AssureMatchVersion();
			dictFile = Require(args, "dictionary");
			minWordSize = GetInt(args, "minWordSize", CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE
				);
			minSubwordSize = GetInt(args, "minSubwordSize", CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE
				);
			maxSubwordSize = GetInt(args, "maxSubwordSize", CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE
				);
			onlyLongestMatch = GetBoolean(args, "onlyLongestMatch", true);
			if (!args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Inform(IResourceLoader loader)
		{
			dictionary = base.GetWordSet(loader, dictFile, false);
		}

		public override TokenStream Create(TokenStream input)
		{
			// if the dictionary is null, it means it was empty
			return dictionary == null ? 
                    input : 
                    new DictionaryCompoundWordTokenFilter(luceneMatchVersion.Value, input, dictionary, minWordSize, minSubwordSize, maxSubwordSize, onlyLongestMatch);
		}
	}
}
