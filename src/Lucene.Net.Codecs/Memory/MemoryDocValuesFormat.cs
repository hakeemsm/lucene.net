/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Index;
using Lucene.Net.Util.Packed;
using Sharpen;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>In-memory docvalues format</summary>
	public class MemoryDocValuesFormat : DocValuesFormat
	{
		/// <summary>Maximum length for each binary doc values field.</summary>
		/// <remarks>Maximum length for each binary doc values field.</remarks>
		public const int MAX_BINARY_FIELD_LENGTH = (1 << 15) - 2;

		internal readonly float acceptableOverheadRatio;

		/// <summary>
		/// Calls
		/// <see cref="MemoryDocValuesFormat(float)">
		/// 
		/// MemoryDocValuesFormat(PackedInts.DEFAULT)
		/// </see>
		/// 
		/// </summary>
		public MemoryDocValuesFormat() : this(PackedInts.DEFAULT)
		{
		}

		/// <summary>
		/// Creates a new MemoryDocValuesFormat with the specified
		/// <code>acceptableOverheadRatio</code> for NumericDocValues.
		/// </summary>
		/// <remarks>
		/// Creates a new MemoryDocValuesFormat with the specified
		/// <code>acceptableOverheadRatio</code> for NumericDocValues.
		/// </remarks>
		/// <param name="acceptableOverheadRatio">
		/// compression parameter for numerics.
		/// Currently this is only used when the number of unique values is small.
		/// </param>
		/// <lucene.experimental></lucene.experimental>
		public MemoryDocValuesFormat(float acceptableOverheadRatio) : base("Memory")
		{
			this.acceptableOverheadRatio = acceptableOverheadRatio;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
		{
			return new MemoryDocValuesConsumer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC
				, METADATA_EXTENSION, acceptableOverheadRatio);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesProducer FieldsProducer(SegmentReadState state)
		{
			return new MemoryDocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC
				, METADATA_EXTENSION);
		}

		internal static readonly string DATA_CODEC = "MemoryDocValuesData";

		internal static readonly string DATA_EXTENSION = "mdvd";

		internal static readonly string METADATA_CODEC = "MemoryDocValuesMetadata";

		internal static readonly string METADATA_EXTENSION = "mdvm";
	}
}
