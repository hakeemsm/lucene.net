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

namespace Lucene.Net.Analysis.HI
{
	/// <summary>Light Stemmer for Hindi.</summary>
	/// <remarks>
	/// Light Stemmer for Hindi.
	/// <p>
	/// Implements the algorithm specified in:
	/// <i>A Lightweight Stemmer for Hindi</i>
	/// Ananthakrishnan Ramanathan and Durgesh D Rao.
	/// http://computing.open.ac.uk/Sites/EACLSouthAsia/Papers/p6-Ramanathan.pdf
	/// </p>
	/// </remarks>
	public class HindiStemmer
	{
		public virtual int Stem(char[] buffer, int len)
		{
			// 5
			if ((len > 6) && (StemmerUtil.EndsWith(buffer, len, "à¤¾à¤�à¤‚à¤—à¥€") || StemmerUtil.EndsWith
				(buffer, len, "à¤¾à¤�à¤‚à¤—à¥‡") || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤Šà¤‚à¤—à¥€"
				) || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤Šà¤‚à¤—à¤¾") || StemmerUtil.EndsWith
				(buffer, len, "à¤¾à¤‡à¤¯à¤¾à¤�") || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤‡à¤¯à¥‹à¤‚"
				) || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤‡à¤¯à¤¾à¤‚")))
			{
				return len - 5;
			}
			// 4
			if ((len > 5) && (StemmerUtil.EndsWith(buffer, len, "à¤¾à¤�à¤—à¥€") || StemmerUtil.EndsWith
				(buffer, len, "à¤¾à¤�à¤—à¤¾") || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤“à¤—à¥€"
				) || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤“à¤—à¥‡") || StemmerUtil.EndsWith(buffer
				, len, "à¤�à¤‚à¤—à¥€") || StemmerUtil.EndsWith(buffer, len, "à¥‡à¤‚à¤—à¥€") || StemmerUtil.EndsWith
				(buffer, len, "à¤�à¤‚à¤—à¥‡") || StemmerUtil.EndsWith(buffer, len, "à¥‡à¤‚à¤—à¥‡"
				) || StemmerUtil.EndsWith(buffer, len, "à¥‚à¤‚à¤—à¥€") || StemmerUtil.EndsWith(buffer
				, len, "à¥‚à¤‚à¤—à¤¾") || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤¤à¥€à¤‚") || StemmerUtil.EndsWith
				(buffer, len, "à¤¨à¤¾à¤“à¤‚") || StemmerUtil.EndsWith(buffer, len, "à¤¨à¤¾à¤�à¤‚"
				) || StemmerUtil.EndsWith(buffer, len, "à¤¤à¤¾à¤“à¤‚") || StemmerUtil.EndsWith(buffer
				, len, "à¤¤à¤¾à¤�à¤‚") || StemmerUtil.EndsWith(buffer, len, "à¤¿à¤¯à¤¾à¤�") || StemmerUtil.EndsWith
				(buffer, len, "à¤¿à¤¯à¥‹à¤‚") || StemmerUtil.EndsWith(buffer, len, "à¤¿à¤¯à¤¾à¤‚"
				)))
			{
				return len - 4;
			}
			// 3
			if ((len > 4) && (StemmerUtil.EndsWith(buffer, len, "à¤¾à¤•à¤°") || StemmerUtil.EndsWith
				(buffer, len, "à¤¾à¤‡à¤�") || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤ˆà¤‚") || 
				StemmerUtil.EndsWith(buffer, len, "à¤¾à¤¯à¤¾") || StemmerUtil.EndsWith(buffer, len
				, "à¥‡à¤—à¥€") || StemmerUtil.EndsWith(buffer, len, "à¥‡à¤—à¤¾") || StemmerUtil.EndsWith
				(buffer, len, "à¥‹à¤—à¥€") || StemmerUtil.EndsWith(buffer, len, "à¥‹à¤—à¥‡") || 
				StemmerUtil.EndsWith(buffer, len, "à¤¾à¤¨à¥‡") || StemmerUtil.EndsWith(buffer, len
				, "à¤¾à¤¨à¤¾") || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤¤à¥‡") || StemmerUtil.EndsWith
				(buffer, len, "à¤¾à¤¤à¥€") || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤¤à¤¾") || 
				StemmerUtil.EndsWith(buffer, len, "à¤¤à¥€à¤‚") || StemmerUtil.EndsWith(buffer, len
				, "à¤¾à¤“à¤‚") || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤�à¤‚") || StemmerUtil.EndsWith
				(buffer, len, "à¥�à¤“à¤‚") || StemmerUtil.EndsWith(buffer, len, "à¥�à¤�à¤‚") || 
				StemmerUtil.EndsWith(buffer, len, "à¥�à¤†à¤‚")))
			{
				return len - 3;
			}
			// 2
			if ((len > 3) && (StemmerUtil.EndsWith(buffer, len, "à¤•à¤°") || StemmerUtil.EndsWith
				(buffer, len, "à¤¾à¤“") || StemmerUtil.EndsWith(buffer, len, "à¤¿à¤�") || StemmerUtil.EndsWith
				(buffer, len, "à¤¾à¤ˆ") || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤�") || StemmerUtil.EndsWith
				(buffer, len, "à¤¨à¥‡") || StemmerUtil.EndsWith(buffer, len, "à¤¨à¥€") || StemmerUtil.EndsWith
				(buffer, len, "à¤¨à¤¾") || StemmerUtil.EndsWith(buffer, len, "à¤¤à¥‡") || StemmerUtil.EndsWith
				(buffer, len, "à¥€à¤‚") || StemmerUtil.EndsWith(buffer, len, "à¤¤à¥€") || StemmerUtil.EndsWith
				(buffer, len, "à¤¤à¤¾") || StemmerUtil.EndsWith(buffer, len, "à¤¾à¤�") || StemmerUtil.EndsWith
				(buffer, len, "à¤¾à¤‚") || StemmerUtil.EndsWith(buffer, len, "à¥‹à¤‚") || StemmerUtil.EndsWith
				(buffer, len, "à¥‡à¤‚")))
			{
				return len - 2;
			}
			// 1
			if ((len > 2) && (StemmerUtil.EndsWith(buffer, len, "à¥‹") || StemmerUtil.EndsWith
				(buffer, len, "à¥‡") || StemmerUtil.EndsWith(buffer, len, "à¥‚") || StemmerUtil.EndsWith
				(buffer, len, "à¥�") || StemmerUtil.EndsWith(buffer, len, "à¥€") || StemmerUtil.EndsWith
				(buffer, len, "à¤¿") || StemmerUtil.EndsWith(buffer, len, "à¤¾")))
			{
				return len - 1;
			}
			return len;
		}
	}
}
