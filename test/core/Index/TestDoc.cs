/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	/// <summary>JUnit adaptation of an older test case DocTest.</summary>
	/// <remarks>JUnit adaptation of an older test case DocTest.</remarks>
	public class TestDoc : LuceneTestCase
	{
		private FilePath workDir;

		private FilePath indexDir;

		private List<FilePath> files;

		/// <summary>Set the test case.</summary>
		/// <remarks>
		/// Set the test case. This test case needs
		/// a few text files created in the current working directory.
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			if (VERBOSE)
			{
				System.Console.Out.WriteLine("TEST: setUp");
			}
			workDir = CreateTempDir("TestDoc");
			workDir.Mkdirs();
			indexDir = CreateTempDir("testIndex");
			indexDir.Mkdirs();
			Directory directory = NewFSDirectory(indexDir);
			directory.Dispose();
			files = new List<FilePath>();
			files.AddItem(CreateOutput("test.txt", "This is the first test file"));
			files.AddItem(CreateOutput("test2.txt", "This is the second test file"));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private FilePath CreateOutput(string name, string text)
		{
			TextWriter fw = null;
			PrintWriter pw = null;
			try
			{
				FilePath f = new FilePath(workDir, name);
				if (f.Exists())
				{
					f.Delete();
				}
				fw = new OutputStreamWriter(new FileOutputStream(f), StandardCharsets.UTF_8);
				pw = new PrintWriter(fw);
				pw.WriteLine(text);
				return f;
			}
			finally
			{
				if (pw != null)
				{
					pw.Dispose();
				}
				if (fw != null)
				{
					fw.Dispose();
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
		/// <exception cref="System.Exception"></exception>
		public virtual void TestIndexAndMerge()
		{
			StringWriter sw = new StringWriter();
			PrintWriter @out = new PrintWriter(sw, true);
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
			PrintSegment(@out, si1);
			SegmentCommitInfo si2 = IndexDoc(writer, "test2.txt");
			PrintSegment(@out, si2);
			writer.Dispose();
			SegmentCommitInfo siMerge = Merge(directory, si1, si2, "_merge", false);
			PrintSegment(@out, siMerge);
			SegmentCommitInfo siMerge2 = Merge(directory, si1, si2, "_merge2", false);
			PrintSegment(@out, siMerge2);
			SegmentCommitInfo siMerge3 = Merge(directory, siMerge, siMerge2, "_merge3", false
				);
			PrintSegment(@out, siMerge3);
			directory.Dispose();
			@out.Dispose();
			sw.Dispose();
			string multiFileOutput = sw.ToString();
			//System.out.println(multiFileOutput);
			sw = new StringWriter();
			@out = new PrintWriter(sw, true);
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
			PrintSegment(@out, si1);
			si2 = IndexDoc(writer, "test2.txt");
			PrintSegment(@out, si2);
			writer.Dispose();
			siMerge = Merge(directory, si1, si2, "_merge", true);
			PrintSegment(@out, siMerge);
			siMerge2 = Merge(directory, si1, si2, "_merge2", true);
			PrintSegment(@out, siMerge2);
			siMerge3 = Merge(directory, siMerge, siMerge2, "_merge3", true);
			PrintSegment(@out, siMerge3);
			directory.Dispose();
			@out.Dispose();
			sw.Dispose();
			string singleFileOutput = sw.ToString();
			AreEqual(multiFileOutput, singleFileOutput);
		}

		/// <exception cref="System.Exception"></exception>
		private SegmentCommitInfo IndexDoc(IndexWriter writer, string fileName)
		{
			FilePath file = new FilePath(workDir, fileName);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			InputStreamReader @is = new InputStreamReader(new FileInputStream(file), StandardCharsets
				.UTF_8);
			doc.Add(new TextField("contents", @is));
			writer.AddDocument(doc);
			writer.Commit();
			@is.Dispose();
			return writer.NewestSegment();
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
			Codec codec = Codec.GetDefault();
			TrackingDirectoryWrapper trackingDir = new TrackingDirectoryWrapper(si1.info.dir);
			SegmentInfo si = new SegmentInfo(si1.info.dir, Constants.LUCENE_MAIN_VERSION, merged
				, -1, false, codec, null);
			SegmentMerger merger = new SegmentMerger(Arrays.AsList<AtomicReader>(r1, r2), si, 
				InfoStream.GetDefault(), trackingDir, IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL
				, MergeState.CheckAbort.NONE, new FieldInfos.FieldNumbers(), context, true);
			MergeState mergeState = merger.Merge();
			r1.Dispose();
			r2.Dispose();
			SegmentInfo info = new SegmentInfo(si1.info.dir, Constants.LUCENE_MAIN_VERSION, merged
				, si1.info.DocCount + si2.info.DocCount, false, codec, null);
			info.SetFiles(new HashSet<string>(trackingDir.GetCreatedFiles()));
			if (useCompoundFile)
			{
				ICollection<string> filesToDelete = IndexWriter.CreateCompoundFile(InfoStream.GetDefault
					(), dir, MergeState.CheckAbort.NONE, info, NewIOContext(Random()));
				info.SetUseCompoundFile(true);
				foreach (string fileToDelete in filesToDelete)
				{
					si1.info.dir.DeleteFile(fileToDelete);
				}
			}
			return new SegmentCommitInfo(info, 0, -1L, -1L);
		}

		/// <exception cref="System.Exception"></exception>
		private void PrintSegment(PrintWriter @out, SegmentCommitInfo si)
		{
			SegmentReader reader = new SegmentReader(si, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR
				, NewIOContext(Random()));
			for (int i = 0; i < reader.NumDocs; i++)
			{
				@out.WriteLine(reader.Document(i));
			}
			Fields fields = reader.Fields();
			foreach (string field in fields)
			{
				Terms terms = fields.Terms(field);
				IsNotNull(terms);
				TermsEnum tis = terms.Iterator(null);
				while (tis.Next() != null)
				{
					@out.Write("  term=" + field + ":" + tis.Term());
					@out.WriteLine("    DF=" + tis.DocFreq);
					DocsAndPositionsEnum positions = tis.DocsAndPositions(reader.LiveDocs, null);
					while (positions.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
					{
						@out.Write(" doc=" + positions.DocID);
						@out.Write(" TF=" + positions.Freq);
						@out.Write(" pos=");
						@out.Write(positions.NextPosition());
						for (int j = 1; j < positions.Freq; j++)
						{
							@out.Write("," + positions.NextPosition());
						}
						@out.WriteLine(string.Empty);
					}
				}
			}
			reader.Dispose();
		}
	}
}
