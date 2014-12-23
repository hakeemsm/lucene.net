/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Com.Carrotsearch.Randomizedtesting.Generators;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Compressing;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Codecs.Compressing
{
	public class TestCompressingStoredFieldsFormat : BaseStoredFieldsFormatTestCase
	{
		// give it a chance to test various compression modes with different chunk sizes
		protected override Codec GetCodec()
		{
			return CompressingCodec.RandomInstance(Random());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDeletePartiallyWrittenFilesIfAbort()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwConf.SetMaxBufferedDocs(RandomInts.RandomIntBetween(Random(), 2, 30));
			iwConf.SetCodec(CompressingCodec.RandomInstance(Random()));
			// disable CFS because this test checks file names
			iwConf.SetMergePolicy(NewLogMergePolicy(false));
			iwConf.SetUseCompoundFile(false);
			// Cannot use RIW because this test wants CFS to stay off:
			IndexWriter iw = new IndexWriter(dir, iwConf);
			Lucene.Net.Document.Document validDoc = new Lucene.Net.Document.Document
				();
			validDoc.Add(new IntField("id", 0, Field.Store.YES));
			iw.AddDocument(validDoc);
			iw.Commit();
			// make sure that #writeField will fail to trigger an abort
			Lucene.Net.Document.Document invalidDoc = new Lucene.Net.Document.Document
				();
			FieldType fieldType = new FieldType();
			fieldType.SetStored(true);
			invalidDoc.Add(new _Field_69("invalid", fieldType));
			// TODO: really bad & scary that this causes IW to
			// abort the segment!!  We should fix this.
			try
			{
				iw.AddDocument(invalidDoc);
				iw.Commit();
			}
			finally
			{
				int counter = 0;
				foreach (string fileName in dir.ListAll())
				{
					if (fileName.EndsWith(".fdt") || fileName.EndsWith(".fdx"))
					{
						counter++;
					}
				}
				// Only one .fdt and one .fdx files must have been found
				NUnit.Framework.Assert.AreEqual(2, counter);
				iw.Close();
				dir.Close();
			}
		}

		private sealed class _Field_69 : Field
		{
			public _Field_69(string baseArg1, FieldType baseArg2) : base(baseArg1, baseArg2)
			{
			}

			public override string StringValue()
			{
				return null;
			}
		}
	}
}
