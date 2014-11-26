/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>
	/// Reader for
	/// <see cref="DirectDocValuesFormat">DirectDocValuesFormat</see>
	/// </summary>
	internal class DirectDocValuesProducer : DocValuesProducer
	{
		private readonly IDictionary<int, DirectDocValuesProducer.NumericEntry> numerics = 
			new Dictionary<int, DirectDocValuesProducer.NumericEntry>();

		private readonly IDictionary<int, DirectDocValuesProducer.BinaryEntry> binaries = 
			new Dictionary<int, DirectDocValuesProducer.BinaryEntry>();

		private readonly IDictionary<int, DirectDocValuesProducer.SortedEntry> sorteds = 
			new Dictionary<int, DirectDocValuesProducer.SortedEntry>();

		private readonly IDictionary<int, DirectDocValuesProducer.SortedSetEntry> sortedSets
			 = new Dictionary<int, DirectDocValuesProducer.SortedSetEntry>();

		private readonly IndexInput data;

		private readonly IDictionary<int, NumericDocValues> numericInstances = new Dictionary
			<int, NumericDocValues>();

		private readonly IDictionary<int, BinaryDocValues> binaryInstances = new Dictionary
			<int, BinaryDocValues>();

		private readonly IDictionary<int, SortedDocValues> sortedInstances = new Dictionary
			<int, SortedDocValues>();

		private readonly IDictionary<int, DirectDocValuesProducer.SortedSetRawValues> sortedSetInstances
			 = new Dictionary<int, DirectDocValuesProducer.SortedSetRawValues>();

		private readonly IDictionary<int, Bits> docsWithFieldInstances = new Dictionary<int
			, Bits>();

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
			maxDoc = state.segmentInfo.GetDocCount();
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
					IOUtils.CloseWhileHandlingException(@in);
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
					IOUtils.CloseWhileHandlingException(this.data);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private DirectDocValuesProducer.NumericEntry ReadNumericEntry(IndexInput meta)
		{
			DirectDocValuesProducer.NumericEntry entry = new DirectDocValuesProducer.NumericEntry
				();
			entry.offset = meta.ReadLong();
			entry.count = meta.ReadInt();
			entry.missingOffset = meta.ReadLong();
			if (entry.missingOffset != -1)
			{
				entry.missingBytes = meta.ReadLong();
			}
			else
			{
				entry.missingBytes = 0;
			}
			entry.byteWidth = meta.ReadByte();
			return entry;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private DirectDocValuesProducer.BinaryEntry ReadBinaryEntry(IndexInput meta)
		{
			DirectDocValuesProducer.BinaryEntry entry = new DirectDocValuesProducer.BinaryEntry
				();
			entry.offset = meta.ReadLong();
			entry.numBytes = meta.ReadInt();
			entry.count = meta.ReadInt();
			entry.missingOffset = meta.ReadLong();
			if (entry.missingOffset != -1)
			{
				entry.missingBytes = meta.ReadLong();
			}
			else
			{
				entry.missingBytes = 0;
			}
			return entry;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private DirectDocValuesProducer.SortedEntry ReadSortedEntry(IndexInput meta)
		{
			DirectDocValuesProducer.SortedEntry entry = new DirectDocValuesProducer.SortedEntry
				();
			entry.docToOrd = ReadNumericEntry(meta);
			entry.values = ReadBinaryEntry(meta);
			return entry;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private DirectDocValuesProducer.SortedSetEntry ReadSortedSetEntry(IndexInput meta
			)
		{
			DirectDocValuesProducer.SortedSetEntry entry = new DirectDocValuesProducer.SortedSetEntry
				();
			entry.docToOrdAddress = ReadNumericEntry(meta);
			entry.ords = ReadNumericEntry(meta);
			entry.values = ReadBinaryEntry(meta);
			return entry;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadFields(IndexInput meta)
		{
			int fieldNumber = meta.ReadVInt();
			while (fieldNumber != -1)
			{
				int fieldType = meta.ReadByte();
				if (fieldType == NUMBER)
				{
					numerics.Put(fieldNumber, ReadNumericEntry(meta));
				}
				else
				{
					if (fieldType == BYTES)
					{
						binaries.Put(fieldNumber, ReadBinaryEntry(meta));
					}
					else
					{
						if (fieldType == SORTED)
						{
							sorteds.Put(fieldNumber, ReadSortedEntry(meta));
						}
						else
						{
							if (fieldType == SORTED_SET)
							{
								sortedSets.Put(fieldNumber, ReadSortedSetEntry(meta));
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

		public override long RamBytesUsed()
		{
			return ramBytesUsed.Get();
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
				NumericDocValues instance = numericInstances.Get(field.number);
				if (instance == null)
				{
					// Lazy load
					instance = LoadNumeric(numerics.Get(field.number));
					numericInstances.Put(field.number, instance);
				}
				return instance;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private NumericDocValues LoadNumeric(DirectDocValuesProducer.NumericEntry entry)
		{
			data.Seek(entry.offset + entry.missingBytes);
			switch (entry.byteWidth)
			{
				case 1:
				{
					byte[] values = new byte[entry.count];
					data.ReadBytes(values, 0, entry.count);
					ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
					return new _NumericDocValues_222(values);
				}

				case 2:
				{
					short[] values = new short[entry.count];
					for (int i = 0; i < entry.count; i++)
					{
						values[i] = data.ReadShort();
					}
					ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
					return new _NumericDocValues_237(values);
				}

				case 4:
				{
					int[] values = new int[entry.count];
					for (int i = 0; i < entry.count; i++)
					{
						values[i] = data.ReadInt();
					}
					ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
					return new _NumericDocValues_252(values);
				}

				case 8:
				{
					long[] values = new long[entry.count];
					for (int i = 0; i < entry.count; i++)
					{
						values[i] = data.ReadLong();
					}
					ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(values));
					return new _NumericDocValues_267(values);
				}

				default:
				{
					throw new Exception();
				}
			}
		}

		private sealed class _NumericDocValues_222 : NumericDocValues
		{
			public _NumericDocValues_222(byte[] values)
			{
				this.values = values;
			}

			public override long Get(int idx)
			{
				return values[idx];
			}

			private readonly byte[] values;
		}

		private sealed class _NumericDocValues_237 : NumericDocValues
		{
			public _NumericDocValues_237(short[] values)
			{
				this.values = values;
			}

			public override long Get(int idx)
			{
				return values[idx];
			}

			private readonly short[] values;
		}

		private sealed class _NumericDocValues_252 : NumericDocValues
		{
			public _NumericDocValues_252(int[] values)
			{
				this.values = values;
			}

			public override long Get(int idx)
			{
				return values[idx];
			}

			private readonly int[] values;
		}

		private sealed class _NumericDocValues_267 : NumericDocValues
		{
			public _NumericDocValues_267(long[] values)
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
				BinaryDocValues instance = binaryInstances.Get(field.number);
				if (instance == null)
				{
					// Lazy load
					instance = LoadBinary(binaries.Get(field.number));
					binaryInstances.Put(field.number, instance);
				}
				return instance;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private BinaryDocValues LoadBinary(DirectDocValuesProducer.BinaryEntry entry)
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
			return new _BinaryDocValues_305(bytes, address);
		}

		private sealed class _BinaryDocValues_305 : BinaryDocValues
		{
			public _BinaryDocValues_305(byte[] bytes, int[] address)
			{
				this.bytes = bytes;
				this.address = address;
			}

			public override void Get(int docID, BytesRef result)
			{
				result.bytes = bytes;
				result.offset = address[docID];
				result.length = address[docID + 1] - result.offset;
			}

			private readonly byte[] bytes;

			private readonly int[] address;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override SortedDocValues GetSorted(FieldInfo field)
		{
			lock (this)
			{
				SortedDocValues instance = sortedInstances.Get(field.number);
				if (instance == null)
				{
					// Lazy load
					instance = LoadSorted(field);
					sortedInstances.Put(field.number, instance);
				}
				return instance;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private SortedDocValues LoadSorted(FieldInfo field)
		{
			DirectDocValuesProducer.SortedEntry entry = sorteds.Get(field.number);
			NumericDocValues docToOrd = LoadNumeric(entry.docToOrd);
			BinaryDocValues values = LoadBinary(entry.values);
			return new _SortedDocValues_331(docToOrd, values, entry);
		}

		private sealed class _SortedDocValues_331 : SortedDocValues
		{
			public _SortedDocValues_331(NumericDocValues docToOrd, BinaryDocValues values, DirectDocValuesProducer.SortedEntry
				 entry)
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

			public override int GetValueCount()
			{
				return entry.values.count;
			}

			private readonly NumericDocValues docToOrd;

			private readonly BinaryDocValues values;

			private readonly DirectDocValuesProducer.SortedEntry entry;
		}

		// Leave lookupTerm to super's binary search
		// Leave termsEnum to super
		/// <exception cref="System.IO.IOException"></exception>
		public override SortedSetDocValues GetSortedSet(FieldInfo field)
		{
			lock (this)
			{
				DirectDocValuesProducer.SortedSetRawValues instance = sortedSetInstances.Get(field
					.number);
				DirectDocValuesProducer.SortedSetEntry entry = sortedSets.Get(field.number);
				if (instance == null)
				{
					// Lazy load
					instance = LoadSortedSet(entry);
					sortedSetInstances.Put(field.number, instance);
				}
				NumericDocValues docToOrdAddress = instance.docToOrdAddress;
				NumericDocValues ords = instance.ords;
				BinaryDocValues values = instance.values;
				// Must make a new instance since the iterator has state:
				return new _RandomAccessOrds_369(ords, docToOrdAddress, values, entry);
			}
		}

		private sealed class _RandomAccessOrds_369 : RandomAccessOrds
		{
			public _RandomAccessOrds_369(NumericDocValues ords, NumericDocValues docToOrdAddress
				, BinaryDocValues values, DirectDocValuesProducer.SortedSetEntry entry)
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
				if (this.ordUpto == this.ordLimit)
				{
					return SortedSetDocValues.NO_MORE_ORDS;
				}
				else
				{
					return ords.Get(this.ordUpto++);
				}
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

			public override long GetValueCount()
			{
				return entry.values.count;
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

			private readonly DirectDocValuesProducer.SortedSetEntry entry;
		}

		// Leave lookupTerm to super's binary search
		// Leave termsEnum to super
		/// <exception cref="System.IO.IOException"></exception>
		private DirectDocValuesProducer.SortedSetRawValues LoadSortedSet(DirectDocValuesProducer.SortedSetEntry
			 entry)
		{
			DirectDocValuesProducer.SortedSetRawValues instance = new DirectDocValuesProducer.SortedSetRawValues
				();
			instance.docToOrdAddress = LoadNumeric(entry.docToOrdAddress);
			instance.ords = LoadNumeric(entry.ords);
			instance.values = LoadBinary(entry.values);
			return instance;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private Bits GetMissingBits(int fieldNumber, long offset, long length)
		{
			if (offset == -1)
			{
				return new Bits.MatchAllBits(maxDoc);
			}
			else
			{
				Bits instance;
				lock (this)
				{
					instance = docsWithFieldInstances.Get(fieldNumber);
					if (instance == null)
					{
						IndexInput data = ((IndexInput)this.data.Clone());
						data.Seek(offset);
						//HM:revisit 
						//assert length % 8 == 0;
						long[] bits = new long[(int)length >> 3];
						for (int i = 0; i < bits.Length; i++)
						{
							bits[i] = data.ReadLong();
						}
						instance = new FixedBitSet(bits, maxDoc);
						docsWithFieldInstances.Put(fieldNumber, instance);
					}
				}
				return instance;
			}
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
					DirectDocValuesProducer.BinaryEntry be = binaries.Get(field.number);
					return GetMissingBits(field.number, be.missingOffset, be.missingBytes);
				}

				case FieldInfo.DocValuesType.NUMERIC:
				{
					DirectDocValuesProducer.NumericEntry ne = numerics.Get(field.number);
					return GetMissingBits(field.number, ne.missingOffset, ne.missingBytes);
				}

				default:
				{
					throw new Exception();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			data.Close();
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
