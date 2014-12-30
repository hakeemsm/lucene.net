using System;
using System.Collections.Generic;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.TestFramework
{
	/// <summary>
	/// a utility class to write missing values for SORTED as if they were the empty string
	/// (to simulate pre-Lucene4.5 dv behavior for testing old codecs)
	/// </summary>
	public class MissingOrdRemapper
	{
		/// <summary>insert an empty byte[] to the front of this iterable</summary>
		public static IEnumerable<BytesRef> InsertEmptyValue(IEnumerable<BytesRef> iterable)
		{
			return new _Iterable_32(iterable);
		}

		private sealed class _Iterable_32 : IEnumerable<BytesRef>
		{
			public _Iterable_32(IEnumerable<BytesRef> iterable)
			{
				this.iterable = iterable;
			}

			public override Iterator<BytesRef> Iterator()
			{
				return new _Iterator_35(iterable);
			}

			private sealed class _Iterator_35 : Iterator<BytesRef>
			{
				public _Iterator_35(IEnumerable<BytesRef> iterable)
				{
					this.iterable = iterable;
					this.seenEmpty = false;
					this.@in = iterable.Iterator();
				}

				internal bool seenEmpty;

				internal Iterator<BytesRef> @in;

				public override bool HasNext()
				{
					return !this.seenEmpty || this.@in.HasNext();
				}

				public override BytesRef Next()
				{
					if (!this.seenEmpty)
					{
						this.seenEmpty = true;
						return new BytesRef();
					}
					else
					{
						return this.@in.Next();
					}
				}

				public override void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly IEnumerable<BytesRef> iterable;
			}

			private readonly IEnumerable<BytesRef> iterable;
		}

		/// <summary>remaps ord -1 to ord 0 on this iterable.</summary>
		/// <remarks>remaps ord -1 to ord 0 on this iterable.</remarks>
		public static IEnumerable<int> MapMissingToOrd0(IEnumerable<int> iterable)
		{
			return new _Iterable_65(iterable);
		}

		private sealed class _Iterable_65 : IEnumerable<Number>
		{
			public _Iterable_65(IEnumerable<Number> iterable)
			{
				this.iterable = iterable;
			}

			public override Iterator<Number> Iterator()
			{
				return new _Iterator_68(iterable);
			}

			private sealed class _Iterator_68 : Iterator<Number>
			{
				public _Iterator_68(IEnumerable<Number> iterable)
				{
					this.iterable = iterable;
					this.@in = iterable.Iterator();
				}

				internal Iterator<Number> @in;

				public override bool HasNext()
				{
					return this.@in.HasNext();
				}

				public override Number Next()
				{
					Number n = this.@in.Next();
					if (n == -1)
					{
						return 0;
					}
					else
					{
						return n;
					}
				}

				public override void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly IEnumerable<Number> iterable;
			}

			private readonly IEnumerable<Number> iterable;
		}

		/// <summary>remaps every ord+1 on this iterable</summary>
		public static IEnumerable<int> MapAllOrds(IEnumerable<int> iterable)
		{
			return new _Iterable_97(iterable);
		}

		private sealed class _Iterable_97 : IEnumerable<Number>
		{
			public _Iterable_97(IEnumerable<Number> iterable)
			{
				this.iterable = iterable;
			}

			public override Iterator<Number> Iterator()
			{
				return new _Iterator_100(iterable);
			}

			private sealed class _Iterator_100 : Iterator<Number>
			{
				public _Iterator_100(IEnumerable<Number> iterable)
				{
					this.iterable = iterable;
					this.@in = iterable.Iterator();
				}

				internal Iterator<Number> @in;

				public override bool HasNext()
				{
					return this.@in.HasNext();
				}

				public override Number Next()
				{
					Number n = this.@in.Next();
					return n + 1;
				}

				public override void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly IEnumerable<Number> iterable;
			}

			private readonly IEnumerable<Number> iterable;
		}
	}
}
