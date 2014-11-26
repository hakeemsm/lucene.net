/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	/// <summary>
	/// Creates queries from the
	/// <see cref="Lucene.Net.Analysis.Analyzer">Lucene.Net.Analysis.Analyzer
	/// 	</see>
	/// chain.
	/// <p>
	/// Example usage:
	/// <pre class="prettyprint">
	/// QueryBuilder builder = new QueryBuilder(analyzer);
	/// Query a = builder.createBooleanQuery("body", "just a test");
	/// Query b = builder.createPhraseQuery("body", "another test");
	/// Query c = builder.createMinShouldMatchQuery("body", "another test", 0.5f);
	/// </pre>
	/// <p>
	/// This can also be used as a subclass for query parsers to make it easier
	/// to interact with the analysis chain. Factory methods such as
	/// <code>newTermQuery</code>
	/// 
	/// are provided so that the generated queries can be customized.
	/// </summary>
	public class QueryBuilder
	{
		private Analyzer analyzer;

		private bool enablePositionIncrements = true;

		/// <summary>Creates a new QueryBuilder using the given analyzer.</summary>
		/// <remarks>Creates a new QueryBuilder using the given analyzer.</remarks>
		public QueryBuilder(Analyzer analyzer)
		{
			this.analyzer = analyzer;
		}

		/// <summary>Creates a boolean query from the query text.</summary>
		/// <remarks>
		/// Creates a boolean query from the query text.
		/// <p>
		/// This is equivalent to
		/// <code>createBooleanQuery(field, queryText, Occur.SHOULD)</code>
		/// </remarks>
		/// <param name="field">field name</param>
		/// <param name="queryText">text to be passed to the analyzer</param>
		/// <returns>
		/// 
		/// <code>TermQuery</code>
		/// or
		/// <code>BooleanQuery</code>
		/// , based on the analysis
		/// of
		/// <code>queryText</code>
		/// </returns>
		public virtual Query CreateBooleanQuery(string field, string queryText)
		{
			return CreateBooleanQuery(field, queryText, BooleanClause.Occur.SHOULD);
		}

		/// <summary>Creates a boolean query from the query text.</summary>
		/// <remarks>
		/// Creates a boolean query from the query text.
		/// <p>
		/// </remarks>
		/// <param name="field">field name</param>
		/// <param name="queryText">text to be passed to the analyzer</param>
		/// <param name="operator">operator used for clauses between analyzer tokens.</param>
		/// <returns>
		/// 
		/// <code>TermQuery</code>
		/// or
		/// <code>BooleanQuery</code>
		/// , based on the analysis
		/// of
		/// <code>queryText</code>
		/// </returns>
		public virtual Query CreateBooleanQuery(string field, string queryText, BooleanClause.Occur
			 @operator)
		{
			if (@operator != BooleanClause.Occur.SHOULD && @operator != BooleanClause.Occur.MUST)
			{
				throw new ArgumentException("invalid operator: only SHOULD or MUST are allowed");
			}
			return CreateFieldQuery(analyzer, @operator, field, queryText, false, 0);
		}

		/// <summary>Creates a phrase query from the query text.</summary>
		/// <remarks>
		/// Creates a phrase query from the query text.
		/// <p>
		/// This is equivalent to
		/// <code>createPhraseQuery(field, queryText, 0)</code>
		/// </remarks>
		/// <param name="field">field name</param>
		/// <param name="queryText">text to be passed to the analyzer</param>
		/// <returns>
		/// 
		/// <code>TermQuery</code>
		/// ,
		/// <code>BooleanQuery</code>
		/// ,
		/// <code>PhraseQuery</code>
		/// , or
		/// <code>MultiPhraseQuery</code>
		/// , based on the analysis of
		/// <code>queryText</code>
		/// </returns>
		public virtual Query CreatePhraseQuery(string field, string queryText)
		{
			return CreatePhraseQuery(field, queryText, 0);
		}

		/// <summary>Creates a phrase query from the query text.</summary>
		/// <remarks>
		/// Creates a phrase query from the query text.
		/// <p>
		/// </remarks>
		/// <param name="field">field name</param>
		/// <param name="queryText">text to be passed to the analyzer</param>
		/// <param name="phraseSlop">number of other words permitted between words in query phrase
		/// 	</param>
		/// <returns>
		/// 
		/// <code>TermQuery</code>
		/// ,
		/// <code>BooleanQuery</code>
		/// ,
		/// <code>PhraseQuery</code>
		/// , or
		/// <code>MultiPhraseQuery</code>
		/// , based on the analysis of
		/// <code>queryText</code>
		/// </returns>
		public virtual Query CreatePhraseQuery(string field, string queryText, int phraseSlop
			)
		{
			return CreateFieldQuery(analyzer, BooleanClause.Occur.MUST, field, queryText, true
				, phraseSlop);
		}

		/// <summary>Creates a minimum-should-match query from the query text.</summary>
		/// <remarks>
		/// Creates a minimum-should-match query from the query text.
		/// <p>
		/// </remarks>
		/// <param name="field">field name</param>
		/// <param name="queryText">text to be passed to the analyzer</param>
		/// <param name="fraction">
		/// of query terms
		/// <code>[0..1]</code>
		/// that should match
		/// </param>
		/// <returns>
		/// 
		/// <code>TermQuery</code>
		/// or
		/// <code>BooleanQuery</code>
		/// , based on the analysis
		/// of
		/// <code>queryText</code>
		/// </returns>
		public virtual Query CreateMinShouldMatchQuery(string field, string queryText, float
			 fraction)
		{
			if (float.IsNaN(fraction) || fraction < 0 || fraction > 1)
			{
				throw new ArgumentException("fraction should be >= 0 and <= 1");
			}
			// TODO: wierd that BQ equals/rewrite/scorer doesn't handle this?
			if (fraction == 1)
			{
				return CreateBooleanQuery(field, queryText, BooleanClause.Occur.MUST);
			}
			Query query = CreateFieldQuery(analyzer, BooleanClause.Occur.SHOULD, field, queryText
				, false, 0);
			if (query is BooleanQuery)
			{
				BooleanQuery bq = (BooleanQuery)query;
				bq.SetMinimumNumberShouldMatch((int)(fraction * bq.Clauses().Count));
			}
			return query;
		}

		/// <summary>Returns the analyzer.</summary>
		/// <remarks>Returns the analyzer.</remarks>
		/// <seealso cref="SetAnalyzer(Lucene.Net.Analysis.Analyzer)">SetAnalyzer(Lucene.Net.Analysis.Analyzer)
		/// 	</seealso>
		public virtual Analyzer GetAnalyzer()
		{
			return analyzer;
		}

		/// <summary>Sets the analyzer used to tokenize text.</summary>
		/// <remarks>Sets the analyzer used to tokenize text.</remarks>
		public virtual void SetAnalyzer(Analyzer analyzer)
		{
			this.analyzer = analyzer;
		}

		/// <summary>Returns true if position increments are enabled.</summary>
		/// <remarks>Returns true if position increments are enabled.</remarks>
		/// <seealso cref="SetEnablePositionIncrements(bool)">SetEnablePositionIncrements(bool)
		/// 	</seealso>
		public virtual bool GetEnablePositionIncrements()
		{
			return enablePositionIncrements;
		}

		/// <summary>Set to <code>true</code> to enable position increments in result query.</summary>
		/// <remarks>
		/// Set to <code>true</code> to enable position increments in result query.
		/// <p>
		/// When set, result phrase and multi-phrase queries will
		/// be aware of position increments.
		/// Useful when e.g. a StopFilter increases the position increment of
		/// the token that follows an omitted token.
		/// <p>
		/// Default: true.
		/// </remarks>
		public virtual void SetEnablePositionIncrements(bool enable)
		{
			this.enablePositionIncrements = enable;
		}

		/// <summary>Creates a query from the analysis chain.</summary>
		/// <remarks>
		/// Creates a query from the analysis chain.
		/// <p>
		/// Expert: this is more useful for subclasses such as queryparsers.
		/// If using this class directly, just use
		/// <see cref="CreateBooleanQuery(string, string)">CreateBooleanQuery(string, string)
		/// 	</see>
		/// and
		/// <see cref="CreatePhraseQuery(string, string)">CreatePhraseQuery(string, string)</see>
		/// </remarks>
		/// <param name="analyzer">analyzer used for this query</param>
		/// <param name="operator">default boolean operator used for this query</param>
		/// <param name="field">field to create queries against</param>
		/// <param name="queryText">text to be passed to the analysis chain</param>
		/// <param name="quoted">true if phrases should be generated when terms occur at more than one position
		/// 	</param>
		/// <param name="phraseSlop">slop factor for phrase/multiphrase queries</param>
		protected internal Query CreateFieldQuery(Analyzer analyzer, BooleanClause.Occur 
			@operator, string field, string queryText, bool quoted, int phraseSlop)
		{
			//HM:revisit 
			//assert operator == BooleanClause.Occur.SHOULD || operator == BooleanClause.Occur.MUST;
			// Use the analyzer to get all the tokens, and then build a TermQuery,
			// PhraseQuery, or nothing based on the term count
			CachingTokenFilter buffer = null;
			TermToBytesRefAttribute termAtt = null;
			PositionIncrementAttribute posIncrAtt = null;
			int numTokens = 0;
			int positionCount = 0;
			bool severalTokensAtSamePosition = false;
			bool hasMoreTokens = false;
			TokenStream source = null;
			try
			{
				source = analyzer.TokenStream(field, queryText);
				source.Reset();
				buffer = new CachingTokenFilter(source);
				buffer.Reset();
				if (buffer.HasAttribute(typeof(TermToBytesRefAttribute)))
				{
					termAtt = buffer.GetAttribute<TermToBytesRefAttribute>();
				}
				if (buffer.HasAttribute(typeof(PositionIncrementAttribute)))
				{
					posIncrAtt = buffer.GetAttribute<PositionIncrementAttribute>();
				}
				if (termAtt != null)
				{
					try
					{
						hasMoreTokens = buffer.IncrementToken();
						while (hasMoreTokens)
						{
							numTokens++;
							int positionIncrement = (posIncrAtt != null) ? posIncrAtt.GetPositionIncrement() : 
								1;
							if (positionIncrement != 0)
							{
								positionCount += positionIncrement;
							}
							else
							{
								severalTokensAtSamePosition = true;
							}
							hasMoreTokens = buffer.IncrementToken();
						}
					}
					catch (IOException)
					{
					}
				}
			}
			catch (IOException e)
			{
				// ignore
				throw new RuntimeException("Error analyzing query text", e);
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(source);
			}
			// rewind the buffer stream
			buffer.Reset();
			BytesRef bytes = termAtt == null ? null : termAtt.GetBytesRef();
			if (numTokens == 0)
			{
				return null;
			}
			else
			{
				if (numTokens == 1)
				{
					try
					{
						bool hasNext = buffer.IncrementToken();
						//HM:revisit 
						//assert hasNext == true;
						termAtt.FillBytesRef();
					}
					catch (IOException)
					{
					}
					// safe to ignore, because we know the number of tokens
					return NewTermQuery(new Term(field, BytesRef.DeepCopyOf(bytes)));
				}
				else
				{
					if (severalTokensAtSamePosition || (!quoted))
					{
						if (positionCount == 1 || (!quoted))
						{
							// no phrase query:
							if (positionCount == 1)
							{
								// simple case: only one position, with synonyms
								BooleanQuery q = NewBooleanQuery(true);
								for (int i = 0; i < numTokens; i++)
								{
									try
									{
										bool hasNext = buffer.IncrementToken();
										//HM:revisit 
										//assert hasNext == true;
										termAtt.FillBytesRef();
									}
									catch (IOException)
									{
									}
									// safe to ignore, because we know the number of tokens
									Query currentQuery = NewTermQuery(new Term(field, BytesRef.DeepCopyOf(bytes)));
									q.Add(currentQuery, BooleanClause.Occur.SHOULD);
								}
								return q;
							}
							else
							{
								// multiple positions
								BooleanQuery q = NewBooleanQuery(false);
								Query currentQuery = null;
								for (int i = 0; i < numTokens; i++)
								{
									try
									{
										bool hasNext = buffer.IncrementToken();
										//HM:revisit 
										//assert hasNext == true;
										termAtt.FillBytesRef();
									}
									catch (IOException)
									{
									}
									// safe to ignore, because we know the number of tokens
									if (posIncrAtt != null && posIncrAtt.GetPositionIncrement() == 0)
									{
										if (!(currentQuery is BooleanQuery))
										{
											Query t = currentQuery;
											currentQuery = NewBooleanQuery(true);
											((BooleanQuery)currentQuery).Add(t, BooleanClause.Occur.SHOULD);
										}
										((BooleanQuery)currentQuery).Add(NewTermQuery(new Term(field, BytesRef.DeepCopyOf
											(bytes))), BooleanClause.Occur.SHOULD);
									}
									else
									{
										if (currentQuery != null)
										{
											q.Add(currentQuery, @operator);
										}
										currentQuery = NewTermQuery(new Term(field, BytesRef.DeepCopyOf(bytes)));
									}
								}
								q.Add(currentQuery, @operator);
								return q;
							}
						}
						else
						{
							// phrase query:
							MultiPhraseQuery mpq = NewMultiPhraseQuery();
							mpq.SetSlop(phraseSlop);
							IList<Term> multiTerms = new AList<Term>();
							int position = -1;
							for (int i = 0; i < numTokens; i++)
							{
								int positionIncrement = 1;
								try
								{
									bool hasNext = buffer.IncrementToken();
									//HM:revisit 
									//assert hasNext == true;
									termAtt.FillBytesRef();
									if (posIncrAtt != null)
									{
										positionIncrement = posIncrAtt.GetPositionIncrement();
									}
								}
								catch (IOException)
								{
								}
								// safe to ignore, because we know the number of tokens
								if (positionIncrement > 0 && multiTerms.Count > 0)
								{
									if (enablePositionIncrements)
									{
										mpq.Add(Sharpen.Collections.ToArray(multiTerms, new Term[0]), position);
									}
									else
									{
										mpq.Add(Sharpen.Collections.ToArray(multiTerms, new Term[0]));
									}
									multiTerms.Clear();
								}
								position += positionIncrement;
								multiTerms.AddItem(new Term(field, BytesRef.DeepCopyOf(bytes)));
							}
							if (enablePositionIncrements)
							{
								mpq.Add(Sharpen.Collections.ToArray(multiTerms, new Term[0]), position);
							}
							else
							{
								mpq.Add(Sharpen.Collections.ToArray(multiTerms, new Term[0]));
							}
							return mpq;
						}
					}
					else
					{
						PhraseQuery pq = NewPhraseQuery();
						pq.SetSlop(phraseSlop);
						int position = -1;
						for (int i = 0; i < numTokens; i++)
						{
							int positionIncrement = 1;
							try
							{
								bool hasNext = buffer.IncrementToken();
								//HM:revisit 
								//assert hasNext == true;
								termAtt.FillBytesRef();
								if (posIncrAtt != null)
								{
									positionIncrement = posIncrAtt.GetPositionIncrement();
								}
							}
							catch (IOException)
							{
							}
							// safe to ignore, because we know the number of tokens
							if (enablePositionIncrements)
							{
								position += positionIncrement;
								pq.Add(new Term(field, BytesRef.DeepCopyOf(bytes)), position);
							}
							else
							{
								pq.Add(new Term(field, BytesRef.DeepCopyOf(bytes)));
							}
						}
						return pq;
					}
				}
			}
		}

		/// <summary>Builds a new BooleanQuery instance.</summary>
		/// <remarks>
		/// Builds a new BooleanQuery instance.
		/// <p>
		/// This is intended for subclasses that wish to customize the generated queries.
		/// </remarks>
		/// <param name="disableCoord">disable coord</param>
		/// <returns>new BooleanQuery instance</returns>
		protected internal virtual BooleanQuery NewBooleanQuery(bool disableCoord)
		{
			return new BooleanQuery(disableCoord);
		}

		/// <summary>Builds a new TermQuery instance.</summary>
		/// <remarks>
		/// Builds a new TermQuery instance.
		/// <p>
		/// This is intended for subclasses that wish to customize the generated queries.
		/// </remarks>
		/// <param name="term">term</param>
		/// <returns>new TermQuery instance</returns>
		protected internal virtual Query NewTermQuery(Term term)
		{
			return new TermQuery(term);
		}

		/// <summary>Builds a new PhraseQuery instance.</summary>
		/// <remarks>
		/// Builds a new PhraseQuery instance.
		/// <p>
		/// This is intended for subclasses that wish to customize the generated queries.
		/// </remarks>
		/// <returns>new PhraseQuery instance</returns>
		protected internal virtual PhraseQuery NewPhraseQuery()
		{
			return new PhraseQuery();
		}

		/// <summary>Builds a new MultiPhraseQuery instance.</summary>
		/// <remarks>
		/// Builds a new MultiPhraseQuery instance.
		/// <p>
		/// This is intended for subclasses that wish to customize the generated queries.
		/// </remarks>
		/// <returns>new MultiPhraseQuery instance</returns>
		protected internal virtual MultiPhraseQuery NewMultiPhraseQuery()
		{
			return new MultiPhraseQuery();
		}
	}
}
