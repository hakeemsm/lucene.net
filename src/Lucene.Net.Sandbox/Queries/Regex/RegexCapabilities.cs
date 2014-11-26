/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Sandbox.Queries.Regex;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries.Regex
{
	/// <summary>
	/// Defines basic operations needed by
	/// <see cref="RegexQuery">RegexQuery</see>
	/// for a regular
	/// expression implementation.
	/// </summary>
	public abstract class RegexCapabilities
	{
		/// <summary>
		/// Called by the constructor of
		/// <see cref="RegexTermsEnum">RegexTermsEnum</see>
		/// allowing
		/// implementations to cache a compiled version of the regular
		/// expression pattern.
		/// </summary>
		/// <param name="pattern">regular expression pattern</param>
		public abstract RegexCapabilities.RegexMatcher Compile(string pattern);

		/// <summary>Interface for basic regex matching.</summary>
		/// <remarks>
		/// Interface for basic regex matching.
		/// <p>
		/// Implementations return true for
		/// <see cref="Match(Lucene.Net.Util.BytesRef)">Match(Lucene.Net.Util.BytesRef)
		/// 	</see>
		/// if the term
		/// matches the regex.
		/// <p>
		/// Implementing
		/// <see cref="Prefix()">Prefix()</see>
		/// can restrict the TermsEnum to only
		/// a subset of terms when the regular expression matches a constant
		/// prefix.
		/// <p>
		/// NOTE: implementations cannot seek.
		/// </remarks>
		public interface RegexMatcher
		{
			/// <param name="term">The term in bytes.</param>
			/// <returns>
			/// true if string matches the pattern last passed to
			/// <see cref="#compile">#compile</see>
			/// .
			/// </returns>
			bool Match(BytesRef term);

			/// <summary>
			/// A wise prefix implementation can reduce the term enumeration (and thus increase performance)
			/// of RegexQuery dramatically!
			/// </summary>
			/// <returns>
			/// static non-regex prefix of the pattern last passed to
			/// <see cref="#compile">#compile</see>
			/// .  May return null.
			/// </returns>
			string Prefix();
		}
	}
}
