using Lucene.Net.Codecs.Lucene41;

namespace Lucene.Net.Codecs.Pulsing
{
	/// <summary>
	/// Concrete pulsing implementation over
	/// <see cref="Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat">Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class Pulsing41PostingsFormat : PulsingPostingsFormat
	{
		/// <summary>Inlines docFreq=1 terms, otherwise uses the normal "Lucene41" format.</summary>
		/// <remarks>Inlines docFreq=1 terms, otherwise uses the normal "Lucene41" format.</remarks>
		public Pulsing41PostingsFormat() : this(1)
		{
		}

		/// <summary>Inlines docFreq=<code>freqCutoff</code> terms, otherwise uses the normal "Lucene41" format.
		/// 	</summary>
		/// <remarks>Inlines docFreq=<code>freqCutoff</code> terms, otherwise uses the normal "Lucene41" format.
		/// 	</remarks>
		public Pulsing41PostingsFormat(int freqCutoff) : this(freqCutoff, BlockTreeTermsWriter
			.DEFAULT_MIN_BLOCK_SIZE, BlockTreeTermsWriter.DEFAULT_MAX_BLOCK_SIZE)
		{
		}

		/// <summary>Inlines docFreq=<code>freqCutoff</code> terms, otherwise uses the normal "Lucene41" format.
		/// 	</summary>
		/// <remarks>Inlines docFreq=<code>freqCutoff</code> terms, otherwise uses the normal "Lucene41" format.
		/// 	</remarks>
		public Pulsing41PostingsFormat(int freqCutoff, int minBlockSize, int maxBlockSize
			) : base("Pulsing41", new Lucene41PostingsBaseFormat(), freqCutoff, minBlockSize
			, maxBlockSize)
		{
		}
		// javadocs
	}
}
