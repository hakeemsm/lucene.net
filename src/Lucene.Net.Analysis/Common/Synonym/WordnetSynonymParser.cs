/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Synonym
{
	/// <summary>
	/// Parser for wordnet prolog format
	/// <p>
	/// See http://wordnet.princeton.edu/man/prologdb.5WN.html for a description of the format.
	/// </summary>
	/// <remarks>
	/// Parser for wordnet prolog format
	/// <p>
	/// See http://wordnet.princeton.edu/man/prologdb.5WN.html for a description of the format.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class WordnetSynonymParser : SynonymMap.Parser
	{
		private readonly bool expand;

		public WordnetSynonymParser(bool dedup, bool expand, Analyzer analyzer) : base(dedup
			, analyzer)
		{
			// TODO: allow you to specify syntactic categories (e.g. just nouns, etc)
			this.expand = expand;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.ParseException"></exception>
		public override void Parse(StreamReader @in)
		{
			LineNumberReader br = new LineNumberReader(@in);
			try
			{
				string line = null;
				string lastSynSetID = string.Empty;
				CharsRef[] synset = new CharsRef[8];
				int synsetSize = 0;
				while ((line = br.ReadLine()) != null)
				{
					string synSetID = Sharpen.Runtime.Substring(line, 2, 11);
					if (!synSetID.Equals(lastSynSetID))
					{
						AddInternal(synset, synsetSize);
						synsetSize = 0;
					}
					if (synset.Length <= synsetSize + 1)
					{
						CharsRef[] larger = new CharsRef[synset.Length * 2];
						System.Array.Copy(synset, 0, larger, 0, synsetSize);
						synset = larger;
					}
					synset[synsetSize] = ParseSynonym(line, synset[synsetSize]);
					synsetSize++;
					lastSynSetID = synSetID;
				}
				// final synset in the file
				AddInternal(synset, synsetSize);
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
		private CharsRef ParseSynonym(string line, CharsRef reuse)
		{
			if (reuse == null)
			{
				reuse = new CharsRef(8);
			}
			int start = line.IndexOf('\'') + 1;
			int end = line.LastIndexOf('\'');
			string text = Sharpen.Runtime.Substring(line, start, end).Replace("''", "'");
			return Analyze(text, reuse);
		}

		private void AddInternal(CharsRef[] synset, int size)
		{
			if (size <= 1)
			{
				return;
			}
			// nothing to do
			if (expand)
			{
				for (int i = 0; i < size; i++)
				{
					for (int j = 0; j < size; j++)
					{
						Add(synset[i], synset[j], false);
					}
				}
			}
			else
			{
				for (int i = 0; i < size; i++)
				{
					Add(synset[i], synset[0], false);
				}
			}
		}
	}
}
