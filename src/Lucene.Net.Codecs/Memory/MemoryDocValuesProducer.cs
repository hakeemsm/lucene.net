/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;
using Sharpen;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>
	/// Reader for
	/// <see cref="MemoryDocValuesFormat">MemoryDocValuesFormat</see>
	/// </summary>
	internal class MemoryDocValuesProducer : DocValuesProducer
	{
		private readonly IDictionary<int, MemoryDocValuesProducer.NumericEntry> numerics;

		private readonly IDictionary<int, MemoryDocValuesProducer.BinaryEntry> binaries;

		private readonly IDictionary<int, MemoryDocValuesProducer.FSTEntry> fsts;

		private readonly IndexInput data;

		private readonly IDictionary<int, NumericDocValues> numericInstances = new Dictionary
			<int, NumericDocValues>();

		private readonly IDictionary<int, BinaryDocValues> binaryInstances = new Dictionary
			<int, BinaryDocValues>();

		private readonly IDictionary<int, FST<long>> fstInstances = new Dictionary<int, FST
			<long>>();

		private readonly IDictionary<int, Bits> docsWithFieldInstances = new Dictionary<int
			, Bits>();

		private readonly int maxDoc;

		private readonly AtomicLong ramBytesUsed;

		private readonly int version;

		internal const byte NUMBER = 0;

		internal const byte BYTES = 1;

		internal const byte FST = 2;

		internal const int BLOCK_SIZE = 4096;

		internal const byte DELTA_COMPRESSED = 0;

		internal const byte TABLE_COMPRESSED = 1;

		internal const byte UNCOMPRESSED = 2;

		internal const byte GCD_COMPRESSED = 3;

		internal const int VERSION_START = 0;

		internal const int VERSION_GCD_COMPRESSION = 1;

		internal const int VERSION_CHECKSUM = 2;

		internal const int VERSION_CURRENT = VERSION_CHECKSUM;

		/// <exception cref="System.IO.IOException"></exception>
		internal MemoryDocValuesProducer(SegmentReadState state, string dataCodec, string
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
			bool success = false;
			try
			{
				version = CodecUtil.CheckHeader(@in, metaCodec, VERSION_START, VERSION_CURRENT);
				numerics = new Dictionary<int, MemoryDocValuesProducer.NumericEntry>();
				binaries = new Dictionary<int, MemoryDocValuesProducer.BinaryEntry>();
				fsts = new Dictionary<int, MemoryDocValuesProducer.FSTEntry>();
				ReadFields(@in, state.fieldInfos);
				if (version >= VERSION_CHECKSUM)
				{
					CodecUtil.CheckFooter(@in);
				}
				else
				{
					CodecUtil.CheckEOF(@in);
				}
				ramBytesUsed = new AtomicLong(RamUsageEstimator.ShallowSizeOfInstance(GetType()));
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
		private void ReadFields(IndexInput meta, FieldInfos infos)
		{
			int fieldNumber = meta.ReadVInt();
			while (fieldNumber != -1)
			{
				int fieldType = meta.ReadByte();
				if (fieldType == NUMBER)
				{
					MemoryDocValuesProducer.NumericEntry entry = new MemoryDocValuesProducer.NumericEntry
						();
					entry.offset = meta.ReadLong();
					entry.missingOffset = meta.ReadLong();
					if (entry.missingOffset != -1)
					{
						entry.missingBytes = meta.ReadLong();
					}
					else
					{
						entry.missingBytes = 0;
					}
					entry.format = meta.ReadByte();
					switch (entry.format)
					{
						case DELTA_COMPRESSED:
						case TABLE_COMPRESSED:
						case GCD_COMPRESSED:
						case UNCOMPRESSED:
						{
							break;
						}

						default:
						{
							throw new CorruptIndexException("Unknown format: " + entry.format + ", input=" + 
								meta);
						}
					}
					if (entry.format != UNCOMPRESSED)
					{
						entry.packedIntsVersion = meta.ReadVInt();
					}
					numerics.Put(fieldNumber, entry);
				}
				else
				{
					if (fieldType == BYTES)
					{
						MemoryDocValuesProducer.BinaryEntry entry = new MemoryDocValuesProducer.BinaryEntry
							();
						entry.offset = meta.ReadLong();
						entry.numBytes = meta.ReadLong();
						entry.missingOffset = meta.ReadLong();
						if (entry.missingOffset != -1)
						{
							entry.missingBytes = meta.ReadLong();
						}
						else
						{
							entry.missingBytes = 0;
						}
						entry.minLength = meta.ReadVInt();
						entry.maxLength = meta.ReadVInt();
						if (entry.minLength != entry.maxLength)
						{
							entry.packedIntsVersion = meta.ReadVInt();
							entry.blockSize = meta.ReadVInt();
						}
						binaries.Put(fieldNumber, entry);
					}
					else
					{
						if (fieldType == FST)
						{
							MemoryDocValuesProducer.FSTEntry entry = new MemoryDocValuesProducer.FSTEntry();
							entry.offset = meta.ReadLong();
							entry.numOrds = meta.ReadVLong();
							fsts.Put(fieldNumber, entry);
						}
						else
						{
							throw new CorruptIndexException("invalid entry type: " + fieldType + ", input=" +
								 meta);
						}
					}
				}
				fieldNumber = meta.ReadVInt();
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
					instance = LoadNumeric(field);
					numericInstances.Put(field.number, instance);
				}
				return instance;
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
		private NumericDocValues LoadNumeric(FieldInfo field)
		{
			MemoryDocValuesProducer.NumericEntry entry = numerics.Get(field.number);
			data.Seek(entry.offset + entry.missingBytes);
			switch (entry.format)
			{
				case TABLE_COMPRESSED:
				{
					int size = data.ReadVInt();
					if (size > 256)
					{
						throw new CorruptIndexException("TABLE_COMPRESSED cannot have more than 256 distinct values, input="
							 + data);
					}
					long[] decode = new long[size];
					for (int i = 0; i < decode.Length; i++)
					{
						decode[i] = data.ReadLong();
					}
					int formatID = data.ReadVInt();
					int bitsPerValue = data.ReadVInt();
					PackedInts.Reader ordsReader = PackedInts.GetReaderNoHeader(data, PackedInts.Format
						.ById(formatID), entry.packedIntsVersion, maxDoc, bitsPerValue);
					ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(decode) + ordsReader.RamBytesUsed
						());
					return new _NumericDocValues_244(decode, ordsReader);
				}

				case DELTA_COMPRESSED:
				{
					int blockSize = data.ReadVInt();
					BlockPackedReader reader = new BlockPackedReader(data, entry.packedIntsVersion, blockSize
						, maxDoc, false);
					ramBytesUsed.AddAndGet(reader.RamBytesUsed());
					return reader;
				}

				case UNCOMPRESSED:
				{
					byte[] bytes = new byte[maxDoc];
					data.ReadBytes(bytes, 0, bytes.Length);
					ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(bytes));
					return new _NumericDocValues_259(bytes);
				}

				case GCD_COMPRESSED:
				{
					long min = data.ReadLong();
					long mult = data.ReadLong();
					int quotientBlockSize = data.ReadVInt();
					BlockPackedReader quotientReader = new BlockPackedReader(data, entry.packedIntsVersion
						, quotientBlockSize, maxDoc, false);
					ramBytesUsed.AddAndGet(quotientReader.RamBytesUsed());
					return new _NumericDocValues_271(min, mult, quotientReader);
				}

				default:
				{
					throw new Exception();
				}
			}
		}

		private sealed class _NumericDocValues_244 : NumericDocValues
		{
			public _NumericDocValues_244(long[] decode, PackedInts.Reader ordsReader)
			{
				this.decode = decode;
				this.ordsReader = ordsReader;
			}

			public override long Get(int docID)
			{
				return decode[(int)ordsReader.Get(docID)];
			}

			private readonly long[] decode;

			private readonly PackedInts.Reader ordsReader;
		}

		private sealed class _NumericDocValues_259 : NumericDocValues
		{
			public _NumericDocValues_259(byte[] bytes)
			{
				this.bytes = bytes;
			}

			public override long Get(int docID)
			{
				return bytes[docID];
			}

			private readonly byte[] bytes;
		}

		private sealed class _NumericDocValues_271 : NumericDocValues
		{
			public _NumericDocValues_271(long min, long mult, BlockPackedReader quotientReader
				)
			{
				this.min = min;
				this.mult = mult;
				this.quotientReader = quotientReader;
			}

			public override long Get(int docID)
			{
				return min + mult * quotientReader.Get(docID);
			}

			private readonly long min;

			private readonly long mult;

			private readonly BlockPackedReader quotientReader;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override BinaryDocValues GetBinary(FieldInfo field)
		{
			lock (this)
			{
				BinaryDocValues instance = binaryInstances.Get(field.number);
				if (instance == null)
				{
					instance = LoadBinary(field);
					binaryInstances.Put(field.number, instance);
				}
				return instance;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private BinaryDocValues LoadBinary(FieldInfo field)
		{
			MemoryDocValuesProducer.BinaryEntry entry = binaries.Get(field.number);
			data.Seek(entry.offset);
			PagedBytes bytes = new PagedBytes(16);
			bytes.Copy(data, entry.numBytes);
			PagedBytes.Reader bytesReader = bytes.Freeze(true);
			if (entry.minLength == entry.maxLength)
			{
				int fixedLength = entry.minLength;
				ramBytesUsed.AddAndGet(bytes.RamBytesUsed());
				return new _BinaryDocValues_301(bytesReader, fixedLength);
			}
			else
			{
				data.Seek(data.GetFilePointer() + entry.missingBytes);
				MonotonicBlockPackedReader addresses = new MonotonicBlockPackedReader(data, entry
					.packedIntsVersion, entry.blockSize, maxDoc, false);
				ramBytesUsed.AddAndGet(bytes.RamBytesUsed() + addresses.RamBytesUsed());
				return new _BinaryDocValues_311(addresses, bytesReader);
			}
		}

		private sealed class _BinaryDocValues_301 : BinaryDocValues
		{
			public _BinaryDocValues_301(PagedBytes.Reader bytesReader, int fixedLength)
			{
				this.bytesReader = bytesReader;
				this.fixedLength = fixedLength;
			}

			public override void Get(int docID, BytesRef result)
			{
				bytesReader.FillSlice(result, fixedLength * (long)docID, fixedLength);
			}

			private readonly PagedBytes.Reader bytesReader;

			private readonly int fixedLength;
		}

		private sealed class _BinaryDocValues_311 : BinaryDocValues
		{
			public _BinaryDocValues_311(MonotonicBlockPackedReader addresses, PagedBytes.Reader
				 bytesReader)
			{
				this.addresses = addresses;
				this.bytesReader = bytesReader;
			}

			public override void Get(int docID, BytesRef result)
			{
				long startAddress = docID == 0 ? 0 : addresses.Get(docID - 1);
				long endAddress = addresses.Get(docID);
				bytesReader.FillSlice(result, startAddress, (int)(endAddress - startAddress));
			}

			private readonly MonotonicBlockPackedReader addresses;

			private readonly PagedBytes.Reader bytesReader;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override SortedDocValues GetSorted(FieldInfo field)
		{
			MemoryDocValuesProducer.FSTEntry entry = fsts.Get(field.number);
			if (entry.numOrds == 0)
			{
				return DocValues.EMPTY_SORTED;
			}
			FST<long> instance;
			lock (this)
			{
				instance = fstInstances.Get(field.number);
				if (instance == null)
				{
					data.Seek(entry.offset);
					instance = new FST<long>(data, PositiveIntOutputs.GetSingleton());
					ramBytesUsed.AddAndGet(instance.SizeInBytes());
					fstInstances.Put(field.number, instance);
				}
			}
			NumericDocValues docToOrd = GetNumeric(field);
			FST<long> fst = instance;
			// per-thread resources
			FST.BytesReader @in = fst.GetBytesReader();
			FST.Arc<long> firstArc = new FST.Arc<long>();
			FST.Arc<long> scratchArc = new FST.Arc<long>();
			IntsRef scratchInts = new IntsRef();
			BytesRefFSTEnum<long> fstEnum = new BytesRefFSTEnum<long>(fst);
			return new _SortedDocValues_348(docToOrd, @in, fst, firstArc, scratchArc, scratchInts
				, fstEnum, entry);
		}

		private sealed class _SortedDocValues_348 : SortedDocValues
		{
			public _SortedDocValues_348(NumericDocValues docToOrd, FST.BytesReader @in, FST<long
				> fst, FST.Arc<long> firstArc, FST.Arc<long> scratchArc, IntsRef scratchInts, BytesRefFSTEnum
				<long> fstEnum, MemoryDocValuesProducer.FSTEntry entry)
			{
				this.docToOrd = docToOrd;
				this.@in = @in;
				this.fst = fst;
				this.firstArc = firstArc;
				this.scratchArc = scratchArc;
				this.scratchInts = scratchInts;
				this.fstEnum = fstEnum;
				this.entry = entry;
			}

			public override int GetOrd(int docID)
			{
				return (int)docToOrd.Get(docID);
			}

			public override void LookupOrd(int ord, BytesRef result)
			{
				try
				{
					@in.SetPosition(0);
					fst.GetFirstArc(firstArc);
					IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(fst, ord, @in, firstArc
						, scratchArc, scratchInts);
					result.bytes = new byte[output.length];
					result.offset = 0;
					result.length = 0;
					Lucene.Net.Util.Fst.Util.ToBytesRef(output, result);
				}
				catch (IOException bogus)
				{
					throw new RuntimeException(bogus);
				}
			}

			public override int LookupTerm(BytesRef key)
			{
				try
				{
					BytesRefFSTEnum.InputOutput<long> o = fstEnum.SeekCeil(key);
					if (o == null)
					{
						return -this.GetValueCount() - 1;
					}
					else
					{
						if (o.input.Equals(key))
						{
							return o.output;
						}
						else
						{
							return (int)-o.output - 1;
						}
					}
				}
				catch (IOException bogus)
				{
					throw new RuntimeException(bogus);
				}
			}

			public override int GetValueCount()
			{
				return (int)entry.numOrds;
			}

			public override TermsEnum TermsEnum()
			{
				return new MemoryDocValuesProducer.FSTTermsEnum(fst);
			}

			private readonly NumericDocValues docToOrd;

			private readonly FST.BytesReader @in;

			private readonly FST<long> fst;

			private readonly FST.Arc<long> firstArc;

			private readonly FST.Arc<long> scratchArc;

			private readonly IntsRef scratchInts;

			private readonly BytesRefFSTEnum<long> fstEnum;

			private readonly MemoryDocValuesProducer.FSTEntry entry;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override SortedSetDocValues GetSortedSet(FieldInfo field)
		{
			MemoryDocValuesProducer.FSTEntry entry = fsts.Get(field.number);
			if (entry.numOrds == 0)
			{
				return DocValues.EMPTY_SORTED_SET;
			}
			// empty FST!
			FST<long> instance;
			lock (this)
			{
				instance = fstInstances.Get(field.number);
				if (instance == null)
				{
					data.Seek(entry.offset);
					instance = new FST<long>(data, PositiveIntOutputs.GetSingleton());
					ramBytesUsed.AddAndGet(instance.SizeInBytes());
					fstInstances.Put(field.number, instance);
				}
			}
			BinaryDocValues docToOrds = GetBinary(field);
			FST<long> fst = instance;
			// per-thread resources
			FST.BytesReader @in = fst.GetBytesReader();
			FST.Arc<long> firstArc = new FST.Arc<long>();
			FST.Arc<long> scratchArc = new FST.Arc<long>();
			IntsRef scratchInts = new IntsRef();
			BytesRefFSTEnum<long> fstEnum = new BytesRefFSTEnum<long>(fst);
			BytesRef @ref = new BytesRef();
			ByteArrayDataInput input = new ByteArrayDataInput();
			return new _SortedSetDocValues_424(input, docToOrds, @ref, @in, fst, firstArc, scratchArc
				, scratchInts, fstEnum, entry);
		}

		private sealed class _SortedSetDocValues_424 : SortedSetDocValues
		{
			public _SortedSetDocValues_424(ByteArrayDataInput input, BinaryDocValues docToOrds
				, BytesRef @ref, FST.BytesReader @in, FST<long> fst, FST.Arc<long> firstArc, FST.Arc
				<long> scratchArc, IntsRef scratchInts, BytesRefFSTEnum<long> fstEnum, MemoryDocValuesProducer.FSTEntry
				 entry)
			{
				this.input = input;
				this.docToOrds = docToOrds;
				this.@ref = @ref;
				this.@in = @in;
				this.fst = fst;
				this.firstArc = firstArc;
				this.scratchArc = scratchArc;
				this.scratchInts = scratchInts;
				this.fstEnum = fstEnum;
				this.entry = entry;
			}

			internal long currentOrd;

			public override long NextOrd()
			{
				if (input.Eof())
				{
					return SortedSetDocValues.NO_MORE_ORDS;
				}
				else
				{
					this.currentOrd += input.ReadVLong();
					return this.currentOrd;
				}
			}

			public override void SetDocument(int docID)
			{
				docToOrds.Get(docID, @ref);
				input.Reset(@ref.bytes, @ref.offset, @ref.length);
				this.currentOrd = 0;
			}

			public override void LookupOrd(long ord, BytesRef result)
			{
				try
				{
					@in.SetPosition(0);
					fst.GetFirstArc(firstArc);
					IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(fst, ord, @in, firstArc
						, scratchArc, scratchInts);
					result.bytes = new byte[output.length];
					result.offset = 0;
					result.length = 0;
					Lucene.Net.Util.Fst.Util.ToBytesRef(output, result);
				}
				catch (IOException bogus)
				{
					throw new RuntimeException(bogus);
				}
			}

			public override long LookupTerm(BytesRef key)
			{
				try
				{
					BytesRefFSTEnum.InputOutput<long> o = fstEnum.SeekCeil(key);
					if (o == null)
					{
						return -this.GetValueCount() - 1;
					}
					else
					{
						if (o.input.Equals(key))
						{
							return o.output;
						}
						else
						{
							return -o.output - 1;
						}
					}
				}
				catch (IOException bogus)
				{
					throw new RuntimeException(bogus);
				}
			}

			public override long GetValueCount()
			{
				return entry.numOrds;
			}

			public override TermsEnum TermsEnum()
			{
				return new MemoryDocValuesProducer.FSTTermsEnum(fst);
			}

			private readonly ByteArrayDataInput input;

			private readonly BinaryDocValues docToOrds;

			private readonly BytesRef @ref;

			private readonly FST.BytesReader @in;

			private readonly FST<long> fst;

			private readonly FST.Arc<long> firstArc;

			private readonly FST.Arc<long> scratchArc;

			private readonly IntsRef scratchInts;

			private readonly BytesRefFSTEnum<long> fstEnum;

			private readonly MemoryDocValuesProducer.FSTEntry entry;
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
					MemoryDocValuesProducer.BinaryEntry be = binaries.Get(field.number);
					return GetMissingBits(field.number, be.missingOffset, be.missingBytes);
				}

				case FieldInfo.DocValuesType.NUMERIC:
				{
					MemoryDocValuesProducer.NumericEntry ne = numerics.Get(field.number);
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

		internal class NumericEntry
		{
			internal long offset;

			internal long missingOffset;

			internal long missingBytes;

			internal byte format;

			internal int packedIntsVersion;
		}

		internal class BinaryEntry
		{
			internal long offset;

			internal long missingOffset;

			internal long missingBytes;

			internal long numBytes;

			internal int minLength;

			internal int maxLength;

			internal int packedIntsVersion;

			internal int blockSize;
		}

		internal class FSTEntry
		{
			internal long offset;

			internal long numOrds;
		}

		internal class FSTTermsEnum : TermsEnum
		{
			internal readonly BytesRefFSTEnum<long> @in;

			internal readonly FST<long> fst;

			internal readonly FST.BytesReader bytesReader;

			internal readonly FST.Arc<long> firstArc = new FST.Arc<long>();

			internal readonly FST.Arc<long> scratchArc = new FST.Arc<long>();

			internal readonly IntsRef scratchInts = new IntsRef();

			internal readonly BytesRef scratchBytes = new BytesRef();

			internal FSTTermsEnum(FST<long> fst)
			{
				// exposes FSTEnum directly as a TermsEnum: avoids binary-search next()
				// this is all for the complicated seek(ord)...
				// maybe we should add a FSTEnum that supports this operation?
				this.fst = fst;
				@in = new BytesRefFSTEnum<long>(fst);
				bytesReader = fst.GetBytesReader();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef Next()
			{
				BytesRefFSTEnum.InputOutput<long> io = @in.Next();
				if (io == null)
				{
					return null;
				}
				else
				{
					return io.input;
				}
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum.SeekStatus SeekCeil(BytesRef text)
			{
				if (@in.SeekCeil(text) == null)
				{
					return TermsEnum.SeekStatus.END;
				}
				else
				{
					if (Term().Equals(text))
					{
						// TODO: add SeekStatus to FSTEnum like in https://issues.apache.org/jira/browse/LUCENE-3729
						// to remove this comparision?
						return TermsEnum.SeekStatus.FOUND;
					}
					else
					{
						return TermsEnum.SeekStatus.NOT_FOUND;
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool SeekExact(BytesRef text)
			{
				if (@in.SeekExact(text) == null)
				{
					return false;
				}
				else
				{
					return true;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SeekExact(long ord)
			{
				// TODO: would be better to make this simpler and faster.
				// but we dont want to introduce a bug that corrupts our enum state!
				bytesReader.SetPosition(0);
				fst.GetFirstArc(firstArc);
				IntsRef output = Lucene.Net.Util.Fst.Util.GetByOutput(fst, ord, bytesReader
					, firstArc, scratchArc, scratchInts);
				scratchBytes.bytes = new byte[output.length];
				scratchBytes.offset = 0;
				scratchBytes.length = 0;
				Lucene.Net.Util.Fst.Util.ToBytesRef(output, scratchBytes);
				// TODO: we could do this lazily, better to try to push into FSTEnum though?
				@in.SeekExact(scratchBytes);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef Term()
			{
				return @in.Current().input;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Ord()
			{
				return @in.Current().output;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int DocFreq()
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long TotalTermFreq()
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
				 reuse, int flags)
			{
				throw new NotSupportedException();
			}
		}
	}
}
