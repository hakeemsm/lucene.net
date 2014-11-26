/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Sharpen;

namespace Lucene.Net.Facet
{
	/// <summary>
	/// Single label and its value, usually contained in a
	/// <see cref="FacetResult">FacetResult</see>
	/// .
	/// </summary>
	public sealed class LabelAndValue
	{
		/// <summary>Facet's label.</summary>
		/// <remarks>Facet's label.</remarks>
		public readonly string label;

		/// <summary>Value associated with this label.</summary>
		/// <remarks>Value associated with this label.</remarks>
		public readonly Number value;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public LabelAndValue(string label, Number value)
		{
			this.label = label;
			this.value = value;
		}

		public override string ToString()
		{
			return label + " (" + value + ")";
		}

		public override bool Equals(object _other)
		{
			if ((_other is Lucene.Net.Facet.LabelAndValue) == false)
			{
				return false;
			}
			Lucene.Net.Facet.LabelAndValue other = (Lucene.Net.Facet.LabelAndValue
				)_other;
			return label.Equals(other.label) && value.Equals(other.value);
		}

		public override int GetHashCode()
		{
			return label.GetHashCode() + 1439 * value.GetHashCode();
		}
	}
}
