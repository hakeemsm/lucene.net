using System;
using Lucene.Net.Util;

namespace Lucene.Net.Index
{
	/// <summary>This class contains utility methods and constants for DocValues</summary>
	public sealed class DocValues
	{
		public DocValues()
		{
		}

		private sealed class _BinaryDocValues_34 : BinaryDocValues
		{
			public _BinaryDocValues_34()
			{
			}

			public override void Get(int docID, BytesRef result)
			{
				result.bytes = BytesRef.EMPTY_BYTES;
				result.offset = 0;
				result.length = 0;
			}
		}

		/// <summary>
		/// An empty BinaryDocValues which returns
		/// <see cref="Lucene.Net.Util.BytesRef.EMPTY_BYTES">Lucene.Net.Util.BytesRef.EMPTY_BYTES
		/// 	</see>
		/// for every document
		/// </summary>
		public static readonly BinaryDocValues EMPTY_BINARY = new _BinaryDocValues_34();

		private sealed class _NumericDocValues_46 : NumericDocValues
		{
			public _NumericDocValues_46()
			{
			}

			public override long Get(int docID)
			{
				return 0;
			}
		}

		/// <summary>An empty NumericDocValues which returns zero for every document</summary>
		public static readonly NumericDocValues EMPTY_NUMERIC = new _NumericDocValues_46(
			);

		private sealed class _SortedDocValues_56 : SortedDocValues
		{
			public _SortedDocValues_56()
			{
			}

			public override int GetOrd(int docID)
			{
				return -1;
			}

			public override void LookupOrd(int ord, BytesRef result)
			{
				result.bytes = BytesRef.EMPTY_BYTES;
				result.offset = 0;
				result.length = 0;
			}

			public override int GetValueCount()
			{
				return 0;
			}
		}

		/// <summary>
		/// An empty SortedDocValues which returns
		/// <see cref="Lucene.Net.Util.BytesRef.EMPTY_BYTES">Lucene.Net.Util.BytesRef.EMPTY_BYTES
		/// 	</see>
		/// for every document
		/// </summary>
		public static readonly SortedDocValues EMPTY_SORTED = new _SortedDocValues_56();

		private sealed class _RandomAccessOrds_78 : RandomAccessOrds
		{
			public _RandomAccessOrds_78()
			{
			}

			public override long NextOrd()
			{
				return SortedSetDocValues.NO_MORE_ORDS;
			}

			public override void SetDocument(int docID)
			{
			}

			public override void LookupOrd(long ord, BytesRef result)
			{
				throw new IndexOutOfRangeException();
			}

			public override long GetValueCount()
			{
				return 0;
			}

			public override long OrdAt(int index)
			{
				throw new IndexOutOfRangeException();
			}

			public override int Cardinality()
			{
				return 0;
			}
		}

		/// <summary>
		/// An empty SortedDocValues which returns
		/// <see cref="SortedSetDocValues.NO_MORE_ORDS">SortedSetDocValues.NO_MORE_ORDS</see>
		/// for every document
		/// </summary>
		public static readonly SortedSetDocValues EMPTY_SORTED_SET = new _RandomAccessOrds_78
			();

		/// <summary>Returns a multi-valued view over the provided SortedDocValues</summary>
		public static SortedSetDocValues Singleton(SortedDocValues dv)
		{
			return new SingletonSortedSetDocValues(dv);
		}

		/// <summary>
		/// Returns a single-valued view of the SortedSetDocValues, if it was previously
		/// wrapped with
		/// <see cref="Singleton(SortedDocValues)">Singleton(SortedDocValues)</see>
		/// , or null.
		/// </summary>
		public static SortedDocValues UnwrapSingleton(SortedSetDocValues dv)
		{
			if (dv is SingletonSortedSetDocValues)
			{
				return ((SingletonSortedSetDocValues)dv).GetSortedDocValues();
			}
			else
			{
				return null;
			}
		}

		/// <summary>Returns a Bits representing all documents from <code>dv</code> that have a value.
		/// 	</summary>
		/// <remarks>Returns a Bits representing all documents from <code>dv</code> that have a value.
		/// 	</remarks>
		public static IBits DocsWithValue(SortedDocValues dv, int maxDoc)
		{
			return new AnonymousBitsImpl2(dv, maxDoc);
		}

		private sealed class AnonymousBitsImpl2 : IBits
		{
			public AnonymousBitsImpl2(SortedDocValues dv, int maxDoc)
			{
				this.dv = dv;
				this.maxDoc = maxDoc;
			}

			private readonly SortedDocValues dv;

			private readonly int maxDoc;

		    public bool this[int index]
		    {
                get { return dv.GetOrd(index) >= 0; }
		    }

		    int IBits.Length
		    {
		        get { return maxDoc; }
		    }
		}

		/// <summary>Returns a Bits representing all documents from <code>dv</code> that have a value.
		/// 	</summary>
		/// <remarks>Returns a Bits representing all documents from <code>dv</code> that have a value.
		/// 	</remarks>
		public static IBits DocsWithValue(SortedSetDocValues dv, int maxDoc)
		{
			return new AnonymousBitsImpl(dv, maxDoc);
		}

		private sealed class AnonymousBitsImpl : IBits
		{
			public AnonymousBitsImpl(SortedSetDocValues dv, int maxDoc)
			{
				this.dv = dv;
				this.maxDoc = maxDoc;
			}

			private readonly SortedSetDocValues dv;

			private readonly int maxDoc;

		    public bool this[int index]
		    {
                get
                {
                    dv.SetDocument(index);
                    return dv.NextOrd() != SortedSetDocValues.NO_MORE_ORDS;
                }
		    }

		    int IBits.Length
		    {
		        get { return maxDoc; }
		    }
		}
	}
}
