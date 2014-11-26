/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Lucene.Net.Facet;
using Sharpen;

namespace Lucene.Net.Facet
{
	/// <summary>Counts or aggregates for a single dimension.</summary>
	/// <remarks>Counts or aggregates for a single dimension.</remarks>
	public sealed class FacetResult
	{
		/// <summary>Dimension that was requested.</summary>
		/// <remarks>Dimension that was requested.</remarks>
		public readonly string dim;

		/// <summary>Path whose children were requested.</summary>
		/// <remarks>Path whose children were requested.</remarks>
		public readonly string[] path;

		/// <summary>
		/// Total value for this path (sum of all child counts, or
		/// sum of all child values), even those not included in
		/// the topN.
		/// </summary>
		/// <remarks>
		/// Total value for this path (sum of all child counts, or
		/// sum of all child values), even those not included in
		/// the topN.
		/// </remarks>
		public readonly Number value;

		/// <summary>How many child labels were encountered.</summary>
		/// <remarks>How many child labels were encountered.</remarks>
		public readonly int childCount;

		/// <summary>Child counts.</summary>
		/// <remarks>Child counts.</remarks>
		public readonly LabelAndValue[] labelValues;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public FacetResult(string dim, string[] path, Number value, LabelAndValue[] labelValues
			, int childCount)
		{
			this.dim = dim;
			this.path = path;
			this.value = value;
			this.labelValues = labelValues;
			this.childCount = childCount;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("dim=");
			sb.Append(dim);
			sb.Append(" path=");
			sb.Append(Arrays.ToString(path));
			sb.Append(" value=");
			sb.Append(value);
			sb.Append(" childCount=");
			sb.Append(childCount);
			sb.Append('\n');
			foreach (LabelAndValue labelValue in labelValues)
			{
				sb.Append("  " + labelValue + "\n");
			}
			return sb.ToString();
		}

		public override bool Equals(object _other)
		{
			if ((_other is Lucene.Net.Facet.FacetResult) == false)
			{
				return false;
			}
			Lucene.Net.Facet.FacetResult other = (Lucene.Net.Facet.FacetResult)
				_other;
			return value.Equals(other.value) && childCount == other.childCount && Arrays.Equals
				(labelValues, other.labelValues);
		}

		public override int GetHashCode()
		{
			int hashCode = value.GetHashCode() + 31 * childCount;
			foreach (LabelAndValue labelValue in labelValues)
			{
				hashCode = labelValue.GetHashCode() + 31 * hashCode;
			}
			return hashCode;
		}
	}
}
