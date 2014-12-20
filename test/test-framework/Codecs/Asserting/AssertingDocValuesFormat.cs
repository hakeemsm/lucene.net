/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Asserting;
using Org.Apache.Lucene.Codecs.Lucene45;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Asserting
{
	/// <summary>
	/// Just like
	/// <see cref="Org.Apache.Lucene.Codecs.Lucene45.Lucene45DocValuesFormat">Org.Apache.Lucene.Codecs.Lucene45.Lucene45DocValuesFormat
	/// 	</see>
	/// but with additional asserts.
	/// </summary>
	public class AssertingDocValuesFormat : DocValuesFormat
	{
		private readonly DocValuesFormat @in = new Lucene45DocValuesFormat();

		public AssertingDocValuesFormat() : base("Asserting")
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
		{
			DocValuesConsumer consumer = @in.FieldsConsumer(state);
			//HM:revisit 
			//assert consumer != null;
			return new AssertingDocValuesFormat.AssertingDocValuesConsumer(consumer, state.segmentInfo
				.GetDocCount());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesProducer FieldsProducer(SegmentReadState state)
		{
			//HM:revisit 
			//assert state.fieldInfos.hasDocValues();
			DocValuesProducer producer = @in.FieldsProducer(state);
			//HM:revisit 
			//assert producer != null;
			return new AssertingDocValuesFormat.AssertingDocValuesProducer(producer, state.segmentInfo
				.GetDocCount());
		}

		internal class AssertingDocValuesConsumer : DocValuesConsumer
		{
			private readonly DocValuesConsumer @in;

			private readonly int maxDoc;

			internal AssertingDocValuesConsumer(DocValuesConsumer @in, int maxDoc)
			{
				this.@in = @in;
				this.maxDoc = maxDoc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddNumericField(FieldInfo field, Iterable<Number> values)
			{
				int count = 0;
				foreach (Number v in values)
				{
					count++;
				}
				//HM:revisit 
				//assert count == maxDoc;
				CheckIterator(values.Iterator(), maxDoc, true);
				@in.AddNumericField(field, values);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddBinaryField(FieldInfo field, Iterable<BytesRef> values)
			{
				int count = 0;
				foreach (BytesRef b in values)
				{
					//HM:revisit 
					//assert b == null || b.isValid();
					count++;
				}
				//HM:revisit 
				//assert count == maxDoc;
				CheckIterator(values.Iterator(), maxDoc, true);
				@in.AddBinaryField(field, values);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddSortedField(FieldInfo field, Iterable<BytesRef> values, Iterable
				<Number> docToOrd)
			{
				int valueCount = 0;
				BytesRef lastValue = null;
				foreach (BytesRef b in values)
				{
					//HM:revisit 
					//assert b != null;
					//HM:revisit 
					//assert b.isValid();
					if (valueCount > 0)
					{
					}
					//HM:revisit 
					//assert b.compareTo(lastValue) > 0;
					lastValue = BytesRef.DeepCopyOf(b);
					valueCount++;
				}
				//HM:revisit 
				//assert valueCount <= maxDoc;
				FixedBitSet seenOrds = new FixedBitSet(valueCount);
				int count = 0;
				foreach (Number v in docToOrd)
				{
					//HM:revisit 
					//assert v != null;
					int ord = v;
					//HM:revisit 
					//assert ord >= -1 && ord < valueCount;
					if (ord >= 0)
					{
						seenOrds.Set(ord);
					}
					count++;
				}
				//HM:revisit 
				//assert count == maxDoc;
				//HM:revisit 
				//assert seenOrds.cardinality() == valueCount;
				CheckIterator(values.Iterator(), valueCount, false);
				CheckIterator(docToOrd.Iterator(), maxDoc, false);
				@in.AddSortedField(field, values, docToOrd);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddSortedSetField(FieldInfo field, Iterable<BytesRef> values
				, Iterable<Number> docToOrdCount, Iterable<Number> ords)
			{
				long valueCount = 0;
				BytesRef lastValue = null;
				foreach (BytesRef b in values)
				{
					//HM:revisit 
					//assert b != null;
					//HM:revisit 
					//assert b.isValid();
					if (valueCount > 0)
					{
					}
					//HM:revisit 
					//assert b.compareTo(lastValue) > 0;
					lastValue = BytesRef.DeepCopyOf(b);
					valueCount++;
				}
				int docCount = 0;
				long ordCount = 0;
				LongBitSet seenOrds = new LongBitSet(valueCount);
				Iterator<Number> ordIterator = ords.Iterator();
				foreach (Number v in docToOrdCount)
				{
					//HM:revisit 
					//assert v != null;
					int count = v;
					//HM:revisit 
					//assert count >= 0;
					docCount++;
					ordCount += count;
					long lastOrd = -1;
					for (int i = 0; i < count; i++)
					{
						Number o = ordIterator.Next();
						//HM:revisit 
						//assert o != null;
						long ord = o;
						//HM:revisit 
						//assert ord >= 0 && ord < valueCount;
						//HM:revisit 
						//assert ord > lastOrd : "ord=" + ord + ",lastOrd=" + lastOrd;
						seenOrds.Set(ord);
						lastOrd = ord;
					}
				}
				//HM:revisit 
				//assert ordIterator.hasNext() == false;
				//HM:revisit 
				//assert docCount == maxDoc;
				//HM:revisit 
				//assert seenOrds.cardinality() == valueCount;
				CheckIterator(values.Iterator(), valueCount, false);
				CheckIterator(docToOrdCount.Iterator(), maxDoc, false);
				CheckIterator(ords.Iterator(), ordCount, false);
				@in.AddSortedSetField(field, values, docToOrdCount, ords);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				@in.Close();
			}
		}

		internal class AssertingNormsConsumer : DocValuesConsumer
		{
			private readonly DocValuesConsumer @in;

			private readonly int maxDoc;

			internal AssertingNormsConsumer(DocValuesConsumer @in, int maxDoc)
			{
				this.@in = @in;
				this.maxDoc = maxDoc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddNumericField(FieldInfo field, Iterable<Number> values)
			{
				int count = 0;
				foreach (Number v in values)
				{
					//HM:revisit 
					//assert v != null;
					count++;
				}
				//HM:revisit 
				//assert count == maxDoc;
				CheckIterator(values.Iterator(), maxDoc, false);
				@in.AddNumericField(field, values);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				@in.Close();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddBinaryField(FieldInfo field, Iterable<BytesRef> values)
			{
				throw new InvalidOperationException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddSortedField(FieldInfo field, Iterable<BytesRef> values, Iterable
				<Number> docToOrd)
			{
				throw new InvalidOperationException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddSortedSetField(FieldInfo field, Iterable<BytesRef> values
				, Iterable<Number> docToOrdCount, Iterable<Number> ords)
			{
				throw new InvalidOperationException();
			}
		}

		private static void CheckIterator<T>(Iterator<T> iterator, long expectedSize, bool
			 allowNull)
		{
			for (long i = 0; i < expectedSize; i++)
			{
				bool hasNext = iterator.HasNext();
				//HM:revisit 
				//assert hasNext;
				T v = iterator.Next();
				//HM:revisit 
				//assert allowNull || v != null;
				try
				{
					iterator.Remove();
					throw new Exception("broken iterator (supports remove): " + iterator);
				}
				catch (NotSupportedException)
				{
				}
			}
			// ok
			//HM:revisit 
			//assert !iterator.hasNext();
			try
			{
				iterator.Next();
				throw new Exception("broken iterator (allows next() when hasNext==false) " + iterator
					);
			}
			catch (NoSuchElementException)
			{
			}
		}

		internal class AssertingDocValuesProducer : DocValuesProducer
		{
			private readonly DocValuesProducer @in;

			private readonly int maxDoc;

			internal AssertingDocValuesProducer(DocValuesProducer @in, int maxDoc)
			{
				// ok
				this.@in = @in;
				this.maxDoc = maxDoc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override NumericDocValues GetNumeric(FieldInfo field)
			{
				//HM:revisit 
				//assert field.getDocValuesType() == FieldInfo.DocValuesType.NUMERIC || field.getNormType() == FieldInfo.DocValuesType.NUMERIC;
				NumericDocValues values = @in.GetNumeric(field);
				//HM:revisit 
				//assert values != null;
				return new AssertingAtomicReader.AssertingNumericDocValues(values, maxDoc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BinaryDocValues GetBinary(FieldInfo field)
			{
				//HM:revisit 
				//assert field.getDocValuesType() == FieldInfo.DocValuesType.BINARY;
				BinaryDocValues values = @in.GetBinary(field);
				//HM:revisit 
				//assert values != null;
				return new AssertingAtomicReader.AssertingBinaryDocValues(values, maxDoc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override SortedDocValues GetSorted(FieldInfo field)
			{
				//HM:revisit 
				//assert field.getDocValuesType() == FieldInfo.DocValuesType.SORTED;
				SortedDocValues values = @in.GetSorted(field);
				//HM:revisit 
				//assert values != null;
				return new AssertingAtomicReader.AssertingSortedDocValues(values, maxDoc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override SortedSetDocValues GetSortedSet(FieldInfo field)
			{
				//HM:revisit 
				//assert field.getDocValuesType() == FieldInfo.DocValuesType.SORTED_SET;
				SortedSetDocValues values = @in.GetSortedSet(field);
				//HM:revisit 
				//assert values != null;
				return new AssertingAtomicReader.AssertingSortedSetDocValues(values, maxDoc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Bits GetDocsWithField(FieldInfo field)
			{
				//HM:revisit 
				//assert field.getDocValuesType() != null;
				Bits bits = @in.GetDocsWithField(field);
				//HM:revisit 
				//assert bits != null;
				//HM:revisit 
				//assert bits.length() == maxDoc;
				return new AssertingAtomicReader.AssertingBits(bits);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				@in.Close();
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
	}
}
