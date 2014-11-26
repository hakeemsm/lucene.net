/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Lucene.Analysis.Util;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;

namespace Lucene.Net.Analysis.PT
{
	/// <summary>Base class for stemmers that use a set of RSLP-like stemming steps.</summary>
	/// <remarks>
	/// Base class for stemmers that use a set of RSLP-like stemming steps.
	/// <p>
	/// RSLP (Removedor de Sufixos da Lingua Portuguesa) is an algorithm designed
	/// originally for stemming the Portuguese language, described in the paper
	/// <i>A Stemming Algorithm for the Portuguese Language</i>, Orengo et. al.
	/// <p>
	/// Since this time a plural-only modification (RSLP-S) as well as a modification
	/// for the Galician language have been implemented. This class parses a configuration
	/// file that describes
	/// <see cref="Step">Step</see>
	/// s, where each Step contains a set of
	/// <see cref="Rule">Rule</see>
	/// s.
	/// <p>
	/// The general rule format is:
	/// <blockquote>{ "suffix", N, "replacement", { "exception1", "exception2", ...}}</blockquote>
	/// where:
	/// <ul>
	/// <li><code>suffix</code> is the suffix to be removed (such as "inho").
	/// <li><code>N</code> is the min stem size, where stem is defined as the candidate stem
	/// after removing the suffix (but before appending the replacement!)
	/// <li><code>replacement</code> is an optimal string to append after removing the suffix.
	/// This can be the empty string.
	/// <li><code>exceptions</code> is an optional list of exceptions, patterns that should
	/// not be stemmed. These patterns can be specified as whole word or suffix (ends-with)
	/// patterns, depending upon the exceptions format flag in the step header.
	/// </ul>
	/// <p>
	/// A step is an ordered list of rules, with a structure in this format:
	/// <blockquote>{ "name", N, B, { "cond1", "cond2", ... }
	/// ... rules ... };
	/// </blockquote>
	/// where:
	/// <ul>
	/// <li><code>name</code> is a name for the step (such as "Plural").
	/// <li><code>N</code> is the min word size. Words that are less than this length bypass
	/// the step completely, as an optimization. Note: N can be zero, in this case this
	/// implementation will automatically calculate the appropriate value from the underlying
	/// rules.
	/// <li><code>B</code> is a "boolean" flag specifying how exceptions in the rules are matched.
	/// A value of 1 indicates whole-word pattern matching, a value of 0 indicates that
	/// exceptions are actually suffixes and should be matched with ends-with.
	/// <li><code>conds</code> are an optional list of conditions to enter the step at all. If
	/// the list is non-empty, then a word must end with one of these conditions or it will
	/// bypass the step completely as an optimization.
	/// </ul>
	/// <p>
	/// </remarks>
	/// <seealso><a href="http://www.inf.ufrgs.br/~viviane/rslp/index.htm">RSLP description</a>
	/// 	</seealso>
	/// <lucene.internal></lucene.internal>
	public abstract class RSLPStemmerBase
	{
		/// <summary>A basic rule, with no exceptions.</summary>
		/// <remarks>A basic rule, with no exceptions.</remarks>
		protected internal class Rule
		{
			protected internal readonly char[] suffix;

			protected internal readonly char[] replacement;

			protected internal readonly int min;

			/// <summary>Create a rule.</summary>
			/// <remarks>Create a rule.</remarks>
			/// <param name="suffix">suffix to remove</param>
			/// <param name="min">minimum stem length</param>
			/// <param name="replacement">replacement string</param>
			public Rule(string suffix, int min, string replacement)
			{
				this.suffix = suffix.ToCharArray();
				this.replacement = replacement.ToCharArray();
				this.min = min;
			}

			/// <returns>true if the word matches this rule.</returns>
			public virtual bool Matches(char[] s, int len)
			{
				return (len - suffix.Length >= min && StemmerUtil.EndsWith(s, len, suffix));
			}

			/// <returns>new valid length of the string after firing this rule.</returns>
			public virtual int Replace(char[] s, int len)
			{
				if (replacement.Length > 0)
				{
					System.Array.Copy(replacement, 0, s, len - suffix.Length, replacement.Length);
				}
				return len - suffix.Length + replacement.Length;
			}
		}

