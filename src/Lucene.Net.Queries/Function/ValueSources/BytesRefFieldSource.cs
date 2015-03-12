﻿using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
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
    /// An implementation for retrieving <seealso cref="FunctionValues"/> instances for str based fields.
    /// </summary>
    public class BytesRefFieldSource : FieldCacheSource
    {

        public BytesRefFieldSource(string field)
            : base(field)
        {
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FieldInfo fieldInfo = readerContext.AtomicReader.FieldInfos.FieldInfo(field);
            // To be sorted or not to be sorted, that is the question
            // TODO: do it cleaner?
            if (fieldInfo != null && fieldInfo.DocValuesType == FieldInfo.DocValuesType_e.BINARY)
            {
                BinaryDocValues binaryValues = Search.FieldCache.DEFAULT.GetTerms(readerContext.AtomicReader, field, true);
                Bits docsWithField = Search.FieldCache.DEFAULT.GetDocsWithField(readerContext.AtomicReader, field);
                return new FunctionValuesAnonymousInnerClassHelper(this, binaryValues, docsWithField);
            }
            else
            {
                return new DocTermsIndexDocValuesAnonymousInnerClassHelper(this, this, readerContext, field);
            }
        }

        private class FunctionValuesAnonymousInnerClassHelper : FunctionValues
        {
            private readonly BytesRefFieldSource outerInstance;

            private readonly BinaryDocValues binaryValues;
            private readonly Bits docsWithField;

            public FunctionValuesAnonymousInnerClassHelper(BytesRefFieldSource outerInstance, BinaryDocValues binaryValues, Bits docsWithField)
            {
                this.outerInstance = outerInstance;
                this.binaryValues = binaryValues;
                this.docsWithField = docsWithField;
            }


            public override bool Exists(int doc)
            {
                return docsWithField.Get(doc);
            }

            public override bool BytesVal(int doc, BytesRef target)
            {
                binaryValues.Get(doc, target);
                return target.Length > 0;
            }

            public override string StrVal(int doc)
            {
                var bytes = new BytesRef();
                return BytesVal(doc, bytes) ? bytes.Utf8ToString() : null;
            }

            public override object ObjectVal(int doc)
            {
                return StrVal(doc);
            }

            public override string ToString(int doc)
            {
                return outerInstance.Description + '=' + StrVal(doc);
            }
        }

        private class DocTermsIndexDocValuesAnonymousInnerClassHelper : DocTermsIndexDocValues
        {
            private readonly BytesRefFieldSource outerInstance;

            public DocTermsIndexDocValuesAnonymousInnerClassHelper(BytesRefFieldSource outerInstance, BytesRefFieldSource @this, AtomicReaderContext readerContext, string field)
                : base(@this, readerContext, field)
            {
                this.outerInstance = outerInstance;
            }


            protected internal override string toTerm(string readableValue)
            {
                return readableValue;
            }

            public override object ObjectVal(int doc)
            {
                return StrVal(doc);
            }

            public override string ToString(int doc)
            {
                return outerInstance.Description + '=' + StrVal(doc);
            }
        }
    }

}