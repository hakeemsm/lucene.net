using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Asserting.TestFramework
{
	/// <summary>
	/// Just like
	/// <see cref="Lucene.Net.Codecs.Lucene45.Lucene45DocValuesFormat">Lucene.Net.Codecs.Lucene45.Lucene45DocValuesFormat
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
			 
			//assert consumer != null;
			return new AssertingDocValuesConsumer(consumer, state.segmentInfo.DocCount);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesProducer FieldsProducer(SegmentReadState state)
		{
			 
			//assert state.fieldInfos.hasDocValues();
			DocValuesProducer producer = @in.FieldsProducer(state);
			 
			//assert producer != null;
			return new AssertingDocValuesProducer(producer, state.segmentInfo.DocCount);
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
			public override void AddNumericField(FieldInfo field, IEnumerable<long> values)
			{
			    var longList = values as IList<long> ?? values.ToList();
			    int count = longList.Count();

			    Debug.Assert(count == maxDoc);
				CheckIterator(longList.GetEnumerator(), maxDoc, true);
				@in.AddNumericField(field, longList);
			}

			
			public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
			{
				int count = 0;
				foreach (BytesRef b in values)
				{
					 
					//Debug.Assert(b == null || b.IsValid);
					count++;
				}
				 
				//assert count == maxDoc;
				CheckIterator(values.GetEnumerator(), maxDoc, true);
				@in.AddBinaryField(field, values);
			}

			
			public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrd)
			{
				int valueCount = 0;
				BytesRef lastValue = null;
				foreach (BytesRef b in values)
				{
                    //assert b != null;
					 
					//assert b.isValid();
					if (valueCount > 0)
					{
                        //assert b.compareTo(lastValue) > 0;
					}
					lastValue = BytesRef.DeepCopyOf(b);
					valueCount++;
				}
				 
				//assert valueCount <= maxDoc;
				FixedBitSet seenOrds = new FixedBitSet(valueCount);
				int count = 0;
				foreach (var v in docToOrd)
				{
					 
					//assert v != null;
					int ord = v;
					 
					//assert ord >= -1 && ord < valueCount;
					if (ord >= 0)
					{
						seenOrds.Set(ord);
					}
					count++;
				}
				 
				//assert count == maxDoc;
				 
				//assert seenOrds.cardinality() == valueCount;
				CheckIterator(values.GetEnumerator(), valueCount, false);
				CheckIterator(docToOrd.GetEnumerator(), maxDoc, false);
				@in.AddSortedField(field, values, docToOrd);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrdCount, IEnumerable<long> ords)
			{
				long valueCount = 0;
				BytesRef lastValue = null;
				foreach (BytesRef b in values)
				{
					 
					//assert b != null;
					 
					//assert b.isValid();
				    if (valueCount > 0)
				    {
				        //assert b.compareTo(lastValue) > 0;
				    }
				    lastValue = BytesRef.DeepCopyOf(b);
					valueCount++;
				}
				int docCount = 0;
				long ordCount = 0;
				LongBitSet seenOrds = new LongBitSet(valueCount);
				IEnumerator<long> ordIterator = ords.GetEnumerator();
				foreach (var v in docToOrdCount)
				{
					//assert v != null;
					int count = v;
					 
					//assert count >= 0;
					docCount++;
					ordCount += count;
					long lastOrd = -1;
					for (int i = 0; i < count; i++)
					{
						ordIterator.MoveNext();
					    long o = ordIterator.Current;
					    //assert o != null;
						long ord = o;
						 
						//assert ord >= 0 && ord < valueCount;
						 
						//assert ord > lastOrd : "ord=" + ord + ",lastOrd=" + lastOrd;
						seenOrds.Set(ord);
						lastOrd = ord;
					}
				}
				 
				//assert ordIterator.hasNext() == false;
				 
				//assert docCount == maxDoc;
				 
				//assert seenOrds.cardinality() == valueCount;
				CheckIterator(values.GetEnumerator(), valueCount, false);
				CheckIterator(docToOrdCount.GetEnumerator(), maxDoc, false);
				CheckIterator(ords.GetEnumerator(), ordCount, false);
				@in.AddSortedSetField(field, values, docToOrdCount, ords);
			}


		    protected override void Dispose(bool disposing)
			{
				@in.Dispose();
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

			
			public override void AddNumericField(FieldInfo field, IEnumerable<long> values)
			{
				int count = 0;
				foreach (var v in values)
				{
					 
					//assert v != null;
					count++;
				}
				 
				//assert count == maxDoc;
				CheckIterator(values.GetEnumerator(), maxDoc, false);
				@in.AddNumericField(field, values);
			}


		    protected override void Dispose(bool disposing)
			{
				@in.Dispose();
			}

			
			public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
			{
				throw new InvalidOperationException();
			}

			
			public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrd)
			{
				throw new InvalidOperationException();
			}

			
			public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrdCount, IEnumerable<long> ords)
			{
				throw new InvalidOperationException();
			}
		}

		private static void CheckIterator<T>(IEnumerator<T> iterator, long expectedSize, bool allowNull)
		{
			for (long i = 0; i < expectedSize; i++)
			{
				bool hasNext = iterator.MoveNext();
				 
				Debug.Assert(hasNext);
				T v = iterator.Current;
				 
				Debug.Assert(allowNull || v != null);
                //try
                //{
                //    iterator.Remove();
                //    throw new Exception("broken iterator (supports remove): " + iterator);
                //}
                //catch (NotSupportedException)
                //{
                //}
			}
			// ok
			 
			 Debug.Assert(!iterator.MoveNext());
			try
			{
			    T current = iterator.Current;
			    throw new Exception("broken enumerator (allows Current when MoveNext==false) " + iterator);
			}
			catch (ArgumentOutOfRangeException)
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
				 
				//assert field.getDocValuesType() == FieldInfo.DocValuesType.NUMERIC || field.getNormType() == FieldInfo.DocValuesType.NUMERIC;
				NumericDocValues values = @in.GetNumeric(field);
				 
				//assert values != null;
				return new AssertingAtomicReader.AssertingNumericDocValues(values, maxDoc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BinaryDocValues GetBinary(FieldInfo field)
			{
				 
				//assert field.getDocValuesType() == FieldInfo.DocValuesType.BINARY;
				BinaryDocValues values = @in.GetBinary(field);
				 
				//assert values != null;
				return new AssertingAtomicReader.AssertingBinaryDocValues(values, maxDoc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override SortedDocValues GetSorted(FieldInfo field)
			{
				 
				//assert field.getDocValuesType() == FieldInfo.DocValuesType.SORTED;
				SortedDocValues values = @in.GetSorted(field);
				 
				//assert values != null;
				return new AssertingAtomicReader.AssertingSortedDocValues(values, maxDoc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override SortedSetDocValues GetSortedSet(FieldInfo field)
			{
				 
				//assert field.getDocValuesType() == FieldInfo.DocValuesType.SORTED_SET;
				SortedSetDocValues values = @in.GetSortedSet(field);
				 
				//assert values != null;
				return new AssertingAtomicReader.AssertingSortedSetDocValues(values, maxDoc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IBits GetDocsWithField(FieldInfo field)
			{
				 
				//assert field.getDocValuesType() != null;
				IBits bits = @in.GetDocsWithField(field);
				 
				//assert bits != null;
				 
				//assert bits.length() == maxDoc;
				return new AssertingAtomicReader.AssertingBits(bits);
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void Dispose(bool disposing)
			{
				@in.Dispose();
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
	}
}
