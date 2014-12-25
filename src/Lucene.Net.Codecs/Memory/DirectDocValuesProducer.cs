using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>
	/// Reader for
	/// <see cref="DirectDocValuesFormat">DirectDocValuesFormat</see>
	/// </summary>
	internal class DirectDocValuesProducer : DocValuesProducer
	{
		private readonly IDictionary<int, NumericEntry> numerics = new Dictionary<int, NumericEntry>();

		private readonly IDictionary<int, BinaryEntry> binaries = new Dictionary<int, BinaryEntry>();

		private readonly IDictionary<int, SortedEntry> sorteds = new Dictionary<int, SortedEntry>();

		private readonly IDictionary<int, SortedSetEntry> sortedSets = new Dictionary<int, SortedSetEntry>();

		private readonly IndexInput data;

		private readonly IDictionary<int, NumericDocValues> numericInstances = new Dictionary<int, NumericDocValues>();

		private readonly IDictionary<int, BinaryDocValues> binaryInstances = new Dictionary<int, BinaryDocValues>();

		private readonly IDictionary<int, SortedDocValues> sortedInstances = new Dictionary<int, SortedDocValues>();

		private readonly IDictionary<int, SortedSetRawValues> sortedSetInstances = new Dictionary<int, SortedSetRawValues>();

		private readonly IDictionary<int, IBits> docsWithFieldInstances = new Dictionary<int, IBits>();

		private readonly int maxDoc;

		private readonly AtomicLong ramBytesUsed;

		private readonly int version;

		internal const byte NUMBER = 0;

		internal const byte BYTES = 1;

		internal const byte SORTED = 2;

		internal const byte SORTED_SET = 3;

		internal const int VERSION_START = 0;

		internal const int VERSION_CHECKSUM = 1;

		internal const int VERSION_CURRENT = VERSION_CHECKSUM;

		/// <exception cref="System.IO.IOException"></exception>
		internal DirectDocValuesProducer(SegmentReadState state, string dataCodec, string
			 dataExtension, string metaCodec, string metaExtension)
		{
			// metadata maps (just file pointers and minimal stuff)
			// ram instances we have already loaded
			maxDoc = state.segmentInfo.DocCount;
			string metaName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
				, metaExtension);
			// read in the entries from the metadata file.
			ChecksumIndexInput @in = state.directory.OpenChecksumInput(metaName, state.context
				);
			ramBytesUsed = new AtomicLong(RamUsageEstimator.ShallowSizeOfInstance(GetType()));
			bool success = false;
			try
			{
				version = CodecUtil.CheckHeader(@in, metaCodec, VERSION_START, VERSION_CURRENT);
				ReadFields(@in);
				if (version >= VERSION_CHECKSUM)
				{
					CodecUtil.CheckFooter(@in);
				}
				else
				{
					CodecUtil.CheckEOF(@in);
				}
				success = true;
			}
			finally
			{
				if (success)
				{
					IOUtils.Close(@in);
				}
				else
				{
					IOUtils.CloseWhileHandlingException((IDisposable)@in);
				}
			}
			success = false;
			try
			{
				string dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
					, dataExtension);
				data = state.directory.OpenInput(dataName, state.context);
				int version2 = CodecUtil.CheckHeader(data, dataCodec, VERSION_START, VERSION_CURRENT
					);
				if (version != version2)
				{
					throw new CorruptIndexException("Format versions mismatch");
				}
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException((IDisposable)this.data);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private NumericEntry ReadNumericEntry(IndexInput meta)
		{
			var entry = new NumericEntry {offset = meta.ReadLong(), count = meta.ReadInt(), missingOffset = meta.ReadLong()};
		    entry.missingBytes = entry.missingOffset != -1 ? meta.ReadLong() : 0;
			entry.byteWidth = meta.ReadByte();
			return entry;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private BinaryEntry ReadBinaryEntry(IndexInput meta)
		{
			var entry = new BinaryEntry
			{
			    offset = meta.ReadLong(),
			    numBytes = meta.ReadInt(),
			    count = meta.ReadInt(),
			    missingOffset = meta.ReadLong()
			};
		    entry.missingBytes = entry.missingOffset != -1 ? meta.ReadLong() : 0;
			return entry;
		}

		
		private SortedEntry ReadSortedEntry(IndexInput meta)
		{
			var entry = new SortedEntry {docToOrd = ReadNumericEntry(meta), values = ReadBinaryEntry(meta)};
		    return entry;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private SortedSetEntry ReadSortedSetEntry(IndexInput meta)
		{
			var entry = new SortedSetEntry
			{
			    docToOrdAddress = ReadNumericEntry(meta),
			    ords = ReadNumericEntry(meta),
			    values = ReadBinaryEntry(meta)
			};
		    return entry;
		}

		
		private void ReadFields(IndexInput meta)
		{
			int fieldNumber = meta.ReadVInt();
			while (fieldNumber != -1)
			{
				int fieldType = meta.ReadByte();
				if (fieldType == NUMBER)
				{
					numerics[fieldNumber] = ReadNumericEntry(meta);
				}
				else
				{
					if (fieldType == BYTES)
					{
						binaries[fieldNumber] = ReadBinaryEntry(meta);
					}
					else
					{
						if (fieldType == SORTED)
						{
							sorteds[fieldNumber] = ReadSortedEntry(meta);
						}
						else
						{
							if (fieldType == SORTED_SET)
							{
								sortedSets[fieldNumber] = ReadSortedSetEntry(meta);
							}
							else
							{
								throw new CorruptIndexException("invalid entry type: " + fieldType + ", input=" +
									 meta);
							}
						}
					}
				}
				fieldNumber = meta.ReadVInt();
			}
		}

		public override long RamBytesUsed
		{
		    get { return ramBytesUsed.Get(); }
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CheckIntegrity()
		{
			if (version >= VERSION_CHECKSUM)
			{
				CodecUtil.ChecksumEntireFile(data);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override NumericDocValues GetNumeric(FieldInfo field)
		{
			lock (this)
			{
				NumericDocValues instance = numericInstances[field.number];
				if (instance == null)
				{
					// Lazy load
					instance = LoadNumeric(numerics[field.number]);
					numericInstances[field.number] = instance;
				}
				return instance;
			}
		}

		
		private NumericDocValues LoadNumeric(NumericEntry entry)
		{
			data.Seek(entry.offset + entry.missingBytes);
			switch (entry.byteWidth)
			{
				case 1:
				{
					byte[] values = new byte[entry.count];
					data.ReadBytes(values, 0, entry.count);
					ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
					return new AnonymousNumericDocValues(values);
				}

				case 2:
				{
					short[] values = new short[entry.count];
					for (int i = 0; i < entry.count; i++)
					{
						values[i] = data.ReadShort();
					}
					ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
					return new AnonymousNumericDocValues2(values);
				}

				case 4:
				{
					int[] values = new int[entry.count];
					for (int i = 0; i < entry.count; i++)
					{
						values[i] = data.ReadInt();
					}
					ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
					return new AnonymousNumericDocValues3(values);
				}

				case 8:
				{
					long[] values = new long[entry.count];
					for (int i = 0; i < entry.count; i++)
					{
						values[i] = data.ReadLong();
					}
					ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
					return new AnonymousNumericDocValues4(values);
				}

				default:
				{
					throw new Exception();
				}
			}
		}

		private sealed class AnonymousNumericDocValues : NumericDocValues
		{
			public AnonymousNumericDocValues(byte[] values)
			{
				this.values = values;
			}

			public override long Get(int idx)
			{
				return values[idx];
			}

			private readonly byte[] values;
		}

		private sealed class AnonymousNumericDocValues2 : NumericDocValues
		{
			public AnonymousNumericDocValues2(short[] values)
			{
				this.values = values;
			}

			public override long Get(int idx)
			{
				return values[idx];
			}

			private readonly short[] values;
		}

		private sealed class AnonymousNumericDocValues3 : NumericDocValues
		{
			public AnonymousNumericDocValues3(int[] values)
			{
				this.values = values;
			}

			public override long Get(int idx)
			{
				return values[idx];
			}

			private readonly int[] values;
		}

		private sealed class AnonymousNumericDocValues4 : NumericDocValues
		{
			public AnonymousNumericDocValues4(long[] values)
			{
				this.values = values;
			}

			public override long Get(int idx)
			{
				return values[idx];
			}

			private readonly long[] values;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override BinaryDocValues GetBinary(FieldInfo field)
		{
			lock (this)
			{
				BinaryDocValues instance = binaryInstances[field.number];
				if (instance == null)
				{
					// Lazy load
					instance = LoadBinary(binaries[field.number]);
					binaryInstances[field.number] = instance;
				}
				return instance;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private BinaryDocValues LoadBinary(BinaryEntry entry)
		{
			data.Seek(entry.offset);
			byte[] bytes = new byte[entry.numBytes];
			data.ReadBytes(bytes, 0, entry.numBytes);
			data.Seek(entry.offset + entry.numBytes + entry.missingBytes);
			int[] address = new int[entry.count + 1];
			for (int i = 0; i < entry.count; i++)
			{
				address[i] = data.ReadInt();
			}
			address[entry.count] = data.ReadInt();
			ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(bytes) + RamUsageEstimator.SizeOf
				(address));
			return new AnonymousBinaryDocValues(bytes, address);
		}

		private sealed class AnonymousBinaryDocValues : BinaryDocValues
		{
			public AnonymousBinaryDocValues(byte[] bytes, int[] address)
			{
			    this.bytes = Array.ConvertAll(bytes, Convert.ToSByte);
				this.address = address;
			}

			public override void Get(int docID, BytesRef result)
			{
				result.bytes = bytes;
				result.offset = address[docID];
				result.length = address[docID + 1] - result.offset;
			}

			private readonly sbyte[] bytes;

			private readonly int[] address;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override SortedDocValues GetSorted(FieldInfo field)
		{
			lock (this)
			{
				SortedDocValues instance = sortedInstances[field.number];
				if (instance == null)
				{
					// Lazy load
					instance = LoadSorted(field);
					sortedInstances[field.number] = instance;
				}
				return instance;
			}
		}

		
		private SortedDocValues LoadSorted(FieldInfo field)
		{
			SortedEntry entry = sorteds[field.number];
			NumericDocValues docToOrd = LoadNumeric(entry.docToOrd);
			BinaryDocValues values = LoadBinary(entry.values);
			return new AnonymousSortedDocValues(docToOrd, values, entry);
		}

		private sealed class AnonymousSortedDocValues : SortedDocValues
		{
			public AnonymousSortedDocValues(NumericDocValues docToOrd, BinaryDocValues values, SortedEntry entry)
			{
				this.docToOrd = docToOrd;
				this.values = values;
				this.entry = entry;
			}

			public override int GetOrd(int docID)
			{
				return (int)docToOrd.Get(docID);
			}

			public override void LookupOrd(int ord, BytesRef result)
			{
				values.Get(ord, result);
			}

			public override int ValueCount
			{
			    get { return entry.values.count; }
			}

			private readonly NumericDocValues docToOrd;

			private readonly BinaryDocValues values;

			private readonly SortedEntry entry;
		}

		// Leave lookupTerm to super's binary search
		// Leave termsEnum to super
		/// <exception cref="System.IO.IOException"></exception>
		public override SortedSetDocValues GetSortedSet(FieldInfo field)
		{
			lock (this)
			{
				SortedSetRawValues instance = sortedSetInstances[field.number];
				SortedSetEntry entry = sortedSets[field.number];
				if (instance == null)
				{
					// Lazy load
					instance = LoadSortedSet(entry);
					sortedSetInstances[field.number] = instance;
				}
				NumericDocValues docToOrdAddress = instance.docToOrdAddress;
				NumericDocValues ords = instance.ords;
				BinaryDocValues values = instance.values;
				// Must make a new instance since the iterator has state:
				return new AnonymousRandomAccessOrds(ords, docToOrdAddress, values, entry);
			}
		}

		private sealed class AnonymousRandomAccessOrds : RandomAccessOrds
		{
			public AnonymousRandomAccessOrds(NumericDocValues ords, NumericDocValues docToOrdAddress
				, BinaryDocValues values, SortedSetEntry entry)
			{
				this.ords = ords;
				this.docToOrdAddress = docToOrdAddress;
				this.values = values;
				this.entry = entry;
			}

			internal int ordStart;

			internal int ordUpto;

			internal int ordLimit;

			public override long NextOrd()
			{
			    return this.ordUpto == this.ordLimit ? NO_MORE_ORDS : ords.Get(this.ordUpto++);
			}

		    public override void SetDocument(int docID)
			{
				this.ordStart = this.ordUpto = (int)docToOrdAddress.Get(docID);
				this.ordLimit = (int)docToOrdAddress.Get(docID + 1);
			}

			public override void LookupOrd(long ord, BytesRef result)
			{
				values.Get((int)ord, result);
			}

			public override long ValueCount
			{
			    get { return entry.values.count; }
			}

			public override long OrdAt(int index)
			{
				return ords.Get(this.ordStart + index);
			}

			public override int Cardinality()
			{
				return this.ordLimit - this.ordStart;
			}

			private readonly NumericDocValues ords;

			private readonly NumericDocValues docToOrdAddress;

			private readonly BinaryDocValues values;

			private readonly SortedSetEntry entry;
		}

		// Leave lookupTerm to super's binary search
		// Leave termsEnum to super
		/// <exception cref="System.IO.IOException"></exception>
		private SortedSetRawValues LoadSortedSet(SortedSetEntry entry)
		{
			var instance = new SortedSetRawValues
			{
			    docToOrdAddress = LoadNumeric(entry.docToOrdAddress),
			    ords = LoadNumeric(entry.ords),
			    values = LoadBinary(entry.values)
			};
		    return instance;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IBits GetMissingBits(int fieldNumber, long offset, long length)
		{
			if (offset == -1)
			{
				return new Bits.MatchAllBits(maxDoc);
			}
		    IBits instance;
		    lock (this)
		    {
		        instance = docsWithFieldInstances[fieldNumber];
		        if (instance == null)
		        {
		            IndexInput data = ((IndexInput)this.data.Clone());
		            data.Seek(offset);
		            
		            //assert length % 8 == 0;
		            long[] bits = new long[(int)length >> 3];
		            for (int i = 0; i < bits.Length; i++)
		            {
		                bits[i] = data.ReadLong();
		            }
		            instance = new FixedBitSet(bits, maxDoc);
		            docsWithFieldInstances[fieldNumber] = instance;
		        }
		    }
		    return instance;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IBits GetDocsWithField(FieldInfo field)
		{
			switch (field.GetDocValuesType())
			{
				case FieldInfo.DocValuesType.SORTED_SET:
				{
					return DocValues.DocsWithValue(GetSortedSet(field), maxDoc);
				}

				case FieldInfo.DocValuesType.SORTED:
				{
					return DocValues.DocsWithValue(GetSorted(field), maxDoc);
				}

				case FieldInfo.DocValuesType.BINARY:
				{
					BinaryEntry be = binaries[field.number];
					return GetMissingBits(field.number, be.missingOffset, be.missingBytes);
				}

				case FieldInfo.DocValuesType.NUMERIC:
				{
					NumericEntry ne = numerics[field.number];
					return GetMissingBits(field.number, ne.missingOffset, ne.missingBytes);
				}

				default:
				{
					throw new Exception();
				}
			}
		}


	    protected override void Dispose(bool disposing)
		{
			data.Dispose();
		}

		internal class SortedSetRawValues
		{
			internal NumericDocValues docToOrdAddress;

			internal NumericDocValues ords;

			internal BinaryDocValues values;
		}

		internal class NumericEntry
		{
			internal long offset;

			internal int count;

			internal long missingOffset;

			internal long missingBytes;

			internal byte byteWidth;

			internal int packedIntsVersion;
		}

		internal class BinaryEntry
		{
			internal long offset;

			internal long missingOffset;

			internal long missingBytes;

			internal int count;

			internal int numBytes;

			internal int minLength;

			internal int maxLength;

			internal int packedIntsVersion;

			internal int blockSize;
		}

		internal class SortedEntry
		{
			internal DirectDocValuesProducer.NumericEntry docToOrd;

			internal DirectDocValuesProducer.BinaryEntry values;
		}

		internal class SortedSetEntry
		{
			internal DirectDocValuesProducer.NumericEntry docToOrdAddress;

			internal DirectDocValuesProducer.NumericEntry ords;

			internal DirectDocValuesProducer.BinaryEntry values;
		}

		internal class FSTEntry
		{
			internal long offset;

			internal long numOrds;
		}
	}
}
