using Lucene.Net.Codecs.Lucene40;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Codecs.Lucene42;
using Lucene.Net.Codecs.PerField;

namespace Lucene.Net.Codecs.Lucene46
{
	/// <summary>
	/// Implements the Lucene 4.6 index format, with configurable per-field postings
	/// and docvalues formats.
	/// </summary>
	/// <remarks>
	/// Implements the Lucene 4.6 index format, with configurable per-field postings
	/// and docvalues formats.
	/// <p>
	/// If you want to reuse functionality of this codec in another codec, extend
	/// <see cref="Lucene.Net.Codecs.FilterCodec">Lucene.Net.Codecs.FilterCodec
	/// 	</see>
	/// .
	/// </remarks>
	/// <seealso cref="Lucene.Net.Codecs.Lucene46">package documentation for file format details.
	/// 	</seealso>
	/// <lucene.experimental></lucene.experimental>
	public class Lucene46Codec : Codec
	{
		private readonly StoredFieldsFormat fieldsFormat = new Lucene41StoredFieldsFormat();

		private readonly TermVectorsFormat vectorsFormat = new Lucene42TermVectorsFormat();

		private readonly FieldInfosFormat fieldInfosFormat = new Lucene46FieldInfosFormat();

		private readonly SegmentInfoFormat segmentInfosFormat = new Lucene46SegmentInfoFormat();

		private readonly LiveDocsFormat liveDocsFormat = new Lucene40LiveDocsFormat();

		private sealed class AnonymousPerFieldPostingsFormat1 : PerFieldPostingsFormat
		{
			public AnonymousPerFieldPostingsFormat1(Lucene46Codec enclosing)
			{
				this._enclosing = enclosing;
			}

			// NOTE: if we make largish changes in a minor release, easier to just make Lucene46Codec or whatever
			// if they are backwards compatible or smallish we can probably do the backwards in the postingsreader
			// (it writes a minor version, etc).
			public override PostingsFormat GetPostingsFormatForField(string field)
			{
				return this._enclosing.GetPostingsFormatForField(field);
			}

			private readonly Lucene46Codec _enclosing;
		}

		private readonly PostingsFormat postingsFormat;

		private sealed class AnonymousPerFieldDocValuesFormat1 : PerFieldDocValuesFormat
		{
			public AnonymousPerFieldDocValuesFormat1(Lucene46Codec enclosing)
			{
				this._enclosing = enclosing;
			}

			public override DocValuesFormat GetDocValuesFormatForField(string field)
			{
				return this._enclosing.GetDocValuesFormatForField(field);
			}

			private readonly Lucene46Codec _enclosing;
		}

		private readonly DocValuesFormat docValuesFormat;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public Lucene46Codec() : base("Lucene46")
		{
			postingsFormat = new AnonymousPerFieldPostingsFormat1(this);
			docValuesFormat = new AnonymousPerFieldDocValuesFormat1(this);
		}

		public sealed override StoredFieldsFormat StoredFieldsFormat
		{
		    get { return fieldsFormat; }
		}

		public sealed override TermVectorsFormat TermVectorsFormat
		{
		    get { return vectorsFormat; }
		}

		public sealed override PostingsFormat PostingsFormat
		{
		    get { return postingsFormat; }
		}

		public sealed override FieldInfosFormat FieldInfosFormat
		{
            get
		    {
		        return fieldInfosFormat;
		    }
		}

		public sealed override SegmentInfoFormat SegmentInfoFormat
		{
		    get { return segmentInfosFormat; }
		}

		public sealed override LiveDocsFormat LiveDocsFormat
		{
		    get { return liveDocsFormat; }
		}

		/// <summary>
		/// Returns the postings format that should be used for writing
		/// new segments of <code>field</code>.
		/// </summary>
		/// <remarks>
		/// Returns the postings format that should be used for writing
		/// new segments of <code>field</code>.
		/// The default implementation always returns "Lucene41"
		/// </remarks>
		public virtual PostingsFormat GetPostingsFormatForField(string field)
		{
			return defaultFormat;
		}

		/// <summary>
		/// Returns the docvalues format that should be used for writing
		/// new segments of <code>field</code>.
		/// </summary>
		/// <remarks>
		/// Returns the docvalues format that should be used for writing
		/// new segments of <code>field</code>.
		/// The default implementation always returns "Lucene45"
		/// </remarks>
		public virtual DocValuesFormat GetDocValuesFormatForField(string field)
		{
			return defaultDVFormat;
		}

		public sealed override DocValuesFormat DocValuesFormat
		{
		    get { return docValuesFormat; }
		}

		private readonly PostingsFormat defaultFormat = PostingsFormat.ForName("Lucene41");

		private readonly DocValuesFormat defaultDVFormat = DocValuesFormat.ForName("Lucene45");

		private readonly NormsFormat normsFormat = new Lucene42NormsFormat();

		public sealed override NormsFormat NormsFormat
		{
		    get { return normsFormat; }
		}
	}
}
