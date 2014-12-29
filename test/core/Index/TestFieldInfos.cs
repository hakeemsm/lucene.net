using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using NUnit.Framework;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestFieldInfos : LuceneTestCase
	{
		private Lucene.Net.Documents.Document testDoc = new Lucene.Net.Documents.Document();

		[SetUp]
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
			IsTrue(testDoc != null);
			FieldInfos.Builder builder = new FieldInfos.Builder();
			foreach (IIndexableField field in testDoc)
			{
				builder.AddOrUpdate(field.Name, field.FieldTypeValue);
			}
			FieldInfos fieldInfos = builder.Finish();
			//Since the complement is stored as well in the fields map
			IsTrue(fieldInfos.Size == DocHelper.all.Count);
			//this is all b/c we are using the no-arg constructor
			IndexOutput output = dir.CreateOutput(filename, NewIOContext(Random()));
			IsTrue(output != null);
			//Use a RAMOutputStream
			FieldInfosWriter writer = Codec.Default.FieldInfosFormat.FieldInfosWriter;
			writer.Write(dir, filename, string.Empty, fieldInfos, IOContext.DEFAULT);
			output.Dispose();
			return fieldInfos;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual FieldInfos ReadFieldInfos(Directory dir, string filename)
		{
			FieldInfosReader reader = Codec.Default.FieldInfosFormat.FieldInfosReader;
			return reader.Read(dir, filename, string.Empty, IOContext.DEFAULT);
		}

		[Test]
		public virtual void TestFieldInfoProps()
		{
			string name = "testFile";
			Directory dir = NewDirectory();
			FieldInfos fieldInfos = CreateAndWriteFieldInfos(dir, name);
			FieldInfos readIn = ReadFieldInfos(dir, name);
			IsTrue(fieldInfos.Size == readIn.Size);
			FieldInfo info = readIn.FieldInfo("textField1");
			IsTrue(info != null);
			IsTrue(info.HasVectors == false);
			IsTrue(info.OmitsNorms == false);
			info = readIn.FieldInfo("textField2");
			IsTrue(info != null);
			IsTrue(info.OmitsNorms == false);
			info = readIn.FieldInfo("textField3");
			IsTrue(info != null);
			IsTrue(info.HasVectors == false);
			IsTrue(info.OmitsNorms);
			info = readIn.FieldInfo("omitNorms");
			IsTrue(info != null);
			IsTrue(info.HasVectors == false);
			IsTrue(info.OmitsNorms);
			dir.Dispose();
		}

		[Test]
		public virtual void TestReadOnly()
		{
			string name = "testFile";
			Directory dir = NewDirectory();
			FieldInfos fieldInfos = CreateAndWriteFieldInfos(dir, name);
			FieldInfos readOnly = ReadFieldInfos(dir, name);
			AssertReadOnly(readOnly, fieldInfos);
			dir.Dispose();
		}

		private void AssertReadOnly(FieldInfos readOnly, FieldInfos modifiable)
		{
			AreEqual(modifiable.Size, readOnly.Size);
			// 
			//HM:revisit 
			//assert we can iterate
			foreach (FieldInfo fi in readOnly)
			{
				AreEqual(fi.name, modifiable.FieldInfo(fi.number).name);
			}
		}
	}
}
