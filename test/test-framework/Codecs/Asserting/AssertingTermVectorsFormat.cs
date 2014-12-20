/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Asserting;
using Org.Apache.Lucene.Codecs.Lucene40;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Asserting
{
	/// <summary>
	/// Just like
	/// <see cref="Org.Apache.Lucene.Codecs.Lucene40.Lucene40TermVectorsFormat">Org.Apache.Lucene.Codecs.Lucene40.Lucene40TermVectorsFormat
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
			public override void Close()
			{
				@in.Close();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Fields Get(int doc)
			{
				Fields fields = @in.Get(doc);
				return fields == null ? null : new AssertingAtomicReader.AssertingFields(fields);
			}

			public override TermVectorsReader Clone()
			{
				return new AssertingTermVectorsFormat.AssertingTermVectorsReader(@in.Clone());
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

		internal class AssertingTermVectorsWriter : TermVectorsWriter
		{
			private readonly TermVectorsWriter @in;

			private AssertingTermVectorsFormat.Status docStatus;

			private AssertingTermVectorsFormat.Status fieldStatus;

			private AssertingTermVectorsFormat.Status termStatus;

			private int docCount;

			private int fieldCount;

			private int termCount;

			private int positionCount;

			internal bool hasPositions;

			internal AssertingTermVectorsWriter(TermVectorsWriter @in)
			{
				this.@in = @in;
				docStatus = AssertingTermVectorsFormat.Status.UNDEFINED;
				fieldStatus = AssertingTermVectorsFormat.Status.UNDEFINED;
				termStatus = AssertingTermVectorsFormat.Status.UNDEFINED;
				fieldCount = termCount = positionCount = 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartDocument(int numVectorFields)
			{
				//HM:revisit 
				//assert fieldCount == 0;
				//HM:revisit 
				//assert docStatus != Status.STARTED;
				@in.StartDocument(numVectorFields);
				docStatus = AssertingTermVectorsFormat.Status.STARTED;
				fieldCount = numVectorFields;
				docCount++;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishDocument()
			{
				//HM:revisit 
				//assert fieldCount == 0;
				//HM:revisit 
				//assert docStatus == Status.STARTED;
				@in.FinishDocument();
				docStatus = AssertingTermVectorsFormat.Status.FINISHED;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartField(FieldInfo info, int numTerms, bool positions, bool
				 offsets, bool payloads)
			{
				//HM:revisit 
				//assert termCount == 0;
				//HM:revisit 
				//assert docStatus == Status.STARTED;
				//HM:revisit 
				//assert fieldStatus != Status.STARTED;
				@in.StartField(info, numTerms, positions, offsets, payloads);
				fieldStatus = AssertingTermVectorsFormat.Status.STARTED;
				termCount = numTerms;
				hasPositions = positions || offsets || payloads;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishField()
			{
				//HM:revisit 
				//assert termCount == 0;
				//HM:revisit 
				//assert fieldStatus == Status.STARTED;
				@in.FinishField();
				fieldStatus = AssertingTermVectorsFormat.Status.FINISHED;
				--fieldCount;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartTerm(BytesRef term, int freq)
			{
				//HM:revisit 
				//assert docStatus == Status.STARTED;
				//HM:revisit 
				//assert fieldStatus == Status.STARTED;
				//HM:revisit 
				//assert termStatus != Status.STARTED;
				@in.StartTerm(term, freq);
				termStatus = AssertingTermVectorsFormat.Status.STARTED;
				positionCount = hasPositions ? freq : 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm()
			{
				//HM:revisit 
				//assert positionCount == 0;
				//HM:revisit 
				//assert docStatus == Status.STARTED;
				//HM:revisit 
				//assert fieldStatus == Status.STARTED;
				//HM:revisit 
				//assert termStatus == Status.STARTED;
				@in.FinishTerm();
				termStatus = AssertingTermVectorsFormat.Status.FINISHED;
				--termCount;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddPosition(int position, int startOffset, int endOffset, BytesRef
				 payload)
			{
				//HM:revisit 
				//assert docStatus == Status.STARTED;
				//HM:revisit 
				//assert fieldStatus == Status.STARTED;
				//HM:revisit 
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
				//HM:revisit 
				//assert docCount == numDocs;
				//HM:revisit 
				//assert docStatus == (numDocs > 0 ? Status.FINISHED : Status.UNDEFINED);
				//HM:revisit 
				//assert fieldStatus != Status.STARTED;
				//HM:revisit 
				//assert termStatus != Status.STARTED;
				@in.Finish(fis, numDocs);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IComparer<BytesRef> GetComparator()
			{
				return @in.GetComparator();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				@in.Close();
			}
		}
	}
}
