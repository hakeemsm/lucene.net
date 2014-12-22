using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Codecs.Lucene41;

namespace Lucene.Net.Codecs.Asserting.TestFramework
{
	/// <summary>
	/// Just like
	/// <see cref="Lucene.Net.Codecs.Lucene41.Lucene41StoredFieldsFormat">Lucene.Net.Codecs.Lucene41.Lucene41StoredFieldsFormat
	/// 	</see>
	/// but with additional asserts.
	/// </summary>
	public class AssertingStoredFieldsFormat : StoredFieldsFormat
	{
		private readonly StoredFieldsFormat @in = new Lucene41StoredFieldsFormat();

		
		public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo si, FieldInfos fn, IOContext context)
		{
			return new AssertingStoredFieldsReader(@in.FieldsReader(directory, si, fn, context), si.DocCount);
		}

		
		public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
		{
			return new AssertingStoredFieldsWriter(@in.FieldsWriter(directory, si, context));
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


		    protected override void Dispose(bool disposing)
			{
				@in.Dispose();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void VisitDocument(int n, StoredFieldVisitor visitor)
			{
				 
				//assert n >= 0 && n < maxDoc;
				@in.VisitDocument(n, visitor);
			}

			public override object Clone()
			{
				return new AssertingStoredFieldsReader((StoredFieldsReader) @in.Clone(), maxDoc);
			}

			public override long RamBytesUsed
			{
			    get { return @in.RamBytesUsed; }
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
				this.docStatus = Status.UNDEFINED;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartDocument(int numStoredFields)
			{
				 
				//assert docStatus != Status.STARTED;
				@in.StartDocument(numStoredFields);
				 
				//assert fieldCount == 0;
				fieldCount = numStoredFields;
				numWritten++;
				docStatus = Status.STARTED;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishDocument()
			{
				 
				//assert docStatus == Status.STARTED;
				 
				//assert fieldCount == 0;
				@in.FinishDocument();
				docStatus = Status.FINISHED;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void WriteField(FieldInfo info, IIndexableField field)
			{
				 
				//assert docStatus == Status.STARTED;
				@in.WriteField(info, field);
				 
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
				 
				//assert docStatus == (numDocs > 0 ? Status.FINISHED : Status.UNDEFINED);
				@in.Finish(fis, numDocs);
			}

			 
			//assert fieldCount == 0;
			 
			//assert numDocs == numWritten;
			/// <exception cref="System.IO.IOException"></exception>
			protected override void Dispose(bool disposing)
			{
				@in.Dispose();
			}
		}
	}
}
