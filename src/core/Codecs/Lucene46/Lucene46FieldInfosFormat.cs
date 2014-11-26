namespace Lucene.Net.Codecs.Lucene46
{
	/// <summary>Lucene 4.6 Field Infos format.</summary>
	/// <remarks>
	/// Lucene 4.6 Field Infos format.
	/// <p>
	/// <p>Field names are stored in the field info file, with suffix <tt>.fnm</tt>.</p>
	/// <p>FieldInfos (.fnm) --&gt; Header,FieldsCount, &lt;FieldName,FieldNumber,
	/// FieldBits,DocValuesBits,DocValuesGen,Attributes&gt; <sup>FieldsCount</sup>,Footer</p>
	/// <p>Data types:
	/// <ul>
	/// <li>Header --&gt;
	/// <see cref="Lucene.Net.Codecs.CodecUtil.CheckHeader(Lucene.Net.Store.DataInput, string, int, int)
	/// 	">CodecHeader</see>
	/// </li>
	/// <li>FieldsCount --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteVInt(int)">VInt</see>
	/// </li>
	/// <li>FieldName --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteString(string)">String</see>
	/// </li>
	/// <li>FieldBits, DocValuesBits --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteByte(byte)">Byte</see>
	/// </li>
	/// <li>FieldNumber --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteInt(int)">VInt</see>
	/// </li>
	/// <li>Attributes --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteStringStringMap(System.Collections.Generic.IDictionary{K, V})
	/// 	">Map&lt;String,String&gt;</see>
	/// </li>
	/// <li>DocValuesGen --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteLong(long)">Int64</see>
	/// </li>
	/// <li>Footer --&gt;
	/// <see cref="Lucene.Net.Codecs.CodecUtil.WriteFooter(Lucene.Net.Store.IndexOutput)
	/// 	">CodecFooter</see>
	/// </li>
	/// </ul>
	/// </p>
	/// Field Descriptions:
	/// <ul>
	/// <li>FieldsCount: the number of fields in this file.</li>
	/// <li>FieldName: name of the field as a UTF-8 String.</li>
	/// <li>FieldNumber: the field's number. Note that unlike previous versions of
	/// Lucene, the fields are not numbered implicitly by their order in the
	/// file, instead explicitly.</li>
	/// <li>FieldBits: a byte containing field options.
	/// <ul>
	/// <li>The low-order bit is one for indexed fields, and zero for non-indexed
	/// fields.</li>
	/// <li>The second lowest-order bit is one for fields that have term vectors
	/// stored, and zero for fields without term vectors.</li>
	/// <li>If the third lowest order-bit is set (0x4), offsets are stored into
	/// the postings list in addition to positions.</li>
	/// <li>Fourth bit is unused.</li>
	/// <li>If the fifth lowest-order bit is set (0x10), norms are omitted for the
	/// indexed field.</li>
	/// <li>If the sixth lowest-order bit is set (0x20), payloads are stored for the
	/// indexed field.</li>
	/// <li>If the seventh lowest-order bit is set (0x40), term frequencies and
	/// positions omitted for the indexed field.</li>
	/// <li>If the eighth lowest-order bit is set (0x80), positions are omitted for the
	/// indexed field.</li>
	/// </ul>
	/// </li>
	/// <li>DocValuesBits: a byte containing per-document value types. The type
	/// recorded as two four-bit integers, with the high-order bits representing
	/// <code>norms</code> options, and the low-order bits representing
	/// <code>DocValues</code>
	/// options. Each four-bit integer can be decoded as such:
	/// <ul>
	/// <li>0: no DocValues for this field.</li>
	/// <li>1: NumericDocValues. (
	/// <see cref="Lucene.Net.Index.FieldInfo.DocValuesType.NUMERIC">Lucene.Net.Index.FieldInfo.DocValuesType.NUMERIC
	/// 	</see>
	/// )</li>
	/// <li>2: BinaryDocValues. (
	/// <code>DocValuesType#BINARY</code>
	/// )</li>
	/// <li>3: SortedDocValues. (
	/// <code>DocValuesType#SORTED</code>
	/// )</li>
	/// </ul>
	/// </li>
	/// <li>DocValuesGen is the generation count of the field's DocValues. If this is -1,
	/// there are no DocValues updates to that field. Anything above zero means there
	/// are updates stored by
	/// <see cref="Lucene.Net.Codecs.DocValuesFormat">Lucene.Net.Codecs.DocValuesFormat
	/// 	</see>
	/// .</li>
	/// <li>Attributes: a key-value map of codec-private attributes.</li>
	/// </ul>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public sealed class Lucene46FieldInfosFormat : FieldInfosFormat
	{
		private readonly FieldInfosReader reader = new Lucene46FieldInfosReader();

		private readonly FieldInfosWriter writer = new Lucene46FieldInfosWriter();

	    /// <exception cref="System.IO.IOException"></exception>
		public override FieldInfosReader FieldInfosReader
		{
	        get { return reader; }
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldInfosWriter FieldInfosWriter
		{
		    get { return writer; }
		}

		/// <summary>Extension of field infos</summary>
		internal static readonly string EXTENSION = "fnm";

		internal static readonly string CODEC_NAME = "Lucene46FieldInfos";

		internal const int FORMAT_START = 0;

		internal const int FORMAT_CHECKSUM = 1;

		internal const int FORMAT_CURRENT = FORMAT_CHECKSUM;

		internal const byte IS_INDEXED = unchecked((int)(0x1));

		internal const byte STORE_TERMVECTOR = unchecked((int)(0x2));

		internal const byte STORE_OFFSETS_IN_POSTINGS = unchecked((int)(0x4));

		internal const byte OMIT_NORMS = unchecked((int)(0x10));

		internal const byte STORE_PAYLOADS = unchecked((int)(0x20));

		internal const byte OMIT_TERM_FREQ_AND_POSITIONS = unchecked((int)(0x40));

		internal const byte OMIT_POSITIONS = unchecked((byte)(-128));
		// Codec header
		// Field flags
	}
}
