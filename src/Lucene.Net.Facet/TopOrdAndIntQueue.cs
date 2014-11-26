/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Facet;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet
{
	/// <summary>
	/// Keeps highest results, first by largest int value,
	/// then tie break by smallest ord.
	/// </summary>
	/// <remarks>
	/// Keeps highest results, first by largest int value,
	/// then tie break by smallest ord.
	/// </remarks>
	public class TopOrdAndIntQueue : PriorityQueue<TopOrdAndIntQueue.OrdAndValue>
	{
		/// <summary>Holds a single entry.</summary>
		/// <remarks>Holds a single entry.</remarks>
		public sealed class OrdAndValue
		{
			/// <summary>Ordinal of the entry.</summary>
			/// <remarks>Ordinal of the entry.</remarks>
			public int ord;

			/// <summary>Value associated with the ordinal.</summary>
			/// <remarks>Value associated with the ordinal.</remarks>
			public int value;

			/// <summary>Default constructor.</summary>
			/// <remarks>Default constructor.</remarks>
			public OrdAndValue()
			{
			}
		}

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public TopOrdAndIntQueue(int topN) : base(topN, false)
		{
		}

		protected override bool LessThan(TopOrdAndIntQueue.OrdAndValue a, TopOrdAndIntQueue.OrdAndValue
			 b)
		{
			if (a.value < b.value)
			{
				return true;
			}
			else
			{
				if (a.value > b.value)
				{
					return false;
				}
				else
				{
					return a.ord > b.ord;
				}
			}
		}
	}
}
