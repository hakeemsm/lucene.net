/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries
{
	/// <summary>
	/// SortField for
	/// <see cref="Lucene.Net.Index.SortedSetDocValues">Lucene.Net.Index.SortedSetDocValues
	/// 	</see>
	/// .
	/// <p>
	/// A SortedSetDocValues contains multiple values for a field, so sorting with
	/// this technique "selects" a value as the representative sort value for the document.
	/// <p>
	/// By default, the minimum value in the set is selected as the sort value, but
	/// this can be customized. Selectors other than the default do have some limitations
	/// (see below) to ensure that all selections happen in constant-time for performance.
	/// <p>
	/// Like sorting by string, this also supports sorting missing values as first or last,
	/// via
	/// <see cref="SetMissingValue(object)">SetMissingValue(object)</see>
	/// .
	/// <p>
	/// Limitations:
	/// <ul>
	/// <li>Fields containing
	/// <see cref="int.MaxValue">int.MaxValue</see>
	/// or more unique values
	/// are unsupported.
	/// <li>Selectors other than the default (
	/// <see cref="Selector.MIN">Selector.MIN</see>
	/// ) require
	/// optional codec support. However several codecs provided by Lucene,
	/// including the current default codec, support this.
	/// </ul>
	/// </summary>
	public class SortedSetSortField : SortField
	{
		/// <summary>Selects a value from the document's set to use as the sort value</summary>
		public enum Selector
		{
			MIN,
			MAX,
			MIDDLE_MIN,
			MIDDLE_MAX
		}

		private readonly SortedSetSortField.Selector selector;

		/// <summary>
		/// Creates a sort, possibly in reverse, by the minimum value in the set
		/// for the document.
		/// </summary>
		/// <remarks>
		/// Creates a sort, possibly in reverse, by the minimum value in the set
		/// for the document.
		/// </remarks>
		/// <param name="field">Name of field to sort by.  Must not be null.</param>
		/// <param name="reverse">True if natural order should be reversed.</param>
		public SortedSetSortField(string field, bool reverse) : this(field, reverse, SortedSetSortField.Selector
			.MIN)
		{
		}

		/// <summary>
		/// Creates a sort, possibly in reverse, specifying how the sort value from
		/// the document's set is selected.
		/// </summary>
		/// <remarks>
		/// Creates a sort, possibly in reverse, specifying how the sort value from
		/// the document's set is selected.
		/// </remarks>
		/// <param name="field">Name of field to sort by.  Must not be null.</param>
		/// <param name="reverse">True if natural order should be reversed.</param>
		/// <param name="selector">
		/// custom selector for choosing the sort value from the set.
		/// <p>
		/// NOTE: selectors other than
		/// <see cref="Selector.MIN">Selector.MIN</see>
		/// require optional codec support.
		/// </param>
		public SortedSetSortField(string field, bool reverse, SortedSetSortField.Selector
			 selector) : base(field, SortField.Type.CUSTOM, reverse)
		{
			if (selector == null)
			{
				throw new ArgumentNullException();
			}
			this.selector = selector;
		}

		/// <summary>Returns the selector in use for this sort</summary>
		public virtual SortedSetSortField.Selector GetSelector()
		{
			return selector;
		}

		public override int GetHashCode()
		{
			return 31 * base.GetHashCode() + selector.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (!base.Equals(obj))
			{
				return false;
			}
			if (GetType() != obj.GetType())
			{
				return false;
			}
			Lucene.Net.Sandbox.Queries.SortedSetSortField other = (Lucene.Net.Sandbox.Queries.SortedSetSortField
				)obj;
			if (selector != other.selector)
			{
				return false;
			}
			return true;
		}

		public override string ToString()
		{
			StringBuilder buffer = new StringBuilder();
			buffer.Append("<sortedset" + ": \"").Append(GetField()).Append("\">");
			if (GetReverse())
			{
				buffer.Append('!');
			}
			if (missingValue != null)
			{
				buffer.Append(" missingValue=");
				buffer.Append(missingValue);
			}
			buffer.Append(" selector=");
			buffer.Append(selector);
			return buffer.ToString();
		}

		/// <summary>Set how missing values (the empty set) are sorted.</summary>
		/// <remarks>
		/// Set how missing values (the empty set) are sorted.
		/// <p>
		/// Note that this must be
		/// <see cref="Lucene.Net.Search.SortField.STRING_FIRST">Lucene.Net.Search.SortField.STRING_FIRST
		/// 	</see>
		/// or
		/// <see cref="Lucene.Net.Search.SortField.STRING_LAST">Lucene.Net.Search.SortField.STRING_LAST
		/// 	</see>
		/// .
		/// </remarks>
		public override void SetMissingValue(object missingValue)
		{
			if (missingValue != STRING_FIRST && missingValue != STRING_LAST)
			{
				throw new ArgumentException("For SORTED_SET type, missing value must be either STRING_FIRST or STRING_LAST"
					);
			}
			this.missingValue = missingValue;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldComparator<object> GetComparator(int numHits, int sortPos)
		{
			return new _TermOrdValComparator_159(this, numHits, GetField(), missingValue == STRING_LAST
				);
		}

		private sealed class _TermOrdValComparator_159 : FieldComparator.TermOrdValComparator
		{
			public _TermOrdValComparator_159(SortedSetSortField _enclosing, int baseArg1, string
				 baseArg2, bool baseArg3) : base(baseArg1, baseArg2, baseArg3)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override SortedDocValues GetSortedDocValues(AtomicReaderContext context
				, string field)
			{
				SortedSetDocValues sortedSet = FieldCache.DEFAULT.GetDocTermOrds(((AtomicReader)context
					.Reader()), field);
				if (sortedSet.GetValueCount() >= int.MaxValue)
				{
					throw new NotSupportedException("fields containing more than " + (int.MaxValue - 
						1) + " unique terms are unsupported");
				}
				SortedDocValues singleton = DocValues.UnwrapSingleton(sortedSet);
				if (singleton != null)
				{
					// it's actually single-valued in practice, but indexed as multi-valued,
					// so just sort on the underlying single-valued dv directly.
					// regardless of selector type, this optimization is safe!
					return singleton;
				}
				else
				{
					if (this._enclosing.selector == SortedSetSortField.Selector.MIN)
					{
						return new SortedSetSortField.MinValue(sortedSet);
					}
					else
					{
						if (sortedSet is RandomAccessOrds == false)
						{
							throw new NotSupportedException("codec does not support random access ordinals, cannot use selector: "
								 + this._enclosing.selector);
						}
						RandomAccessOrds randomOrds = (RandomAccessOrds)sortedSet;
						switch (this._enclosing.selector)
						{
							case SortedSetSortField.Selector.MAX:
							{
								return new SortedSetSortField.MaxValue(randomOrds);
							}

							case SortedSetSortField.Selector.MIDDLE_MIN:
							{
								return new SortedSetSortField.MiddleMinValue(randomOrds);
							}

							case SortedSetSortField.Selector.MIDDLE_MAX:
							{
								return new SortedSetSortField.MiddleMaxValue(randomOrds);
							}

							case SortedSetSortField.Selector.MIN:
							default:
							{
								throw new Exception();
							}
						}
					}
				}
			}

			private readonly SortedSetSortField _enclosing;
		}

		/// <summary>Wraps a SortedSetDocValues and returns the first ordinal (min)</summary>
		internal class MinValue : SortedDocValues
		{
			internal readonly SortedSetDocValues @in;

			internal MinValue(SortedSetDocValues @in)
			{
				this.@in = @in;
			}

			public override int GetOrd(int docID)
			{
				@in.SetDocument(docID);
				return (int)@in.NextOrd();
			}

			public override void LookupOrd(int ord, BytesRef result)
			{
				@in.LookupOrd(ord, result);
			}

			public override int GetValueCount()
			{
				return (int)@in.GetValueCount();
			}

			public override int LookupTerm(BytesRef key)
			{
				return (int)@in.LookupTerm(key);
			}
		}

		/// <summary>Wraps a SortedSetDocValues and returns the last ordinal (max)</summary>
		internal class MaxValue : SortedDocValues
		{
			internal readonly RandomAccessOrds @in;

			internal MaxValue(RandomAccessOrds @in)
			{
				this.@in = @in;
			}

			public override int GetOrd(int docID)
			{
				@in.SetDocument(docID);
				int count = @in.Cardinality();
				if (count == 0)
				{
					return -1;
				}
				else
				{
					return (int)@in.OrdAt(count - 1);
				}
			}

			public override void LookupOrd(int ord, BytesRef result)
			{
				@in.LookupOrd(ord, result);
			}

			public override int GetValueCount()
			{
				return (int)@in.GetValueCount();
			}

			public override int LookupTerm(BytesRef key)
			{
				return (int)@in.LookupTerm(key);
			}
		}

		/// <summary>Wraps a SortedSetDocValues and returns the middle ordinal (or min of the two)
		/// 	</summary>
		internal class MiddleMinValue : SortedDocValues
		{
			internal readonly RandomAccessOrds @in;

			internal MiddleMinValue(RandomAccessOrds @in)
			{
				this.@in = @in;
			}

			public override int GetOrd(int docID)
			{
				@in.SetDocument(docID);
				int count = @in.Cardinality();
				if (count == 0)
				{
					return -1;
				}
				else
				{
					return (int)@in.OrdAt((int)(((uint)(count - 1)) >> 1));
				}
			}

			public override void LookupOrd(int ord, BytesRef result)
			{
				@in.LookupOrd(ord, result);
			}

			public override int GetValueCount()
			{
				return (int)@in.GetValueCount();
			}

			public override int LookupTerm(BytesRef key)
			{
				return (int)@in.LookupTerm(key);
			}
		}

		/// <summary>Wraps a SortedSetDocValues and returns the middle ordinal (or max of the two)
		/// 	</summary>
		internal class MiddleMaxValue : SortedDocValues
		{
			internal readonly RandomAccessOrds @in;

			internal MiddleMaxValue(RandomAccessOrds @in)
			{
				this.@in = @in;
			}

			public override int GetOrd(int docID)
			{
				@in.SetDocument(docID);
				int count = @in.Cardinality();
				if (count == 0)
				{
					return -1;
				}
				else
				{
					return (int)@in.OrdAt((int)(((uint)count) >> 1));
				}
			}

			public override void LookupOrd(int ord, BytesRef result)
			{
				@in.LookupOrd(ord, result);
			}

			public override int GetValueCount()
			{
				return (int)@in.GetValueCount();
			}

			public override int LookupTerm(BytesRef key)
			{
				return (int)@in.LookupTerm(key);
			}
		}
	}
}
