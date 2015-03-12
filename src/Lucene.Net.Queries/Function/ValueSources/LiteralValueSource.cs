﻿using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
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
    /// Pass a the field value through as a String, no matter the type // Q: doesn't this mean it's a "str"?
    /// 
    /// 
    /// </summary>
    public class LiteralValueSource : ValueSource
    {
        protected readonly string str;
        protected readonly BytesRef bytesRef;

        public LiteralValueSource(string str)
        {
            this.str = str;
            this.bytesRef = new BytesRef(str);
        }

        /// <summary>
        /// returns the literal value </summary>
        public virtual string Value
        {
            get
            {
                return str;
            }
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {

            return new StrDocValuesAnonymousInnerClassHelper(this, this);
        }

        private class StrDocValuesAnonymousInnerClassHelper : StrDocValues
        {
            private readonly LiteralValueSource outerInstance;

            public StrDocValuesAnonymousInnerClassHelper(LiteralValueSource outerInstance, LiteralValueSource @this)
                : base(@this)
            {
                this.outerInstance = outerInstance;
            }

            public override string StrVal(int doc)
            {
                return outerInstance.str;
            }

            public override bool BytesVal(int doc, BytesRef target)
            {
                target.CopyBytes(outerInstance.bytesRef);
                return true;
            }

            public override string ToString(int doc)
            {
                return outerInstance.str;
            }
        }

        public override string Description
        {
            get { return "literal(" + str + ")"; }
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            var that = o as LiteralValueSource;
            if (that == null)
                return false;
            return str.Equals(that.str);

        }

        public static readonly int hash = typeof(LiteralValueSource).GetHashCode();
        public override int GetHashCode()
        {
            return hash + str.GetHashCode();
        }
    }
}