/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>
	/// Holds a sequence of string components, specifying the hierarchical name of a
	/// category.
	/// </summary>
	/// <remarks>
	/// Holds a sequence of string components, specifying the hierarchical name of a
	/// category.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class CategoryPath : Comparable<Lucene.Net.Facet.Taxonomy.CategoryPath
		>
	{
		/// <summary>
		/// An empty
		/// <see cref="CategoryPath">CategoryPath</see>
		/// 
		/// </summary>
		public static readonly Lucene.Net.Facet.Taxonomy.CategoryPath EMPTY = new 
			Lucene.Net.Facet.Taxonomy.CategoryPath();

		/// <summary>
		/// The components of this
		/// <see cref="CategoryPath">CategoryPath</see>
		/// . Note that this array may be
		/// shared with other
		/// <see cref="CategoryPath">CategoryPath</see>
		/// instances, e.g. as a result of
		/// <see cref="Subpath(int)">Subpath(int)</see>
		/// , therefore you should traverse the array up to
		/// <see cref="length">length</see>
		/// for this path's components.
		/// </summary>
		public readonly string[] components;

		/// <summary>
		/// The number of components of this
		/// <see cref="CategoryPath">CategoryPath</see>
		/// .
		/// </summary>
		public readonly int length;

		public CategoryPath()
		{
			// Used by singleton EMPTY
			components = null;
			length = 0;
		}

		private CategoryPath(Lucene.Net.Facet.Taxonomy.CategoryPath copyFrom, int 
			prefixLen)
		{
			// Used by subpath
			// while the code which calls this method is safe, at some point a test
			// tripped on AIOOBE in toString, but we failed to reproduce. adding the
			// assert as a safety check.
			//HM:revisit
			this.components = copyFrom.components;
			length = prefixLen;
		}

		/// <summary>Construct from the given path components.</summary>
		/// <remarks>Construct from the given path components.</remarks>
		public CategoryPath(params string[] components)
		{
			//HM:revisit
			//assert components.length > 0 : "use CategoryPath.EMPTY to create an empty path";
			foreach (string comp in components)
			{
				if (comp == null || comp.IsEmpty())
				{
					throw new ArgumentException("empty or null components not allowed: " + Arrays.ToString
						(components));
				}
			}
			this.components = components;
			length = components.Length;
		}

		/// <summary>
		/// Construct from a given path, separating path components with
		/// <code>delimiter</code>
		/// .
		/// </summary>
		public CategoryPath(string pathString, char delimiter)
		{
			string[] comps = pathString.Split(Sharpen.Pattern.Quote(char.ToString(delimiter))
				);
			if (comps.Length == 1 && comps[0].IsEmpty())
			{
				components = null;
				length = 0;
			}
			else
			{
				foreach (string comp in comps)
				{
					if (comp == null || comp.IsEmpty())
					{
						throw new ArgumentException("empty or null components not allowed: " + Arrays.ToString
							(comps));
					}
				}
				components = comps;
				length = components.Length;
			}
		}

		/// <summary>
		/// Returns the number of characters needed to represent the path, including
		/// delimiter characters, for using with
		/// <see cref="CopyFullPath(char[], int, char)">CopyFullPath(char[], int, char)</see>
		/// .
		/// </summary>
		public virtual int FullPathLength()
		{
			if (length == 0)
			{
				return 0;
			}
			int charsNeeded = 0;
			for (int i = 0; i < length; i++)
			{
				charsNeeded += components[i].Length;
			}
			charsNeeded += length - 1;
			// num delimter chars
			return charsNeeded;
		}

		/// <summary>
		/// Compares this path with another
		/// <see cref="CategoryPath">CategoryPath</see>
		/// for lexicographic
		/// order.
		/// </summary>
		public virtual int CompareTo(Lucene.Net.Facet.Taxonomy.CategoryPath other)
		{
			int len = length < other.length ? length : other.length;
			for (int i = 0; i < len; i++, j++)
			{
				int cmp = Sharpen.Runtime.CompareOrdinal(components[i], other.components[j]);
				if (cmp < 0)
				{
					return -1;
				}
				// this is 'before'
				if (cmp > 0)
				{
					return 1;
				}
			}
			// this is 'after'
			// one is a prefix of the other
			return length - other.length;
		}

		private void HasDelimiter(string offender, char delimiter)
		{
			throw new ArgumentException("delimiter character '" + delimiter + "' (U+" + Sharpen.Extensions.ToHexString
				(delimiter) + ") appears in path component \"" + offender + "\"");
		}

		private void NoDelimiter(char[] buf, int offset, int len, char delimiter)
		{
			for (int idx = 0; idx < len; idx++)
			{
				if (buf[offset + idx] == delimiter)
				{
					HasDelimiter(new string(buf, offset, len), delimiter);
				}
			}
		}

		/// <summary>
		/// Copies the path components to the given
		/// <code>char[]</code>
		/// , starting at index
		/// <code>start</code>
		/// .
		/// <code>delimiter</code>
		/// is copied between the path components.
		/// Returns the number of chars copied.
		/// <p>
		/// <b>NOTE:</b> this method relies on the array being large enough to hold the
		/// components and separators - the amount of needed space can be calculated
		/// with
		/// <see cref="FullPathLength()">FullPathLength()</see>
		/// .
		/// </summary>
		public virtual int CopyFullPath(char[] buf, int start, char delimiter)
		{
			if (length == 0)
			{
				return 0;
			}
			int idx = start;
			int upto = length - 1;
			for (int i = 0; i < upto; i++)
			{
				int len = components[i].Length;
				Sharpen.Runtime.GetCharsForString(components[i], 0, len, buf, idx);
				NoDelimiter(buf, idx, len, delimiter);
				idx += len;
				buf[idx++] = delimiter;
			}
			Sharpen.Runtime.GetCharsForString(components[upto], 0, components[upto].Length, buf
				, idx);
			NoDelimiter(buf, idx, components[upto].Length, delimiter);
			return idx + components[upto].Length - start;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Lucene.Net.Facet.Taxonomy.CategoryPath))
			{
				return false;
			}
			Lucene.Net.Facet.Taxonomy.CategoryPath other = (Lucene.Net.Facet.Taxonomy.CategoryPath
				)obj;
			if (length != other.length)
			{
				return false;
			}
			// not same length, cannot be equal
			// CategoryPaths are more likely to differ at the last components, so start
			// from last-first
			for (int i = length - 1; i >= 0; i--)
			{
				if (!components[i].Equals(other.components[i]))
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			if (length == 0)
			{
				return 0;
			}
			int hash = length;
			for (int i = 0; i < length; i++)
			{
				hash = hash * 31 + components[i].GetHashCode();
			}
			return hash;
		}

		/// <summary>Calculate a 64-bit hash function for this path.</summary>
		/// <remarks>Calculate a 64-bit hash function for this path.</remarks>
		public virtual long LongHashCode()
		{
			if (length == 0)
			{
				return 0;
			}
			long hash = length;
			for (int i = 0; i < length; i++)
			{
				hash = hash * 65599 + components[i].GetHashCode();
			}
			return hash;
		}

		/// <summary>
		/// Returns a sub-path of this path up to
		/// <code>length</code>
		/// components.
		/// </summary>
		public virtual Lucene.Net.Facet.Taxonomy.CategoryPath Subpath(int length)
		{
			if (length >= this.length || length < 0)
			{
				return this;
			}
			else
			{
				if (length == 0)
				{
					return EMPTY;
				}
				else
				{
					return new Lucene.Net.Facet.Taxonomy.CategoryPath(this, length);
				}
			}
		}

		/// <summary>
		/// Returns a string representation of the path, separating components with
		/// '/'.
		/// </summary>
		/// <remarks>
		/// Returns a string representation of the path, separating components with
		/// '/'.
		/// </remarks>
		/// <seealso cref="ToString(char)">ToString(char)</seealso>
		public override string ToString()
		{
			return ToString('/');
		}

		/// <summary>
		/// Returns a string representation of the path, separating components with the
		/// given delimiter.
		/// </summary>
		/// <remarks>
		/// Returns a string representation of the path, separating components with the
		/// given delimiter.
		/// </remarks>
		public virtual string ToString(char delimiter)
		{
			if (length == 0)
			{
				return string.Empty;
			}
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < length; i++)
			{
				if (components[i].IndexOf(delimiter) != -1)
				{
					HasDelimiter(components[i], delimiter);
				}
				sb.Append(components[i]).Append(delimiter);
			}
			sb.Length = sb.Length - 1;
			// remove last delimiter
			return sb.ToString();
		}
	}
}
