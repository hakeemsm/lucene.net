/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestFieldInfos : LuceneTestCase
	{
		private Lucene.Net.Document.Document testDoc = new Lucene.Net.Document.Document
			();

		//import org.cnlp.utils.properties.ResourceBundleHelper;
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			DocHelper.SetupDoc(testDoc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual FieldInfos CreateAndWriteFieldInfos(Directory dir, string filename
			)
		{
			//Positive test of FieldInfos
			NUnit.Framework.Assert.IsTrue(testDoc != null);
			FieldInfos.Builder builder = new FieldInfos.Builder();
			foreach (IndexableField field in testDoc)
			{
				builder.AddOrUpdate(field.Name(), field.FieldType());
			}
			FieldInfos fieldInfos = builder.Finish();
			//Since the complement is stored as well in the fields map
			NUnit.Framework.Assert.IsTrue(fieldInfos.Size() == DocHelper.all.Count);
			//this is all b/c we are using the no-arg constructor
			IndexOutput output = dir.CreateOutput(filename, NewIOContext(Random()));
			NUnit.Framework.Assert.IsTrue(output != null);
			//Use a RAMOutputStream
			FieldInfosWriter writer = Codec.GetDefault().FieldInfosFormat().GetFieldInfosWriter
				();
			writer.Write(dir, filename, string.Empty, fieldInfos, IOContext.DEFAULT);
			output.Close();
			return fieldInfos;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual FieldInfos ReadFieldInfos(Directory dir, string filename)
		{
			FieldInfosReader reader = Codec.GetDefault().FieldInfosFormat().GetFieldInfosReader
				();
			return reader.Read(dir, filename, string.Empty, IOContext.DEFAULT);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Test()
		{
			string name = "testFile";
			Directory dir = NewDirectory();
			FieldInfos fieldInfos = CreateAndWriteFieldInfos(dir, name);
			FieldInfos readIn = ReadFieldInfos(dir, name);
			NUnit.Framework.Assert.IsTrue(fieldInfos.Size() == readIn.Size());
			FieldInfo info = readIn.FieldInfo("textField1");
			NUnit.Framework.Assert.IsTrue(info != null);
			NUnit.Framework.Assert.IsTrue(info.HasVectors() == false);
			NUnit.Framework.Assert.IsTrue(info.OmitsNorms() == false);
			info = readIn.FieldInfo("textField2");
			NUnit.Framework.Assert.IsTrue(info != null);
			NUnit.Framework.Assert.IsTrue(info.OmitsNorms() == false);
			info = readIn.FieldInfo("textField3");
			NUnit.Framework.Assert.IsTrue(info != null);
			NUnit.Framework.Assert.IsTrue(info.HasVectors() == false);
			NUnit.Framework.Assert.IsTrue(info.OmitsNorms() == true);
			info = readIn.FieldInfo("omitNorms");
			NUnit.Framework.Assert.IsTrue(info != null);
			NUnit.Framework.Assert.IsTrue(info.HasVectors() == false);
			NUnit.Framework.Assert.IsTrue(info.OmitsNorms() == true);
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestReadOnly()
		{
			string name = "testFile";
			Directory dir = NewDirectory();
			FieldInfos fieldInfos = CreateAndWriteFieldInfos(dir, name);
			FieldInfos readOnly = ReadFieldInfos(dir, name);
			AssertReadOnly(readOnly, fieldInfos);
			dir.Close();
		}

		private void AssertReadOnly(FieldInfos readOnly, FieldInfos modifiable)
		{
			NUnit.Framework.Assert.AreEqual(modifiable.Size(), readOnly.Size());
			// 
			//HM:revisit 
			//assert we can iterate
			foreach (FieldInfo fi in readOnly)
			{
				NUnit.Framework.Assert.AreEqual(fi.name, modifiable.FieldInfo(fi.number).name);
			}
		}
	}
}
