/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using SF.Snowball.Ext;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Snowball
{

    /// <summary>Filters <see cref="StandardTokenizer"/> with <see cref="StandardFilter"/>, {@link
    /// LowerCaseFilter}, <see cref="StopFilter"/> and <see cref="SnowballFilter"/>.
    /// 
    /// Available stemmers are listed in <see cref="SF.Snowball.Ext"/>.  The name of a
    /// stemmer is the part of the class name before "Stemmer", e.g., the stemmer in
    /// <see cref="EnglishStemmer"/> is named "English".
    /// 
    /// <p><b>NOTE:</b> This class uses the same <see cref="Version"/>
    /// dependent settings as <see cref="StandardAnalyzer"/></p>
    /// </summary>
    public class SnowballAnalyzer : Analyzer
    {
        private System.String name;
        private CharArraySet stopSet;
        private readonly Version matchVersion;

        /// <summary>Builds the named analyzer with no stop words. </summary>
        public SnowballAnalyzer(Version matchVersion, System.String name)
        {
            this.name = name;
            this.matchVersion = matchVersion;
        }

        /// <summary>Builds the named analyzer with the given stop words. </summary>
        [Obsolete("Use SnowballAnalyzer(Version, string, ISet) instead.")]
        public SnowballAnalyzer(Version matchVersion, System.String name, System.String[] stopWords)
            : this(matchVersion, name)
        {
            stopSet = CharArraySet.UnmodifiableSet(CharArraySet.Copy(matchVersion, stopWords));

        }

        /// <summary>
        /// Builds the named analyzer with the given stop words.
        /// </summary>
        public SnowballAnalyzer(Version matchVersion, string name, ICollection<object> stopWords)
            : this(matchVersion, name)
        {
            stopSet = CharArraySet.UnmodifiableSet(CharArraySet.Copy(this.matchVersion, stopWords));
        }

        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer tokenizer = new StandardTokenizer(matchVersion, reader);
            TokenStream result = new StandardFilter(matchVersion, tokenizer);
            // remove the possessive 's for english stemmers
            if (matchVersion.OnOrAfter(Version.LUCENE_31) && (name.Equals("English"
                ) || name.Equals("Porter") || name.Equals("Lovins")))
            {
                result = new EnglishPossessiveFilter(result);
            }
            // Use a special lowercase filter for turkish, the stemmer expects it.
            if (matchVersion.OnOrAfter(Version.LUCENE_31) && name.Equals("Turkish"
                ))
            {
                result = new TurkishLowerCaseFilter(result);
            }
            else
            {
                result = new LowerCaseFilter(matchVersion, result);
            }
            if (stopSet != null)
            {
                result = new StopFilter(matchVersion, result, stopSet);
            }
            result = new SnowballFilter(result, name);
            return new Analyzer.TokenStreamComponents(tokenizer, result);
        }

        
    }
}