/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Asserting;
using Org.Apache.Lucene.Codecs.Lucene41;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Asserting
{
	/// <summary>
	/// Just like
	/// <see cref="Org.Apache.Lucene.Codecs.Lucene41.Lucene41StoredFieldsFormat">Org.Apache.Lucene.Codecs.Lucene41.Lucene41StoredFieldsFormat
	/// 	</see>
	/// but with additional asserts.
	/// </summary>
	public class AssertingStoredFieldsFormat : StoredFieldsFormat
	{
		private readonly StoredFieldsFormat @in = new Lucene41StoredFieldsFormat();

		/// <exception cref="System.IO.IOException"></exception>
		public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo 
			si, FieldInfos fn, IOContext context)
		{
			return new AssertingStoredFieldsFormat.AssertingStoredFieldsReader(@in.FieldsReader
				(directory, si, fn, context), si.GetDocCount());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo 
			si, IOContext context)
		{
			return new AssertingStoredFieldsFormat.AssertingStoredFieldsWriter(@in.FieldsWriter
				(directory, si, context));
		}

		internal class AssertingStoredFieldsReader : StoredFieldsReader
		{
			private readonly StoredFieldsReader @in;

			private readonly int maxDoc;

			internal AssertingStoredFieldsReader(StoredFieldsReader @in, int maxDoc)
			{
				this.@in = @in;
				this.maxDoc = maxDoc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				@in.Close();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void VisitDocument(int n, StoredFieldVisitor visitor)
			{
				//HM:revisit 
				//assert n >= 0 && n < maxDoc;
				@in.VisitDocument(n, visitor);
			}

			public override StoredFieldsReader Clone()
			{
				return new AssertingStoredFieldsFormat.AssertingStoredFieldsReader(@in.Clone(), maxDoc
					);
			}

			public override long RamBytesUsed()
			{
				return @in.RamBytesUsed();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void CheckIntegrity()
			{
				@in.CheckIntegrity();
			}
		}

		internal enum Status
		{
			UNDEFINED,
			STARTED,
			FINISHED
		}

		internal class AssertingStoredFieldsWriter : StoredFieldsWriter
		{
			private readonly StoredFieldsWriter @in;

			private int numWritten;

			private int fieldCount;

			private AssertingStoredFieldsFormat.Status docStatus;

			internal AssertingStoredFieldsWriter(StoredFieldsWriter @in)
			{
				this.@in = @in;
				this.docStatus = AssertingStoredFieldsFormat.Status.UNDEFINED;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartDocument(int numStoredFields)
			{
				//HM:revisit 
				//assert docStatus != Status.STARTED;
				@in.StartDocument(numStoredFields);
				//HM:revisit 
				//assert fieldCount == 0;
				fieldCount = numStoredFields;
				numWritten++;
				docStatus = AssertingStoredFieldsFormat.Status.STARTED;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishDocument()
			{
				//HM:revisit 
				//assert docStatus == Status.STARTED;
				//HM:revisit 
				//assert fieldCount == 0;
				@in.FinishDocument();
				docStatus = AssertingStoredFieldsFormat.Status.FINISHED;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void WriteField(FieldInfo info, IndexableField field)
			{
				//HM:revisit 
				//assert docStatus == Status.STARTED;
				@in.WriteField(info, field);
				//HM:revisit 
				//assert fieldCount > 0;
				fieldCount--;
			}

			public override void Abort()
			{
				@in.Abort();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(FieldInfos fis, int numDocs)
			{
				//HM:revisit 
				//assert docStatus == (numDocs > 0 ? Status.FINISHED : Status.UNDEFINED);
				@in.Finish(fis, numDocs);
			}

			//HM:revisit 
			//assert fieldCount == 0;
			//HM:revisit 
			//assert numDocs == numWritten;
			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				@in.Close();
			}
		}
	}
}
