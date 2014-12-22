/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Cranky;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Cranky
{
	internal class CrankyDocValuesFormat : DocValuesFormat
	{
		internal readonly DocValuesFormat delegate_;

		internal readonly Random random;

		internal CrankyDocValuesFormat(DocValuesFormat delegate_, Random random) : base(delegate_
			.GetName())
		{
			// we impersonate the passed-in codec, so we don't need to be in SPI,
			// and so we dont change file formats
			this.delegate_ = delegate_;
			this.random = random;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from DocValuesFormat.fieldsConsumer()");
			}
			return new CrankyDocValuesFormat.CrankyDocValuesConsumer(delegate_.FieldsConsumer
				(state), random);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesProducer FieldsProducer(SegmentReadState state)
		{
			return delegate_.FieldsProducer(state);
		}

		internal class CrankyDocValuesConsumer : DocValuesConsumer
		{
			internal readonly DocValuesConsumer delegate_;

			internal readonly Random random;

			internal CrankyDocValuesConsumer(DocValuesConsumer delegate_, Random random)
			{
				this.delegate_ = delegate_;
				this.random = random;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				delegate_.Close();
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from DocValuesConsumer.close()");
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddNumericField(FieldInfo field, Iterable<Number> values)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from DocValuesConsumer.addNumericField()"
						);
				}
				delegate_.AddNumericField(field, values);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddBinaryField(FieldInfo field, Iterable<BytesRef> values)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from DocValuesConsumer.addBinaryField()");
				}
				delegate_.AddBinaryField(field, values);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddSortedField(FieldInfo field, Iterable<BytesRef> values, Iterable
				<Number> docToOrd)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from DocValuesConsumer.addSortedField()");
				}
				delegate_.AddSortedField(field, values, docToOrd);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddSortedSetField(FieldInfo field, Iterable<BytesRef> values
				, Iterable<Number> docToOrdCount, Iterable<Number> ords)
			{
				if (random.Next(100) == 0)
				{
					throw new IOException("Fake IOException from DocValuesConsumer.addSortedSetField()"
						);
				}
				delegate_.AddSortedSetField(field, values, docToOrdCount, ords);
			}
		}
	}
}
