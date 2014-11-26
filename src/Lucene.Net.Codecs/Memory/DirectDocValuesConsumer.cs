
using System;
using Lucene.Net.Index;
using Lucene.Net.Store;
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
					IOUtils.CloseWhileHandlingException(this);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddNumericField(FieldInfo field, Iterable<Number> values)
		{
			meta.WriteVInt(field.number);
			meta.WriteByte(DirectDocValuesProducer.NUMBER);
			AddNumericFieldValues(field, values);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddNumericFieldValues(FieldInfo field, Iterable<Number> values)
		{
			meta.WriteLong(data.GetFilePointer());
			long minValue = long.MaxValue;
			long maxValue = long.MinValue;
			bool missing = false;
			long count = 0;
			foreach (Number nv in values)
			{
				if (nv != null)
				{
					long v = nv;
					minValue = Math.Min(minValue, v);
					maxValue = Math.Max(maxValue, v);
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
				long start = data.GetFilePointer();
				WriteMissingBitset(values);
				meta.WriteLong(start);
				meta.WriteLong(data.GetFilePointer() - start);
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
			foreach (Number nv_1 in values)
			{
				long v;
				if (nv_1 != null)
				{
					v = nv_1;
				}
				else
				{
					v = 0;
				}
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

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
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
					IOUtils.CloseWhileHandlingException(data, meta);
				}
				data = meta = null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddBinaryField(FieldInfo field, Iterable<BytesRef> values)
		{
			meta.WriteVInt(field.number);
			meta.WriteByte(DirectDocValuesProducer.BYTES);
			AddBinaryFieldValues(field, values);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddBinaryFieldValues(FieldInfo field, Iterable<BytesRef> values)
		{
			// write the byte[] data
			long startFP = data.GetFilePointer();
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
				long start = data.GetFilePointer();
				WriteMissingBitset(values);
				meta.WriteLong(start);
				meta.WriteLong(data.GetFilePointer() - start);
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
		internal virtual void WriteMissingBitset<_T0>(Iterable<_T0> values)
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

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddSortedField(FieldInfo field, Iterable<BytesRef> values, Iterable
			<Number> docToOrd)
		{
			meta.WriteVInt(field.number);
			meta.WriteByte(DirectDocValuesProducer.SORTED);
			// write the ordinals as numerics
			AddNumericFieldValues(field, docToOrd);
			// write the values as binary
			AddBinaryFieldValues(field, values);
		}

		// note: this might not be the most efficient... but its fairly simple
		/// <exception cref="System.IO.IOException"></exception>
		public override void AddSortedSetField(FieldInfo field, Iterable<BytesRef> values
			, Iterable<Number> docToOrdCount, Iterable<Number> ords)
		{
			meta.WriteVInt(field.number);
			meta.WriteByte(DirectDocValuesProducer.SORTED_SET);
			// First write docToOrdCounts, except we "aggregate" the
			// counts so they turn into addresses, and add a final
			// value = the total aggregate:
			AddNumericFieldValues(field, new _Iterable_251(docToOrdCount));
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

		private sealed class _Iterable_251 : Iterable<Number>
		{
			public _Iterable_251(Iterable<Number> docToOrdCount)
			{
				this.docToOrdCount = docToOrdCount;
			}

			public override Iterator<Number> Iterator()
			{
				Iterator<Number> iter = docToOrdCount.Iterator();
				return new _Iterator_261(iter);
			}

			private sealed class _Iterator_261 : Iterator<Number>
			{
				public _Iterator_261(Iterator<Number> iter)
				{
					this.iter = iter;
				}

				internal long sum;

				internal bool ended;

				public override bool HasNext()
				{
					return iter.HasNext() || !this.ended;
				}

				public override Number Next()
				{
					long toReturn = this.sum;
					if (iter.HasNext())
					{
						Number n = iter.Next();
						if (n != null)
						{
							this.sum += n;
						}
					}
					else
					{
						if (!this.ended)
						{
							this.ended = true;
						}
					}
					return toReturn;
				}

				public override void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly Iterator<Number> iter;
			}

			private readonly Iterable<Number> docToOrdCount;
		}
	}
}
