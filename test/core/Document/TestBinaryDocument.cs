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

using NUnit.Framework;

using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Documents
{
	
	/// <summary>Tests {@link Document} class.</summary>
    [TestFixture]
	public class TestBinaryDocument:LuceneTestCase
	{
		
		internal System.String binaryValStored = "this text will be stored as a byte array in the index";
		internal System.String binaryValCompressed = "this text will be also stored and compressed as a byte array in the index";
		
        [Test]
		public virtual void  TestBinaryFieldInIndex()
		{
			FieldType ft = new FieldType();
			ft.SetStored(true);
			IIndexableField binaryFldStored = new Field("binaryStored", System.Text.UTF8Encoding.UTF8.GetBytes(binaryValStored), Field.Store.YES);
			IIndexableField stringFldStored = new Field("stringStored", binaryValStored, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
			
			// binary fields with store off are not allowed
            Assert.Throws<ArgumentException>(
                () => new Field("fail", System.Text.Encoding.UTF8.GetBytes(binaryValStored), Field.Store.NO));
			
			Document doc = new Document();
			
			doc.Add(binaryFldStored);
			
			doc.Add(stringFldStored);
			
			/* test for field count */
			NUnit.Framework.Assert.AreEqual(2, doc.GetFields().Count);
			
			/* add the doc to a ram index */
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			writer.AddDocument(doc);
			
			/* open a reader and fetch the document */
			IndexReader reader = writer.GetReader();
			Document docFromReader = reader.Document(0);
			Assert.IsTrue(docFromReader != null);
			BytesRef bytes = docFromReader.GetBinaryValue("binaryStored");
			/* fetch the binary stored field and compare it's content with the original one */
			string binaryFldStoredTest = new string(bytes.bytes, bytes.offset, bytes.length, 
				StandardCharsets.UTF_8);
			Assert.IsTrue(binaryFldStoredTest.Equals(binaryValStored));
			
			/* fetch the string field and compare it's content with the original one */
			System.String stringFldStoredTest = docFromReader.Get("stringStored");
			Assert.IsTrue(stringFldStoredTest.Equals(binaryValStored));
			
			writer.Close();
			
			reader.Close();
			dir.Close();
		}
		
        [Test]
		public virtual void  TestCompressionTools()
		{
			IIndexableField binaryFldCompressed = new StoredField("binaryCompressed", CompressionTools
				.Compress(Sharpen.Runtime.GetBytesForString(binaryValCompressed, StandardCharsets
				.UTF_8)));
			IIndexableField stringFldCompressed = new StoredField("stringCompressed", CompressionTools
				.CompressString(binaryValCompressed));
			Document doc = new Document();
			
			doc.Add(binaryFldCompressed);
			doc.Add(stringFldCompressed);
			
			/* add the doc to a ram index */
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			writer.AddDocument(doc);
			IndexReader reader = writer.GetReader();
			Org.Apache.Lucene.Document.Document docFromReader = reader.Document(0);
			NUnit.Framework.Assert.IsTrue(docFromReader != null);
			string binaryFldCompressedTest = new string(CompressionTools.Decompress(docFromReader
				.GetBinaryValue("binaryCompressed")), StandardCharsets.UTF_8);
			NUnit.Framework.Assert.IsTrue(binaryFldCompressedTest.Equals(binaryValCompressed)
				);
			NUnit.Framework.Assert.IsTrue(CompressionTools.DecompressString(docFromReader.GetBinaryValue
				("stringCompressed")).Equals(binaryValCompressed));
			writer.Close();
			reader.Close();
			dir.Close();
		}
	}
}