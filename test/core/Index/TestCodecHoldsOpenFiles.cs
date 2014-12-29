using System.IO;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestCodecHoldsOpenFiles : LuceneTestCase
	{
		[Test]
		public virtual void TestCodecOpenFiles()
		{
			var d = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d);
			int numDocs = AtLeast(100);
			for (int i = 0; i < numDocs; i++)
			{
				var doc = new Lucene.Net.Documents.Document
				{
				    NewField("foo", "bar", TextField.TYPE_NOT_STORED)
				};
			    w.AddDocument(doc);
			}
			IndexReader r = w.Reader;
			w.Close();
			foreach (string fileName in d.ListAll())
			{
				try
				{
					d.DeleteFile(fileName);
				}
				catch (IOException)
				{
				}
			}
			// ignore: this means codec (correctly) is holding
			// the file open
			foreach (AtomicReaderContext cxt in r.Leaves)
			{
				TestUtil.CheckReader(cxt.Reader);
			}
			r.Dispose();
			d.Dispose();
		}
	}
}
