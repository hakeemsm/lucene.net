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


using System.IO;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.IN
{
	/// <summary>Simple Tokenizer for text in Indian Languages.</summary>
	/// <remarks>Simple Tokenizer for text in Indian Languages.</remarks>
	[System.ObsoleteAttribute(@"(3.6) Use Lucene.Net.Analysis.Standard.StandardTokenizer instead."
		)]
	public sealed class IndicTokenizer : CharTokenizer
	{
		public IndicTokenizer(Version matchVersion, AttributeSource.AttributeFactory factory
			, StreamReader input) : base(matchVersion, factory, input)
		{
		}

		public IndicTokenizer(Version matchVersion, StreamReader input) : base(matchVersion
			, input)
		{
		}

		// javadocs
		protected override bool IsTokenChar(int c)
		{
			return char.IsLetter((char)c) || c == Character.NON_SPACING_MARK || c == Character.FORMAT || c == Character.COMBINING_SPACING_MARK;
		}
	}
}
