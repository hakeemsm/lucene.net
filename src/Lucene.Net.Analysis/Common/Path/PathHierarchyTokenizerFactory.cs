/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Path;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Analysis.Path
{
	/// <summary>
	/// Factory for
	/// <see cref="PathHierarchyTokenizer">PathHierarchyTokenizer</see>
	/// .
	/// <p>
	/// This factory is typically configured for use only in the <code>index</code>
	/// Analyzer (or only in the <code>query</code> Analyzer, but never both).
	/// </p>
	/// <p>
	/// For example, in the configuration below a query for
	/// <code>Books/NonFic</code> will match documents indexed with values like
	/// <code>Books/NonFic</code>, <code>Books/NonFic/Law</code>,
	/// <code>Books/NonFic/Science/Physics</code>, etc. But it will not match
	/// documents indexed with values like <code>Books</code>, or
	/// <code>Books/Fic</code>...
	/// </p>
	/// <pre class="prettyprint">
	/// &lt;fieldType name="descendent_path" class="solr.TextField"&gt;
	/// &lt;analyzer type="index"&gt;
	/// &lt;tokenizer class="solr.PathHierarchyTokenizerFactory" delimiter="/" /&gt;
	/// &lt;/analyzer&gt;
	/// &lt;analyzer type="query"&gt;
	/// &lt;tokenizer class="solr.KeywordTokenizerFactory" /&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;
	/// </pre>
	/// <p>
	/// In this example however we see the oposite configuration, so that a query
	/// for <code>Books/NonFic/Science/Physics</code> would match documents
	/// containing <code>Books/NonFic</code>, <code>Books/NonFic/Science</code>,
	/// or <code>Books/NonFic/Science/Physics</code>, but not
	/// <code>Books/NonFic/Science/Physics/Theory</code> or
	/// <code>Books/NonFic/Law</code>.
	/// </p>
	/// <pre class="prettyprint">
	/// &lt;fieldType name="descendent_path" class="solr.TextField"&gt;
	/// &lt;analyzer type="index"&gt;
	/// &lt;tokenizer class="solr.KeywordTokenizerFactory" /&gt;
	/// &lt;/analyzer&gt;
	/// &lt;analyzer type="query"&gt;
	/// &lt;tokenizer class="solr.PathHierarchyTokenizerFactory" delimiter="/" /&gt;
	/// &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;
	/// </pre>
	/// </summary>
	public class PathHierarchyTokenizerFactory : TokenizerFactory
	{
		private readonly char delimiter;

		private readonly char replacement;

		private readonly bool reverse;

		private readonly int skip;

		/// <summary>Creates a new PathHierarchyTokenizerFactory</summary>
		protected internal PathHierarchyTokenizerFactory(IDictionary<string, string> args
			) : base(args)
		{
			delimiter = GetChar(args, "delimiter", PathHierarchyTokenizer.DEFAULT_DELIMITER);
			replacement = GetChar(args, "replace", delimiter);
			reverse = GetBoolean(args, "reverse", false);
			skip = GetInt(args, "skip", PathHierarchyTokenizer.DEFAULT_SKIP);
			if (args.Any())
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		public override Tokenizer Create(AttributeSource.AttributeFactory factory, StreamReader
			 input)
		{
			if (reverse)
			{
				return new ReversePathHierarchyTokenizer(factory, input, delimiter, replacement, 
					skip);
			}
			return new PathHierarchyTokenizer(factory, input, delimiter, replacement, skip);
		}
	}
}
