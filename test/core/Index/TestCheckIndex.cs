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
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Analysis;
using NUnit.Framework;


namespace Lucene.Net.Test.Index
{
	
	[TestFixture]
	public class TestCheckIndex:LuceneTestCase
	{
		
		[Test]
		public virtual void  TestDeletedDocs()
		{
			Directory dir = NewDirectory();
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2)));
			for (int i = 0; i < 19; i++)
			{
				var doc = new Documents.Document();
				FieldType customType = new FieldType(TextField.TYPE_STORED)
				{
				    StoreTermVectors = (true),
				    StoreTermVectorPositions = (true),
				    StoreTermVectorOffsets = (true)
				};
			    doc.Add(NewField("field", "aaa" + i, customType));
				writer.AddDocument(doc);
			}
			writer.ForceMerge(1);
			writer.Commit();
			writer.DeleteDocuments(new Term("field", "aaa5"));
			writer.Dispose();
			
			System.IO.MemoryStream bos = new System.IO.MemoryStream(1024);
			CheckIndex checker = new CheckIndex(dir);
			checker.SetInfoStream(new System.IO.StreamWriter(bos));
			//checker.setInfoStream(System.out);
			CheckIndex.Status indexStatus = checker.CheckIndex_Renamed_Method();
			if (indexStatus.clean == false)
			{
				System.Console.Out.WriteLine("CheckIndex failed");
				char[] tmpChar;
				byte[] tmpByte;
				tmpByte = bos.GetBuffer();
				tmpChar = new char[bos.Length];
				System.Array.Copy(tmpByte, 0, tmpChar, 0, tmpChar.Length);
				System.Console.Out.WriteLine(new System.String(tmpChar));
				Assert.Fail();
			}
			
			CheckIndex.Status.SegmentInfoStatus seg = indexStatus.segmentInfos[0];
			IsTrue(seg.openReaderPassed);
			
			IsNotNull(seg.diagnostics);
			
			IsNotNull(seg.fieldNormStatus);
			IsNull(seg.fieldNormStatus.error);
			AreEqual(1, seg.fieldNormStatus.totFields);
			
			IsNotNull(seg.termIndexStatus);
			IsNull(seg.termIndexStatus.error);
			AreEqual(1, seg.termIndexStatus.termCount);
			AreEqual(19, seg.termIndexStatus.totFreq);
			AreEqual(18, seg.termIndexStatus.totPos);
			
			IsNotNull(seg.storedFieldStatus);
			IsNull(seg.storedFieldStatus.error);
			AreEqual(18, seg.storedFieldStatus.docCount);
			AreEqual(18, seg.storedFieldStatus.totFields);
			
			IsNotNull(seg.termVectorStatus);
			IsNull(seg.termVectorStatus.error);
			AreEqual(18, seg.termVectorStatus.docCount);
			AreEqual(18, seg.termVectorStatus.totVectors);
			
			Assert.IsTrue(seg.diagnostics.Count > 0);
			List<string> onlySegments = new List<string>();
			onlySegments.Add("_0");
			
			Assert.IsTrue(checker.CheckIndex_Renamed_Method(onlySegments).clean == true);
		}
		
		[Test]
		public virtual void TestBogusTermVectors()
		{
			Directory dir = NewDirectory();
			IndexWriter iw = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, 
				null));
			Documents.Document doc = new Documents.Document();
			FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
			ft.StoreTermVectors = (true);
			ft.StoreTermVectorOffsets = (true);
			Field field = new Field("foo", string.Empty, ft);
			field.SetTokenStream(new CannedTokenStream(new Token("bar", 5, 10), new Token("bar"
				, 1, 4)));
			doc.Add(field);
			iw.AddDocument(doc);
			iw.Dispose();
			dir.Dispose();
		}
	}
}