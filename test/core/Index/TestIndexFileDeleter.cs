/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestIndexFileDeleter : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDeleteLeftoverFiles()
		{
			Directory dir = NewDirectory();
			if (dir is MockDirectoryWrapper)
			{
				((MockDirectoryWrapper)dir).SetPreventDoubleWrite(false);
			}
			MergePolicy mergePolicy = NewLogMergePolicy(true, 10);
			// This test expects all of its segments to be in CFS
			mergePolicy.SetNoCFSRatio(1.0);
			mergePolicy.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
			IndexWriter writer = new IndexWriter(dir, ((IndexWriterConfig)((IndexWriterConfig
				)NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs
				(10)).SetMergePolicy(mergePolicy).SetUseCompoundFile(true)));
			int i;
			for (i = 0; i < 35; i++)
			{
				AddDoc(writer, i);
			}
			writer.GetConfig().GetMergePolicy().SetNoCFSRatio(0.0);
			writer.GetConfig().SetUseCompoundFile(false);
			for (; i < 45; i++)
			{
				AddDoc(writer, i);
			}
			writer.Close();
			// Delete one doc so we get a .del file:
			writer = new IndexWriter(dir, ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random())).SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES).SetUseCompoundFile
				(true)));
			Term searchTerm = new Term("id", "7");
			writer.DeleteDocuments(searchTerm);
			writer.Close();
			// Now, artificially create an extra .del file & extra
			// .s0 file:
			string[] files = dir.ListAll();
			// TODO: fix this test better
			string ext = Codec.GetDefault().GetName().Equals("SimpleText") ? ".liv" : ".del";
			// Create a bogus separate del file for a
			// segment that already has a separate del file: 
			CopyFile(dir, "_0_1" + ext, "_0_2" + ext);
			// Create a bogus separate del file for a
			// segment that does not yet have a separate del file:
			CopyFile(dir, "_0_1" + ext, "_1_1" + ext);
			// Create a bogus separate del file for a
			// non-existent segment:
			CopyFile(dir, "_0_1" + ext, "_188_1" + ext);
			// Create a bogus segment file:
			CopyFile(dir, "_0.cfs", "_188.cfs");
			// Create a bogus fnm file when the CFS already exists:
			CopyFile(dir, "_0.cfs", "_0.fnm");
			// Create some old segments file:
			CopyFile(dir, "segments_2", "segments");
			CopyFile(dir, "segments_2", "segments_1");
			// Create a bogus cfs file shadowing a non-cfs segment:
			// TODO: 
			//HM:revisit 
			//assert is bogus (relies upon codec-specific filenames)
			IsTrue(SlowFileExists(dir, "_3.fdt") || SlowFileExists(dir
				, "_3.fld"));
			IsTrue(!SlowFileExists(dir, "_3.cfs"));
			CopyFile(dir, "_1.cfs", "_3.cfs");
			string[] filesPre = dir.ListAll();
			// Open & close a writer: it should delete the above 4
			// files and nothing more:
			writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random())).SetOpenMode(IndexWriterConfig.OpenMode.APPEND));
			writer.Close();
			string[] files2 = dir.ListAll();
			dir.Close();
			Arrays.Sort(files);
			Arrays.Sort(files2);
			ICollection<string> dif = DifFiles(files, files2);
			if (!Arrays.Equals(files, files2))
			{
				Fail("IndexFileDeleter failed to delete unreferenced extra files: should have deleted "
					 + (filesPre.Length - files.Length) + " files but only deleted " + (filesPre.Length
					 - files2.Length) + "; expected files:\n    " + AsString(files) + "\n  actual files:\n    "
					 + AsString(files2) + "\ndiff: " + dif);
			}
		}

		private static ICollection<string> DifFiles(string[] files1, string[] files2)
		{
			ICollection<string> set1 = new HashSet<string>();
			ICollection<string> set2 = new HashSet<string>();
			ICollection<string> extra = new HashSet<string>();
			for (int x = 0; x < files1.Length; x++)
			{
				set1.AddItem(files1[x]);
			}
			for (int x_1 = 0; x_1 < files2.Length; x_1++)
			{
				set2.AddItem(files2[x_1]);
			}
			Iterator<string> i1 = set1.Iterator();
			while (i1.HasNext())
			{
				string o = i1.Next();
				if (!set2.Contains(o))
				{
					extra.AddItem(o);
				}
			}
			Iterator<string> i2 = set2.Iterator();
			while (i2.HasNext())
			{
				string o = i2.Next();
				if (!set1.Contains(o))
				{
					extra.AddItem(o);
				}
			}
			return extra;
		}

		private string AsString(string[] l)
		{
			string s = string.Empty;
			for (int i = 0; i < l.Length; i++)
			{
				if (i > 0)
				{
					s += "\n    ";
				}
				s += l[i];
			}
			return s;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void CopyFile(Directory dir, string src, string dest)
		{
			IndexInput @in = dir.OpenInput(src, NewIOContext(Random()));
			IndexOutput @out = dir.CreateOutput(dest, NewIOContext(Random()));
			byte[] b = new byte[1024];
			long remainder = @in.Length();
			while (remainder > 0)
			{
				int len = (int)Math.Min(b.Length, remainder);
				@in.ReadBytes(b, 0, len);
				@out.WriteBytes(b, len);
				remainder -= len;
			}
			@in.Close();
			@out.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDoc(IndexWriter writer, int id)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("content", "aaa", Field.Store.NO));
			doc.Add(NewStringField("id", Sharpen.Extensions.ToString(id), Field.Store.NO));
			writer.AddDocument(doc);
		}
	}
}
