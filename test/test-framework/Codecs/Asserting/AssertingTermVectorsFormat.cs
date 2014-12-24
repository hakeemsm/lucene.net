using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Asserting.TestFramework
{
	/// <summary>
	/// Just like
	/// <see cref="Lucene.Net.Codecs.Lucene40.Lucene40TermVectorsFormat">Lucene.Net.Codecs.Lucene40.Lucene40TermVectorsFormat
	/// 	</see>
	/// but with additional asserts.
	/// </summary>
	public class AssertingTermVectorsFormat : TermVectorsFormat
	{
		private readonly TermVectorsFormat @in = new Lucene40TermVectorsFormat();

		/// <exception cref="System.IO.IOException"></exception>
		public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo 
			segmentInfo, FieldInfos fieldInfos, IOContext context)
		{
			return new AssertingTermVectorsFormat.AssertingTermVectorsReader(@in.VectorsReader
				(directory, segmentInfo, fieldInfos, context));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo 
			segmentInfo, IOContext context)
		{
			return new AssertingTermVectorsFormat.AssertingTermVectorsWriter(@in.VectorsWriter
				(directory, segmentInfo, context));
		}

		internal class AssertingTermVectorsReader : TermVectorsReader
		{
			private readonly TermVectorsReader @in;

			internal AssertingTermVectorsReader(TermVectorsReader @in)
			{
				this.@in = @in;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void Dispose(bool disposing)
			{
				@in.Dispose();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Fields Get(int doc)
			{
				Fields fields = @in.Get(doc);
				return fields == null ? null : new AssertingAtomicReader.AssertingFields(fields);
			}

			public override object Clone()
			{
				return new AssertingTermVectorsReader(@in.Clone());
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

		internal class AssertingTermVectorsWriter : TermVectorsWriter
		{
			private readonly TermVectorsWriter @in;

			private Status docStatus;

			private Status fieldStatus;

			private Status termStatus;

			private int docCount;

			private int fieldCount;

			private int termCount;

			private int positionCount;

			internal bool hasPositions;

			internal AssertingTermVectorsWriter(TermVectorsWriter @in)
			{
				this.@in = @in;
				docStatus = Status.UNDEFINED;
				fieldStatus = Status.UNDEFINED;
				termStatus = Status.UNDEFINED;
				fieldCount = termCount = positionCount = 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartDocument(int numVectorFields)
			{
				 
				//assert fieldCount == 0;
				 
				//assert docStatus != Status.STARTED;
				@in.StartDocument(numVectorFields);
				docStatus = Status.STARTED;
				fieldCount = numVectorFields;
				docCount++;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishDocument()
			{
				 
				//assert fieldCount == 0;
				 
				//assert docStatus == Status.STARTED;
				@in.FinishDocument();
				docStatus = Status.FINISHED;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartField(FieldInfo info, int numTerms, bool positions, bool
				 offsets, bool payloads)
			{
				 
				//assert termCount == 0;
				 
				//assert docStatus == Status.STARTED;
				 
				//assert fieldStatus != Status.STARTED;
				@in.StartField(info, numTerms, positions, offsets, payloads);
				fieldStatus = Status.STARTED;
				termCount = numTerms;
				hasPositions = positions || offsets || payloads;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishField()
			{
				 
				//assert termCount == 0;
				 
				//assert fieldStatus == Status.STARTED;
				@in.FinishField();
				fieldStatus = Status.FINISHED;
				--fieldCount;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartTerm(BytesRef term, int freq)
			{
				 
				//assert docStatus == Status.STARTED;
				 
				//assert fieldStatus == Status.STARTED;
				 
				//assert termStatus != Status.STARTED;
				@in.StartTerm(term, freq);
				termStatus = Status.STARTED;
				positionCount = hasPositions ? freq : 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm()
			{
				 
				//assert positionCount == 0;
				 
				//assert docStatus == Status.STARTED;
				 
				//assert fieldStatus == Status.STARTED;
				 
				//assert termStatus == Status.STARTED;
				@in.FinishTerm();
				termStatus = Status.FINISHED;
				--termCount;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddPosition(int position, int startOffset, int endOffset, BytesRef
				 payload)
			{
				 
				//assert docStatus == Status.STARTED;
				 
				//assert fieldStatus == Status.STARTED;
				 
				//assert termStatus == Status.STARTED;
				@in.AddPosition(position, startOffset, endOffset, payload);
				--positionCount;
			}

			public override void Abort()
			{
				@in.Abort();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(FieldInfos fis, int numDocs)
			{
				 
				//assert docCount == numDocs;
				 
				//assert docStatus == (numDocs > 0 ? Status.FINISHED : Status.UNDEFINED);
				 
				//assert fieldStatus != Status.STARTED;
				 
				//assert termStatus != Status.STARTED;
				@in.Finish(fis, numDocs);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IComparer<BytesRef> Comparator
			{
			    get { return @in.Comparator; }
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void Dispose(bool disposing)
			{
				@in.Dispose();
			}
		}
	}
}
