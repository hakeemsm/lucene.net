/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Reflection;
using Lucene.Net.Sandbox.Queries.Regex;
using Lucene.Net.Util;
using Org.Apache.Regexp;
using Sharpen;

namespace Lucene.Net.Sandbox.Queries.Regex
{
	/// <summary>
	/// Implementation tying <a href="http://jakarta.apache.org/regexp">Jakarta
	/// Regexp</a> to RegexQuery.
	/// </summary>
	/// <remarks>
	/// Implementation tying <a href="http://jakarta.apache.org/regexp">Jakarta
	/// Regexp</a> to RegexQuery. Jakarta Regexp internally supports a
	/// <see cref="RegexMatcher.Prefix()">RegexMatcher.Prefix()</see>
	/// implementation which can offer
	/// performance gains under certain circumstances. Yet, the implementation appears
	/// to be rather shaky as it doesn't always provide a prefix even if one would exist.
	/// </remarks>
	public class JakartaRegexpCapabilities : RegexCapabilities
	{
		private static FieldInfo prefixField;

		private static MethodInfo getPrefixMethod;

		static JakartaRegexpCapabilities()
		{
			try
			{
				getPrefixMethod = typeof(REProgram).GetMethod("getPrefix");
			}
			catch (Exception)
			{
				getPrefixMethod = null;
			}
			try
			{
				prefixField = Sharpen.Runtime.GetDeclaredField(typeof(REProgram), "prefix");
			}
			catch (Exception)
			{
				prefixField = null;
			}
		}

		private int flags = RE.MATCH_NORMAL;

		/// <summary>Flag to specify normal, case-sensitive matching behaviour.</summary>
		/// <remarks>Flag to specify normal, case-sensitive matching behaviour. This is the default.
		/// 	</remarks>
		public const int FLAG_MATCH_NORMAL = RE.MATCH_NORMAL;

		/// <summary>Flag to specify that matching should be case-independent (folded)</summary>
		public const int FLAG_MATCH_CASEINDEPENDENT = RE.MATCH_CASEINDEPENDENT;

		/// <summary>Constructs a RegexCapabilities with the default MATCH_NORMAL match style.
		/// 	</summary>
		/// <remarks>Constructs a RegexCapabilities with the default MATCH_NORMAL match style.
		/// 	</remarks>
		public JakartaRegexpCapabilities()
		{
		}

		/// <summary>Constructs a RegexCapabilities with the provided match flags.</summary>
		/// <remarks>
		/// Constructs a RegexCapabilities with the provided match flags.
		/// Multiple flags should be ORed together.
		/// </remarks>
		/// <param name="flags">The matching style</param>
		public JakartaRegexpCapabilities(int flags)
		{
			// Define the flags that are possible. Redefine them here
			// to avoid exposing the RE class to the caller.
			this.flags = flags;
		}

		public override RegexCapabilities.RegexMatcher Compile(string regex)
		{
			return new JakartaRegexpCapabilities.JakartaRegexMatcher(this, regex, flags);
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
			Lucene.Net.Sandbox.Queries.Regex.JakartaRegexpCapabilities other = (Lucene.Net.Sandbox.Queries.Regex.JakartaRegexpCapabilities
				)obj;
			return flags == other.flags;
		}

		internal class JakartaRegexMatcher : RegexCapabilities.RegexMatcher
		{
			private RE regexp;

			private readonly CharsRef utf16 = new CharsRef(10);

			private sealed class _CharacterIterator_117 : CharacterIterator
			{
				public _CharacterIterator_117(JakartaRegexMatcher _enclosing)
				{
					this._enclosing = _enclosing;
				}

				public char CharAt(int pos)
				{
					return this._enclosing.utf16.chars[pos];
				}

				public bool IsEnd(int pos)
				{
					return pos >= this._enclosing.utf16.length;
				}

				public string Substring(int beginIndex)
				{
					return this.Substring(beginIndex, this._enclosing.utf16.length);
				}

				public string Substring(int beginIndex, int endIndex)
				{
					return new string(this._enclosing.utf16.chars, beginIndex, endIndex - beginIndex);
				}

				private readonly JakartaRegexMatcher _enclosing;
			}

			private readonly CharacterIterator utf16wrapper;

			public JakartaRegexMatcher(JakartaRegexpCapabilities _enclosing, string regex, int
				 flags)
			{
				this._enclosing = _enclosing;
				utf16wrapper = new _CharacterIterator_117(this);
				this.regexp = new RE(regex, flags);
			}

			public virtual bool Match(BytesRef term)
			{
				UnicodeUtil.UTF8toUTF16(term.bytes, term.offset, term.length, this.utf16);
				return this.regexp.Match(this.utf16wrapper, 0);
			}

			public virtual string Prefix()
			{
				try
				{
					char[] prefix;
					if (JakartaRegexpCapabilities.getPrefixMethod != null)
					{
						prefix = (char[])JakartaRegexpCapabilities.getPrefixMethod.Invoke(this.regexp.GetProgram
							());
					}
					else
					{
						if (JakartaRegexpCapabilities.prefixField != null)
						{
							prefix = (char[])JakartaRegexpCapabilities.prefixField.GetValue(this.regexp.GetProgram
								());
						}
						else
						{
							return null;
						}
					}
					return prefix == null ? null : new string(prefix);
				}
				catch (Exception)
				{
					// if we cannot get the prefix, return none
					return null;
				}
			}

			private readonly JakartaRegexpCapabilities _enclosing;
		}
	}
}
