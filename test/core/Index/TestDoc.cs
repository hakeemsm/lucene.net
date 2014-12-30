using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.TestFramework;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Test.Index
{
	[TestFixture]
	public class TestDoc : LuceneTestCase
	{
		private DirectoryInfo workDir;

		private DirectoryInfo indexDir;

		private List<FileInfo> files;

		/// <summary>Set the test case.</summary>
		/// <remarks>
		/// Set the test case. This test case needs
		/// a few text files created in the current working directory.
		/// </remarks>
		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: setUp");
			}
			workDir = CreateTempDir("TestDoc");
			
			indexDir = CreateTempDir("testIndex");
			
			Directory directory = NewFSDirectory(indexDir);
			directory.Dispose();
			files = new List<FileInfo>();
			files.Add(CreateOutput("test.txt", "This is the first test file"));
			files.Add(CreateOutput("test2.txt", "This is the second test file"));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private FileInfo CreateOutput(string name, string text)
		{
			
			StreamWriter sw = null;
			try
			{
				FileInfo f = new FileInfo(Path.Combine(workDir.FullName,name));
				if (f.Exists)
				{
					f.Delete();
				    f.Create();
				}
			    sw = new StreamWriter(f.FullName);
				sw.WriteLine(text);
				return f;
			}
			finally
			{
				if (sw != null)
				{
					sw.Dispose();
				}
				
			}
		}

		/// <summary>
		/// This test executes a number of merges and compares the contents of
		/// the segments created when using compound file or not using one.
		/// </summary>
		/// <remarks>
		/// This test executes a number of merges and compares the contents of
		/// the segments created when using compound file or not using one.
		/// TODO: the original test used to print the segment contents to System.out
		/// for visual validation. To have the same effect, a new method
		/// checkSegment(String name, ...) should be created that would
		/// //HM:revisit
		/// //assert various things about the segment.
		/// </remarks>
		[Test]
		public virtual void TestIndexAndMerge()
		{
			StringWriter sw = new StringWriter();
			StreamWriter streamWriter = new StreamWriter(sw.ToString(), true);
			Directory directory = NewFSDirectory(indexDir, null);
			if (directory is MockDirectoryWrapper)
			{
				// We create unreferenced files (we don't even write
				// a segments file):
				((MockDirectoryWrapper)directory).SetAssertNoUnrefencedFilesOnClose(false);
			}
			IndexWriter writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode
				.CREATE).SetMaxBufferedDocs(-1)).SetMergePolicy(NewLogMergePolicy(10)));
			SegmentCommitInfo si1 = IndexDoc(writer, "test.txt");
			PrintSegment(streamWriter, si1);
			SegmentCommitInfo si2 = IndexDoc(writer, "test2.txt");
			PrintSegment(streamWriter, si2);
			writer.Dispose();
			SegmentCommitInfo siMerge = Merge(directory, si1, si2, "_merge", false);
			PrintSegment(streamWriter, siMerge);
			SegmentCommitInfo siMerge2 = Merge(directory, si1, si2, "_merge2", false);
			PrintSegment(streamWriter, siMerge2);
			SegmentCommitInfo siMerge3 = Merge(directory, siMerge, siMerge2, "_merge3", false
				);
			PrintSegment(streamWriter, siMerge3);
			directory.Dispose();
			streamWriter.Dispose();
			sw.Dispose();
			string multiFileOutput = sw.ToString();
			//System.out.println(multiFileOutput);
			sw = new StringWriter();
			streamWriter = new StreamWriter(sw.ToString(), true);
			directory = NewFSDirectory(indexDir, null);
			if (directory is MockDirectoryWrapper)
			{
				// We create unreferenced files (we don't even write
				// a segments file):
				((MockDirectoryWrapper)directory).SetAssertNoUnrefencedFilesOnClose(false);
			}
			writer = new IndexWriter(directory, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetOpenMode(IndexWriterConfig.OpenMode.CREATE).SetMaxBufferedDocs
				(-1)).SetMergePolicy(NewLogMergePolicy(10)));
			si1 = IndexDoc(writer, "test.txt");
			PrintSegment(streamWriter, si1);
			si2 = IndexDoc(writer, "test2.txt");
			PrintSegment(streamWriter, si2);
			writer.Dispose();
			siMerge = Merge(directory, si1, si2, "_merge", true);
			PrintSegment(streamWriter, siMerge);
			siMerge2 = Merge(directory, si1, si2, "_merge2", true);
			PrintSegment(streamWriter, siMerge2);
			siMerge3 = Merge(directory, siMerge, siMerge2, "_merge3", true);
			PrintSegment(streamWriter, siMerge3);
			directory.Dispose();
			streamWriter.Dispose();
			sw.Dispose();
			string singleFileOutput = sw.ToString();
			AreEqual(multiFileOutput, singleFileOutput);
		}

		/// <exception cref="System.Exception"></exception>
		private SegmentCommitInfo IndexDoc(IndexWriter writer, string fileName)
		{
			FileInfo file = new FileInfo(Path.Combine(workDir.FullName, fileName));
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			StreamReader sr = new StreamReader(new FileStream(file.FullName,FileMode.Open));
			doc.Add(new TextField("contents", sr));
			writer.AddDocument(doc);
			writer.Commit();
			sr.Dispose();
			return writer.NewestSegment;
		}

		/// <exception cref="System.Exception"></exception>
		private SegmentCommitInfo Merge(Directory dir, SegmentCommitInfo si1, SegmentCommitInfo
			 si2, string merged, bool useCompoundFile)
		{
			IOContext context = NewIOContext(Random());
			SegmentReader r1 = new SegmentReader(si1, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR
				, context);
			SegmentReader r2 = new SegmentReader(si2, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR
				, context);
			Codec codec = Codec.Default;
			TrackingDirectoryWrapper trackingDir = new TrackingDirectoryWrapper(si1.info.dir);
			SegmentInfo si = new SegmentInfo(si1.info.dir, Constants.LUCENE_MAIN_VERSION, merged
				, -1, false, codec, null);
			SegmentMerger merger = new SegmentMerger(Arrays.AsList<AtomicReader>(r1, r2), si, 
				InfoStream.Default, trackingDir, IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL
				, MergeState.CheckAbort.NONE, new FieldInfos.FieldNumbers(), context, true);
			MergeState mergeState = merger.Merge();
			r1.Dispose();
			r2.Dispose();
			SegmentInfo info = new SegmentInfo(si1.info.dir, Constants.LUCENE_MAIN_VERSION, merged
				, si1.info.DocCount + si2.info.DocCount, false, codec, null);
			info.SetFiles(new HashSet<string>(trackingDir.CreatedFiles));
			if (useCompoundFile)
			{
				ICollection<string> filesToDelete = IndexWriter.CreateCompoundFile(InfoStream.Default, dir, MergeState.CheckAbort.NONE, info, NewIOContext(Random()));
				info.UseCompoundFile = (true);
				foreach (string fileToDelete in filesToDelete)
				{
					si1.info.dir.DeleteFile(fileToDelete);
				}
			}
			return new SegmentCommitInfo(info, 0, -1L, -1L);
		}

		/// <exception cref="System.Exception"></exception>
		private void PrintSegment(StreamWriter sw, SegmentCommitInfo si)
		{
			SegmentReader reader = new SegmentReader(si, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR
				, NewIOContext(Random()));
			for (int i = 0; i < reader.NumDocs; i++)
			{
				sw.WriteLine(reader.Document(i));
			}
			Fields fields = reader.Fields;
			foreach (string field in fields)
			{
				Terms terms = fields.Terms(field);
				IsNotNull(terms);
				TermsEnum tis = terms.IEnumerator(null);
				while (tis.Next() != null)
				{
					sw.Write("  term=" + field + ":" + tis.Term);
					sw.WriteLine("    DF=" + tis.DocFreq);
					DocsAndPositionsEnum positions = tis.DocsAndPositions(reader.LiveDocs, null);
					while (positions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
					{
						sw.Write(" doc=" + positions.DocID);
						sw.Write(" TF=" + positions.Freq);
						sw.Write(" pos=");
						sw.Write(positions.NextPosition());
						for (int j = 1; j < positions.Freq; j++)
						{
							sw.Write("," + positions.NextPosition());
						}
						sw.WriteLine(string.Empty);
					}
				}
			}
			reader.Dispose();
		}
	}
}
