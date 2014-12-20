/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Cranky;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Cranky
{
	internal class CrankyTermVectorsFormat : TermVectorsFormat
	{
		internal readonly TermVectorsFormat delegate_;

		internal readonly Random random;

		internal CrankyTermVectorsFormat(TermVectorsFormat delegate_, Random random)
		{
			this.delegate_ = delegate_;
			this.random = random;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo 
			segmentInfo, FieldInfos fieldInfos, IOContext context)
		{
			return delegate_.VectorsReader(directory, segmentInfo, fieldInfos, context);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo 
			segmentInfo, IOContext context)
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from TermVectorsFormat.vectorsWriter()");
			}
			return new CrankyTermVectorsFormat.CrankyTermVectorsWriter(delegate_.VectorsWriter
				(directory, segmentInfo, context), random);
		}

		internal class CrankyTermVectorsWriter : TermVectorsWriter
		{
			internal readonly TermVectorsWriter delegate_;

			internal readonly Random random;

			internal CrankyTermVectorsWriter(TermVectorsWriter delegate_, Random random)
			{
				this.delegate_ = delegate_;
				this.random = random;
			}

			public override void Abort()
			{
				delegate_.Abort();
				if (random.Next(100) == 0)
				{
					throw new RuntimeException(new IOException("Fake IOException from TermVectorsWriter.abort()"
						));
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Merge(MergeState mergeState)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.merge()");
				}
				return base.Merge(mergeState);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Finish(FieldInfos fis, int numDocs)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.finish()");
				}
				delegate_.Finish(fis, numDocs);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				delegate_.Close();
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.close()");
				}
			}

			// per doc/field methods: lower probability since they are invoked so many times.
			/// <exception cref="System.IO.IOException"></exception>
			public override void StartDocument(int numVectorFields)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.startDocument()");
				}
				delegate_.StartDocument(numVectorFields);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishDocument()
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.finishDocument()");
				}
				delegate_.FinishDocument();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartField(FieldInfo info, int numTerms, bool positions, bool
				 offsets, bool payloads)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.startField()");
				}
				delegate_.StartField(info, numTerms, positions, offsets, payloads);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishField()
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.finishField()");
				}
				delegate_.FinishField();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartTerm(BytesRef term, int freq)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.startTerm()");
				}
				delegate_.StartTerm(term, freq);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm()
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.finishTerm()");
				}
				delegate_.FinishTerm();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddPosition(int position, int startOffset, int endOffset, BytesRef
				 payload)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.addPosition()");
				}
				delegate_.AddPosition(position, startOffset, endOffset, payload);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddProx(int numProx, DataInput positions, DataInput offsets)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.addProx()");
				}
				base.AddProx(numProx, positions, offsets);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IComparer<BytesRef> GetComparator()
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.getComparator()");
				}
				return delegate_.GetComparator();
			}
		}
	}
}
