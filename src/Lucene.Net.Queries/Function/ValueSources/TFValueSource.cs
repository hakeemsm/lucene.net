﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Util;

namespace Lucene.Net.Queries.Function.ValueSources
{

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
    /// <summary>
    /// Function that returns <seealso cref="TFIDFSimilarity#tf(float)"/>
    /// for every document.
    /// <para>
    /// Note that the configured Similarity for the field must be
    /// a subclass of <seealso cref="TFIDFSimilarity"/>
    /// @lucene.internal 
    /// </para>
    /// </summary>
    public class TFValueSource : TermFreqValueSource
    {
        public TFValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
            : base(field, val, indexedField, indexedBytes)
        {
        }

        public override string Name
        {
            get
            {
                {
                    return "tf";
                }
            }
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            var fields = readerContext.AtomicReader.Fields;
            var terms = fields.Terms(indexedField);
            var searcher = (IndexSearcher)context["searcher"];
            var similarity = IDFValueSource.AsTFIDF(searcher.Similarity, indexedField);
            if (similarity == null)
            {
                throw new System.NotSupportedException("requires a TFIDFSimilarity (such as DefaultSimilarity)");
            }

            return new FloatDocValuesAnonymousInnerClassHelper(this, this, terms, similarity);
        }

        private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
        {
            private readonly TFValueSource outerInstance;

            private readonly Terms terms;
            private readonly TFIDFSimilarity similarity;

            public FloatDocValuesAnonymousInnerClassHelper(TFValueSource outerInstance, TFValueSource @this, Terms terms, TFIDFSimilarity similarity)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.terms = terms;
                this.similarity = similarity;
                lastDocRequested = -1;
            }

            private DocsEnum docs;
            private int atDoc;
            private int lastDocRequested;

            //JAVA TO C# CONVERTER TODO TASK: Initialization blocks declared within anonymous inner classes are not converted:
            //	  {
            //		  reset();
            //	  }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: public void reset() throws java.io.IOException
            public virtual void Reset()
            {
                // no one should call us for deleted docs?

                if (terms != null)
                {
                    TermsEnum termsEnum = terms.Iterator(null);
                    if (termsEnum.SeekExact(outerInstance.indexedBytes))
                    {
                        docs = termsEnum.Docs(null, null);
                    }
                    else
                    {
                        docs = null;
                    }
                }
                else
                {
                    docs = null;
                }

                if (docs == null)
                {
                    docs = new DocsEnumAnonymousInnerClassHelper(this);
                }
                atDoc = -1;
            }

            private class DocsEnumAnonymousInnerClassHelper : DocsEnum
            {
                private readonly FloatDocValuesAnonymousInnerClassHelper outerInstance;

                public DocsEnumAnonymousInnerClassHelper(FloatDocValuesAnonymousInnerClassHelper outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                public override int Freq()
                {
                    return 0;
                }

                public override int DocID()
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                public override int NextDoc()
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                public override int Advance(int target)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                public override long Cost()
                {
                    return 0;
                }
            }

            public override float FloatVal(int doc)
            {
                try
                {
                    if (doc < lastDocRequested)
                    {
                        // out-of-order access.... reset
                        Reset();
                    }
                    lastDocRequested = doc;

                    if (atDoc < doc)
                    {
                        atDoc = docs.Advance(doc);
                    }

                    if (atDoc > doc)
                    {
                        // term doesn't match this document... either because we hit the
                        // end, or because the next doc is after this doc.
                        return similarity.Tf(0);
                    }

                    // a match!
                    return similarity.Tf(docs.Freq());
                }
                catch (IOException e)
                {
                    throw new Exception("caught exception in function " + outerInstance.Description + " : doc=" + doc, e);
                }
            }
        }
    }
}