/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Expressions;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.Valuesource;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Expressions
{
	/// <summary>
	/// Simple class that binds expression variable names to
	/// <see cref="Lucene.Net.Search.SortField">Lucene.Net.Search.SortField
	/// 	</see>
	/// s
	/// or other
	/// <see cref="Expression">Expression</see>
	/// s.
	/// <p>
	/// Example usage:
	/// <pre class="prettyprint">
	/// SimpleBindings bindings = new SimpleBindings();
	/// // document's text relevance score
	/// bindings.add(new SortField("_score", SortField.Type.SCORE));
	/// // integer NumericDocValues field (or from FieldCache)
	/// bindings.add(new SortField("popularity", SortField.Type.INT));
	/// // another expression
	/// bindings.add("recency", myRecencyExpression);
	/// // create a sort field in reverse order
	/// Sort sort = new Sort(expr.getSortField(bindings, true));
	/// </pre>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public sealed class SimpleBindings : Bindings
	{
		internal readonly IDictionary<string, object> map = new Dictionary<string, object
			>();

		/// <summary>Creates a new empty Bindings</summary>
		public SimpleBindings()
		{
		}

		/// <summary>Adds a SortField to the bindings.</summary>
		/// <remarks>
		/// Adds a SortField to the bindings.
		/// <p>
		/// This can be used to reference a DocValuesField, a field from
		/// FieldCache, the document's score, etc.
		/// </remarks>
		public void Add(SortField sortField)
		{
			map.Put(sortField.GetField(), sortField);
		}

		/// <summary>Adds an Expression to the bindings.</summary>
		/// <remarks>
		/// Adds an Expression to the bindings.
		/// <p>
		/// This can be used to reference expressions from other expressions.
		/// </remarks>
		public void Add(string name, Expression expression)
		{
			map.Put(name, expression);
		}

		public override ValueSource GetValueSource(string name)
		{
			object o = map.Get(name);
			if (o == null)
			{
				throw new ArgumentException("Invalid reference '" + name + "'");
			}
			else
			{
				if (o is Expression)
				{
					return ((Expression)o).GetValueSource(this);
				}
			}
			SortField field = (SortField)o;
			switch (field.GetType())
			{
				case SortField.Type.INT:
				{
					return new IntFieldSource(field.GetField(), (FieldCache.IntParser)field.GetParser
						());
				}

				case SortField.Type.LONG:
				{
					return new LongFieldSource(field.GetField(), (FieldCache.LongParser)field.GetParser
						());
				}

				case SortField.Type.FLOAT:
				{
					return new FloatFieldSource(field.GetField(), (FieldCache.FloatParser)field.GetParser
						());
				}

				case SortField.Type.DOUBLE:
				{
					return new DoubleFieldSource(field.GetField(), (FieldCache.DoubleParser)field.GetParser
						());
				}

				case SortField.Type.SCORE:
				{
					return GetScoreValueSource();
				}

				default:
				{
					throw new NotSupportedException();
				}
			}
		}

		/// <summary>Traverses the graph of bindings, checking there are no cycles or missing references
		/// 	</summary>
		/// <exception cref="System.ArgumentException">if the bindings is inconsistent</exception>
		public void Validate()
		{
			foreach (object o in map.Values)
			{
				if (o is Expression)
				{
					Expression expr = (Expression)o;
					try
					{
						expr.GetValueSource(this);
					}
					catch (StackOverflowError)
					{
						throw new ArgumentException("Recursion Error: Cycle detected originating in (" + 
							expr.sourceText + ")");
					}
				}
			}
		}
	}
}
