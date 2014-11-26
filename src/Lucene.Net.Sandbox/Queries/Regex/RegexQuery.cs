/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Sandbox.Queries.Regex;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries.Regex
{
	/// <summary>Implements the regular expression term search query.</summary>
	/// <remarks>
	/// Implements the regular expression term search query.
	/// The expressions supported depend on the regular expression implementation
	/// used by way of the
	/// <see cref="RegexCapabilities">RegexCapabilities</see>
	/// interface.
	/// <p>
	/// NOTE: You may wish to consider using the regex query support
	/// in
	/// <see cref="Lucene.Net.Search.RegexpQuery">Lucene.Net.Search.RegexpQuery
	/// 	</see>
	/// instead, as it has better performance.
	/// </remarks>
	/// <seealso cref="RegexTermsEnum">RegexTermsEnum</seealso>
	public class RegexQuery : MultiTermQuery, RegexQueryCapable
	{
		private RegexCapabilities regexImpl = new JavaUtilRegexCapabilities();

		private Term term;

		/// <summary>Constructs a query for terms matching <code>term</code>.</summary>
		/// <remarks>Constructs a query for terms matching <code>term</code>.</remarks>
		public RegexQuery(Term term) : base(term.Field())
		{
			// javadoc
			this.term = term;
		}

		public virtual Term GetTerm()
		{
			return term;
		}

		public virtual void SetRegexImplementation(RegexCapabilities impl)
		{
			this.regexImpl = impl;
		}

		public virtual RegexCapabilities GetRegexImplementation()
		{
			return regexImpl;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
		{
			return new RegexTermsEnum(terms.Iterator(null), term, regexImpl);
		}

		public override string ToString(string field)
		{
			StringBuilder buffer = new StringBuilder();
			if (!term.Field().Equals(field))
			{
				buffer.Append(term.Field());
				buffer.Append(":");
			}
			buffer.Append(term.Text());
			buffer.Append(ToStringUtils.Boost(GetBoost()));
			return buffer.ToString();
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int result = base.GetHashCode();
			result = prime * result + ((regexImpl == null) ? 0 : regexImpl.GetHashCode());
			result = prime * result + ((term == null) ? 0 : term.GetHashCode());
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
			Lucene.Net.Sandbox.Queries.Regex.RegexQuery other = (Lucene.Net.Sandbox.Queries.Regex.RegexQuery
				)obj;
			if (regexImpl == null)
			{
				if (other.regexImpl != null)
				{
					return false;
				}
			}
			else
			{
				if (!regexImpl.Equals(other.regexImpl))
				{
					return false;
				}
			}
			if (term == null)
			{
				if (other.term != null)
				{
					return false;
				}
			}
			else
			{
				if (!term.Equals(other.term))
				{
					return false;
				}
			}
			return true;
		}
	}
}