		/// <summary>A rule with a set of whole-word exceptions.</summary>
		/// <remarks>A rule with a set of whole-word exceptions.</remarks>
		protected internal class RuleWithSetExceptions : RSLPStemmerBase.Rule
		{
			protected internal readonly CharArraySet exceptions;

			public RuleWithSetExceptions(string suffix, int min, string replacement, string[]
				 exceptions) : base(suffix, min, replacement)
			{
				for (int i = 0; i < exceptions.Length; i++)
				{
					if (!exceptions[i].EndsWith(suffix))
					{
						throw new Exception("useless exception '" + exceptions[i] + "' does not end with '"
							 + suffix + "'");
					}
				}
				this.exceptions = new CharArraySet(Lucene.Net.Util.Version.LUCENE_CURRENT, exceptions, false);
			}

			public override bool Matches(char[] s, int len)
			{
				return base.Matches(s, len) && !exceptions.Contains(s, 0, len);
			}
		}

		/// <summary>A rule with a set of exceptional suffixes.</summary>
		/// <remarks>A rule with a set of exceptional suffixes.</remarks>
		protected internal class RuleWithSuffixExceptions : RSLPStemmerBase.Rule
		{
			protected internal readonly char[][] exceptions;

			public RuleWithSuffixExceptions(string suffix, int min, string replacement, string
				[] exceptions) : base(suffix, min, replacement)
			{
				// TODO: use a more efficient datastructure: automaton?
				for (int i = 0; i < exceptions.Length; i++)
				{
					if (!exceptions[i].EndsWith(suffix))
					{
						throw new Exception("warning: useless exception '" + exceptions[i] + "' does not end with '"
							 + suffix + "'");
					}
				}
				this.exceptions = new char[exceptions.Length][];
				for (int i_1 = 0; i_1 < exceptions.Length; i_1++)
				{
					this.exceptions[i_1] = exceptions[i_1].ToCharArray();
				}
			}

			public override bool Matches(char[] s, int len)
			{
				if (!base.Matches(s, len))
				{
					return false;
				}
				for (int i = 0; i < exceptions.Length; i++)
				{
					if (StemmerUtil.EndsWith(s, len, exceptions[i]))
					{
						return false;
					}
				}
				return true;
			}
		}

		/// <summary>A step containing a list of rules.</summary>
		/// <remarks>A step containing a list of rules.</remarks>
		protected internal class Step
		{
			protected internal readonly string name;

			protected internal readonly Rule[] rules;

			protected internal readonly int min;

			protected internal readonly char[][] suffixes;

			/// <summary>Create a new step</summary>
			/// <param name="name">Step's name.</param>
			/// <param name="rules">an ordered list of rules.</param>
			/// <param name="min">minimum word size. if this is 0 it is automatically calculated.
			/// 	</param>
			/// <param name="suffixes">optional list of conditional suffixes. may be null.</param>
			public Step(string name, RSLPStemmerBase.Rule[] rules, int min, string[] suffixes
				)
			{
				this.name = name;
				this.rules = rules;
				if (min == 0)
				{
					min = int.MaxValue;
					foreach (RSLPStemmerBase.Rule r in rules)
					{
						min = Math.Min(min, r.min + r.suffix.Length);
					}
				}
				this.min = min;
				if (suffixes == null || suffixes.Length == 0)
				{
					this.suffixes = null;
				}
				else
				{
					this.suffixes = new char[suffixes.Length][];
					for (int i = 0; i < suffixes.Length; i++)
					{
						this.suffixes[i] = suffixes[i].ToCharArray();
					}
				}
			}

			/// <returns>new valid length of the string after applying the entire step.</returns>
			public virtual int Apply(char[] s, int len)
			{
				if (len < min)
				{
					return len;
				}
				if (suffixes != null)
				{
					bool found = false;
					for (int i = 0; i < suffixes.Length; i++)
					{
						if (StemmerUtil.EndsWith(s, len, suffixes[i]))
						{
							found = true;
							break;
						}
					}
					if (!found)
					{
						return len;
					}
				}
				for (int i_1 = 0; i_1 < rules.Length; i_1++)
				{
					if (rules[i_1].Matches(s, len))
					{
						return rules[i_1].Replace(s, len);
					}
				}
				return len;
			}
		}

