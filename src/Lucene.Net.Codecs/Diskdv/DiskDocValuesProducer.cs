/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util.Packed;
using Sharpen;

namespace Lucene.Net.Codecs.Diskdv
{
	internal class DiskDocValuesProducer : Lucene45DocValuesProducer
	{
		/// <exception cref="System.IO.IOException"></exception>
		protected DiskDocValuesProducer(SegmentReadState state, string dataCodec, string 
			dataExtension, string metaCodec, string metaExtension) : base(state, dataCodec, 
			dataExtension, metaCodec, metaExtension)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override MonotonicBlockPackedReader GetAddressInstance(IndexInput data, 
			FieldInfo field, Lucene45DocValuesProducer.BinaryEntry bytes)
		{
			data.Seek(bytes.addressesOffset);
			return new MonotonicBlockPackedReader(((IndexInput)data.Clone()), bytes.packedIntsVersion
				, bytes.blockSize, bytes.count, true);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override MonotonicBlockPackedReader GetIntervalInstance(IndexInput data
			, FieldInfo field, Lucene45DocValuesProducer.BinaryEntry bytes)
		{
			throw new Exception();
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override MonotonicBlockPackedReader GetOrdIndexInstance(IndexInput data
			, FieldInfo field, Lucene45DocValuesProducer.NumericEntry entry)
		{
			data.Seek(entry.offset);
			return new MonotonicBlockPackedReader(((IndexInput)data.Clone()), entry.packedIntsVersion
				, entry.blockSize, entry.count, true);
		}
	}
}
