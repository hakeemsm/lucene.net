/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Lucene.Net.Expressions;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Expressions
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Search.SortField">Lucene.Net.Search.SortField
	/// 	</see>
	/// which sorts documents by the evaluated value of an expression for each document
	/// </summary>
	internal class ExpressionSortField : SortField
	{
		private readonly ExpressionValueSource source;

		internal ExpressionSortField(string name, ExpressionValueSource source, bool reverse
			) : base(name, SortField.Type.CUSTOM, reverse)
		{
			this.source = source;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldComparator<object> GetComparator(int numHits, int sortPos)
		{
			return new ExpressionComparator(source, numHits);
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			result = prime * result + ((source == null) ? 0 : source.GetHashCode());
			return result;
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
			Lucene.Net.Expressions.ExpressionSortField other = (Lucene.Net.Expressions.ExpressionSortField
				)obj;
			if (source == null)
			{
				if (other.source != null)
				{
					return false;
				}
			}
			else
			{
				if (!source.Equals(other.source))
				{
					return false;
				}
			}
			return true;
		}

		public override string ToString()
		{
			StringBuilder buffer = new StringBuilder();
			buffer.Append("<expr \"");
			buffer.Append(GetField());
			buffer.Append("\">");
			if (GetReverse())
			{
				buffer.Append('!');
			}
			return buffer.ToString();
		}

		public override bool NeedsScores()
		{
			return source.NeedsScores();
		}
	}
}
