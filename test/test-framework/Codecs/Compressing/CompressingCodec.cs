using System;
using Lucene.Net.Codecs.Compressing.Dummy;
using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Randomized.Generators;

namespace Lucene.Net.Codecs.Compressing.TestFramework
{
	/// <summary>
	/// A codec that uses
	/// <see cref="CompressingStoredFieldsFormat">CompressingStoredFieldsFormat</see>
	/// for its stored
	/// fields and delegates to
	/// <see cref="Lucene.Net.Codecs.Lucene46.Lucene46Codec">Lucene.Net.Codecs.Lucene46.Lucene46Codec
	/// 	</see>
	/// for everything else.
	/// </summary>
	public abstract class CompressingCodec : FilterCodec
	{
		/// <summary>Create a random instance.</summary>
		/// <remarks>Create a random instance.</remarks>
		public static CompressingCodec RandomInstance
			(Random random, int chunkSize, bool withSegmentSuffix)
		{
			switch (random.Next(4))
			{
				case 0:
				{
					return new FastCompressingCodec(chunkSize, withSegmentSuffix);
				}

				case 1:
				{
					return new FastDecompressionCompressingCodec(chunkSize, withSegmentSuffix);
				}

				case 2:
				{
					return new HighCompressionCompressingCodec(chunkSize, withSegmentSuffix);
				}

				case 3:
				{
					return new DummyCompressingCodec(chunkSize, withSegmentSuffix);
				}

				default:
				{
					throw new Exception();
				}
			}
		}

		/// <summary>
		/// Creates a random
		/// <see cref="CompressingCodec">CompressingCodec</see>
		/// that is using an empty segment
		/// suffix
		/// </summary>
		public static Lucene.Net.Codecs.Compressing.CompressingCodec RandomInstance
			(Random random)
		{
			return RandomInstance(random, RandomInts.RandomIntBetween(random, 1, 500), false);
		}

		/// <summary>
		/// Creates a random
		/// <see cref="CompressingCodec">CompressingCodec</see>
		/// that is using a segment suffix
		/// </summary>
		public static CompressingCodec RandomInstance(Random random, bool withSegmentSuffix)
		{
			return RandomInstance(random, RandomInts.RandomIntBetween(random, 1, 500), withSegmentSuffix);
		}

		private readonly CompressingStoredFieldsFormat storedFieldsFormat;

		private readonly CompressingTermVectorsFormat termVectorsFormat;

		/// <summary>Creates a compressing codec with a given segment suffix</summary>
		public CompressingCodec(string name, string segmentSuffix, CompressionMode compressionMode
			, int chunkSize) : base(name, new Lucene46Codec())
		{
			this.storedFieldsFormat = new CompressingStoredFieldsFormat(name, segmentSuffix, 
				compressionMode, chunkSize);
			this.termVectorsFormat = new CompressingTermVectorsFormat(name, segmentSuffix, compressionMode
				, chunkSize);
		}

		/// <summary>Creates a compressing codec with an empty segment suffix</summary>
		public CompressingCodec(string name, CompressionMode compressionMode, int chunkSize
			) : this(name, string.Empty, compressionMode, chunkSize)
		{
		}

		public override StoredFieldsFormat StoredFieldsFormat
		{
		    get { return storedFieldsFormat; }
		}

		public override TermVectorsFormat TermVectorsFormat
		{
		    get { return termVectorsFormat; }
		}

		public override string ToString()
		{
			return Name + "(storedFieldsFormat=" + storedFieldsFormat + ", termVectorsFormat="+ termVectorsFormat + ")";
		}
	}
}
