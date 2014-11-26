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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.Version;


namespace Lucene.Net.Search.Vectorhighlight
{

    /// <summary>
    /// <c>FieldTermStack</c> is a stack that keeps query terms in the specified field
    /// of the document to be highlighted.
    /// </summary>
    public class FieldTermStack
    {
        private String fieldName;
        public LinkedList<TermInfo> termList = new LinkedList<TermInfo>();



        /// <summary>
        /// a constructor. 
        /// </summary>
        /// <param name="reader">IndexReader of the index</param>
        /// <param name="docId">document id to be highlighted</param>
        /// <param name="fieldName">field of the document to be highlighted</param>
        /// <param name="fieldQuery">FieldQuery object</param>
#if LUCENENET_350 //Lucene.Net specific code. See https://issues.apache.org/jira/browse/LUCENENET-350
        public FieldTermStack(IndexReader reader, int docId, String fieldName, FieldQuery fieldQuery)
        {
            this.fieldName = fieldName;

            List<string> termSet = fieldQuery.getTermSet(fieldName);

            // just return to make null snippet if un-matched fieldName specified when fieldMatch == true
            if (termSet == null) return;

            //TermFreqVector tfv = reader.GetTermFreqVector(docId, fieldName);
            
            Fields vectors = reader.GetTermVectors(docId);
            Terms vector = vectors.Terms(fieldName);
            if (vector == null)
            {
                return;
            }

            CharsRef spare = new CharsRef();
            TermsEnum termsEnum = vector.Iterator(null);
            DocsAndPositionsEnum dpEnum = null;
            BytesRef text;
            int numDocs = reader.MaxDoc;
            while ((text = termsEnum.Next()) != null)
            {
                UnicodeUtil.UTF8toUTF16(text, spare);
                string term = spare.ToString();
                if (!termSet.Contains(term))
                {
                    continue;
                }
                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                if (dpEnum == null)
                {
                    // null snippet
                    return;
                }
                dpEnum.NextDoc();
                // For weight look here: http://lucene.apache.org/core/3_6_0/api/core/org/apache/lucene/search/DefaultSimilarity.html
                float weight = (float)(Math.Log(numDocs / (double)(reader.DocFreq(new Term(fieldName
                    , text)) + 1)) + 1.0);
                int freq = dpEnum.Freq;
                for (int i = 0; i < freq; i++)
                {
                    int pos = dpEnum.NextPosition();
                    if (dpEnum.StartOffset < 0)
                    {
                        return;
                    }
                    // no offsets, null snippet
                    termList.AddLast(new TermInfo(term, dpEnum.StartOffset, dpEnum.EndOffset
                        , pos, weight));
                }
            }
            // sort by position
            Sort(termList);
            // now look for dups at the same position, linking them together
            int currentPos = -1;
            TermInfo previous = null;
            TermInfo first = null;
            foreach (var termInfo in termList)
            {
                if (termInfo.Position == currentPos)
                {
                    previous.Next = termInfo;
                    previous = termInfo;
                    termList.Remove(termInfo);
                }
                else
                {
                    if (previous != null)
                    {
                        previous.Next = first;
                    }
                    previous = first = termInfo;
                    currentPos = termInfo.Position;
                }
            }
            
            if (previous != null)
            {
                previous.Next = first;
            }
        }
#else   //Original Port
        public FieldTermStack(IndexReader reader, int docId, String fieldName, FieldQuery fieldQuery)
        {
            this.fieldName = fieldName;

            TermFreqVector tfv = reader.GetTermFreqVector(docId, fieldName);
            if (tfv == null) return; // just return to make null snippets
            TermPositionVector tpv = null;
            try
            {
                tpv = (TermPositionVector)tfv;
            }
            catch (InvalidCastException e)
            {
                return; // just return to make null snippets
            }

            List<String> termSet = fieldQuery.getTermSet(fieldName);
            // just return to make null snippet if un-matched fieldName specified when fieldMatch == true
            if (termSet == null) return;

            foreach (String term in tpv.GetTerms())
            {
                if (!termSet.Contains(term)) continue;
                int index = tpv.IndexOf(term);
                TermVectorOffsetInfo[] tvois = tpv.GetOffsets(index);
                if (tvois == null) return; // just return to make null snippets
                int[] poss = tpv.GetTermPositions(index);
                if (poss == null) return; // just return to make null snippets
                for (int i = 0; i < tvois.Length; i++)
                    termList.AddLast(new TermInfo(term, tvois[i].GetStartOffset(), tvois[i].GetEndOffset(), poss[i]));
            }

            // sort by position
            //Collections.sort(termList);
            Sort(termList);
        }
#endif

        void Sort(LinkedList<TermInfo> linkList)
        {
            TermInfo[] arr = new TermInfo[linkList.Count];
            linkList.CopyTo(arr, 0);
            Array.Sort(arr, new Comparison<TermInfo>(PosComparer));

            linkList.Clear();
            foreach (TermInfo t in arr) linkList.AddLast(t);
        }

        int PosComparer(TermInfo t1, TermInfo t2)
        {
            return t1.Position - t2.Position;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> field name </value>
        public string FieldName
        {
            get { return fieldName; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>the top TermInfo object of the stack</returns>
        public TermInfo Pop()
        {
            if (termList.Count == 0) return null;

            LinkedListNode<TermInfo> top = termList.First;
            termList.RemoveFirst();
            return top.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="termInfo">the TermInfo object to be put on the top of the stack</param>
        public void Push(TermInfo termInfo)
        {
            // termList.push( termInfo );  // avoid Java 1.6 feature
            termList.AddFirst(termInfo);
        }

        /// <summary>
        /// to know whether the stack is empty 
        /// </summary>
        /// <returns>true if the stack is empty, false if not</returns>
        public bool IsEmpty()
        {
            return termList == null || termList.Count == 0;
        }

        public class TermInfo : IComparable<TermInfo>
        {

            String text;
            int startOffset;
            int endOffset;
            int position;
            private readonly float weight;
            private TermInfo next;

            public TermInfo(String text, int startOffset, int endOffset, int position, float weight)
            {
                this.text = text;
                this.startOffset = startOffset;
                this.endOffset = endOffset;
                this.position = position;
                this.weight = weight;
                this.next = this;
            }

            public string Text
            {
                get { return text; }
            }

            public int StartOffset
            {
                get { return startOffset; }
            }

            public int EndOffset
            {
                get { return endOffset; }
            }

            public int Position
            {
                get { return position; }
            }

            public float Weight
            {
                get { return weight; }
            }

            public TermInfo Next
            {
                get { return next; }
                set { next = value; }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(text).Append('(').Append(startOffset).Append(',').Append(endOffset).Append(',').Append(position).Append(')');
                return sb.ToString();
            }

            public int CompareTo(TermInfo o)
            {
                return (this.position - o.position);
            }
        }
    }
}
