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
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;

namespace Lucene.Net.Analysis.Core
{
	/// <summary>Normalizes token text to UPPER CASE.</summary>
	/// <remarks>
	/// Normalizes token text to UPPER CASE.
	/// <a name="version"/>
	/// <p>You must specify the required
	/// <see cref="Util.Version">Lucene.Net.Util.Version</see>
	/// compatibility when creating UpperCaseFilter
	/// <p><b>NOTE:</b> In Unicode, this transformation may lose information when the
	/// upper case character represents more than one lower case character. Use this filter
	/// when you require uppercase tokens.  Use the
	/// <see cref="LowerCaseFilter">LowerCaseFilter</see>
	/// for
	/// general search matching
	/// </remarks>
	public sealed class UpperCaseFilter : TokenFilter
	{
		private readonly CharacterUtils charUtils;

		private readonly ICharTermAttribute termAtt;

		/// <summary>Create a new UpperCaseFilter, that normalizes token text to upper case.</summary>
		/// <remarks>Create a new UpperCaseFilter, that normalizes token text to upper case.</remarks>
		/// <param name="matchVersion">See <a href="#version">above</a></param>
		/// <param name="in">TokenStream to filter</param>
		public UpperCaseFilter(Version matchVersion, TokenStream @in) : base(@in)
		{
            termAtt = AddAttribute<ICharTermAttribute>();
			charUtils = CharacterUtils.GetInstance(matchVersion);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
		    if (input.IncrementToken())
			{
				charUtils.ToUpperCase(termAtt.Buffer, 0, termAtt.Length);
				return true;
			}
		    return false;
		}
	}
}
