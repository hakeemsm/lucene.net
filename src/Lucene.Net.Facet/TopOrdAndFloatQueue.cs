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
	/// Keeps highest results, first by largest float value,
	/// then tie break by smallest ord.
	/// </summary>
	/// <remarks>
	/// Keeps highest results, first by largest float value,
	/// then tie break by smallest ord.
	/// </remarks>
	public class TopOrdAndFloatQueue : PriorityQueue<TopOrdAndFloatQueue.OrdAndValue>
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
			public float value;

			/// <summary>Default constructor.</summary>
			/// <remarks>Default constructor.</remarks>
			public OrdAndValue()
			{
			}
		}

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public TopOrdAndFloatQueue(int topN) : base(topN, false)
		{
		}

		protected override bool LessThan(TopOrdAndFloatQueue.OrdAndValue a, TopOrdAndFloatQueue.OrdAndValue
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
