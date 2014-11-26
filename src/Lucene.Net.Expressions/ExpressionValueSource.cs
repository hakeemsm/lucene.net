/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Expressions;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Expressions
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
	/// 	</see>
	/// which evaluates a
	/// <see cref="Expression">Expression</see>
	/// given the context of an
	/// <see cref="Bindings">Bindings</see>
	/// .
	/// </summary>
	internal sealed class ExpressionValueSource : ValueSource
	{
		internal readonly ValueSource variables;

		internal readonly Expression expression;

		internal readonly bool needsScores;

		internal ExpressionValueSource(Bindings bindings, Expression expression)
		{
			if (bindings == null)
			{
				throw new ArgumentNullException();
			}
			if (expression == null)
			{
				throw new ArgumentNullException();
			}
			this.expression = expression;
			variables = new ValueSource[expression.variables.Length];
			bool needsScores = false;
			for (int i = 0; i < variables.Length; i++)
			{
				ValueSource source = bindings.GetValueSource(expression.variables[i]);
				if (source is ScoreValueSource)
				{
					needsScores = true;
				}
				else
				{
					if (source is Lucene.Net.Expressions.ExpressionValueSource)
					{
						if (((Lucene.Net.Expressions.ExpressionValueSource)source).NeedsScores())
						{
							needsScores = true;
						}
					}
					else
					{
						if (source == null)
						{
							throw new RuntimeException("Internal error. Variable (" + expression.variables[i]
								 + ") does not exist.");
						}
					}
				}
				variables[i] = source;
			}
			this.needsScores = needsScores;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			IDictionary<string, FunctionValues> valuesCache = (IDictionary<string, FunctionValues
				>)context.Get("valuesCache");
			if (valuesCache == null)
			{
				valuesCache = new Dictionary<string, FunctionValues>();
				context = new Hashtable(context);
				context.Put("valuesCache", valuesCache);
			}
			FunctionValues[] externalValues = new FunctionValues[expression.variables.Length]
				;
			for (int i = 0; i < variables.Length; ++i)
			{
				string externalName = expression.variables[i];
				FunctionValues values = valuesCache.Get(externalName);
				if (values == null)
				{
					values = variables[i].GetValues(context, readerContext);
					if (values == null)
					{
						throw new RuntimeException("Internal error. External (" + externalName + ") does not exist."
							);
					}
					valuesCache.Put(externalName, values);
				}
				externalValues[i] = values;
			}
			return new ExpressionFunctionValues(this, expression, externalValues);
		}

		public override SortField GetSortField(bool reverse)
		{
			return new ExpressionSortField(expression.sourceText, this, reverse);
		}

		public override string Description()
		{
			return "expr(" + expression.sourceText + ")";
		}

		public override int GetHashCode()
		{
			int prime = 31;
			int result = 1;
			result = prime * result + ((expression == null) ? 0 : expression.GetHashCode());
			result = prime * result + (needsScores ? 1231 : 1237);
			result = prime * result + Arrays.HashCode(variables);
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
			Lucene.Net.Expressions.ExpressionValueSource other = (Lucene.Net.Expressions.ExpressionValueSource
				)obj;
			if (expression == null)
			{
				if (other.expression != null)
				{
					return false;
				}
			}
			else
			{
				if (!expression.Equals(other.expression))
				{
					return false;
				}
			}
			if (needsScores != other.needsScores)
			{
				return false;
			}
			if (!Arrays.Equals(variables, other.variables))
			{
				return false;
			}
			return true;
		}

		internal bool NeedsScores()
		{
			return needsScores;
		}
	}
}
