/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Store
{
	public class TestFileSwitchDirectory : LuceneTestCase
	{
		/// <summary>Test if writing doc stores to disk and everything else to ram works.</summary>
		/// <remarks>Test if writing doc stores to disk and everything else to ram works.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestBasic()
		{
			ICollection<string> fileExtensions = new HashSet<string>();
			fileExtensions.Add(Lucene40StoredFieldsWriter.FIELDS_EXTENSION);
			fileExtensions.Add(Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION);
			MockDirectoryWrapper primaryDir = new MockDirectoryWrapper(Random(), new RAMDirectory
				());
			primaryDir.SetCheckIndexOnClose(false);
			// only part of an index
			MockDirectoryWrapper secondaryDir = new MockDirectoryWrapper(Random(), new RAMDirectory
				());
			secondaryDir.SetCheckIndexOnClose(false);
			// only part of an index
			FileSwitchDirectory fsd = new FileSwitchDirectory(fileExtensions, primaryDir, secondaryDir
				, true);
			// for now we wire Lucene40Codec because we rely upon its specific impl
			bool oldValue = OLD_FORMAT_IMPERSONATION_IS_ACTIVE;
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
			IndexWriter writer = new IndexWriter(fsd, ((IndexWriterConfig)new IndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				(false)).SetCodec(Codec.ForName("Lucene40")).UseCompoundFile = (false)));
			TestIndexWriterReader.CreateIndexNoClose(true, "ram", writer);
			IndexReader reader = DirectoryReader.Open(writer, true);
			AreEqual(100, reader.MaxDoc);
			writer.Commit();
			// we should see only fdx,fdt files here
			string[] files = primaryDir.ListAll();
			IsTrue(files.Length > 0);
			for (int x = 0; x < files.Length; x++)
			{
				string ext = FileSwitchDirectory.GetExtension(files[x]);
				IsTrue(fileExtensions.Contains(ext));
			}
			files = secondaryDir.ListAll();
			IsTrue(files.Length > 0);
			// we should not see fdx,fdt files here
			for (int x_1 = 0; x_1 < files.Length; x_1++)
			{
				string ext = FileSwitchDirectory.GetExtension(files[x_1]);
				IsFalse(fileExtensions.Contains(ext));
			}
			reader.Dispose();
			writer.Dispose();
			files = fsd.ListAll();
			for (int i = 0; i < files.Length; i++)
			{
				IsNotNull(files[i]);
			}
			fsd.Dispose();
			OLD_FORMAT_IMPERSONATION_IS_ACTIVE = oldValue;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private Directory NewFSSwitchDirectory(ICollection<string> primaryExtensions)
		{
			DirectoryInfo primDir = CreateTempDir("foo");
			DirectoryInfo secondDir = CreateTempDir("bar");
			return NewFSSwitchDirectory(primDir, secondDir, primaryExtensions);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private Directory NewFSSwitchDirectory(DirectoryInfo aDir, DirectoryInfo bDir, ICollection<
			string> primaryExtensions)
		{
			Directory a = new SimpleFSDirectory(aDir);
			Directory b = new SimpleFSDirectory(bDir);
			FileSwitchDirectory switchDir = new FileSwitchDirectory(primaryExtensions, a, b, 
				true);
			return new MockDirectoryWrapper(Random(), switchDir);
		}

		// LUCENE-3380 -- make sure we get exception if the directory really does not exist.
		/// <exception cref="System.Exception"></exception>
		public virtual void TestNoDir()
		{
			DirectoryInfo primDir = CreateTempDir("foo");
			DirectoryInfo secondDir = CreateTempDir("bar");
			TestUtil.Rm(primDir);
			TestUtil.Rm(secondDir);
			Directory dir = NewFSSwitchDirectory(primDir, secondDir, Collections.EmptySet
				<string>());
			try
			{
				DirectoryReader.Open(dir);
				Fail("did not hit expected exception");
			}
			catch (NoSuchDirectoryException)
			{
			}
			// expected
			dir.Dispose();
		}

		// LUCENE-3380 test that we can add a file, and then when we call list() we get it back
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestDirectoryFilter()
		{
			Directory dir = NewFSSwitchDirectory(Collections.EmptySet<string>());
			string name = "file";
			try
			{
				dir.CreateOutput(name, NewIOContext(Random())).Dispose();
				IsTrue(SlowFileExists(dir, name));
				IsTrue(Arrays.AsList(dir.ListAll()).Contains(name));
			}
			finally
			{
				dir.Dispose();
			}
		}

		// LUCENE-3380 test that delegate compound files correctly.
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCompoundFileAppendTwice()
		{
			Directory newDir = NewFSSwitchDirectory(Collections.Singleton("cfs"));
			CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext
				(Random()), true);
			CreateSequenceFile(newDir, "d1", unchecked((byte)0), 15);
			IndexOutput @out = csw.CreateOutput("d.xyz", NewIOContext(Random()));
			@out.WriteInt(0);
			@out.Dispose();
			AreEqual(1, csw.ListAll().Length);
			AreEqual("d.xyz", csw.ListAll()[0]);
			csw.Dispose();
			CompoundFileDirectory cfr = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext
				(Random()), false);
			AreEqual(1, cfr.ListAll().Length);
			AreEqual("d.xyz", cfr.ListAll()[0]);
			cfr.Dispose();
			newDir.Dispose();
		}

		/// <summary>Creates a file of the specified size with sequential data.</summary>
		/// <remarks>
		/// Creates a file of the specified size with sequential data. The first
		/// byte is written as the start byte provided. All subsequent bytes are
		/// computed as start + offset where offset is the number of the byte.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		private void CreateSequenceFile(Directory dir, string name, byte start, int size)
		{
			IndexOutput os = dir.CreateOutput(name, NewIOContext(Random()));
			for (int i = 0; i < size; i++)
			{
				os.WriteByte(start);
				start++;
			}
			os.Dispose();
		}
	}
}
