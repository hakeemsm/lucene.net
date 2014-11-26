/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Synonym
{
	/// <summary>
	/// Mapping rules for use with
	/// <see cref="SlowSynonymFilter">SlowSynonymFilter</see>
	/// </summary>
	[System.ObsoleteAttribute(@"(3.4) use SynonymFilterFactory instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0"
		)]
	internal class SlowSynonymMap
	{
		/// <lucene.internal></lucene.internal>
		public CharArrayMap<Lucene.Net.Analysis.Synonym.SlowSynonymMap> submap;

		/// <lucene.internal></lucene.internal>
		public Token[] synonyms;

		internal int flags;

		internal const int INCLUDE_ORIG = unchecked((int)(0x01));

		internal const int IGNORE_CASE = unchecked((int)(0x02));

		public SlowSynonymMap()
		{
		}

		public SlowSynonymMap(bool ignoreCase)
		{
			// recursive: Map<String, SynonymMap>
			if (ignoreCase)
			{
				flags |= IGNORE_CASE;
			}
		}

		public virtual bool IncludeOrig()
		{
			return (flags & INCLUDE_ORIG) != 0;
		}

		public virtual bool IgnoreCase()
		{
			return (flags & IGNORE_CASE) != 0;
		}

		/// <param name="singleMatch">List<String>, the sequence of strings to match</param>
		/// <param name="replacement">List<Token> the list of tokens to use on a match</param>
		/// <param name="includeOrig">sets a flag on this mapping signaling the generation of matched tokens in addition to the replacement tokens
		/// 	</param>
		/// <param name="mergeExisting">merge the replacement tokens with any other mappings that exist
		/// 	</param>
		public virtual void Add(IList<string> singleMatch, IList<Token> replacement, bool
			 includeOrig, bool mergeExisting)
		{
			Lucene.Net.Analysis.Synonym.SlowSynonymMap currMap = this;
			foreach (string str in singleMatch)
			{
				if (currMap.submap == null)
				{
					// for now hardcode at 4.0, as its what the old code did.
					// would be nice to fix, but shouldn't store a version in each submap!!!
					currMap.submap = new CharArrayMap<Lucene.Net.Analysis.Synonym.SlowSynonymMap
						>(Version.LUCENE_CURRENT, 1, IgnoreCase());
				}
				Lucene.Net.Analysis.Synonym.SlowSynonymMap map = currMap.submap.Get(str);
				if (map == null)
				{
					map = new Lucene.Net.Analysis.Synonym.SlowSynonymMap();
					map.flags |= flags & IGNORE_CASE;
					currMap.submap.Put(str, map);
				}
				currMap = map;
			}
			if (currMap.synonyms != null && !mergeExisting)
			{
				throw new ArgumentException("SynonymFilter: there is already a mapping for " + singleMatch
					);
			}
			IList<Token> superset = currMap.synonyms == null ? replacement : MergeTokens(Arrays
				.AsList(currMap.synonyms), replacement);
			currMap.synonyms = Sharpen.Collections.ToArray(superset, new Token[superset.Count
				]);
			if (includeOrig)
			{
				currMap.flags |= INCLUDE_ORIG;
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder("<");
			if (synonyms != null)
			{
				sb.Append("[");
				for (int i = 0; i < synonyms.Length; i++)
				{
					if (i != 0)
					{
						sb.Append(',');
					}
					sb.Append(synonyms[i]);
				}
				if ((flags & INCLUDE_ORIG) != 0)
				{
					sb.Append(",ORIG");
				}
				sb.Append("],");
			}
			sb.Append(submap);
			sb.Append(">");
			return sb.ToString();
		}

		/// <summary>Produces a List<Token> from a List<String></summary>
		public static IList<Token> MakeTokens(IList<string> strings)
		{
			IList<Token> ret = new AList<Token>(strings.Count);
			foreach (string str in strings)
			{
				//Token newTok = new Token(str,0,0,"SYNONYM");
				Token newTok = new Token(str, 0, 0, "SYNONYM");
				ret.AddItem(newTok);
			}
			return ret;
		}

		/// <summary>
		/// Merge two lists of tokens, producing a single list with manipulated positionIncrements so that
		/// the tokens end up at the same position.
		/// </summary>
		/// <remarks>
		/// Merge two lists of tokens, producing a single list with manipulated positionIncrements so that
		/// the tokens end up at the same position.
		/// Example:  [a b] merged with [c d] produces [a/b c/d]  ('/' denotes tokens in the same position)
		/// Example:  [a,5 b,2] merged with [c d,4 e,4] produces [c a,5/d b,2 e,2]  (a,n means a has posInc=n)
		/// </remarks>
		public static IList<Token> MergeTokens(IList<Token> lst1, IList<Token> lst2)
		{
			AList<Token> result = new AList<Token>();
			if (lst1 == null || lst2 == null)
			{
				if (lst2 != null)
				{
					Sharpen.Collections.AddAll(result, lst2);
				}
				if (lst1 != null)
				{
					Sharpen.Collections.AddAll(result, lst1);
				}
				return result;
			}
			int pos = 0;
			Iterator<Token> iter1 = lst1.Iterator();
			Iterator<Token> iter2 = lst2.Iterator();
			Token tok1 = iter1.HasNext() ? iter1.Next() : null;
			Token tok2 = iter2.HasNext() ? iter2.Next() : null;
			int pos1 = tok1 != null ? tok1.GetPositionIncrement() : 0;
			int pos2 = tok2 != null ? tok2.GetPositionIncrement() : 0;
			while (tok1 != null || tok2 != null)
			{
				while (tok1 != null && (pos1 <= pos2 || tok2 == null))
				{
					Token tok = new Token(tok1.StartOffset(), tok1.EndOffset(), tok1.Type());
					tok.CopyBuffer(tok1.Buffer(), 0, tok1.Length);
					tok.SetPositionIncrement(pos1 - pos);
					result.AddItem(tok);
					pos = pos1;
					tok1 = iter1.HasNext() ? iter1.Next() : null;
					pos1 += tok1 != null ? tok1.GetPositionIncrement() : 0;
				}
				while (tok2 != null && (pos2 <= pos1 || tok1 == null))
				{
					Token tok = new Token(tok2.StartOffset(), tok2.EndOffset(), tok2.Type());
					tok.CopyBuffer(tok2.Buffer(), 0, tok2.Length);
					tok.SetPositionIncrement(pos2 - pos);
					result.AddItem(tok);
					pos = pos2;
					tok2 = iter2.HasNext() ? iter2.Next() : null;
					pos2 += tok2 != null ? tok2.GetPositionIncrement() : 0;
				}
			}
			return result;
		}
	}
}
