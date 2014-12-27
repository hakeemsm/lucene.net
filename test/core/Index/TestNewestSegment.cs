/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Test.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestNewestSegment : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNewestSegment()
		{
			Directory directory = NewDirectory();
			IndexWriter writer = new IndexWriter(directory, NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())));
			IsNull(writer.NewestSegment());
			writer.Dispose();
			directory.Dispose();
		}
	}
}
