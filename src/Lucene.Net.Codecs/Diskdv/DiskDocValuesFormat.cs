/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Diskdv;
using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Diskdv
{
	/// <summary>DocValues format that keeps most things on disk.</summary>
	/// <remarks>
	/// DocValues format that keeps most things on disk.
	/// <p>
	/// Only things like disk offsets are loaded into ram.
	/// <p>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class DiskDocValuesFormat : DocValuesFormat
	{
		public DiskDocValuesFormat() : base("Disk")
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
		{
			return new _Lucene45DocValuesConsumer_46(state, DATA_CODEC, DATA_EXTENSION, META_CODEC
				, META_EXTENSION);
		}

		private sealed class _Lucene45DocValuesConsumer_46 : Lucene45DocValuesConsumer
		{
			public _Lucene45DocValuesConsumer_46(SegmentWriteState baseArg1, string baseArg2, 
				string baseArg3, string baseArg4, string baseArg5) : base(baseArg1, baseArg2, baseArg3
				, baseArg4, baseArg5)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void AddTermsDict(FieldInfo field, Iterable<BytesRef> values)
			{
				this.AddBinaryField(field, values);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesProducer FieldsProducer(SegmentReadState state)
		{
			return new DiskDocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, META_CODEC, META_EXTENSION
				);
		}

		public static readonly string DATA_CODEC = "DiskDocValuesData";

		public static readonly string DATA_EXTENSION = "dvdd";

		public static readonly string META_CODEC = "DiskDocValuesMetadata";

		public static readonly string META_EXTENSION = "dvdm";
	}
}
