using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Directory = System.IO.Directory;

namespace Lucene.Net.Codecs.Cranky.TestFramework
{
	internal class CrankyTermVectorsFormat : TermVectorsFormat
	{
		internal readonly TermVectorsFormat tvFormat;

		internal readonly Random random;

		internal CrankyTermVectorsFormat(TermVectorsFormat del, Random random)
		{
			this.tvFormat = del;
			this.random = random;
		}

		
		public override TermVectorsReader VectorsReader(Lucene.Net.Store.Directory directory, SegmentInfo segmentInfo, FieldInfos fieldInfos, IOContext context)
		{
			return tvFormat.VectorsReader(directory, segmentInfo, fieldInfos, context);
		}

		
		public override TermVectorsWriter VectorsWriter(Lucene.Net.Store.Directory directory, SegmentInfo segmentInfo, IOContext context)
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from TermVectorsFormat.vectorsWriter()");
			}
			return new CrankyTermVectorsFormat.CrankyTermVectorsWriter(tvFormat.VectorsWriter
				(directory, segmentInfo, context), random);
		}

		internal class CrankyTermVectorsWriter : TermVectorsWriter
		{
			internal readonly TermVectorsWriter tvWriter;

			internal readonly Random random;

			internal CrankyTermVectorsWriter(TermVectorsWriter delegate_, Random random)
			{
				this.tvWriter = delegate_;
				this.random = random;
			}

			public override void Abort()
			{
				tvWriter.Abort();
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.abort()");
				}
			}

			
			public override int Merge(MergeState mergeState)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.merge()");
				}
				return base.Merge(mergeState);
			}

			
			public override void Finish(FieldInfos fis, int numDocs)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.finish()");
				}
				tvWriter.Finish(fis, numDocs);
			}


		    protected override void Dispose(bool disposing)
			{
				tvWriter.Dispose();
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
				tvWriter.StartDocument(numVectorFields);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishDocument()
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.finishDocument()");
				}
				tvWriter.FinishDocument();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartField(FieldInfo info, int numTerms, bool positions, bool
				 offsets, bool payloads)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.startField()");
				}
				tvWriter.StartField(info, numTerms, positions, offsets, payloads);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishField()
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.finishField()");
				}
				tvWriter.FinishField();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StartTerm(BytesRef term, int freq)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.startTerm()");
				}
				tvWriter.StartTerm(term, freq);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishTerm()
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.finishTerm()");
				}
				tvWriter.FinishTerm();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddPosition(int position, int startOffset, int endOffset, BytesRef
				 payload)
			{
				if (random.Next(10000) == 0)
				{
					throw new IOException("Fake IOException from TermVectorsWriter.addPosition()");
				}
				tvWriter.AddPosition(position, startOffset, endOffset, payload);
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
			public override IComparer<BytesRef> Comparator
			{
			    get
			    {
			        if (random.Next(10000) == 0)
			        {
			            throw new IOException("Fake IOException from TermVectorsWriter.getComparator()");
			        }
			        return tvWriter.Comparator;
			    }
			}
		}
	}
}
