/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestCodecHoldsOpenFiles : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			Directory d = NewDirectory();
			RandomIndexWriter w = new RandomIndexWriter(Random(), d);
			int numDocs = AtLeast(100);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				doc.Add(NewField("foo", "bar", TextField.TYPE_NOT_STORED));
				w.AddDocument(doc);
			}
			IndexReader r = w.GetReader();
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
			foreach (AtomicReaderContext cxt in r.Leaves())
			{
				TestUtil.CheckReader(((AtomicReader)cxt.Reader()));
			}
			r.Close();
			d.Close();
		}
	}
}
