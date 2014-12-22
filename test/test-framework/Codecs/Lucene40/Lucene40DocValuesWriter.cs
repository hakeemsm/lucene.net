/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene40;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Org.Apache.Lucene.Util.Packed;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene40
{
	internal class Lucene40DocValuesWriter : DocValuesConsumer
	{
		private readonly Directory dir;

		private readonly SegmentWriteState state;

		private readonly string legacyKey;

		private static readonly string segmentSuffix = "dv";

		/// <exception cref="System.IO.IOException"></exception>
		internal Lucene40DocValuesWriter(SegmentWriteState state, string filename, string
			 legacyKey)
		{
			// note: intentionally ignores seg suffix
			this.state = state;
			this.legacyKey = legacyKey;
			this.dir = new CompoundFileDirectory(state.directory, filename, state.context, true
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddNumericField(FieldInfo field, Iterable<Number> values)
		{
			// examine the values to determine best type to use
			long minValue = long.MaxValue;
			long maxValue = long.MinValue;
			foreach (Number n in values)
			{
				long v = n == null ? 0 : n;
				minValue = Math.Min(minValue, v);
				maxValue = Math.Max(maxValue, v);
			}
			string fileName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + Sharpen.Extensions.ToString
				(field.number), segmentSuffix, "dat");
			IndexOutput data = dir.CreateOutput(fileName, state.context);
			bool success = false;
			try
			{
				if (minValue >= byte.MinValue && maxValue <= byte.MaxValue && PackedInts.BitsRequired
					(maxValue - minValue) > 4)
				{
					// fits in a byte[], would be more than 4bpv, just write byte[]
					AddBytesField(field, data, values);
				}
				else
				{
					if (minValue >= short.MinValue && maxValue <= short.MaxValue && PackedInts.BitsRequired
						(maxValue - minValue) > 8)
					{
						// fits in a short[], would be more than 8bpv, just write short[]
						AddShortsField(field, data, values);
					}
					else
					{
						if (minValue >= int.MinValue && maxValue <= int.MaxValue && PackedInts.BitsRequired
							(maxValue - minValue) > 16)
						{
							// fits in a int[], would be more than 16bpv, just write int[]
							AddIntsField(field, data, values);
						}
						else
						{
							AddVarIntsField(field, data, values, minValue, maxValue);
						}
					}
				}
				success = true;
			}
			finally
			{
				if (success)
				{
					IOUtils.Close(data);
				}
				else
				{
					IOUtils.CloseWhileHandlingException(data);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddBytesField(FieldInfo field, IndexOutput output, Iterable<Number> 
			values)
		{
			field.PutAttribute(legacyKey, Lucene40FieldInfosReader.LegacyDocValuesType.FIXED_INTS_8
				.ToString());
			CodecUtil.WriteHeader(output, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat
				.INTS_VERSION_CURRENT);
			output.WriteInt(1);
			// size
			foreach (Number n in values)
			{
				output.WriteByte(n == null ? 0 : n);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddShortsField(FieldInfo field, IndexOutput output, Iterable<Number>
			 values)
		{
			field.PutAttribute(legacyKey, Lucene40FieldInfosReader.LegacyDocValuesType.FIXED_INTS_16
				.ToString());
			CodecUtil.WriteHeader(output, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat
				.INTS_VERSION_CURRENT);
			output.WriteInt(2);
			// size
			foreach (Number n in values)
			{
				output.WriteShort(n == null ? 0 : n);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddIntsField(FieldInfo field, IndexOutput output, Iterable<Number> values
			)
		{
			field.PutAttribute(legacyKey, Lucene40FieldInfosReader.LegacyDocValuesType.FIXED_INTS_32
				.ToString());
			CodecUtil.WriteHeader(output, Lucene40DocValuesFormat.INTS_CODEC_NAME, Lucene40DocValuesFormat
				.INTS_VERSION_CURRENT);
			output.WriteInt(4);
			// size
			foreach (Number n in values)
			{
				output.WriteInt(n == null ? 0 : n);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddVarIntsField(FieldInfo field, IndexOutput output, Iterable<Number
			> values, long minValue, long maxValue)
		{
			field.PutAttribute(legacyKey, Lucene40FieldInfosReader.LegacyDocValuesType.VAR_INTS
				.ToString());
			CodecUtil.WriteHeader(output, Lucene40DocValuesFormat.VAR_INTS_CODEC_NAME, Lucene40DocValuesFormat
				.VAR_INTS_VERSION_CURRENT);
			long delta = maxValue - minValue;
			if (delta < 0)
			{
				// writes longs
				output.WriteByte(Lucene40DocValuesFormat.VAR_INTS_FIXED_64);
				foreach (Number n in values)
				{
					output.WriteLong(n == null ? 0 : n);
				}
			}
			else
			{
				// writes packed ints
				output.WriteByte(Lucene40DocValuesFormat.VAR_INTS_PACKED);
				output.WriteLong(minValue);
				output.WriteLong(0 - minValue);
				// default value (representation of 0)
				PackedInts.Writer writer = PackedInts.GetWriter(output, state.segmentInfo.GetDocCount
					(), PackedInts.BitsRequired(delta), PackedInts.DEFAULT);
				foreach (Number n in values)
				{
					long v = n == null ? 0 : n;
					writer.Add(v - minValue);
				}
				writer.Finish();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddBinaryField(FieldInfo field, Iterable<BytesRef> values)
		{
			// examine the values to determine best type to use
			HashSet<BytesRef> uniqueValues = new HashSet<BytesRef>();
			int minLength = int.MaxValue;
			int maxLength = int.MinValue;
			foreach (BytesRef b in values)
			{
				if (b == null)
				{
					b = new BytesRef();
				}
				// 4.0 doesnt distinguish
				if (b.length > Lucene40DocValuesFormat.MAX_BINARY_FIELD_LENGTH)
				{
					throw new ArgumentException("DocValuesField \"" + field.name + "\" is too large, must be <= "
						 + Lucene40DocValuesFormat.MAX_BINARY_FIELD_LENGTH);
				}
				minLength = Math.Min(minLength, b.length);
				maxLength = Math.Max(maxLength, b.length);
				if (uniqueValues != null)
				{
					if (uniqueValues.AddItem(BytesRef.DeepCopyOf(b)))
					{
						if (uniqueValues.Count > 256)
						{
							uniqueValues = null;
						}
					}
				}
			}
			int maxDoc = state.segmentInfo.GetDocCount();
			bool fixed = minLength == maxLength;
			bool dedup = uniqueValues != null && uniqueValues.Count * 2 < maxDoc;
			if (dedup)
			{
				// we will deduplicate and deref values
				bool success = false;
				IndexOutput data = null;
				IndexOutput index = null;
				string dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + Sharpen.Extensions.ToString
					(field.number), segmentSuffix, "dat");
				string indexName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + 
					Sharpen.Extensions.ToString(field.number), segmentSuffix, "idx");
				try
				{
					data = dir.CreateOutput(dataName, state.context);
					index = dir.CreateOutput(indexName, state.context);
					if (fixed)
					{
						AddFixedDerefBytesField(field, data, index, values, minLength);
					}
					else
					{
						AddVarDerefBytesField(field, data, index, values);
					}
					success = true;
				}
				finally
				{
					if (success)
					{
						IOUtils.Close(data, index);
					}
					else
					{
						IOUtils.CloseWhileHandlingException(data, index);
					}
				}
			}
			else
			{
				// we dont deduplicate, just write values straight
				if (fixed)
				{
					// fixed byte[]
					string fileName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + Sharpen.Extensions.ToString
						(field.number), segmentSuffix, "dat");
					IndexOutput data = dir.CreateOutput(fileName, state.context);
					bool success = false;
					try
					{
						AddFixedStraightBytesField(field, data, values, minLength);
						success = true;
					}
					finally
					{
						if (success)
						{
							IOUtils.Close(data);
						}
						else
						{
							IOUtils.CloseWhileHandlingException(data);
						}
					}
				}
				else
				{
					// variable byte[]
					bool success = false;
					IndexOutput data = null;
					IndexOutput index = null;
					string dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + Sharpen.Extensions.ToString
						(field.number), segmentSuffix, "dat");
					string indexName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + 
						Sharpen.Extensions.ToString(field.number), segmentSuffix, "idx");
					try
					{
						data = dir.CreateOutput(dataName, state.context);
						index = dir.CreateOutput(indexName, state.context);
						AddVarStraightBytesField(field, data, index, values);
						success = true;
					}
					finally
					{
						if (success)
						{
							IOUtils.Close(data, index);
						}
						else
						{
							IOUtils.CloseWhileHandlingException(data, index);
						}
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddFixedStraightBytesField(FieldInfo field, IndexOutput output, Iterable
			<BytesRef> values, int length)
		{
			field.PutAttribute(legacyKey, Lucene40FieldInfosReader.LegacyDocValuesType.BYTES_FIXED_STRAIGHT
				.ToString());
			CodecUtil.WriteHeader(output, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_CODEC_NAME
				, Lucene40DocValuesFormat.BYTES_FIXED_STRAIGHT_VERSION_CURRENT);
			output.WriteInt(length);
			foreach (BytesRef v in values)
			{
				if (v != null)
				{
					output.WriteBytes(v.bytes, v.offset, v.length);
				}
			}
		}

		// NOTE: 4.0 file format docs are crazy/wrong here...
		/// <exception cref="System.IO.IOException"></exception>
		private void AddVarStraightBytesField(FieldInfo field, IndexOutput data, IndexOutput
			 index, Iterable<BytesRef> values)
		{
			field.PutAttribute(legacyKey, Lucene40FieldInfosReader.LegacyDocValuesType.BYTES_VAR_STRAIGHT
				.ToString());
			CodecUtil.WriteHeader(data, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_DAT
				, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);
			CodecUtil.WriteHeader(index, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_CODEC_NAME_IDX
				, Lucene40DocValuesFormat.BYTES_VAR_STRAIGHT_VERSION_CURRENT);
			long startPos = data.GetFilePointer();
			foreach (BytesRef v in values)
			{
				if (v != null)
				{
					data.WriteBytes(v.bytes, v.offset, v.length);
				}
			}
			long maxAddress = data.GetFilePointer() - startPos;
			index.WriteVLong(maxAddress);
			int maxDoc = state.segmentInfo.GetDocCount();
			 
			//assert maxDoc != Integer.MAX_VALUE; // unsupported by the 4.0 impl
			PackedInts.Writer w = PackedInts.GetWriter(index, maxDoc + 1, PackedInts.BitsRequired
				(maxAddress), PackedInts.DEFAULT);
			long currentPosition = 0;
			foreach (BytesRef v_1 in values)
			{
				w.Add(currentPosition);
				if (v_1 != null)
				{
					currentPosition += v_1.length;
				}
			}
			// write sentinel
			 
			//assert currentPosition == maxAddress;
			w.Add(currentPosition);
			w.Finish();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddFixedDerefBytesField(FieldInfo field, IndexOutput data, IndexOutput
			 index, Iterable<BytesRef> values, int length)
		{
			field.PutAttribute(legacyKey, Lucene40FieldInfosReader.LegacyDocValuesType.BYTES_FIXED_DEREF
				.ToString());
			CodecUtil.WriteHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_DAT
				, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);
			CodecUtil.WriteHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_CODEC_NAME_IDX
				, Lucene40DocValuesFormat.BYTES_FIXED_DEREF_VERSION_CURRENT);
			// deduplicate
			TreeSet<BytesRef> dictionary = new TreeSet<BytesRef>();
			foreach (BytesRef v in values)
			{
				dictionary.AddItem(v == null ? new BytesRef() : BytesRef.DeepCopyOf(v));
			}
			data.WriteInt(length);
			foreach (BytesRef v_1 in dictionary)
			{
				data.WriteBytes(v_1.bytes, v_1.offset, v_1.length);
			}
			int valueCount = dictionary.Count;
			 
			//assert valueCount > 0;
			index.WriteInt(valueCount);
			int maxDoc = state.segmentInfo.GetDocCount();
			PackedInts.Writer w = PackedInts.GetWriter(index, maxDoc, PackedInts.BitsRequired
				(valueCount - 1), PackedInts.DEFAULT);
			foreach (BytesRef v_2 in values)
			{
				if (v_2 == null)
				{
					v_2 = new BytesRef();
				}
				int ord = dictionary.HeadSet(v_2).Count;
				w.Add(ord);
			}
			w.Finish();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddVarDerefBytesField(FieldInfo field, IndexOutput data, IndexOutput
			 index, Iterable<BytesRef> values)
		{
			field.PutAttribute(legacyKey, Lucene40FieldInfosReader.LegacyDocValuesType.BYTES_VAR_DEREF
				.ToString());
			CodecUtil.WriteHeader(data, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_DAT
				, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);
			CodecUtil.WriteHeader(index, Lucene40DocValuesFormat.BYTES_VAR_DEREF_CODEC_NAME_IDX
				, Lucene40DocValuesFormat.BYTES_VAR_DEREF_VERSION_CURRENT);
			// deduplicate
			TreeSet<BytesRef> dictionary = new TreeSet<BytesRef>();
			foreach (BytesRef v in values)
			{
				dictionary.AddItem(v == null ? new BytesRef() : BytesRef.DeepCopyOf(v));
			}
			long startPosition = data.GetFilePointer();
			long currentAddress = 0;
			Dictionary<BytesRef, long> valueToAddress = new Dictionary<BytesRef, long>();
			foreach (BytesRef v_1 in dictionary)
			{
				currentAddress = data.GetFilePointer() - startPosition;
				valueToAddress.Put(v_1, currentAddress);
				WriteVShort(data, v_1.length);
				data.WriteBytes(v_1.bytes, v_1.offset, v_1.length);
			}
			long totalBytes = data.GetFilePointer() - startPosition;
			index.WriteLong(totalBytes);
			int maxDoc = state.segmentInfo.GetDocCount();
			PackedInts.Writer w = PackedInts.GetWriter(index, maxDoc, PackedInts.BitsRequired
				(currentAddress), PackedInts.DEFAULT);
			foreach (BytesRef v_2 in values)
			{
				w.Add(valueToAddress.Get(v_2 == null ? new BytesRef() : v_2));
			}
			w.Finish();
		}

		// the little vint encoding used for var-deref
		/// <exception cref="System.IO.IOException"></exception>
		private static void WriteVShort(IndexOutput o, int i)
		{
			 
			//assert i >= 0 && i <= Short.MAX_VALUE;
			if (i < 128)
			{
				o.WriteByte(unchecked((byte)i));
			}
			else
			{
				o.WriteByte(unchecked((byte)(unchecked((int)(0x80)) | (i >> 8))));
				o.WriteByte(unchecked((byte)(i & unchecked((int)(0xff)))));
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddSortedField(FieldInfo field, Iterable<BytesRef> values, Iterable
			<Number> docToOrd)
		{
			// examine the values to determine best type to use
			int minLength = int.MaxValue;
			int maxLength = int.MinValue;
			foreach (BytesRef b in values)
			{
				minLength = Math.Min(minLength, b.length);
				maxLength = Math.Max(maxLength, b.length);
			}
			// but dont use fixed if there are missing values (we are simulating how lucene40 wrote dv...)
			bool anyMissing = false;
			foreach (Number n in docToOrd)
			{
				if (n == -1)
				{
					anyMissing = true;
					break;
				}
			}
			bool success = false;
			IndexOutput data = null;
			IndexOutput index = null;
			string dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + Sharpen.Extensions.ToString
				(field.number), segmentSuffix, "dat");
			string indexName = IndexFileNames.SegmentFileName(state.segmentInfo.name + "_" + 
				Sharpen.Extensions.ToString(field.number), segmentSuffix, "idx");
			try
			{
				data = dir.CreateOutput(dataName, state.context);
				index = dir.CreateOutput(indexName, state.context);
				if (minLength == maxLength && !anyMissing)
				{
					// fixed byte[]
					AddFixedSortedBytesField(field, data, index, values, docToOrd, minLength);
				}
				else
				{
					// var byte[]
					// three cases for simulating the old writer:
					// 1. no missing
					// 2. missing (and empty string in use): remap ord=-1 -> ord=0
					// 3. missing (and empty string not in use): remap all ords +1, insert empty string into values
					if (!anyMissing)
					{
						AddVarSortedBytesField(field, data, index, values, docToOrd);
					}
					else
					{
						if (minLength == 0)
						{
							AddVarSortedBytesField(field, data, index, values, MissingOrdRemapper.MapMissingToOrd0
								(docToOrd));
						}
						else
						{
							AddVarSortedBytesField(field, data, index, MissingOrdRemapper.InsertEmptyValue(values
								), MissingOrdRemapper.MapAllOrds(docToOrd));
						}
					}
				}
				success = true;
			}
			finally
			{
				if (success)
				{
					IOUtils.Close(data, index);
				}
				else
				{
					IOUtils.CloseWhileHandlingException(data, index);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddFixedSortedBytesField(FieldInfo field, IndexOutput data, IndexOutput
			 index, Iterable<BytesRef> values, Iterable<Number> docToOrd, int length)
		{
			field.PutAttribute(legacyKey, Lucene40FieldInfosReader.LegacyDocValuesType.BYTES_FIXED_SORTED
				.ToString());
			CodecUtil.WriteHeader(data, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_CODEC_NAME_DAT
				, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_CURRENT);
			CodecUtil.WriteHeader(index, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_CODEC_NAME_IDX
				, Lucene40DocValuesFormat.BYTES_FIXED_SORTED_VERSION_CURRENT);
			data.WriteInt(length);
			int valueCount = 0;
			foreach (BytesRef v in values)
			{
				data.WriteBytes(v.bytes, v.offset, v.length);
				valueCount++;
			}
			index.WriteInt(valueCount);
			int maxDoc = state.segmentInfo.GetDocCount();
			 
			//assert valueCount > 0;
			PackedInts.Writer w = PackedInts.GetWriter(index, maxDoc, PackedInts.BitsRequired
				(valueCount - 1), PackedInts.DEFAULT);
			foreach (Number n in docToOrd)
			{
				w.Add(n);
			}
			w.Finish();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddVarSortedBytesField(FieldInfo field, IndexOutput data, IndexOutput
			 index, Iterable<BytesRef> values, Iterable<Number> docToOrd)
		{
			field.PutAttribute(legacyKey, Lucene40FieldInfosReader.LegacyDocValuesType.BYTES_VAR_SORTED
				.ToString());
			CodecUtil.WriteHeader(data, Lucene40DocValuesFormat.BYTES_VAR_SORTED_CODEC_NAME_DAT
				, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_CURRENT);
			CodecUtil.WriteHeader(index, Lucene40DocValuesFormat.BYTES_VAR_SORTED_CODEC_NAME_IDX
				, Lucene40DocValuesFormat.BYTES_VAR_SORTED_VERSION_CURRENT);
			long startPos = data.GetFilePointer();
			int valueCount = 0;
			foreach (BytesRef v in values)
			{
				data.WriteBytes(v.bytes, v.offset, v.length);
				valueCount++;
			}
			long maxAddress = data.GetFilePointer() - startPos;
			index.WriteLong(maxAddress);
			 
			//assert valueCount != Integer.MAX_VALUE; // unsupported by the 4.0 impl
			PackedInts.Writer w = PackedInts.GetWriter(index, valueCount + 1, PackedInts.BitsRequired
				(maxAddress), PackedInts.DEFAULT);
			long currentPosition = 0;
			foreach (BytesRef v_1 in values)
			{
				w.Add(currentPosition);
				currentPosition += v_1.length;
			}
			// write sentinel
			 
			//assert currentPosition == maxAddress;
			w.Add(currentPosition);
			w.Finish();
			int maxDoc = state.segmentInfo.GetDocCount();
			 
			//assert valueCount > 0;
			PackedInts.Writer ords = PackedInts.GetWriter(index, maxDoc, PackedInts.BitsRequired
				(valueCount - 1), PackedInts.DEFAULT);
			foreach (Number n in docToOrd)
			{
				ords.Add(n);
			}
			ords.Finish();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddSortedSetField(FieldInfo field, Iterable<BytesRef> values
			, Iterable<Number> docToOrdCount, Iterable<Number> ords)
		{
			throw new NotSupportedException("Lucene 4.0 does not support SortedSet docvalues"
				);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			dir.Close();
		}
	}
}
