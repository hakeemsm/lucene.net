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

using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Commongrams
{
	/// <summary>
	/// Constructs a
	/// <see cref="CommonGramsFilter">CommonGramsFilter</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_cmmngrms" class="solr.TextField" positionIncrementGap="100"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	/// &lt;filter class="solr.CommonGramsFilterFactory" words="commongramsstopwords.txt" ignoreCase="false"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class CommonGramsFilterFactory : TokenFilterFactory, IResourceLoaderAware
	{
		private CharArraySet commonWords;

		private readonly string commonWordFiles;

		private readonly string format;

		private readonly bool ignoreCase;

		/// <summary>Creates a new CommonGramsFilterFactory</summary>
		protected internal CommonGramsFilterFactory(IDictionary<string, string> args) : base
			(args)
		{
			// TODO: shared base class for Stop/Keep/CommonGrams? 
			commonWordFiles = Get(args, "words");
			format = Get(args, "format");
			ignoreCase = GetBoolean(args, "ignoreCase", false);
			if (!args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Inform(IResourceLoader loader)
		{
			if (commonWordFiles != null)
			{
				if ("snowball".Equals(format,StringComparison.OrdinalIgnoreCase))
				{
					commonWords = GetSnowballWordSet(loader, commonWordFiles, ignoreCase);
				}
				else
				{
					commonWords = GetWordSet(loader, commonWordFiles, ignoreCase);
				}
			}
			else
			{
				commonWords = StopAnalyzer.ENGLISH_STOP_WORDS_SET;
			}
		}

		public virtual bool IsIgnoreCase()
		{
			return ignoreCase;
		}

		public virtual CharArraySet GetCommonWords()
		{
			return commonWords;
		}

		public override TokenStream Create(TokenStream input)
		{
			CommonGramsFilter commonGrams = new CommonGramsFilter(luceneMatchVersion.Value, input, 
				commonWords);
			return commonGrams;
		}
	}
}
