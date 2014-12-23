using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Cranky.TestFramework
{
	internal class CrankyDocValuesFormat : DocValuesFormat
	{
		internal readonly DocValuesFormat docValueFormat;

		internal readonly Random random;

		internal CrankyDocValuesFormat(DocValuesFormat dvFormat, Random random) : base(dvFormat.Name)
		{
			// we impersonate the passed-in codec, so we don't need to be in SPI,
			// and so we dont change file formats
			this.docValueFormat = dvFormat;
			this.random = random;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from DocValuesFormat.fieldsConsumer()");
			}
			return new CrankyDocValuesConsumer(docValueFormat.FieldsConsumer(state), random);
		}

		
		public override DocValuesProducer FieldsProducer(SegmentReadState state)
		{
			return docValueFormat.FieldsProducer(state);
		}

		internal class CrankyDocValuesConsumer : DocValuesConsumer
		{
			internal readonly DocValuesConsumer dvConsumer;

			internal readonly Random random;

			internal CrankyDocValuesConsumer(DocValuesConsumer delegate_, Random random)
			{
				this.dvConsumer = delegate_;
				this.random = random;
			}

			
			protected override void Dispose(bool disposing)
			{
				dvConsumer.Dispose();
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from DocValuesConsumer.close()");
				}
			}

			
			public override void AddNumericField(FieldInfo field, IEnumerable<long> values)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from DocValuesConsumer.addNumericField()");
				}
				dvConsumer.AddNumericField(field, values);
			}

			
			public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from DocValuesConsumer.addBinaryField()");
				}
				dvConsumer.AddBinaryField(field, values);
			}

			
			public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrd)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from DocValuesConsumer.addSortedField()");
				}
				dvConsumer.AddSortedField(field, values, docToOrd);
			}

			
			public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrdCount, IEnumerable<long> ords)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from DocValuesConsumer.addSortedSetField()");
				}
				dvConsumer.AddSortedSetField(field, values, docToOrdCount, ords);
			}
		}
	}
}
