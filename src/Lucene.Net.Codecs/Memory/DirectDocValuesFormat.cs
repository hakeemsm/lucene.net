using Lucene.Net.Index;
using Lucene.Net.Util;


namespace Lucene.Net.Codecs.Memory
{
	/// <summary>
	/// In-memory docvalues format that does no (or very little)
	/// compression.
	/// </summary>
	/// <remarks>
	/// In-memory docvalues format that does no (or very little)
	/// compression.  Indexed values are stored on disk, but
	/// then at search time all values are loaded into memory as
	/// simple java arrays.  For numeric values, it uses
	/// byte[], short[], int[], long[] as necessary to fit the
	/// range of the values.  For binary values, there is an int
	/// (4 bytes) overhead per value.
	/// <p>Limitations:
	/// <ul>
	/// <li>For binary and sorted fields the total space
	/// required for all binary values cannot exceed about
	/// 2.1 GB (see #MAX_TOTAL_BYTES_LENGTH).</li>
	/// <li>For sorted set fields, the sum of the size of each
	/// document's set of values cannot exceed about 2.1 B
	/// values (see #MAX_SORTED_SET_ORDS).  For example,
	/// if every document has 10 values (10 instances of
	/// SortedSetDocValuesFieldValuesField">Lucene.Net.Document.SortedSetDocValuesField
	/// 	</see>
	/// ) added, then no
	/// more than ~210 M documents can be added to one
	/// segment. </li>
	/// </ul>
	/// </remarks>
	public class DirectDocValuesFormat : DocValuesFormat
	{
	    public static readonly int MAX_TOTAL_BYTES_LENGTH = ArrayUtil.MAX_ARRAY_LENGTH;

	    public static readonly int MAX_SORTED_SET_ORDS = ArrayUtil.MAX_ARRAY_LENGTH;

	    /// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public DirectDocValuesFormat() : base("Direct")
		{
		}

		// javadocs
		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
		{
			return new DirectDocValuesConsumer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC
				, METADATA_EXTENSION);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesProducer FieldsProducer(SegmentReadState state)
		{
			return new DirectDocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC
				, METADATA_EXTENSION);
		}

		internal static readonly string DATA_CODEC = "DirectDocValuesData";

		internal static readonly string DATA_EXTENSION = "dvdd";

		internal static readonly string METADATA_CODEC = "DirectDocValuesMetadata";

		internal static readonly string METADATA_EXTENSION = "dvdm";
	}
}
