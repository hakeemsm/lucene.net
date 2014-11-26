using System;
using Lucene.Net.Search;

namespace Lucene.Net.Util.Packed
{
	/// <summary>A DocIdSet in Elias-Fano encoding.</summary>
	/// <remarks>A DocIdSet in Elias-Fano encoding.</remarks>
	/// <lucene.internal></lucene.internal>
	public class EliasFanoDocIdSet : DocIdSet
	{
		internal readonly EliasFanoEncoder efEncoder;

		/// <summary>Construct an EliasFanoDocIdSet.</summary>
		/// <remarks>Construct an EliasFanoDocIdSet. For efficient encoding, the parameters should be chosen as low as possible.
		/// 	</remarks>
		/// <param name="numValues">At least the number of document ids that will be encoded.
		/// 	</param>
		/// <param name="upperBound">At least the highest document id that will be encoded.</param>
		public EliasFanoDocIdSet(int numValues, int upperBound)
		{
			// for javadocs
			efEncoder = new EliasFanoEncoder(numValues, upperBound);
		}

		/// <summary>
		/// Provide an indication that is better to use an
		/// <see cref="EliasFanoDocIdSet">EliasFanoDocIdSet</see>
		/// than a
		/// <see cref="Lucene.Net.Util.FixedBitSet">Lucene.Net.Util.FixedBitSet
		/// 	</see>
		/// to encode document identifiers.
		/// </summary>
		/// <param name="numValues">The number of document identifiers that is to be encoded. Should be non negative.
		/// 	</param>
		/// <param name="upperBound">The maximum possible value for a document identifier. Should be at least <code>numValues</code>.
		/// 	</param>
		/// <returns>
		/// See
		/// <see cref="EliasFanoEncoder.SufficientlySmallerThanBitSet(long, long)">EliasFanoEncoder.SufficientlySmallerThanBitSet(long, long)
		/// 	</see>
		/// </returns>
		public static bool SufficientlySmallerThanBitSet(long numValues, long upperBound)
		{
			return EliasFanoEncoder.SufficientlySmallerThanBitSet(numValues, upperBound);
		}

		/// <summary>Encode the document ids from a DocIdSetIterator.</summary>
		/// <remarks>Encode the document ids from a DocIdSetIterator.</remarks>
		/// <param name="disi">
		/// This DocIdSetIterator should provide document ids that are consistent
		/// with <code>numValues</code> and <code>upperBound</code> as provided to the constructor.
		/// </param>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void EncodeFromDisi(DocIdSetIterator disi)
		{
			while (efEncoder.numEncoded < efEncoder.numValues)
			{
				int x = disi.NextDoc();
				if (x == DocIdSetIterator.NO_MORE_DOCS)
				{
					throw new ArgumentException("disi: " + disi.ToString() + "\nhas " + efEncoder.numEncoded
						 + " docs, but at least " + efEncoder.numValues + " are required.");
				}
				efEncoder.EncodeNext(x);
			}
		}

		/// <summary>
		/// Provides a
		/// <see cref="Lucene.Net.Search.DocIdSetIterator">Lucene.Net.Search.DocIdSetIterator
		/// 	</see>
		/// to access encoded document ids.
		/// </summary>
		public override DocIdSetIterator Iterator()
		{
			if (efEncoder.lastEncoded >= DocIdSetIterator.NO_MORE_DOCS)
			{
				throw new NotSupportedException("Highest encoded value too high for DocIdSetIterator.NO_MORE_DOCS: "
					 + efEncoder.lastEncoded);
			}
			return new AnonymousDocIdSetIterator(this);
		}

		private sealed class AnonymousDocIdSetIterator : DocIdSetIterator
		{
		    private readonly EliasFanoDocIdSet _enclosing;

		    public AnonymousDocIdSetIterator(EliasFanoDocIdSet enclosing)
			{
			    _enclosing = enclosing;
			    this.curDocId = -1;
				this.efDecoder = this._enclosing.efEncoder.GetDecoder();
			}

			private int curDocId;

			private readonly EliasFanoDecoder efDecoder;

			public override int DocID
			{
			    get { return this.curDocId; }
			}

			private int SetCurDocID(long value)
			{
				this.curDocId = (value == EliasFanoDecoder.NO_MORE_VALUES) ? DocIdSetIterator.NO_MORE_DOCS
					 : (int)value;
				return this.curDocId;
			}

			public override int NextDoc()
			{
				return this.SetCurDocID(this.efDecoder.NextValue());
			}

			public override int Advance(int target)
			{
				return this.SetCurDocID(this.efDecoder.AdvanceToValue(target));
			}

			public override long Cost
			{
			    get { return this.efDecoder.NumEncoded(); }
			}
		}

		/// <summary>This DocIdSet implementation is cacheable.</summary>
		/// <remarks>This DocIdSet implementation is cacheable.</remarks>
		/// <returns><code>true</code></returns>
		public override bool IsCacheable
		{
		    get { return true; }
		}

		public override bool Equals(object other)
		{
			return ((other is Lucene.Net.Util.Packed.EliasFanoDocIdSet)) && efEncoder.
				Equals(((Lucene.Net.Util.Packed.EliasFanoDocIdSet)other).efEncoder);
		}

		public override int GetHashCode()
		{
			return efEncoder.GetHashCode() ^ GetType().GetHashCode();
		}
	}
}
