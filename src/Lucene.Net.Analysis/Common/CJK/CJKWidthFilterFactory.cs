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
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Cjk
{
	/// <summary>
	/// Factory for
	/// <see cref="CJKWidthFilter">CJKWidthFilter</see>
	/// .
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_cjk" class="solr.TextField"&gt;
	/// &lt;analyzer&gt;
	/// &lt;tokenizer class="StandardTokenizerFactory"/&gt;
	/// &lt;filter class="CJKWidthFilterFactory"/&gt;
	/// &lt;filter class="LowerCaseFilterFactory"/&gt;
	/// &lt;filter class="CJKBigramFilterFactory"/&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class CJKWidthFilterFactory : TokenFilterFactory, IMultiTermAwareComponent
	{
		/// <summary>Creates a new CJKWidthFilterFactory</summary>
		protected internal CJKWidthFilterFactory(IDictionary<string, string> args) : base
			(args)
		{
			if (!args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		public override TokenStream Create(TokenStream input)
		{
			return new CJKWidthFilter(input);
		}

		public virtual AbstractAnalysisFactory MultiTermComponent
		{
		    get { return this; }
		}
	}
}