		/// <summary>Parse a resource file into an RSLP stemmer description.</summary>
		/// <remarks>Parse a resource file into an RSLP stemmer description.</remarks>
		/// <returns>a Map containing the named Steps in this description.</returns>
		protected internal static IDictionary<string, Step> Parse<T>(T clazz, string resource) //where T:RSLPStemmerBase
		{
			// TODO: this parser is ugly, but works. use a jflex grammar instead.
			try
			{
			    IDictionary<string, Step> steps = new Dictionary<string, Step>();
			    using (Stream res = typeof (T).Assembly.GetManifestResourceStream(resource))
                using(StreamReader sr = new StreamReader(res))
                {
                    int lineNum = 0;
                    while (!sr.EndOfStream)
			        {
			            string line = sr.ReadLine();
			            if (!string.IsNullOrEmpty(line) && line[0] != '#')
			            {
			                Step step = ParseStep(lineNum, line);
                            steps.Add(step.name,step);

			            }
			        }
			    }
				
				return steps;
			}
			catch (IOException e)
			{
				throw new IOException(e.Message);
			}
		}

		private static readonly Regex headerPattern = new Regex("^\\{\\s*\"([^\"]*)\",\\s*([0-9]+),\\s*(0|1),\\s*\\{(.*)\\},\\s*$");
        

		private static readonly Regex stripPattern = new Regex("^\\{\\s*\"([^\"]*)\",\\s*([0-9]+)\\s*\\}\\s*(,|(\\}\\s*;))$");

		private static readonly Regex repPattern = new Regex("^\\{\\s*\"([^\"]*)\",\\s*([0-9]+),\\s*\"([^\"]*)\"\\}\\s*(,|(\\}\\s*;))$");

		private static readonly Regex excPattern = new Regex("^\\{\\s*\"([^\"]*)\",\\s*([0-9]+),\\s*\"([^\"]*)\",\\s*\\{(.*)\\}\\s*\\}\\s*(,|(\\}\\s*;))$");

		/// <exception cref="System.IO.IOException"></exception>
		private static Step ParseStep(int lineNum, string line)
		{
            
			Match matcher = headerPattern.Match(line);
			if (!matcher.Success)
			{
				throw new Exception("Illegal Step header specified at line " + lineNum);
			}
			//HM:revisit 
			//assert matcher.groupCount() == 4;
			string name = matcher.Groups[1].Value;
			int min = Convert.ToInt32(matcher.Groups[2].Value);
			int type = Convert.ToInt32(matcher.Groups[3].Value);
			string[] suffixes = ParseList(matcher.Groups[4].Value);
			Rule[] rules = ParseRules(lineNum,line, type);
			return new Step(name, rules, min, suffixes);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static Rule[] ParseRules(int lineNum, string line, int type)
		{
			var rules = new List<Rule>();
			
			
				Match matcher = stripPattern.Match(line);
				if (matcher.Success)
				{
					rules.Add(new Rule(matcher.Groups[1].Value, Convert.ToInt32(matcher
						.Groups[2].Value), string.Empty));
				}
				else
				{
					matcher = repPattern.Match(line);
					if (matcher.Success)
					{
						rules.Add(new Rule(matcher.Groups[1].Value, Convert.ToInt32(matcher
							.Groups[2].Value), matcher.Groups[3].Value));
					}
					else
					{
						matcher = excPattern.Match(line);
						if (matcher.Success)
						{
							if (type == 0)
							{
								rules.Add(new RuleWithSuffixExceptions(matcher.Groups[1].Value, Convert.ToInt32
									(matcher.Groups[2].Value), matcher.Groups[3].Value, ParseList(matcher.Groups[4].Value)));
							}
							else
							{
								rules.Add(new RuleWithSetExceptions(matcher.Groups[1].Value, Convert.ToInt32
									(matcher.Groups[2]), matcher.Groups[3].Value, ParseList(matcher.Groups[4].Value)));
							}
						}
						else
						{
							throw new Exception("Illegal Step rule specified at line " + lineNum);
						}
					}
				}
				if (line.EndsWith(";"))
				{
					return rules.ToArray();
				}
			
			return null;
		}

		private static string[] ParseList(string s)
		{
			if (s.Length == 0)
			{
				return null;
			}
			string[] list = s.Split(new []{','});
			for (int i = 0; i < list.Length; i++)
			{
				list[i] = ParseString(list[i].Trim());
			}
			return list;
		}

		private static string ParseString(string s)
		{
			return s.Substring(1, s.Length - 1);
		}
	}
}
