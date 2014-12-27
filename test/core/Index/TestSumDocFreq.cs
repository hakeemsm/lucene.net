/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// Tests
	/// <see cref="Terms.SumDocFreq">Terms.SumDocFreq</see>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class TestSumDocFreq : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestSumDocFreq()
		{
			int numDocs = AtLeast(500);
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			Field id = NewStringField("id", string.Empty, Field.Store.NO);
			Field field1 = NewTextField("foo", string.Empty, Field.Store.NO);
			Field field2 = NewTextField("bar", string.Empty, Field.Store.NO);
			doc.Add(id);
			doc.Add(field1);
			doc.Add(field2);
			for (int i = 0; i < numDocs; i++)
			{
				id.StringValue = string.Empty + i);
				char ch1 = (char)TestUtil.NextInt(Random(), 'a', 'z');
				char ch2 = (char)TestUtil.NextInt(Random(), 'a', 'z');
				field1.StringValue = string.Empty + ch1 + " " + ch2);
				ch1 = (char)TestUtil.NextInt(Random(), 'a', 'z');
				ch2 = (char)TestUtil.NextInt(Random(), 'a', 'z');
				field2.StringValue = string.Empty + ch1 + " " + ch2);
				writer.AddDocument(doc);
			}
			IndexReader ir = writer.GetReader();
			AssertSumDocFreq(ir);
			ir.Dispose();
			int numDeletions = AtLeast(20);
			for (int i_1 = 0; i_1 < numDeletions; i_1++)
			{
				writer.DeleteDocuments(new Term("id", string.Empty + Random().Next(numDocs)));
			}
			writer.ForceMerge(1);
			writer.Dispose();
			ir = DirectoryReader.Open(dir);
			AssertSumDocFreq(ir);
			ir.Dispose();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertSumDocFreq(IndexReader ir)
		{
			// compute sumDocFreq across all fields
			Fields fields = MultiFields.GetFields(ir);
			foreach (string f in fields)
			{
				Terms terms = fields.Terms(f);
				long sumDocFreq = terms.SumDocFreq;
				if (sumDocFreq == -1)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("skipping field: " + f + ", codec does not support sumDocFreq"
							);
					}
					continue;
				}
				long computedSumDocFreq = 0;
				TermsEnum termsEnum = terms.Iterator(null);
				while (termsEnum.Next() != null)
				{
					computedSumDocFreq += termsEnum.DocFreq;
				}
				AreEqual(computedSumDocFreq, sumDocFreq);
			}
		}
	}
}
