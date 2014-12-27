using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework.Codecs.Compressing;
using Lucene.Net.TestFramework.Index;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Compressing
{
    [TestFixture]
	public class TestCompressingStoredFieldsFormat : BaseStoredFieldsFormatTestCase
	{
		// give it a chance to test various compression modes with different chunk sizes
		protected override Codec Codec
		{
		    get { return CompressingCodec.RandomInstance(Random()); }
		}

        protected override void AddRandomFields(Documents.Document doc)
        {
            throw new System.NotImplementedException();
        }

        [Test]
		public virtual void TestDeletePartiallyWrittenFilesIfAbort()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwConf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwConf.SetMaxBufferedDocs(RandomInts.RandomIntBetween(Random(), 2, 30));
			iwConf.SetCodec(CompressingCodec.RandomInstance(Random()));
			// disable CFS because this test checks file names
			iwConf.SetMergePolicy(NewLogMergePolicy(false));
			iwConf.UseCompoundFile = false;
			// Cannot use RIW because this test wants CFS to stay off:
			var iw = new IndexWriter(dir, iwConf);
			var validDoc = new Lucene.Net.Documents.Document {new IntField("id", 0, Field.Store.YES)};
		    iw.AddDocument(validDoc);
			iw.Commit();
			// make sure that #writeField will fail to trigger an abort
			var invalidDoc = new Lucene.Net.Documents.Document();
			var fieldType = new FieldType {Stored = true};
		    invalidDoc.Add(new AnonymousField("invalid", fieldType));
			// TODO: really bad & scary that this causes IW to
			// abort the segment!!  We should fix this.
			try
			{
				iw.AddDocument(invalidDoc);
				iw.Commit();
			}
			finally
			{
				int counter = dir.ListAll().Count(fileName => fileName.EndsWith(".fdt") || fileName.EndsWith(".fdx"));
			    // Only one .fdt and one .fdx files must have been found
				AreEqual(2, counter);
				iw.Dispose();
				dir.Dispose();
			}
		}

		private sealed class AnonymousField : Field
		{
			public AnonymousField(string baseArg1, FieldType baseArg2) : base(baseArg1, baseArg2)
			{
			}

			public override string StringValue
			{
			    get { return null; }
			}
		}
	}
}
