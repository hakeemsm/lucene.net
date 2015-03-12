﻿/*
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
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// Function that returns <seealso cref="TFIDFSimilarity #idf(long, long)"/>
    /// for every document.
    /// <para>
    /// Note that the configured Similarity for the field must be
    /// a subclass of <seealso cref="TFIDFSimilarity"/>
    /// @lucene.internal 
    /// </para>
    /// </summary>
    public class IDFValueSource : DocFreqValueSource
    {
        public IDFValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
            : base(field, val, indexedField, indexedBytes)
        {
        }

        public override string Name
        {
            get { return "idf"; }
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var searcher = (IndexSearcher)context["searcher"];
            TFIDFSimilarity sim = AsTFIDF(searcher.Similarity, field);
            if (sim == null)
            {
                throw new System.NotSupportedException("requires a TFIDFSimilarity (such as DefaultSimilarity)");
            }
            int docfreq = searcher.IndexReader.DocFreq(new Term(indexedField, indexedBytes));
            float idf = sim.Idf(docfreq, searcher.IndexReader.MaxDoc);
            return new ConstDoubleDocValues(idf, this);
        }

        // tries extra hard to cast the sim to TFIDFSimilarity
        internal static TFIDFSimilarity AsTFIDF(Similarity sim, string field)
        {
            while (sim is PerFieldSimilarityWrapper)
            {
                sim = ((PerFieldSimilarityWrapper)sim).Get(field);
            }
            if (sim is TFIDFSimilarity)
            {
                return (TFIDFSimilarity)sim;
            }
            else
            {
                return null;
            }
        }
    }
}