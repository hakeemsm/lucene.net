
using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>
	/// Writer for
	/// <see cref="DirectDocValuesFormat">DirectDocValuesFormat</see>
	/// </summary>
	internal class DirectDocValuesConsumer : DocValuesConsumer
	{
		internal IndexOutput data;

		internal IndexOutput meta;

		internal readonly int maxDoc;

		/// <exception cref="System.IO.IOException"></exception>
		internal DirectDocValuesConsumer(SegmentWriteState state, string dataCodec, string
			 dataExtension, string metaCodec, string metaExtension)
		{
			maxDoc = state.segmentInfo.DocCount;
			bool success = false;
			try
			{
				string dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
					, dataExtension);
				data = state.directory.CreateOutput(dataName, state.context);
				CodecUtil.WriteHeader(data, dataCodec, DirectDocValuesProducer.VERSION_CURRENT);
				string metaName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
					, metaExtension);
				meta = state.directory.CreateOutput(metaName, state.context);
				CodecUtil.WriteHeader(meta, metaCodec, DirectDocValuesProducer.VERSION_CURRENT);
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

		
		public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
		{
			meta.WriteVInt(field.number);
			meta.WriteByte(DirectDocValuesProducer.NUMBER);
			AddNumericFieldValues(field, values);
		}

		/// <exception cref="System.IO.IOException"></exception>
        private void AddNumericFieldValues(FieldInfo field, IEnumerable<long?> values)
		{
			meta.WriteLong(data.FilePointer);
			long minValue = long.MaxValue;
			long maxValue = long.MinValue;
			bool missing = false;
			long count = 0;
			foreach (var nv in values)
			{
				if (nv!=null) 
				{
					var v = nv;
					minValue = Math.Min(minValue, v.Value);
					maxValue = Math.Max(maxValue, v.Value);
				}
				else
				{
					missing = true;
				}
				count++;
				if (count >= DirectDocValuesFormat.MAX_SORTED_SET_ORDS)
				{
					throw new ArgumentException("DocValuesField \"" + field.name + "\" is too large, must be <= "
						 + DirectDocValuesFormat.MAX_SORTED_SET_ORDS + " values/total ords");
				}
			}
			meta.WriteInt((int)count);
			if (missing)
			{
				long start = data.FilePointer;
				WriteMissingBitset(values);
				meta.WriteLong(start);
				meta.WriteLong(data.FilePointer - start);
			}
			else
			{
				meta.WriteLong(-1L);
			}
			byte byteWidth;
			if (minValue >= byte.MinValue && maxValue <= byte.MaxValue)
			{
				byteWidth = 1;
			}
			else
			{
				if (minValue >= short.MinValue && maxValue <= short.MaxValue)
				{
					byteWidth = 2;
				}
				else
				{
					if (minValue >= int.MinValue && maxValue <= int.MaxValue)
					{
						byteWidth = 4;
					}
					else
					{
						byteWidth = 8;
					}
				}
			}
			meta.WriteByte(byteWidth);
			foreach (var nv in values)
			{
			    long v = nv.HasValue ? nv.Value : 0;
			    switch (byteWidth)
				{
					case 1:
					{
						data.WriteByte(unchecked((byte)v));
						break;
					}

					case 2:
					{
						data.WriteShort((short)v);
						break;
					}

					case 4:
					{
						data.WriteInt((int)v);
						break;
					}

					case 8:
					{
						data.WriteLong(v);
						break;
					}
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
					// write EOF marker
					CodecUtil.WriteFooter(meta);
				}
				// write checksum
				if (data != null)
				{
					CodecUtil.WriteFooter(data);
				}
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
				data = meta = null;
			}
		}

		
		public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
		{
			meta.WriteVInt(field.number);
			meta.WriteByte(DirectDocValuesProducer.BYTES);
			AddBinaryFieldValues(field, values);
		}

		
		private void AddBinaryFieldValues(FieldInfo field, IEnumerable<BytesRef> values)
		{
			// write the byte[] data
			long startFP = data.FilePointer;
			bool missing = false;
			long totalBytes = 0;
			int count = 0;
			foreach (BytesRef v in values)
			{
				if (v != null)
				{
					data.WriteBytes(v.bytes, v.offset, v.length);
					totalBytes += v.length;
					if (totalBytes > DirectDocValuesFormat.MAX_TOTAL_BYTES_LENGTH)
					{
						throw new ArgumentException("DocValuesField \"" + field.name + "\" is too large, cannot have more than DirectDocValuesFormat.MAX_TOTAL_BYTES_LENGTH ("
							 + DirectDocValuesFormat.MAX_TOTAL_BYTES_LENGTH + ") bytes");
					}
				}
				else
				{
					missing = true;
				}
				count++;
			}
			meta.WriteLong(startFP);
			meta.WriteInt((int)totalBytes);
			meta.WriteInt(count);
			if (missing)
			{
				long start = data.FilePointer;
				WriteMissingBitset(values);
				meta.WriteLong(start);
				meta.WriteLong(data.FilePointer - start);
			}
			else
			{
				meta.WriteLong(-1L);
			}
			int addr = 0;
			foreach (BytesRef v_1 in values)
			{
				data.WriteInt(addr);
				if (v_1 != null)
				{
					addr += v_1.length;
				}
			}
			data.WriteInt(addr);
		}

		// TODO: in some cases representing missing with minValue-1 wouldn't take up additional space and so on,
		// but this is very simple, and algorithms only check this for values of 0 anyway (doesnt slow down normal decode)
		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void WriteMissingBitset<T>(IEnumerable<T> values)
		{
			long bits = 0;
			int count = 0;
			foreach (object v in values)
			{
				if (count == 64)
				{
					data.WriteLong(bits);
					count = 0;
					bits = 0;
				}
				if (v != null)
				{
					bits |= 1L << (count & unchecked((int)(0x3f)));
				}
				count++;
			}
			if (count > 0)
			{
				data.WriteLong(bits);
			}
		}

		
		public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int?> docToOrd)
		{
			meta.WriteVInt(field.number);
			meta.WriteByte(DirectDocValuesProducer.SORTED);
			// write the ordinals as numerics
			AddNumericFieldValues(field, docToOrd.ToList().ConvertAll(i=>(long?)i));
			// write the values as binary
			AddBinaryFieldValues(field, values);
		}

		// note: this might not be the most efficient... but its fairly simple
		
		public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int?> docToOrdCount, IEnumerable<long?> ords)
		{
			meta.WriteVInt(field.number);
			meta.WriteByte(DirectDocValuesProducer.SORTED_SET);
			// First write docToOrdCounts, except we "aggregate" the
			// counts so they turn into addresses, and add a final
			// value = the total aggregate:
			AddNumericFieldValues(field, GetNumericIterator(docToOrdCount));
			// Just aggregates the count values so they become
			// "addresses", and adds one more value in the end
			// (the final sum):
			//HM:revisit 
			//assert false;
			// Write ordinals for all docs, appended into one big
			// numerics:
			AddNumericFieldValues(field, ords);
			// write the values as binary
			AddBinaryFieldValues(field, values);
		}

        private IEnumerable<long?> GetNumericIterator(IEnumerable<int?> intList)
        {
            // .NET Port: using yield return instead of custom iterator type. Much less code.

            long? sum = 0;
            long? retVal;
            foreach (var i in intList)
            {
                retVal = sum;
                sum += i;
                yield return retVal;
            }
        }
	}
}
