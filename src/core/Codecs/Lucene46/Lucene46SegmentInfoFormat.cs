namespace Lucene.Net.Codecs.Lucene46
{
	/// <summary>Lucene 4.6 Segment info format.</summary>
	/// <remarks>
	/// Lucene 4.6 Segment info format.
	/// <p>
	/// Files:
	/// <ul>
	/// <li><tt>.si</tt>: Header, SegVersion, SegSize, IsCompoundFile, Diagnostics, Files, Footer
	/// </ul>
	/// </p>
	/// Data types:
	/// <p>
	/// <ul>
	/// <li>Header --&gt;
	/// <see cref="Lucene.Net.Codecs.CodecUtil.WriteHeader(Lucene.Net.Store.DataOutput, string, int)
	/// 	">CodecHeader</see>
	/// </li>
	/// <li>SegSize --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteInt(int)">Int32</see>
	/// </li>
	/// <li>SegVersion --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteString(string)">String</see>
	/// </li>
	/// <li>Files --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteStringSet(System.Collections.Generic.ICollection{E})
	/// 	">Set&lt;String&gt;</see>
	/// </li>
	/// <li>Diagnostics --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteStringStringMap(System.Collections.Generic.IDictionary{K, V})
	/// 	">Map&lt;String,String&gt;</see>
	/// </li>
	/// <li>IsCompoundFile --&gt;
	/// <see cref="Lucene.Net.Store.DataOutput.WriteByte(byte)">Int8</see>
	/// </li>
	/// <li>Footer --&gt;
	/// <see cref="Lucene.Net.Codecs.CodecUtil.WriteFooter(Lucene.Net.Store.IndexOutput)
	/// 	">CodecFooter</see>
	/// </li>
	/// </ul>
	/// </p>
	/// Field Descriptions:
	/// <p>
	/// <ul>
	/// <li>SegVersion is the code version that created the segment.</li>
	/// <li>SegSize is the number of documents contained in the segment index.</li>
	/// <li>IsCompoundFile records whether the segment is written as a compound file or
	/// not. If this is -1, the segment is not a compound file. If it is 1, the segment
	/// is a compound file.</li>
	/// <li>The Diagnostics Map is privately written by
	/// <see cref="Lucene.Net.Index.IndexWriter">Lucene.Net.Index.IndexWriter
	/// 	</see>
	/// , as a debugging aid,
	/// for each segment it creates. It includes metadata like the current Lucene
	/// version, OS, Java version, why the segment was created (merge, flush,
	/// addIndexes), etc.</li>
	/// <li>Files is a list of files referred to by this segment.</li>
	/// </ul>
	/// </p>
	/// </remarks>
	/// <seealso cref="Lucene.Net.Index.SegmentInfos">Lucene.Net.Index.SegmentInfos
	/// 	</seealso>
	/// <lucene.experimental></lucene.experimental>
	public class Lucene46SegmentInfoFormat : SegmentInfoFormat
	{
		private readonly SegmentInfoReader reader = new Lucene46SegmentInfoReader();

		private readonly SegmentInfoWriter writer = new Lucene46SegmentInfoWriter();

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public Lucene46SegmentInfoFormat()
		{
		}

		// javadocs
		// javadocs
		// javadocs
		// javadocs
		public override SegmentInfoReader SegmentInfoReader
		{
		    get { return reader; }
		}

		public override SegmentInfoWriter SegmentInfoWriter
		{
		    get { return writer; }
		}

		/// <summary>
		/// File extension used to store
		/// <see cref="Lucene.Net.Index.SegmentInfo">Lucene.Net.Index.SegmentInfo
		/// 	</see>
		/// .
		/// </summary>
		public static readonly string SI_EXTENSION = "si";

		internal static readonly string CODEC_NAME = "Lucene46SegmentInfo";

		internal const int VERSION_START = 0;

		internal const int VERSION_CHECKSUM = 1;

		internal const int VERSION_CURRENT = VERSION_CHECKSUM;
	}
}
