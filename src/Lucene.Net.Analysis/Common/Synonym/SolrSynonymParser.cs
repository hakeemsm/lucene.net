/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Synonym
{
	/// <summary>Parser for the Solr synonyms format.</summary>
	/// <remarks>
	/// Parser for the Solr synonyms format.
	/// <ol>
	/// <li> Blank lines and lines starting with '#' are comments.
	/// <li> Explicit mappings match any token sequence on the LHS of "=&gt;"
	/// and replace with all alternatives on the RHS.  These types of mappings
	/// ignore the expand parameter in the constructor.
	/// Example:
	/// <blockquote>i-pod, i pod =&gt; ipod</blockquote>
	/// <li> Equivalent synonyms may be separated with commas and give
	/// no explicit mapping.  In this case the mapping behavior will
	/// be taken from the expand parameter in the constructor.  This allows
	/// the same synonym file to be used in different synonym handling strategies.
	/// Example:
	/// <blockquote>ipod, i-pod, i pod</blockquote>
	/// <li> Multiple synonym mapping entries are merged.
	/// Example:
	/// <blockquote>
	/// foo =&gt; foo bar<br />
	/// foo =&gt; baz<br /><br />
	/// is equivalent to<br /><br />
	/// foo =&gt; foo bar, baz
	/// </blockquote>
	/// </ol>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SolrSynonymParser : SynonymMap.Parser
	{
		private readonly bool expand;

		public SolrSynonymParser(bool dedup, bool expand, Analyzer analyzer) : base(dedup
			, analyzer)
		{
			this.expand = expand;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.ParseException"></exception>
		public override void Parse(StreamReader @in)
		{
			LineNumberReader br = new LineNumberReader(@in);
			try
			{
				AddInternal(br);
			}
			catch (ArgumentException e)
			{
				ParseException ex = new ParseException("Invalid synonym rule at line " + br.GetLineNumber
					(), 0);
				Sharpen.Extensions.InitCause(ex, e);
				throw ex;
			}
			finally
			{
				br.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddInternal(BufferedReader @in)
		{
			string line = null;
			while ((line = @in.ReadLine()) != null)
			{
				if (line.Length == 0 || line[0] == '#')
				{
					continue;
				}
				// ignore empty lines and comments
				CharsRef[] inputs;
				CharsRef[] outputs;
				// TODO: we could process this more efficiently.
				string[] sides = Split(line, "=>");
				if (sides.Length > 1)
				{
					// explicit mapping
					if (sides.Length != 2)
					{
						throw new ArgumentException("more than one explicit mapping specified on the same line"
							);
					}
					string[] inputStrings = Split(sides[0], ",");
					inputs = new CharsRef[inputStrings.Length];
					for (int i = 0; i < inputs.Length; i++)
					{
						inputs[i] = Analyze(Unescape(inputStrings[i]).Trim(), new CharsRef());
					}
					string[] outputStrings = Split(sides[1], ",");
					outputs = new CharsRef[outputStrings.Length];
					for (int i_1 = 0; i_1 < outputs.Length; i_1++)
					{
						outputs[i_1] = Analyze(Unescape(outputStrings[i_1]).Trim(), new CharsRef());
					}
				}
				else
				{
					string[] inputStrings = Split(line, ",");
					inputs = new CharsRef[inputStrings.Length];
					for (int i = 0; i < inputs.Length; i++)
					{
						inputs[i] = Analyze(Unescape(inputStrings[i]).Trim(), new CharsRef());
					}
					if (expand)
					{
						outputs = inputs;
					}
					else
					{
						outputs = new CharsRef[1];
						outputs[0] = inputs[0];
					}
				}
				// currently we include the term itself in the map,
				// and use includeOrig = false always.
				// this is how the existing filter does it, but its actually a bug,
				// especially if combined with ignoreCase = true
				for (int i_2 = 0; i_2 < inputs.Length; i_2++)
				{
					for (int j = 0; j < outputs.Length; j++)
					{
						Add(inputs[i_2], outputs[j], false);
					}
				}
			}
		}

		private static string[] Split(string s, string separator)
		{
			AList<string> list = new AList<string>(2);
			StringBuilder sb = new StringBuilder();
			int pos = 0;
			int end = s.Length;
			while (pos < end)
			{
				if (s.StartsWith(separator, pos))
				{
					if (sb.Length > 0)
					{
						list.AddItem(sb.ToString());
						sb = new StringBuilder();
					}
					pos += separator.Length;
					continue;
				}
				char ch = s[pos++];
				if (ch == '\\')
				{
					sb.Append(ch);
					if (pos >= end)
					{
						break;
					}
					// ERROR, or let it go?
					ch = s[pos++];
				}
				sb.Append(ch);
			}
			if (sb.Length > 0)
			{
				list.AddItem(sb.ToString());
			}
			return Sharpen.Collections.ToArray(list, new string[list.Count]);
		}

		private string Unescape(string s)
		{
			if (s.IndexOf("\\") >= 0)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < s.Length; i++)
				{
					char ch = s[i];
					if (ch == '\\' && i < s.Length - 1)
					{
						sb.Append(s[++i]);
					}
					else
					{
						sb.Append(ch);
					}
				}
				return sb.ToString();
			}
			return s;
		}
	}
}
