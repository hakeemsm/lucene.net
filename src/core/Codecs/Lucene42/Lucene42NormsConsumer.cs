using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Lucene42
{
	/// <summary>
	/// Writer for
	/// <see cref="Lucene42NormsFormat">Lucene42NormsFormat</see>
	/// </summary>
	internal class Lucene42NormsConsumer : DocValuesConsumer
	{
		internal const byte NUMBER = 0;

		internal const int BLOCK_SIZE = 4096;

		internal const byte DELTA_COMPRESSED = 0;

		internal const byte TABLE_COMPRESSED = 1;

		internal const byte UNCOMPRESSED = 2;

		internal const byte GCD_COMPRESSED = 3;

		internal IndexOutput data;

		internal IndexOutput meta;

		internal readonly int maxDoc;

		internal readonly float acceptableOverheadRatio;

		/// <exception cref="System.IO.IOException"></exception>
		internal Lucene42NormsConsumer(SegmentWriteState state, string dataCodec, string 
			dataExtension, string metaCodec, string metaExtension, float acceptableOverheadRatio)
		{
			this.acceptableOverheadRatio = acceptableOverheadRatio;
			maxDoc = state.segmentInfo.DocCount;
			bool success = false;
			try
			{
				string dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
					, dataExtension);
				data = state.directory.CreateOutput(dataName, state.context);
				CodecUtil.WriteHeader(data, dataCodec, Lucene42DocValuesProducer.VERSION_CURRENT);
				string metaName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
					, metaExtension);
				meta = state.directory.CreateOutput(metaName, state.context);
				CodecUtil.WriteHeader(meta, metaCodec, Lucene42DocValuesProducer.VERSION_CURRENT);
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
			meta.WriteVInt(field.number);
			meta.WriteByte(NUMBER);
			meta.WriteLong(data.FilePointer);
			long minValue = long.MaxValue;
			long maxValue = long.MinValue;
			long gcd = 0;
			// TODO: more efficient?
			HashSet<long> uniqueValues;
		    var valuesList = values as IList<long> ?? values.ToList();
		    if (true)
			{
				uniqueValues = new HashSet<long>();
				long count = 0;
				foreach (var nv in valuesList)
				{
					
					long v = nv;
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
			//HM:revisit 
			//assert count == maxDoc;
			if (uniqueValues != null)
			{
				// small number of unique values
				int bitsPerValue = PackedInts.BitsRequired(uniqueValues.Count - 1);
				PackedInts.FormatAndBits formatAndBits = PackedInts.FastestFormatAndBits(maxDoc, 
					bitsPerValue, acceptableOverheadRatio);
				if (formatAndBits.BitsPerValue == 8 && minValue >= byte.MinValue && maxValue <= byte.MaxValue)
				{
					meta.WriteByte(UNCOMPRESSED);
					// uncompressed
					foreach (var nv in valuesList)
					{
						data.WriteByte(unchecked((byte)nv));
					}
				}
				else
				{
					meta.WriteByte(TABLE_COMPRESSED);
					// table-compressed
				    long[] decode = uniqueValues.ToArray();
					Dictionary<long, int> encode = new Dictionary<long, int>();
					data.WriteVInt(decode.Length);
					for (int i = 0; i < decode.Length; i++)
					{
						data.WriteLong(decode[i]);
						encode[decode[i]] =  i;
					}
					meta.WriteVInt(PackedInts.VERSION_CURRENT);
					data.WriteVInt(formatAndBits.Format.id);
					data.WriteVInt(formatAndBits.BitsPerValue);
					PackedInts.Writer writer = PackedInts.GetWriterNoHeader(data, formatAndBits.Format
						, maxDoc, formatAndBits.BitsPerValue, PackedInts.DEFAULT_BUFFER_SIZE);
					foreach (var nv in valuesList)
					{
						writer.Add(encode[nv]);
					}
					writer.Finish();
				}
			}
			else
			{
				if (gcd != 0 && gcd != 1)
				{
					meta.WriteByte(GCD_COMPRESSED);
					meta.WriteVInt(PackedInts.VERSION_CURRENT);
					data.WriteLong(minValue);
					data.WriteLong(gcd);
					data.WriteVInt(BLOCK_SIZE);
					BlockPackedWriter writer = new BlockPackedWriter(data, BLOCK_SIZE);
					foreach (var nv in valuesList)
					{
						long value = nv;
						writer.Add((value - minValue) / gcd);
					}
					writer.Finish();
				}
				else
				{
					meta.WriteByte(DELTA_COMPRESSED);
					// delta-compressed
					meta.WriteVInt(PackedInts.VERSION_CURRENT);
					data.WriteVInt(BLOCK_SIZE);
					BlockPackedWriter writer = new BlockPackedWriter(data, BLOCK_SIZE);
					foreach (var nv in valuesList)
					{
						writer.Add(nv);
					}
					writer.Finish();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
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
				// write checksum
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
				meta = data = null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
		{
			throw new NotSupportedException();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<int> docToOrd)
		{
			throw new NotSupportedException();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values
			, IEnumerable<int> docToOrdCount, IEnumerable<long> ords)
		{
			throw new NotSupportedException();
		}
	}
}
