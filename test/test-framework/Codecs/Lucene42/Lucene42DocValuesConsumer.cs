using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Codecs.TestFramework;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Codecs.Lucene42;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Lucene42.TestFramework.TestFramework
{
	/// <summary>
	/// Writer for
	/// <see cref="Lucene42DocValuesFormat">Lucene42DocValuesFormat</see>
	/// </summary>
	internal class Lucene42DocValuesConsumer : DocValuesConsumer
	{
		internal readonly IndexOutput data;

		internal readonly IndexOutput meta;

		internal readonly int maxDoc;

		internal readonly float acceptableOverheadRatio;

		/// <exception cref="System.IO.IOException"></exception>
		internal Lucene42DocValuesConsumer(SegmentWriteState state, string dataCodec, string
			 dataExtension, string metaCodec, string metaExtension, float acceptableOverheadRatio
			)
		{
			this.acceptableOverheadRatio = acceptableOverheadRatio;
			maxDoc = state.segmentInfo.DocCount;
			bool success = false;
			try
			{
				string dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
					, dataExtension);
				data = state.directory.CreateOutput(dataName, state.context);
				// this writer writes the format 4.2 did!
				CodecUtil.WriteHeader(data, dataCodec, Lucene42DocValuesProducer.VERSION_GCD_COMPRESSION
					);
				string metaName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
					, metaExtension);
				meta = state.directory.CreateOutput(metaName, state.context);
				CodecUtil.WriteHeader(meta, metaCodec, Lucene42DocValuesProducer.VERSION_GCD_COMPRESSION
					);
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException((IDisposable)this);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddNumericField(FieldInfo field, IEnumerable<long> values)
		{
			AddNumericField(field, values, true);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void AddNumericField(FieldInfo field, IEnumerable<long> values, bool optimizeStorage)
		{
			meta.WriteVInt(field.number);
			meta.WriteByte(Lucene42DocValuesProducer.NUMBER);
			meta.WriteLong(data.FilePointer);
			long minValue = long.MaxValue;
			long maxValue = long.MinValue;
			long gcd = 0;
			// TODO: more efficient?
			HashSet<long> uniqueValues = null;
		    var longList = values as IList<long> ?? values.ToList();
		    if (optimizeStorage)
			{
				uniqueValues = new HashSet<long>();
				long count = 0;
				foreach (var nv in longList)
				{
					// TODO: support this as MemoryDVFormat (and be smart about missing maybe)
					long v = nv == null ? 0 : nv;
					if (gcd != 1)
					{
						if (v < long.MinValue / 2 || v > long.MaxValue / 2)
						{
							// in that case v - minValue might overflow and make the GCD computation return
							// wrong results. Since these extreme values are unlikely, we just discard
							// GCD computation for them
							gcd = 1;
						}
						else
						{
							if (count != 0)
							{
								// minValue needs to be set first
								gcd = MathUtil.Gcd(gcd, v - minValue);
							}
						}
					}
					minValue = Math.Min(minValue, v);
					maxValue = Math.Max(maxValue, v);
					if (uniqueValues != null)
					{
						if (uniqueValues.Add(v))
						{
							if (uniqueValues.Count > 256)
							{
								uniqueValues = null;
							}
						}
					}
					++count;
				}
			}
			 
			//assert count == maxDoc;
			if (uniqueValues != null)
			{
				// small number of unique values
				int bitsPerValue = PackedInts.BitsRequired(uniqueValues.Count - 1);
				PackedInts.FormatAndBits formatAndBits = PackedInts.FastestFormatAndBits(maxDoc, 
					bitsPerValue, acceptableOverheadRatio);
				if (formatAndBits.bitsPerValue == 8 && minValue >= byte.MinValue && maxValue <= byte.MaxValue)
				{
					meta.WriteByte(Lucene42DocValuesProducer.UNCOMPRESSED);
					// uncompressed
					foreach (var nv in longList)
					{
						data.WriteLong(nv == null ? 0 : ((byte)nv));
					}
				}
				else
				{
					meta.WriteByte(Lucene42DocValuesProducer.TABLE_COMPRESSED);
					// table-compressed
                    long[] decode = uniqueValues.ToArray();
					Dictionary<long, int> encode = new Dictionary<long, int>();
					data.WriteVInt(decode.Length);
					for (int i = 0; i < decode.Length; i++)
					{
						data.WriteLong(decode[i]);
						encode[decode[i]] = i;
					}
					meta.WriteVInt(PackedInts.VERSION_CURRENT);
					data.WriteVInt(formatAndBits.format.GetId());
					data.WriteVInt(formatAndBits.bitsPerValue);
					PackedInts.Writer writer = PackedInts.GetWriterNoHeader(data, formatAndBits.format
						, maxDoc, formatAndBits.bitsPerValue, PackedInts.DEFAULT_BUFFER_SIZE);
					foreach (var nv in longList)
					{
						writer.Add(encode[nv == null ? 0 : nv]);
					}
					writer.Finish();
				}
			}
			else
			{
				if (gcd != 0 && gcd != 1)
				{
					meta.WriteByte(Lucene42DocValuesProducer.GCD_COMPRESSED);
					meta.WriteVInt(PackedInts.VERSION_CURRENT);
					data.WriteLong(minValue);
					data.WriteLong(gcd);
					data.WriteVInt(Lucene42DocValuesProducer.BLOCK_SIZE);
					BlockPackedWriter writer = new BlockPackedWriter(data, Lucene42DocValuesProducer.
						BLOCK_SIZE);
					foreach (var nv in longList)
					{
						long value = nv == null ? 0 : nv;
						writer.Add((value - minValue) / gcd);
					}
					writer.Finish();
				}
				else
				{
					meta.WriteByte(Lucene42DocValuesProducer.DELTA_COMPRESSED);
					// delta-compressed
					meta.WriteVInt(PackedInts.VERSION_CURRENT);
					data.WriteVInt(Lucene42DocValuesProducer.BLOCK_SIZE);
					BlockPackedWriter writer = new BlockPackedWriter(data, Lucene42DocValuesProducer.
						BLOCK_SIZE);
					foreach (var nv in longList)
					{
						writer.Add(nv == null ? 0 : nv);
					}
					writer.Finish();
				}
			}
		}


	    protected override void Dispose(bool disposing)
		{
			bool success = false;
			try
			{
				if (meta != null)
				{
					meta.WriteVInt(-1);
				}
				// write EOF marker
				success = true;
			}
			finally
			{
				if (success)
				{
					IOUtils.Close(data, meta);
				}
				else
				{
					IOUtils.CloseWhileHandlingException((IDisposable)data, meta);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
		{
			// write the byte[] data
			meta.WriteVInt(field.number);
			meta.WriteByte(Lucene42DocValuesProducer.BYTES);
			int minLength = int.MaxValue;
			int maxLength = int.MinValue;
			long startFP = data.FilePointer;
			foreach (BytesRef v in values)
			{
				int length = v == null ? 0 : v.length;
				if (length > Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH)
				{
					throw new ArgumentException("DocValuesField \"" + field.name + "\" is too large, must be <= "
						 + Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH);
				}
				minLength = Math.Min(minLength, length);
				maxLength = Math.Max(maxLength, length);
				if (v != null)
				{
					data.WriteBytes(v.bytes, v.offset, v.length);
				}
			}
			meta.WriteLong(startFP);
			meta.WriteLong(data.FilePointer - startFP);
			meta.WriteVInt(minLength);
			meta.WriteVInt(maxLength);
			// if minLength == maxLength, its a fixed-length byte[], we are done (the addresses are implicit)
			// otherwise, we need to record the length fields...
			if (minLength != maxLength)
			{
				meta.WriteVInt(PackedInts.VERSION_CURRENT);
				meta.WriteVInt(Lucene42DocValuesProducer.BLOCK_SIZE);
				MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(data, Lucene42DocValuesProducer
					.BLOCK_SIZE);
				long addr = 0;
				foreach (BytesRef v_1 in values)
				{
					if (v_1 != null)
					{
						addr += v_1.length;
					}
					writer.Add(addr);
				}
				writer.Finish();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteFST(FieldInfo field, IEnumerable<BytesRef> values)
		{
			meta.WriteVInt(field.number);
			meta.WriteByte(Lucene42DocValuesProducer.FST);
			meta.WriteLong(data.FilePointer);
			PositiveIntOutputs outputs = PositiveIntOutputs.GetSingleton();
			Builder<long> builder = new Builder<long>(FST.INPUT_TYPE.BYTE1, outputs);
			IntsRef scratch = new IntsRef();
			long ord = 0;
			foreach (BytesRef v in values)
			{
				builder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(v, scratch), ord);
				ord++;
			}
			FST<long> fst = builder.Finish();
			if (fst != null)
			{
				fst.Save(data);
			}
			meta.WriteVLong(ord);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable
			<int> docToOrd)
		{
			// three cases for simulating the old writer:
			// 1. no missing
			// 2. missing (and empty string in use): remap ord=-1 -> ord=0
			// 3. missing (and empty string not in use): remap all ords +1, insert empty string into values
			bool anyMissing = docToOrd.Any(n => n == -1);
		    bool hasEmptyString = values.Select(b => b.length == 0).FirstOrDefault();
		    if (!anyMissing)
			{
			}
			else
			{
				// nothing to do
				if (hasEmptyString)
				{
					docToOrd = MissingOrdRemapper.MapMissingToOrd0(docToOrd);
				}
				else
				{
					docToOrd = MissingOrdRemapper.MapAllOrds(docToOrd);
					values = MissingOrdRemapper.InsertEmptyValue(values);
				}
			}
			// write the ordinals as numerics
			AddNumericField(field, docToOrd, false);
			// write the values as FST
			WriteFST(field, values);
		}

		// note: this might not be the most efficient... but its fairly simple
		/// <exception cref="System.IO.IOException"></exception>
		public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values
			, IEnumerable<int> docToOrdCount, IEnumerable<long> ords)
		{
			// write the ordinals as a binary field
			
            AddBinaryField(field, new AnonBytesRefEnumerable(docToOrdCount, ords));
			// write the values as FST
			WriteFST(field, values);
		}

	    

	    private sealed class AnonBytesRefEnumerable : IEnumerable<BytesRef>
		{
			public AnonBytesRefEnumerable(IEnumerable<int> docToOrdCount, IEnumerable<long> ords)
			{
				this.docToOrdCount = docToOrdCount;
				this.ords = ords;
			}

			public IEnumerator<BytesRef> GetEnumerator()
			{
				return new SortedSetIterator(docToOrdCount.GetEnumerator(), ords.GetEnumerator());
			}

			private readonly IEnumerable<int> docToOrdCount;

			private readonly IEnumerable<long> ords;
	        IEnumerator IEnumerable.GetEnumerator()
	        {
	            return GetEnumerator();
	        }
		}

		internal class SortedSetIterator : IEnumerator<BytesRef>
		{
			internal sbyte[] buffer = new sbyte[10];

			internal ByteArrayDataOutput @out = new ByteArrayDataOutput();

			internal BytesRef @ref = new BytesRef();

			internal readonly IEnumerator<int> counts;

			internal readonly IEnumerator<long> ords;

			internal SortedSetIterator(IEnumerator<int> counts, IEnumerator<long> ords)
			{
				// per-document vint-encoded byte[]
				this.counts = counts;
				this.ords = ords;
			}

			public bool MoveNext()
			{
				return counts.MoveNext();
			}

		    public void Reset()
		    {
		        throw new NotImplementedException();
		    }

		    object IEnumerator.Current
		    {
		        get { return Current; }
		    }

		    public BytesRef Current
			{
			    get
			    {
			        if (!MoveNext())
			        {
			            throw new ArgumentOutOfRangeException();
			        }
			        int count = counts.Current;
			        int maxSize = count*9;
			        // worst case
			        if (maxSize > buffer.Length)
			        {
			            buffer = ArrayUtil.Grow(buffer, maxSize);
			        }
			        EncodeValues(count);
			        @ref.bytes = buffer;
			        @ref.offset = 0;
			        @ref.length = @out.Position;
			        return @ref;
			    }
			}

			// encodes count values to buffer
			/// <exception cref="System.IO.IOException"></exception>
			private void EncodeValues(int count)
			{
			    var bytes = Array.ConvertAll(buffer, Convert.ToByte);
			    @out.Reset(bytes);
				long lastOrd = 0;
				for (int i = 0; i < count; i++)
				{
					long ord = ords.Current;
					@out.WriteVLong(ord - lastOrd);
					lastOrd = ord;
				}
			}

			public void Remove()
			{
				throw new NotSupportedException();
			}

		    public void Dispose()
		    {
		        ords.Dispose();
                counts.Dispose();
		    }
		}
	}
}
