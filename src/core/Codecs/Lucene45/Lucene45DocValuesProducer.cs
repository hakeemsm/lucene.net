
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Codecs.Lucene45
{
	/// <summary>
	/// reader for
	/// <see cref="Lucene45DocValuesFormat">Lucene45DocValuesFormat</see>
	/// 
	/// </summary>
	public class Lucene45DocValuesProducer : DocValuesProducer, IDisposable
	{
		private readonly IDictionary<int, NumericEntry> numerics;

		private readonly IDictionary<int, BinaryEntry> binaries;

		private readonly IDictionary<int, SortedSetEntry> sortedSets;

		private readonly IDictionary<int, NumericEntry> ords;

		private readonly IDictionary<int, NumericEntry> ordIndexes;

		private long ramBytesUsed;

		private readonly IndexInput data;

		private readonly int maxDoc;

		private readonly int version;

		private readonly IDictionary<int, MonotonicBlockPackedReader> addressInstances = 
			new Dictionary<int, MonotonicBlockPackedReader>();

		private readonly IDictionary<int, MonotonicBlockPackedReader> ordIndexInstances = 
			new Dictionary<int, MonotonicBlockPackedReader>();

		/// <summary>expert: instantiates a new reader</summary>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal Lucene45DocValuesProducer(SegmentReadState state, string dataCodec
			, string dataExtension, string metaCodec, string metaExtension)
		{
			// javadocs
			// memory-resident structures
			string metaName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
				, metaExtension);
			// read in the entries from the metadata file.
			ChecksumIndexInput input = state.directory.OpenChecksumInput(metaName, state.context
				);
			this.maxDoc = state.segmentInfo.DocCount;
			bool success = false;
			try
			{
				version = CodecUtil.CheckHeader(input, metaCodec, Lucene45DocValuesFormat.VERSION_START
					, Lucene45DocValuesFormat.VERSION_CURRENT);
				numerics = new Dictionary<int, NumericEntry>();
				ords = new Dictionary<int, NumericEntry>();
				ordIndexes = new Dictionary<int, NumericEntry>();
				binaries = new Dictionary<int, BinaryEntry>();
				sortedSets = new Dictionary<int, SortedSetEntry>();
				ReadFields(input, state.fieldInfos);
				if (version >= Lucene45DocValuesFormat.VERSION_CHECKSUM)
				{
					CodecUtil.CheckFooter(input);
				}
				else
				{
					CodecUtil.CheckEOF(input);
				}
				success = true;
			}
			finally
			{
				if (success)
				{
					IOUtils.Close(input);
				}
				else
				{
					IOUtils.CloseWhileHandlingException((IDisposable)input);
				}
			}
			success = false;
			try
			{
				string dataName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix
					, dataExtension);
				data = state.directory.OpenInput(dataName, state.context);
				int version2 = CodecUtil.CheckHeader(data, dataCodec, Lucene45DocValuesFormat.VERSION_START
					, Lucene45DocValuesFormat.VERSION_CURRENT);
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
					IOUtils.CloseWhileHandlingException((IDisposable)data);
				}
			}
		    var sizeOfInstance = RamUsageEstimator.ShallowSizeOfInstance(GetType());
		    ramBytesUsed = Interlocked.Read(ref sizeOfInstance);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadSortedField(int fieldNumber, IndexInput meta, FieldInfos infos)
		{
			// sorted = binary + numeric
			if (meta.ReadVInt() != fieldNumber)
			{
				throw new CorruptIndexException("sorted entry for field: " + fieldNumber + " is corrupt (resource="
					 + meta + ")");
			}
			if (meta.ReadByte() != Lucene45DocValuesFormat.BINARY)
			{
				throw new CorruptIndexException("sorted entry for field: " + fieldNumber + " is corrupt (resource="
					 + meta + ")");
			}
			Lucene45DocValuesProducer.BinaryEntry b = ReadBinaryEntry(meta);
			binaries[fieldNumber] = b;
			if (meta.ReadVInt() != fieldNumber)
			{
				throw new CorruptIndexException("sorted entry for field: " + fieldNumber + " is corrupt (resource="
					 + meta + ")");
			}
			if (meta.ReadByte() != Lucene45DocValuesFormat.NUMERIC)
			{
				throw new CorruptIndexException("sorted entry for field: " + fieldNumber + " is corrupt (resource="
					 + meta + ")");
			}
			Lucene45DocValuesProducer.NumericEntry n = ReadNumericEntry(meta);
			ords[fieldNumber] = n;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadSortedSetFieldWithAddresses(int fieldNumber, IndexInput meta, FieldInfos infos)
		{
			// sortedset = binary + numeric (addresses) + ordIndex
			if (meta.ReadVInt() != fieldNumber)
			{
				throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource="
					 + meta + ")");
			}
			if (meta.ReadByte() != Lucene45DocValuesFormat.BINARY)
			{
				throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource="
					 + meta + ")");
			}
			BinaryEntry b = ReadBinaryEntry(meta);
			binaries[fieldNumber] = b;
			if (meta.ReadVInt() != fieldNumber)
			{
				throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource="+ meta + ")");
			}
			if (meta.ReadByte() != Lucene45DocValuesFormat.NUMERIC)
			{
				throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource="+ meta + ")");
			}
			var n1 = ReadNumericEntry(meta);
			ords[fieldNumber] = n1;
			if (meta.ReadVInt() != fieldNumber)
			{
				throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
			}
			if (meta.ReadByte() != Lucene45DocValuesFormat.NUMERIC)
			{
				throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource=" + meta + ")");
			}
			var n2 = ReadNumericEntry(meta);
			ordIndexes[fieldNumber] = n2;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadFields(IndexInput meta, FieldInfos infos)
		{
			int fieldNumber = meta.ReadVInt();
			while (fieldNumber != -1)
			{
				// check should be: infos.fieldInfo(fieldNumber) != null, which incorporates negative check
				// but docvalues updates are currently buggy here (loading extra stuff, etc): LUCENE-5616
				if (fieldNumber < 0)
				{
					// trickier to validate more: because we re-use for norms, because we use multiple entries
					// for "composite" types like sortedset, etc.
					throw new CorruptIndexException("Invalid field number: " + fieldNumber + " (resource="
						 + meta + ")");
				}
				byte type = meta.ReadByte();
				if (type == Lucene45DocValuesFormat.NUMERIC)
				{
					numerics[fieldNumber] = ReadNumericEntry(meta);
				}
				else
				{
					if (type == Lucene45DocValuesFormat.BINARY)
					{
						BinaryEntry b = ReadBinaryEntry(meta);
						binaries[fieldNumber] = b;
					}
					else
					{
						if (type == Lucene45DocValuesFormat.SORTED)
						{
							ReadSortedField(fieldNumber, meta, infos);
						}
						else
						{
							if (type == Lucene45DocValuesFormat.SORTED_SET)
							{
								SortedSetEntry ss = ReadSortedSetEntry(meta);
								sortedSets[fieldNumber] = ss;
								if (ss.format == Lucene45DocValuesConsumer.SORTED_SET_WITH_ADDRESSES)
								{
									ReadSortedSetFieldWithAddresses(fieldNumber, meta, infos);
								}
								else
								{
									if (ss.format == Lucene45DocValuesConsumer.SORTED_SET_SINGLE_VALUED_SORTED)
									{
										if (meta.ReadVInt() != fieldNumber)
										{
											throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource="
												 + meta + ")");
										}
										if (meta.ReadByte() != Lucene45DocValuesFormat.SORTED)
										{
											throw new CorruptIndexException("sortedset entry for field: " + fieldNumber + " is corrupt (resource="
												 + meta + ")");
										}
										ReadSortedField(fieldNumber, meta, infos);
									}
									else
									{
										throw new Exception();
									}
								}
							}
							else
							{
								throw new CorruptIndexException("invalid type: " + type + ", resource=" + meta);
							}
						}
					}
				}
				fieldNumber = meta.ReadVInt();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static NumericEntry ReadNumericEntry(IndexInput meta)
		{
			var entry = new NumericEntry
			{
			    format = meta.ReadVInt(),
			    missingOffset = meta.ReadLong(),
			    packedIntsVersion = meta.ReadVInt(),
			    offset = meta.ReadLong(),
			    count = meta.ReadVLong(),
			    blockSize = meta.ReadVInt()
			};
		    switch (entry.format)
			{
				case Lucene45DocValuesConsumer.GCD_COMPRESSED:
				{
					entry.minValue = meta.ReadLong();
					entry.gcd = meta.ReadLong();
					break;
				}

				case Lucene45DocValuesConsumer.TABLE_COMPRESSED:
				{
					if (entry.count > int.MaxValue)
					{
						throw new CorruptIndexException("Cannot use TABLE_COMPRESSED with more than MAX_VALUE values, input="
							 + meta);
					}
					int uniqueValues = meta.ReadVInt();
					if (uniqueValues > 256)
					{
						throw new CorruptIndexException("TABLE_COMPRESSED cannot have more than 256 distinct values, input="
							 + meta);
					}
					entry.table = new long[uniqueValues];
					for (int i = 0; i < uniqueValues; ++i)
					{
						entry.table[i] = meta.ReadLong();
					}
					break;
				}

				case Lucene45DocValuesConsumer.DELTA_COMPRESSED:
				{
					break;
				}

				default:
				{
					throw new CorruptIndexException("Unknown format: " + entry.format + ", input=" + 
						meta);
				}
			}
			return entry;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal static BinaryEntry ReadBinaryEntry(IndexInput meta)
		{
			var entry = new BinaryEntry
			{
			    format = meta.ReadVInt(),
			    missingOffset = meta.ReadLong(),
			    minLength = meta.ReadVInt(),
			    maxLength = meta.ReadVInt(),
			    count = meta.ReadVLong(),
			    offset = meta.ReadLong()
			};
		    switch (entry.format)
			{
				case Lucene45DocValuesConsumer.BINARY_FIXED_UNCOMPRESSED:
				{
					break;
				}

				case Lucene45DocValuesConsumer.BINARY_PREFIX_COMPRESSED:
				{
					entry.addressInterval = meta.ReadVInt();
					entry.addressesOffset = meta.ReadLong();
					entry.packedIntsVersion = meta.ReadVInt();
					entry.blockSize = meta.ReadVInt();
					break;
				}

				case Lucene45DocValuesConsumer.BINARY_VARIABLE_UNCOMPRESSED:
				{
					entry.addressesOffset = meta.ReadLong();
					entry.packedIntsVersion = meta.ReadVInt();
					entry.blockSize = meta.ReadVInt();
					break;
				}

				default:
				{
					throw new CorruptIndexException("Unknown format: " + entry.format + ", input=" + meta);
				}
			}
			return entry;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual SortedSetEntry ReadSortedSetEntry(IndexInput meta)
		{
			var entry = new SortedSetEntry
				();
			entry.format = version >= Lucene45DocValuesFormat.VERSION_SORTED_SET_SINGLE_VALUE_OPTIMIZED ? meta.ReadVInt() : Lucene45DocValuesConsumer.SORTED_SET_WITH_ADDRESSES;
			if (entry.format != Lucene45DocValuesConsumer.SORTED_SET_SINGLE_VALUED_SORTED && 
				entry.format != Lucene45DocValuesConsumer.SORTED_SET_WITH_ADDRESSES)
			{
				throw new CorruptIndexException("Unknown format: " + entry.format + ", input=" + 
					meta);
			}
			return entry;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override NumericDocValues GetNumeric(FieldInfo field)
		{
			NumericEntry entry = numerics[field.number];
			return GetNumeric(entry);
		}

		public override long RamBytesUsed
		{
		    get { return ramBytesUsed; }
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CheckIntegrity()
		{
			if (version >= Lucene45DocValuesFormat.VERSION_CHECKSUM)
			{
				CodecUtil.ChecksumEntireFile(data);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual LongValues GetNumeric(NumericEntry entry)
		{
			IndexInput data = ((IndexInput)this.data.Clone());
			data.Seek(entry.offset);
			switch (entry.format)
			{
				case Lucene45DocValuesConsumer.DELTA_COMPRESSED:
				{
					BlockPackedReader reader = new BlockPackedReader(data, entry.packedIntsVersion, entry
						.blockSize, entry.count, true);
					return reader;
				}

				case Lucene45DocValuesConsumer.GCD_COMPRESSED:
				{
					long min = entry.minValue;
					long mult = entry.gcd;
					BlockPackedReader quotientReader = new BlockPackedReader(data, entry.packedIntsVersion
						, entry.blockSize, entry.count, true);
					return new AnonymousLongValues1(min, mult, quotientReader);
				}

				case Lucene45DocValuesConsumer.TABLE_COMPRESSED:
				{
					long[] table = entry.table;
					int bitsRequired = PackedInts.BitsRequired(table.Length - 1);
					PackedInts.Reader ords = PackedInts.GetDirectReaderNoHeader(data, PackedInts.Format
						.PACKED, entry.packedIntsVersion, (int)entry.count, bitsRequired);
					return new AnonymousLongValues2(table, ords);
				}

				default:
				{
					throw new Exception();
				}
			}
		}

		private sealed class AnonymousLongValues1 : LongValues
		{
			public AnonymousLongValues1(long min, long mult, BlockPackedReader quotientReader)
			{
				this.min = min;
				this.mult = mult;
				this.quotientReader = quotientReader;
			}

			public override long Get(long id)
			{
				return min + mult * quotientReader.Get(id);
			}

			private readonly long min;

			private readonly long mult;

			private readonly BlockPackedReader quotientReader;
		}

		private sealed class AnonymousLongValues2 : LongValues
		{
			public AnonymousLongValues2(long[] table, PackedInts.Reader ords)
			{
				this.table = table;
				this.ords = ords;
			}

			public override long Get(long id)
			{
				return table[(int)ords.Get((int)id)];
			}

			private readonly long[] table;

			private readonly PackedInts.Reader ords;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override BinaryDocValues GetBinary(FieldInfo field)
		{
			Lucene45DocValuesProducer.BinaryEntry bytes = binaries[field.number];
			switch (bytes.format)
			{
				case Lucene45DocValuesConsumer.BINARY_FIXED_UNCOMPRESSED:
				{
					return GetFixedBinary(field, bytes);
				}

				case Lucene45DocValuesConsumer.BINARY_VARIABLE_UNCOMPRESSED:
				{
					return GetVariableBinary(field, bytes);
				}

				case Lucene45DocValuesConsumer.BINARY_PREFIX_COMPRESSED:
				{
					return GetCompressedBinary(field, bytes);
				}

				default:
				{
					throw new Exception();
				}
			}
		}

		private BinaryDocValues GetFixedBinary(FieldInfo field, BinaryEntry
			 bytes)
		{
			IndexInput data = ((IndexInput)this.data.Clone());
			return new AnonymousLongBinaryDocValues1(bytes, data);
		}

		private sealed class AnonymousLongBinaryDocValues1 : LongBinaryDocValues
		{
			public AnonymousLongBinaryDocValues1(BinaryEntry bytes, IndexInput data)
			{
				this.bytes = bytes;
				this.data = data;
			}

			internal override void Get(long id, BytesRef result)
			{
				long address = bytes.offset + id * bytes.maxLength;
			    data.Seek(address);
			    // NOTE: we could have one buffer, but various consumers (e.g. FieldComparatorSource) 
			    // assume "they" own the bytes after calling this!
			    sbyte[] buffer = new sbyte[bytes.maxLength];
			    data.ReadBytes(buffer, 0, buffer.Length);
			    result.bytes = buffer;
			    result.offset = 0;
			    result.length = buffer.Length;
			}

			private readonly BinaryEntry bytes;

			private readonly IndexInput data;
		}

		/// <summary>returns an address instance for variable-length binary values.</summary>
		/// <remarks>returns an address instance for variable-length binary values.</remarks>
		/// <lucene.internal></lucene.internal>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual MonotonicBlockPackedReader GetAddressInstance(IndexInput data, FieldInfo field, BinaryEntry bytes)
		{
			MonotonicBlockPackedReader addresses;
			lock (addressInstances)
			{
				MonotonicBlockPackedReader addrInstance = addressInstances[field.number];
				if (addrInstance == null)
				{
					data.Seek(bytes.addressesOffset);
					addrInstance = new MonotonicBlockPackedReader(data, bytes.packedIntsVersion, bytes.blockSize, bytes.count, false);
					addressInstances[field.number] = addrInstance;
					Interlocked.Add(ramBytesUsed,addrInstance.RamBytesUsed + RamUsageEstimator.NUM_BYTES_INT);
				}
				addresses = addrInstance;
			}
			return addresses;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private BinaryDocValues GetVariableBinary(FieldInfo field, BinaryEntry bytes)
		{
			IndexInput data = ((IndexInput)this.data.Clone());
			MonotonicBlockPackedReader addresses = GetAddressInstance(data, field, bytes);
			return new AnonymousLongBinaryDocValues2(bytes, addresses, data);
		}

		private sealed class AnonymousLongBinaryDocValues2 : LongBinaryDocValues
		{
			public AnonymousLongBinaryDocValues2(BinaryEntry bytes, MonotonicBlockPackedReader
				 addresses, IndexInput data)
			{
				this.bytes = bytes;
				this.addresses = addresses;
				this.data = data;
			}

			internal override void Get(long id, BytesRef result)
			{
				long startAddress = bytes.offset + (id == 0 ? 0 : addresses.Get(id - 1));
				long endAddress = bytes.offset + addresses.Get(id);
				int length = (int)(endAddress - startAddress);
			    data.Seek(startAddress);
			    // NOTE: we could have one buffer, but various consumers (e.g. FieldComparatorSource) 
			    // assume "they" own the bytes after calling this!
			    sbyte[] buffer = new sbyte[length];
			    data.ReadBytes(buffer, 0, buffer.Length);
			    result.bytes = buffer;
			    result.offset = 0;
			    result.length = length;
			}

			private readonly BinaryEntry bytes;

			private readonly MonotonicBlockPackedReader addresses;

			private readonly IndexInput data;
		}

		/// <summary>returns an address instance for prefix-compressed binary values.</summary>
		/// <remarks>returns an address instance for prefix-compressed binary values.</remarks>
		/// <lucene.internal></lucene.internal>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual MonotonicBlockPackedReader GetIntervalInstance(IndexInput data, FieldInfo field, BinaryEntry bytes)
		{
			MonotonicBlockPackedReader addresses;
			long interval = bytes.addressInterval;
			lock (addressInstances)
			{
				MonotonicBlockPackedReader addrInstance = addressInstances[field.number];
				if (addrInstance == null)
				{
					data.Seek(bytes.addressesOffset);
					long size;
					if (bytes.count % interval == 0)
					{
						size = bytes.count / interval;
					}
					else
					{
						size = 1L + bytes.count / interval;
					}
					addrInstance = new MonotonicBlockPackedReader(data, bytes.packedIntsVersion, bytes
						.blockSize, size, false);
					addressInstances[field.number] = addrInstance;
                    Interlocked.Add(ramBytesUsed,addrInstance.RamBytesUsed + RamUsageEstimator.NUM_BYTES_INT
						);
				}
				addresses = addrInstance;
			}
			return addresses;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private BinaryDocValues GetCompressedBinary(FieldInfo field, BinaryEntry bytes)
		{
			IndexInput data = ((IndexInput)this.data.Clone());
			MonotonicBlockPackedReader addresses = GetIntervalInstance(data, field, bytes);
			return new CompressedBinaryDocValues(bytes, addresses, data);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override SortedDocValues GetSorted(FieldInfo field)
		{
			int valueCount = (int)binaries[field.number].count;
			BinaryDocValues binary = GetBinary(field);
			Lucene45DocValuesProducer.NumericEntry entry = ords[field.number];
			IndexInput data = ((IndexInput)this.data.Clone());
			data.Seek(entry.offset);
			BlockPackedReader ordinals = new BlockPackedReader(data, entry.packedIntsVersion, 
				entry.blockSize, entry.count, true);
			return new AnonymousSortedDocValues1(ordinals, binary, valueCount);
		}

		private sealed class AnonymousSortedDocValues1 : SortedDocValues
		{
			public AnonymousSortedDocValues1(BlockPackedReader ordinals, BinaryDocValues binary, int
				 valueCount)
			{
				this.ordinals = ordinals;
				this.binary = binary;
				this.valueCount = valueCount;
			}

			public override int GetOrd(int docID)
			{
				return (int)ordinals.Get(docID);
			}

			public override void LookupOrd(int ord, BytesRef result)
			{
				binary.Get(ord, result);
			}

			public override int ValueCount
			{
			    get { return valueCount; }
			}

			public override int LookupTerm(BytesRef key)
			{
			    if (binary is CompressedBinaryDocValues)
				{
					return (int)((CompressedBinaryDocValues)binary).LookupTerm(key);
				}
			    return base.LookupTerm(key);
			}

		    public override TermsEnum TermsEnum
			{
		        get
		        {
		            if (binary is CompressedBinaryDocValues)
		            {
		                return ((CompressedBinaryDocValues) binary).GetTermsEnum();
		            }
		            return base.TermsEnum;
		        }
			}

			private readonly BlockPackedReader ordinals;

			private readonly BinaryDocValues binary;

			private readonly int valueCount;
		}

		/// <summary>returns an address instance for sortedset ordinal lists</summary>
		/// <lucene.internal></lucene.internal>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual MonotonicBlockPackedReader GetOrdIndexInstance(IndexInput
			 data, FieldInfo field, Lucene45DocValuesProducer.NumericEntry entry)
		{
			MonotonicBlockPackedReader ordIndex;
			lock (ordIndexInstances)
			{
				MonotonicBlockPackedReader ordIndexInstance = ordIndexInstances[field.number];
				if (ordIndexInstance == null)
				{
					data.Seek(entry.offset);
					ordIndexInstance = new MonotonicBlockPackedReader(data, entry.packedIntsVersion, 
						entry.blockSize, entry.count, false);
					ordIndexInstances[field.number] = ordIndexInstance;
                    Interlocked.Add(ramBytesUsed,ordIndexInstance.RamBytesUsed + RamUsageEstimator.NUM_BYTES_INT);
				}
				ordIndex = ordIndexInstance;
			}
			return ordIndex;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override SortedSetDocValues GetSortedSet(FieldInfo field)
		{
			SortedSetEntry ss = sortedSets[field.number];
			if (ss.format == Lucene45DocValuesConsumer.SORTED_SET_SINGLE_VALUED_SORTED)
			{
				SortedDocValues values = GetSorted(field);
				return DocValues.Singleton(values);
			}
			else
			{
				if (ss.format != Lucene45DocValuesConsumer.SORTED_SET_WITH_ADDRESSES)
				{
					throw new Exception();
				}
			}
			IndexInput data = ((IndexInput)this.data.Clone());
			long valueCount = binaries[field.number].count;
			// we keep the byte[]s and list of ords on disk, these could be large
			var binary = (LongBinaryDocValues)GetBinary(field);
			LongValues ordinals = GetNumeric(ords[field.number]);
			// but the addresses to the ord stream are in RAM
			MonotonicBlockPackedReader ordIndex = GetOrdIndexInstance(data, field, ordIndexes[field.number]);
			return new AnonymousRandomAccessOrds(ordinals, ordIndex, binary, valueCount);
		}

		private sealed class AnonymousRandomAccessOrds : RandomAccessOrds
		{
			public AnonymousRandomAccessOrds(LongValues ordinals, MonotonicBlockPackedReader ordIndex, LongBinaryDocValues binary, long valueCount)
			{
				this.ordinals = ordinals;
				this.ordIndex = ordIndex;
				this.binary = binary;
				this.valueCount = valueCount;
			}

			internal long startOffset;

			internal long offset;

			internal long endOffset;

			public override long NextOrd()
			{
				if (this.offset == this.endOffset)
				{
					return NO_MORE_ORDS;
				}
			    long ord = ordinals.Get(this.offset);
			    this.offset++;
			    return ord;
			}

			public override void SetDocument(int docID)
			{
				this.startOffset = this.offset = (docID == 0 ? 0 : ordIndex.Get(docID - 1));
				this.endOffset = ordIndex.Get(docID);
			}

			public override void LookupOrd(long ord, BytesRef result)
			{
				binary.Get(ord, result);
			}

			public override long ValueCount
			{
			    get { return valueCount; }
			}

			public override long LookupTerm(BytesRef key)
			{
			    return binary is CompressedBinaryDocValues
			        ? ((CompressedBinaryDocValues) binary).LookupTerm(key)
			        : base.LookupTerm(key);
			}

		    public override TermsEnum TermsEnum
			{
		        get {
		            return binary is CompressedBinaryDocValues
		                ? ((CompressedBinaryDocValues) binary).GetTermsEnum()
		                : base.TermsEnum;
		        }
			}

			public override long OrdAt(int index)
			{
				return ordinals.Get(this.startOffset + index);
			}

			public override int Cardinality()
			{
				return (int)(this.endOffset - this.startOffset);
			}

			private readonly LongValues ordinals;

			private readonly MonotonicBlockPackedReader ordIndex;

			private readonly Lucene45DocValuesProducer.LongBinaryDocValues binary;

			private readonly long valueCount;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IBits GetMissingBits(long offset)
		{
			if (offset == -1)
			{
				return new Bits.MatchAllBits(maxDoc);
			}
			else
			{
				IndexInput @in = ((IndexInput)data.Clone());
				return new AnonymousIBitsImpl(this, @in, offset);
			}
		}

		private sealed class AnonymousIBitsImpl : IBits
		{
			public AnonymousIBitsImpl(Lucene45DocValuesProducer _enclosing, IndexInput indexInput, long offset
				)
			{
				this._enclosing = _enclosing;
				this.indexInput = indexInput;
				this.offset = offset;
			}

			private readonly Lucene45DocValuesProducer _enclosing;

			private readonly IndexInput indexInput;

			private readonly long offset;

		    public bool this[int index]
		    {
                get
                {
                    indexInput.Seek(offset + (index >> 3));
                    return (indexInput.ReadByte() & (1 << (index & 7))) != 0;
                }
		    }

		    int IBits.Length
		    {
		        get
		        {
                    return _enclosing.maxDoc;
		        }
		    }
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IBits GetDocsWithField(FieldInfo field)
		{
			switch (field.DocValuesTypeValue)
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
					Lucene45DocValuesProducer.BinaryEntry be = binaries[field.number];
					return GetMissingBits(be.missingOffset);
				}

				case FieldInfo.DocValuesType.NUMERIC:
				{
					Lucene45DocValuesProducer.NumericEntry ne = numerics[field.number];
					return GetMissingBits(ne.missingOffset);
				}

				default:
				{
					throw new Exception();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override void Dispose(bool disposing)
		{
			data.Dispose();
		}

		/// <summary>metadata entry for a numeric docvalues field</summary>
		protected internal class NumericEntry
		{
			public NumericEntry()
			{
			}

			/// <summary>offset to the bitset representing docsWithField, or -1 if no documents have missing values
			/// 	</summary>
			internal long missingOffset;

			/// <summary>offset to the actual numeric values</summary>
			public long offset;

			internal int format;

			/// <summary>packed ints version used to encode these numerics</summary>
			public int packedIntsVersion;

			/// <summary>count of values written</summary>
			public long count;

			/// <summary>packed ints blocksize</summary>
			public int blockSize;

			internal long minValue;

			internal long gcd;

			internal long[] table;
		}

		/// <summary>metadata entry for a binary docvalues field</summary>
		protected internal class BinaryEntry
		{
			public BinaryEntry()
			{
			}

			/// <summary>offset to the bitset representing docsWithField, or -1 if no documents have missing values
			/// 	</summary>
			internal long missingOffset;

			/// <summary>offset to the actual binary values</summary>
			internal long offset;

			internal int format;

			/// <summary>count of values written</summary>
			public long count;

			internal int minLength;

			internal int maxLength;

			/// <summary>offset to the addressing data that maps a value to its slice of the byte[]
			/// 	</summary>
			public long addressesOffset;

			/// <summary>interval of shared prefix chunks (when using prefix-compressed binary)</summary>
			public long addressInterval;

			/// <summary>packed ints version used to encode addressing information</summary>
			public int packedIntsVersion;

			/// <summary>packed ints blocksize</summary>
			public int blockSize;
		}

		/// <summary>metadata entry for a sorted-set docvalues field</summary>
		protected internal class SortedSetEntry
		{
			public SortedSetEntry()
			{
			}

			internal int format;
		}

		internal abstract class LongBinaryDocValues : BinaryDocValues
		{
			// internally we compose complex dv (sorted/sortedset) from other ones
			public sealed override void Get(int docID, BytesRef result)
			{
				Get((long)docID, result);
			}

			internal abstract void Get(long id, BytesRef Result);
		}

		internal class CompressedBinaryDocValues : Lucene45DocValuesProducer.LongBinaryDocValues
		{
			internal readonly Lucene45DocValuesProducer.BinaryEntry bytes;

			internal readonly long interval;

			internal readonly long numValues;

			internal readonly long numIndexValues;

			internal readonly MonotonicBlockPackedReader addresses;

			internal readonly IndexInput data;

			internal readonly TermsEnum termsEnum;

			/// <exception cref="System.IO.IOException"></exception>
			public CompressedBinaryDocValues(Lucene45DocValuesProducer.BinaryEntry bytes, MonotonicBlockPackedReader
				 addresses, IndexInput data)
			{
				// in the compressed case, we add a few additional operations for
				// more efficient reverse lookup and enumeration
				this.bytes = bytes;
				this.interval = bytes.addressInterval;
				this.addresses = addresses;
				this.data = data;
				this.numValues = bytes.count;
				this.numIndexValues = addresses.Size;
				this.termsEnum = GetTermsEnum(data);
			}

			internal override void Get(long id, BytesRef result)
			{
			    termsEnum.SeekExact(id);
			    BytesRef term = termsEnum.Term;
			    result.bytes = term.bytes;
			    result.offset = term.offset;
			    result.length = term.length;
			}

			internal virtual long LookupTerm(BytesRef key)
			{
			    TermsEnum.SeekStatus status = termsEnum.SeekCeil(key);
			    return status == TermsEnum.SeekStatus.END
			        ? -numValues - 1
			        : (status == TermsEnum.SeekStatus.FOUND ? termsEnum.Ord : -termsEnum.Ord - 1);
			}

			internal virtual TermsEnum GetTermsEnum()
			{
			    return GetTermsEnum(((IndexInput)data.Clone()));
			}

		    /// <exception cref="System.IO.IOException"></exception>
			private TermsEnum GetTermsEnum(IndexInput input)
			{
				input.Seek(bytes.offset);
				return new AnonymousTermsEnum1(this, input);
			}

			private sealed class AnonymousTermsEnum1 : TermsEnum
			{
				public AnonymousTermsEnum1(CompressedBinaryDocValues _enclosing, IndexInput input)
				{
					this._enclosing = _enclosing;
					this.input = input;
					this.currentOrd = -1;
					this.termBuffer = new BytesRef(this._enclosing.bytes.maxLength < 0 ? 0 : this._enclosing
						.bytes.maxLength);
					this.term = new BytesRef();
				}

				private long currentOrd;

				private readonly BytesRef termBuffer;

				private readonly BytesRef term;

				// TODO: maxLength is negative when all terms are merged away...
				// TODO: paranoia?
				/// <exception cref="System.IO.IOException"></exception>
				public override BytesRef Next()
				{
					if (this.DoNext() == null)
					{
						return null;
					}
				    this.SetTerm();
				    return this.term;
				}

				/// <exception cref="System.IO.IOException"></exception>
				private BytesRef DoNext()
				{
					if (++this.currentOrd >= this._enclosing.numValues)
					{
						return null;
					}
				    int start = input.ReadVInt();
				    int suffix = input.ReadVInt();
				    input.ReadBytes(this.termBuffer.bytes, start, suffix);
				    this.termBuffer.length = start + suffix;
				    return this.termBuffer;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override TermsEnum.SeekStatus SeekCeil(BytesRef text)
				{
					// binary-search just the index values to find the block,
					// then scan within the block
					long low = 0;
					long high = this._enclosing.numIndexValues - 1;
					while (low <= high)
					{
						long mid = (long)(((ulong)(low + high)) >> 1);
						this.DoSeek(mid * this._enclosing.interval);
						int cmp = this.termBuffer.CompareTo(text);
						if (cmp < 0)
						{
							low = mid + 1;
						}
						else
						{
							if (cmp > 0)
							{
								high = mid - 1;
							}
							else
							{
								// we got lucky, found an indexed term
								this.SetTerm();
								return TermsEnum.SeekStatus.FOUND;
							}
						}
					}
					if (this._enclosing.numIndexValues == 0)
					{
						return TermsEnum.SeekStatus.END;
					}
					// block before insertion point
					long block = low - 1;
					this.DoSeek(block < 0 ? -1 : block * this._enclosing.interval);
					while (this.DoNext() != null)
					{
						int cmp = this.termBuffer.CompareTo(text);
						if (cmp == 0)
						{
							this.SetTerm();
							return TermsEnum.SeekStatus.FOUND;
						}
						else
						{
							if (cmp > 0)
							{
								this.SetTerm();
								return TermsEnum.SeekStatus.NOT_FOUND;
							}
						}
					}
					return TermsEnum.SeekStatus.END;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override void SeekExact(long ord)
				{
					this.DoSeek(ord);
					this.SetTerm();
				}

				/// <exception cref="System.IO.IOException"></exception>
				private void DoSeek(long ord)
				{
					long block = ord / this._enclosing.interval;
					if (ord >= this.currentOrd && block == this.currentOrd / this._enclosing.interval)
					{
					}
					else
					{
						// seek within current block
						// position before start of block
						this.currentOrd = ord - ord % this._enclosing.interval - 1;
						input.Seek(this._enclosing.bytes.offset + this._enclosing.addresses.Get(block));
					}
					while (this.currentOrd < ord)
					{
						this.DoNext();
					}
				}

				private void SetTerm()
				{
					// TODO: is there a cleaner way
					this.term.bytes = new sbyte[this.termBuffer.length];
					this.term.offset = 0;
					this.term.CopyBytes(this.termBuffer);
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override BytesRef Term
				{
				    get { return this.term; }
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override long Ord
				{
				    get { return this.currentOrd; }
				}

				public override IComparer<BytesRef> Comparator
				{
				    get { return BytesRef.UTF8SortedAsUnicodeComparer; }
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override int DocFreq
				{
				    get { throw new NotSupportedException(); }
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override long TotalTermFreq
				{
				    get { return -1; }
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override DocsEnum Docs(IBits liveDocs, DocsEnum reuse, int flags)
				{
					throw new NotSupportedException();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override DocsAndPositionsEnum DocsAndPositions(IBits liveDocs, DocsAndPositionsEnum
					 reuse, int flags)
				{
					throw new NotSupportedException();
				}

				private readonly CompressedBinaryDocValues _enclosing;

				private readonly IndexInput input;
			}
		}
	}
}
