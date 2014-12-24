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

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.Util;
using NUnit.Framework;
using IndexReader = Lucene.Net.Index.IndexReader;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Analysis
{

    [TestFixture]
    public class TestCachingTokenFilter : BaseTokenStreamTestCase
    {
        private class AnonymousClassTokenStream : TokenStream
        {
            public AnonymousClassTokenStream(TestCachingTokenFilter enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }

            private void InitBlock(TestCachingTokenFilter enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
            }

            private TestCachingTokenFilter enclosingInstance;
            
            private int index = 0;
            private ICharTermAttribute termAtt;
            private IOffsetAttribute offsetAtt;

            public override bool IncrementToken()
            {
                if (index == enclosingInstance.tokens.Length)
                {
                    return false;
                }
                else
                {
                    ClearAttributes();
					this.termAtt.Append(this.enclosingInstance.tokens[this.index++]);
                    offsetAtt.SetOffset(0, 0);
                    return true;
                }
            }

            protected override void Dispose(bool disposing)
            {
                // Do Nothing
            }
        }

        private string[] tokens = { "term1", "term2", "term3", "term2" };

        [Test]
        public virtual void TestCaching()
        {
            Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
            var doc = new Lucene.Net.Documents.Document();
            TokenStream stream = new AnonymousClassTokenStream(this);

            stream = new CachingTokenFilter(stream);

			doc.Add(new TextField("preanalyzed", stream));

            // 1) we consume all tokens twice before we add the doc to the index
            checkTokens(stream);
            stream.Reset();
            checkTokens(stream);

            // 2) now add the document to the index and verify if all tokens are indexed
            //    don't reset the stream here, the DocumentWriter should do that implicitly
            writer.AddDocument(doc);
            writer.Close();

			IndexReader reader = writer.GetReader();
			DocsAndPositionsEnum termPositions = MultiFields.GetTermPositionsEnum(reader, MultiFields
				.GetLiveDocs(reader), "preanalyzed", new BytesRef("term1"));
			IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
            AreEqual(1, termPositions.Freq);
            AreEqual(0, termPositions.NextPosition());
			termPositions = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(reader), "preanalyzed", new BytesRef("term2"));
			IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            AreEqual(2, termPositions.Freq);
            AreEqual(1, termPositions.NextPosition());
            AreEqual(3, termPositions.NextPosition());
			termPositions = MultiFields.GetTermPositionsEnum(reader, MultiFields.GetLiveDocs(
				reader), "preanalyzed", new BytesRef("term3"));
			IsTrue(termPositions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
            AreEqual(1, termPositions.Freq);
            AreEqual(2, termPositions.NextPosition());
            reader.Dispose();
			writer.Close();
            // 3) reset stream and consume tokens again
            stream.Reset();
            checkTokens(stream);
        }

        private void checkTokens(TokenStream stream)
        {
            int count = 0;

			CharTermAttribute termAtt = stream.GetAttribute<CharTermAttribute>();
            Assert.IsNotNull(termAtt);
            while (stream.IncrementToken())
            {
                Assert.IsTrue(count < tokens.Length);
                Assert.AreEqual(tokens[count], termAtt.ToString());
                count++;
            }

            Assert.AreEqual(tokens.Length, count);
        }
    }
}