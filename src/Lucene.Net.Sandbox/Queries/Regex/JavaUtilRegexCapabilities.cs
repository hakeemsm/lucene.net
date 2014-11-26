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
	/// <summary>An implementation tying Java's built-in java.util.regex to RegexQuery.</summary>
	/// <remarks>
	/// An implementation tying Java's built-in java.util.regex to RegexQuery.
	/// Note that because this implementation currently only returns null from
	/// <see cref="RegexMatcher.Prefix()">RegexMatcher.Prefix()</see>
	/// that queries using this implementation
	/// will enumerate and attempt to
	/// <see cref="RegexMatcher.Match(Lucene.Net.Util.BytesRef)">RegexMatcher.Match(Lucene.Net.Util.BytesRef)
	/// 	</see>
	/// each
	/// term for the specified field in the index.
	/// </remarks>
	public class JavaUtilRegexCapabilities : RegexCapabilities
	{
		private int flags = 0;

		public const int FLAG_CANON_EQ = Sharpen.Pattern.CANON_EQ;

		public const int FLAG_CASE_INSENSITIVE = Sharpen.Pattern.CASE_INSENSITIVE;

		public const int FLAG_COMMENTS = Sharpen.Pattern.COMMENTS;

		public const int FLAG_DOTALL = Sharpen.Pattern.DOTALL;

		public const int FLAG_LITERAL = Sharpen.Pattern.LITERAL;

		public const int FLAG_MULTILINE = Sharpen.Pattern.MULTILINE;

		public const int FLAG_UNICODE_CASE = Sharpen.Pattern.UNICODE_CASE;

		public const int FLAG_UNIX_LINES = Sharpen.Pattern.UNIX_LINES;

		/// <summary>
		/// Default constructor that uses java.util.regex.Pattern
		/// with its default flags.
		/// </summary>
		/// <remarks>
		/// Default constructor that uses java.util.regex.Pattern
		/// with its default flags.
		/// </remarks>
		public JavaUtilRegexCapabilities()
		{
			// Define the optional flags from Pattern that can be used.
			// Do this here to keep Pattern contained within this class.
			this.flags = 0;
		}

		/// <summary>
		/// Constructor that allows for the modification of the flags that
		/// the java.util.regex.Pattern will use to compile the regular expression.
		/// </summary>
		/// <remarks>
		/// Constructor that allows for the modification of the flags that
		/// the java.util.regex.Pattern will use to compile the regular expression.
		/// This gives the user the ability to fine-tune how the regular expression
		/// to match the functionality that they need.
		/// The
		/// <see cref="Sharpen.Pattern">Pattern</see>
		/// class supports specifying
		/// these fields via the regular expression text itself, but this gives the caller
		/// another option to modify the behavior. Useful in cases where the regular expression text
		/// cannot be modified, or if doing so is undesired.
		/// </remarks>
		/// <param name="flags">The flags that are ORed together.</param>
		public JavaUtilRegexCapabilities(int flags)
		{
			this.flags = flags;
		}

		public override RegexCapabilities.RegexMatcher Compile(string regex)
		{
			return new JavaUtilRegexCapabilities.JavaUtilRegexMatcher(this, regex, flags);
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int result = 1;
			result = prime * result + flags;
			return result;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (obj == null)
			{
				return false;
			}
			if (GetType() != obj.GetType())
			{
				return false;
			}
			Lucene.Net.Sandbox.Queries.Regex.JavaUtilRegexCapabilities other = (Lucene.Net.Sandbox.Queries.Regex.JavaUtilRegexCapabilities
				)obj;
			return flags == other.flags;
		}

		internal class JavaUtilRegexMatcher : RegexCapabilities.RegexMatcher
		{
			private readonly Sharpen.Pattern pattern;

			private readonly Matcher matcher;

			private readonly CharsRef utf16 = new CharsRef(10);

			public JavaUtilRegexMatcher(JavaUtilRegexCapabilities _enclosing, string regex, int
				 flags)
			{
				this._enclosing = _enclosing;
				this.pattern = Sharpen.Pattern.Compile(regex, flags);
				this.matcher = this.pattern.Matcher(this.utf16);
			}

			public virtual bool Match(BytesRef term)
			{
				UnicodeUtil.UTF8toUTF16(term.bytes, term.offset, term.length, this.utf16);
				return this.matcher.Reset().Matches();
			}

			public virtual string Prefix()
			{
				return null;
			}

			private readonly JavaUtilRegexCapabilities _enclosing;
		}
	}
}
